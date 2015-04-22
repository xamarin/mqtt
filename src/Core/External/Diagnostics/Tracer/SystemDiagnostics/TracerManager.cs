#region BSD License
/* 
Copyright (c) 2011, Clarius Consulting
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, 
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list 
  of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this 
  list of conditions and the following disclaimer in the documentation and/or other 
  materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be 
  used to endorse or promote products derived from this software without specific 
  prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY 
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT 
SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH 
DAMAGE.
*/
#endregion

namespace Hermes.Diagnostics
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// Implements the common tracer interface using <see cref="TraceSource"/> instances. 
    /// </summary>
    /// <remarks>
    /// All tracing is performed asynchronously transparently for faster speed.
    /// </remarks>
    ///	<nuget id="Tracer.SystemDiagnostics" />
    partial class TracerManager : ITracerManager, IDisposable
    {
        /// <summary>
        /// Implicit default trace source name which can be used to setup 
        /// global tracing and listeners.
        /// </summary>
        public const string DefaultSourceName = "*";

        // To handle concurrency for the async tracing.
        private BlockingCollection<Tuple<ExecutionContext, Action>> traceQueue = new BlockingCollection<Tuple<ExecutionContext, Action>>();
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="TracerManager"/> class.
        /// </summary>
        public TracerManager()
        {
            // Note we have only one async task to perform all tracing. This 
            // is an optimization, so that we don't consume too much resources
            // from the running app for this.
            Task.Factory.StartNew(DoTrace, cancellation.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            InitializeConfiguredSources();
        }

        private void InitializeConfiguredSources()
        {
            var configFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            if (!File.Exists(configFile))
                return;

            var sourceNames = from diagnostics in XDocument.Load(configFile).Root.Elements("system.diagnostics")
                              from sources in diagnostics.Elements("sources")
                              from source in sources.Elements("source")
                              select source.Attribute("name").Value;

            foreach (var sourceName in sourceNames)
            {
                // Cause eager initialization, which is needed for the trace source configuration 
                // to be properly read.
                GetSource(sourceName);
            }
        }

        /// <summary>
        /// Gets a tracer instance with the specified name.
        /// </summary>
        public ITracer Get(string name)
        {
            return new AggregateTracer(this, name, CompositeFor(name)
                .Select(tracerName => new DiagnosticsTracer(
                    this.GetOrAdd(tracerName, sourceName => CreateSource(sourceName)))));
        }

        /// <summary>
        /// Gets the underlying <see cref="TraceSource"/> for the given name.
        /// </summary>
        public TraceSource GetSource(string name)
        {
            return this.GetOrAdd(name, sourceName => CreateSource(sourceName));
        }

        /// <summary>
        /// Adds a listener to the source with the given <paramref name="sourceName"/>.
        /// </summary>
        public void AddListener(string sourceName, TraceListener listener)
        {
            this.GetOrAdd(sourceName, name => CreateSource(name)).Listeners.Add(listener);
        }

        /// <summary>
        /// Removes a listener from the source with the given <paramref name="sourceName"/>.
        /// </summary>
        public void RemoveListener(string sourceName, TraceListener listener)
        {
            this.GetOrAdd(sourceName, name => CreateSource(name)).Listeners.Remove(listener);
        }

        /// <summary>
        /// Removes a listener from the source with the given <paramref name="sourceName"/>.
        /// </summary>
        public void RemoveListener(string sourceName, string listenerName)
        {
            this.GetOrAdd(sourceName, name => CreateSource(name)).Listeners.Remove(listenerName);
        }

        /// <summary>
        /// Sets the tracing level for the source with the given <paramref name="sourceName"/>
        /// </summary>
        public void SetTracingLevel(string sourceName, SourceLevels level)
        {
            this.GetOrAdd(sourceName, name => CreateSource(name)).Switch.Level = level;
        }

        /// <summary>
        /// Cleans up the manager, cancelling any pending tracing 
        /// messages.
        /// </summary>
        public void Dispose()
        {
            cancellation.Cancel();
            traceQueue.Dispose();
        }

        /// <summary>
        /// Enqueues the specified trace action to be executed by the trace 
        /// async task.
        /// </summary>
        internal void Enqueue(Action traceAction)
        {
            traceQueue.Add(Tuple.Create(ExecutionContext.Capture(), traceAction));
        }

        private TraceSource CreateSource(string name)
        {
            var source = new TraceSource(name);
            source.TraceInformation("Initialized with initial level {0}", source.Switch.Level);
            return source;
        }

        private void DoTrace()
        {
            foreach (var action in traceQueue.GetConsumingEnumerable())
            {
                if (cancellation.IsCancellationRequested)
                    break;

                // Tracing should never cause the app to fail. 
                // Since this is async, it might if we don't catch.
                try
                {
                    ExecutionContext.Run(action.Item1, state => action.Item2(), null);
                }
                catch { }
            }
        }

        /// <summary>
        /// Gets the list of trace source names that are used to inherit trace source logging for the given <paramref name="name"/>.
        /// </summary>
        private static IEnumerable<string> CompositeFor(string name)
        {
            if (name != DefaultSourceName)
                yield return DefaultSourceName;

            var indexOfGeneric = name.IndexOf('<');
            var indexOfLastDot = name.LastIndexOf('.');

            if (indexOfGeneric == -1 && indexOfLastDot == -1)
            {
                yield return name;
                yield break;
            }

            var parts = default(string[]);

            if (indexOfGeneric == -1)
                parts = name
                    .Substring(0, name.LastIndexOf('.'))
                    .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            else
                parts = name
                    .Substring(0, indexOfGeneric)
                    .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i <= parts.Length; i++)
            {
                yield return string.Join(".", parts, 0, i);
            }

            yield return name;
        }

        /// <summary>
        /// Gets an AppDomain-cached trace source of the given name, or creates it. 
        /// This means that even if multiple libraries are using their own 
        /// trace manager instance, they will all still share the same 
        /// underlying sources.
        /// </summary>
        private TraceSource GetOrAdd(string sourceName, Func<string, TraceSource> factory)
        {
            var cachedSources = AppDomain.CurrentDomain.GetData<ConcurrentDictionary<string, TraceSource>>();
            if (cachedSources == null)
            {
                // This lock guarantees that throughout the current 
                // app domain, only a single root trace source is 
                // created ever.
                lock (AppDomain.CurrentDomain)
                {
                    cachedSources = AppDomain.CurrentDomain.GetData<ConcurrentDictionary<string, TraceSource>>();
                    if (cachedSources == null)
                    {
                        cachedSources = new ConcurrentDictionary<string, TraceSource>();
                        AppDomain.CurrentDomain.SetData(cachedSources);
                    }
                }
            }

            return cachedSources.GetOrAdd(sourceName, factory);
        }

        /// <summary>
        /// Logs to multiple tracers simulateously. Used for the 
        /// source "inheritance"
        /// </summary>
        private class AggregateTracer : ITracer
        {
            private TracerManager manager;
            private List<DiagnosticsTracer> tracers;
            private string name;

            public AggregateTracer(TracerManager manager, string name, IEnumerable<DiagnosticsTracer> tracers)
            {
                this.manager = manager;
                this.name = name;
                this.tracers = tracers.ToList();
            }

            /// <summary>
            /// Traces the specified message with the given <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, object message)
            {
                manager.Enqueue(() => tracers.AsParallel().ForAll(tracer => tracer.Trace(name, type, message)));
            }

            /// <summary>
            /// Traces the specified formatted message with the given <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, string format, params object[] args)
            {
                manager.Enqueue(() => tracers.AsParallel().ForAll(tracer => tracer.Trace(name, type, format, args)));
            }

            /// <summary>
            /// Traces an exception with the specified message and <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, Exception exception, object message)
            {
                manager.Enqueue(() => tracers.AsParallel().ForAll(tracer => tracer.Trace(name, type, exception, message)));
            }

            /// <summary>
            /// Traces an exception with the specified formatted message and <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, Exception exception, string format, params object[] args)
            {
                manager.Enqueue(() => tracers.AsParallel().ForAll(tracer => tracer.Trace(name, type, exception, format, args)));
            }

            public override string ToString()
            {
                return "Aggregate for " + this.name;
            }
        }

        partial class DiagnosticsTracer
        {
            private TraceSource source;

            public DiagnosticsTracer(TraceSource source)
            {
                this.source = source;
            }

            public void Trace(string sourceName, TraceEventType type, object message)
            {
                // Because we know there is a single tracer thread executing these, 
                // we know it's safe to replace the name without locking.
                using (new SourceNameReplacer(source, sourceName))
                {
                    // Add support for Xml-based Service Trace Viewer-compatible 
                    // activity tracing.
                    var data = message as XPathNavigator;
                    // Transfers with a Guid payload should instead trace a transfer 
                    // with that as the related Guid.
                    var guid = message as Guid?;
                    if (data != null)
                        source.TraceData(type, 0, data);
                    else if (guid != null && type == TraceEventType.Transfer)
                        source.TraceTransfer(0, "", guid.Value);
                    else
                        source.TraceEvent(type, 0, message.ToString());
                }
            }

            public void Trace(string sourceName, TraceEventType type, string format, params object[] args)
            {
                // Because we know there is a single tracer thread executing these, 
                // we know it's safe to replace the name without locking.
                using (new SourceNameReplacer(source, sourceName))
                {
                    source.TraceEvent(type, 0, format, args);
                }
            }

            public void Trace(string sourceName, TraceEventType type, Exception exception, object message)
            {
                // Because we know there is a single tracer thread executing these, 
                // we know it's safe to replace the name without locking.
                using (new SourceNameReplacer(source, sourceName))
                {
                    source.TraceEvent(type, 0, message.ToString() + Environment.NewLine + exception);
                }
            }

            public void Trace(string sourceName, TraceEventType type, Exception exception, string format, params object[] args)
            {
                // Because we know there is a single tracer thread executing these, 
                // we know it's safe to replace the name without locking.
                using (new SourceNameReplacer(source, sourceName))
                {
                    source.TraceEvent(type, 0, string.Format(format, args) + Environment.NewLine + exception);
                }
            }

            /// <summary>
            /// The TraceSource instance name matches the name of each of the "segments" 
            /// we built the aggregate source from. This means that when we trace, we issue 
            /// multiple trace statements, one for each. If a listener is added to (say) "*" 
            /// source name, all traces done through it will appear as coming from the source 
            /// "*", rather than (say) "Foo.Bar" which might be the actual source class. 
            /// This diminishes the usefulness of hierarchical loggers significantly, since 
            /// it now means that you need to add listeners too all trace sources you're 
            /// interested in receiving messages from, and all its "children" potentially, 
            /// some of them which might not have been created even yet. This is not feasible.
            /// Instead, since we issue the trace call to each trace source (which is what 
            /// enables the configurability of all those sources in the app.config file), 
            /// we need to fix the source name right before tracing, so that a configured 
            /// listener at "*" still receives as the source name the original (aggregate) one, 
            /// and not "*". This requires some private reflection, and a lock to guarantee 
            /// proper logging, but this decreases its performance. However, since we log 
            /// asynchronously, it's fine.
            /// </summary>
            private class SourceNameReplacer : IDisposable
            {
                // Private reflection needed here in order to make the inherited source names still 
                // log as if the original source name was the one logging, so as not to lose the 
                // originating class name.
                static readonly FieldInfo sourceNameField = typeof(TraceSource).GetField("sourceName", BindingFlags.Instance | BindingFlags.NonPublic);
                static readonly FieldInfo sourceNameFieldForMono = typeof(Switch).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic);
                static Action<TraceSource, string> sourceNameSetter;

                static SourceNameReplacer()
                {
                    if (sourceNameField != null)
                        sourceNameSetter = (source, name) => sourceNameField.SetValue(source, name);
                    else if (sourceNameFieldForMono != null)
                        sourceNameSetter = (source, name) => sourceNameFieldForMono.SetValue(source.Switch, name);
                    else 
                        // We don't know how to replace the source name unfortunately.
                        sourceNameSetter = (source, name) => { };
                }

                private TraceSource source;
                private string originalName;

                public SourceNameReplacer(TraceSource source, string sourceName)
                {
                    this.source = source;
                    this.originalName = source.Name;

                    // Transient change of the source name while the trace call 
                    // is issued. Multi-threading might still cause messages to come 
                    // out with wrong source names :(
                    sourceNameSetter(source, sourceName);
                }

                public void Dispose()
                {
                    sourceNameSetter(source, originalName);
                }
            }
        }
    }
}
