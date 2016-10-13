﻿using System.Net.Mqtt.Sdk;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	/// <summary>
	/// Creates instances of <see cref="IMqttClient"/> for connecting to 
	/// an MQTT server.
	/// </summary>
	public static class MqttClient
	{
		/// <summary>
		/// Creates an <see cref="IMqttClient"/> and connects it to the destination 
		/// <paramref name="hostAddress"/> server via TCP using the specified 
		/// MQTT configuration to customize the protocol parameters.
		/// </summary>
		public static Task<IMqttClient> CreateAsync(string hostAddress, MqttConfiguration configuration)
		{
			return new MqttClientFactory(hostAddress).CreateClientAsync(configuration);
		}
	}
}
