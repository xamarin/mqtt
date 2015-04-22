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
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// Extensions to <see cref="ITracer"/> for activity tracing.
    /// </summary>
    static partial class StartActivityExtension
    {
        /// <summary>
        /// Starts a new activity scope.
        /// </summary>
        public static IDisposable StartActivity(this ITracer tracer, string format, params object[] args)
        {
            return new TraceActivity(tracer, format, args);
        }

        /// <summary>
        /// Starts a new activity scope.
        /// </summary>
        public static IDisposable StartActivity(this ITracer tracer, string displayName)
        {
            return new TraceActivity(tracer, displayName);
        }

        /// <devdoc>
        /// In order for activity tracing to happen, the trace source needs to 
        /// have <see cref="SourceLevels.ActivityTracing"/> enabled.
        /// </devdoc>
        partial class TraceActivity : IDisposable
        {
            string displayName;
            bool disposed;
            ITracer tracer;
            Guid oldId;
            Guid newId;

            public TraceActivity(ITracer tracer, string displayName)
                : this(tracer, displayName, null)
            {
            }

            public TraceActivity(ITracer tracer, string displayName, params object[] args)
            {
                this.tracer = tracer;
                this.displayName = displayName;
                if (args != null && args.Length > 0)
                    this.displayName = string.Format(displayName, args, CultureInfo.CurrentCulture);

                newId = Guid.NewGuid();
                oldId = Trace.CorrelationManager.ActivityId;

                tracer.Trace(TraceEventType.Transfer, this.newId);
                Trace.CorrelationManager.ActivityId = newId;

                // The XmlWriterTraceListener expects Start/Stop events to receive an XPathNavigator 
                // with XML in a specific format so that the Service Trace Viewer can properly render 
                // the activity graph.
                tracer.Trace(TraceEventType.Start, new ActivityData(this.displayName, true));
            }

            public void Dispose()
            {
                if (!this.disposed)
                {
                    tracer.Trace(TraceEventType.Stop, new ActivityData(displayName, false));
                    tracer.Trace(TraceEventType.Transfer, oldId);
                    Trace.CorrelationManager.ActivityId = oldId;
                }

                this.disposed = true;
            }

            class ActivityData : XPathNavigator
            {
                string displayName;
                XPathNavigator xml;

                public ActivityData(string displayName, bool isStart)
                {
                    this.displayName = displayName;

                    // The particular XML format expected by the Service Trace Viewer was 
                    // inferred from the actual tool behavior and usage.
                    this.xml = XDocument.Parse(string.Format(@"
        <TraceRecord xmlns='http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord' Severity='{0}'>
            <TraceIdentifier>http://msdn.microsoft.com/en-US/library/System.ServiceModel.Diagnostics.ActivityBoundary.aspx</TraceIdentifier>
            <Description>Activity boundary.</Description>
            <AppDomain>client.vshost.exe</AppDomain>
            <ExtendedData xmlns='http://schemas.microsoft.com/2006/08/ServiceModel/DictionaryTraceRecord'>
                <ActivityName>{1}</ActivityName>
                <ActivityType>ActivityTracing</ActivityType>
            </ExtendedData>
        </TraceRecord>", isStart ? "Start" : "Stop", displayName)).CreateNavigator();
                }

                public override string BaseURI
                {
                    get { return xml.BaseURI; }
                }

                public override XPathNavigator Clone()
                {
                    return xml.Clone();
                }

                public override bool IsEmptyElement
                {
                    get { return xml.IsEmptyElement; }
                }

                public override bool IsSamePosition(XPathNavigator other)
                {
                    return xml.IsSamePosition(other);
                }

                public override string LocalName
                {
                    get { return xml.LocalName; }
                }

                public override bool MoveTo(XPathNavigator other)
                {
                    return xml.MoveTo(other);
                }

                public override bool MoveToFirstAttribute()
                {
                    return xml.MoveToFirstAttribute();
                }

                public override bool MoveToFirstChild()
                {
                    return xml.MoveToFirstChild();
                }

                public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
                {
                    return xml.MoveToFirstNamespace(namespaceScope);
                }

                public override bool MoveToId(string id)
                {
                    return xml.MoveToId(id);
                }

                public override bool MoveToNext()
                {
                    return xml.MoveToNext();
                }

                public override bool MoveToNextAttribute()
                {
                    return xml.MoveToNextAttribute();
                }

                public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
                {
                    return xml.MoveToNextNamespace(namespaceScope);
                }

                public override bool MoveToParent()
                {
                    return xml.MoveToParent();
                }

                public override bool MoveToPrevious()
                {
                    return xml.MoveToPrevious();
                }

                public override string Name
                {
                    get { return xml.Name; }
                }

                public override XmlNameTable NameTable
                {
                    get { return xml.NameTable; }
                }

                public override string NamespaceURI
                {
                    get { return xml.NamespaceURI; }
                }

                public override XPathNodeType NodeType
                {
                    get { return xml.NodeType; }
                }

                public override string Prefix
                {
                    get { return xml.Prefix; }
                }

                public override string Value
                {
                    get { return xml.Value; }
                }

                public override string ToString()
                {
                    return displayName;
                }
            }
        }
    }
}
