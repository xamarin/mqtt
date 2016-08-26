﻿using System.Net.Mqtt.Exceptions;
using System.Runtime.Serialization;

namespace System.Net.Mqtt.Server.Exceptions
{
	[DataContract]
	public class MqttServerException : MqttException
	{
		public MqttServerException ()
		{
		}

		public MqttServerException (string message)
			: base (message)
		{
		}

		public MqttServerException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
