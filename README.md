## Introduction

> Code name: Hermes (messenger of the Greek gods)

MQTT is a Client Server publish/subscribe messaging transport protocol, designed to be light weight, open and simple to use and implement. This makes it suitable for Internet of Things (IoT) messaging, machine to machine communication, mobile devices, etc.

System.Net.Mqtt is a light weight and simple implementation of the MQTT protocol version 3.1.1, written entirely in C# and divided in two libraries: System.Net.Mqtt and System.Net.Mqtt.Server.

The foundation of the System.Net.Mqtt libraries is to provide an intuitive and very easy to use API, hiding most of the protocol concepts that are not needed to be exposed, letting the consumers to just focus on the main protocol messages that are: CONNECT, SUBSCRIBE, UNSUBSCRIBE, PUBLISH, DISCONNECT. 

All the protocol packets acknowledgement happens under the hood in an asynchronous way, adding a level of simplicity reduced to just await the client method calls in a native .net style without worring about low level protocol concepts.
Also, the reception of the subscribed messages is handled using an IObservable implementation, which makes the stream of messages to receive more naturally and aligned to the concept of Publish/Subscribe on which the protocol already relies.
Finally, this allows the subscribers of the received messages to query and filter them using Reactive Extensions, which adds an extra level of flexibility and control over the messages outcome.

## Usage samples

* Creating and connecting an MQTT client with default options:
	
		var configuration = new MqttConfiguration();	
		var client = await MqttClient.CreateAsync("192.168.1.10", configuration);
		var sessionState = await client.ConnectAsync (new MqttClientCredentials(clientId: "foo"));
	
	The ConnectAsync method returns once the CONNACK packet has been received from the Server.
	
	The session state value contains information about if the session has been re used or cleared

* Creating and connecting an MQTT client specifying configuration and clean session:

		var configuration = new MqttConfiguration {
			BufferSize = 128 * 1024,
			Port = 55555,
			KeepAliveSecs = 10,
			WaitTimeoutSecs = 2,
			MaximumQualityOfService = MqttQualityOfService.AtMostOnce,	
			AllowWildcardsInTopicFilters = true };
		var client = await MqttClient.CreateAsync("192.168.1.10", configuration);
		var sessionState = await client.ConnectAsync (new MqttClientCredentials(clientId: "foo"), cleanSession: true);
		
	The ConnectAsync method returns once the CONNACK packet has been received from the Server

* Subscribing and unsubscribing to topics:

		await client.SubscribeAsync("foo/bar/topic1", MqttQualityOfService.AtMostOnce); //QoS0
		await client.SubscribeAsync("foo/bar/topic2", MqttQualityOfService.AtLeastOnce); //QoS1
		await client.SubscribeAsync("foo/bar/topic3", MqttQualityOfService.ExactlyOnce); //QoS2

	The SubscribeAsync method returns once the SUBACK packet has been received from the Server

		await client.UnsubscribeAsync("foo/bar/topic1");
		await client.UnsubscribeAsync("foo/bar/topic2");
		await client.UnsubscribeAsync("foo/bar/topic3");
	
	The UnsubscribeAsync method returns once the UNSUBACK packet has been received from the Server
	
* Reception of messages from the subscribed topics:

		client
			.MessageStream
			.Subscribe(msg => Console.WriteLine($"Message received in topic {msg.Topic}"));
			
		client
			.MessageStream
			.Where(msg => msg.Topic == "foo/bar/topic2")
			.Subscribe(msg => Console.WriteLine($"Message received in topic {msg.Topic}"));`
	
* Publishing messages to topics:

		var message1 = new MqttApplicationMessage("foo/bar/topic1", Encoding.UTF8.GetBytes("Foo Message 1"));
		var message2 = new MqttApplicationMessage("foo/bar/topic2", Encoding.UTF8.GetBytes("Foo Message 2"));
		var message3 = new MqttApplicationMessage("foo/bar/topic3", Encoding.UTF8.GetBytes("Foo Message 3"));

		await client.PublishAsync(message1, MqttQualityOfService.AtMostOnce); //QoS0
		await client.PublishAsync(message2, MqttQualityOfService.AtLeastOnce); //QoS1
		await client.PublishAsync(message3, MqttQualityOfService.ExactlyOnce); //QoS2
		
	The PublishAsync method returns once the corresponding ACK packet has been received from the Server, based on the configured QoS

* Disconnecting an MQTT client:

		await client.DisconnectAsync();

## Installing

System.Net.Mqtt and System.Net.Mqtt.Server are distributed as [NuGet][1] packages and can be installed from Visual Studio by searching for the "System.Net.Mqtt" packages or by running the following commands from the Package Manager Console:

Client Package:
	`Install-Package System.Net.Mqtt -Pre`
	
Server Package:
	`Install-Package System.Net.Mqtt.Server -Pre`
	
Current package dependencies:

System.Diagnostics.Tracer (>= 2.0.4)
System.Net.Sockets (>= 4.1.0)
System.Reactive (>= 3.0.0)
System.Runtime.Serialization.Primitives (>= 4.1.1)

## More

For more specific information about the MQTT protocol, please see the [latest Oasis spec](http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/csprd02/mqtt-v3.1.1-csprd02.html).
