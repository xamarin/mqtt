﻿using System.Net.Mqtt.Packets;
using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
	[DataContract]
	public class MqttConnectionException : MqttException
	{
		public MqttConnectionException (ConnectionStatus status)
		{
			ReturnCode = status;
		}

		public MqttConnectionException (ConnectionStatus status, string message) : base (message)
		{
			ReturnCode = status;
		}

		public MqttConnectionException (ConnectionStatus status, string message, Exception innerException) : base (message, innerException)
		{
			ReturnCode = status;
		}

		public ConnectionStatus ReturnCode { get; set; }
	}
}