﻿using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	internal interface IPacketListener : IDisposable
	{
		IObservable<IPacket> Packets { get; }

		void Listen ();
	}
}
