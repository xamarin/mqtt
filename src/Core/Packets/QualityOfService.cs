﻿namespace System.Net.Mqtt.Packets
{
	public enum QualityOfService : byte
    {
        AtMostOnce = 0x00,
        AtLeastOnce = 0x01,
        ExactlyOnce = 0x02,
    }
}
