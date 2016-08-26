namespace System.Net.Mqtt.Diagnostics
{
	using System;
	using Mqtt.Diagnostics;
	using Reflection;
	using Linq;

	/// <summary>
	/// Provides usability overloads for getting a tracer to a <see cref="ITracerManager"/>.
	/// </summary>
	///	<nuget id="Tracer.Interfaces" />
	static partial class ITracerManagerExtensions
	{
		/// <summary>
		/// Gets a tracer instance with the full type name of <typeparamref name="T"/>.
		/// </summary>
		public static ITracer Get<T> (this ITracerManager manager)
		{
			return manager.Get (typeof (T));
		}

		/// <summary>
		/// Gets a tracer instance with the full type name of the <paramref name="type"/>.
		/// </summary>
		public static ITracer Get (this ITracerManager manager, Type type)
		{
			return manager.Get (NameFor (type));
		}

		static string NameFor (Type type)
		{
			var typeInfo = type.GetTypeInfo ();

			if (typeInfo.IsGenericType) {
				var genericName = type.GetGenericTypeDefinition().FullName;

				return genericName.Substring (0, genericName.IndexOf ('`')) +
					"<" +
					string.Join (",", typeInfo.GenericTypeArguments.Select (t => NameFor (t)).ToArray ()) +
					">";
			}

			return type.FullName;
		}
	}
}
