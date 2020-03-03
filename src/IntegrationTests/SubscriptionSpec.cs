using IntegrationTests.Context;
using System;
using System.Collections.Generic;
using System.Net.Mqtt;
using System.Net.Mqtt.Properties;
using System.Threading.Tasks;
using Xunit;

namespace IntegrationTests
{
	public class SubscriptionSpec : ConnectedContext, IDisposable
	{
		readonly IMqttServer server;

		public SubscriptionSpec ()
		{
			server = GetServerAsync ().Result;
		}

        [Fact]
        public async Task when_server_is_closed_then_error_occurs_when_client_send_message()
        {
            var client = await GetClientAsync();

            server.Stop();

            var aggregateException = Assert.Throws<AggregateException>(() => client.SubscribeAsync("test\foo", MqttQualityOfService.AtLeastOnce).Wait());

            Assert.NotNull(aggregateException);
            Assert.NotNull(aggregateException.InnerException);
            Assert.True(aggregateException.InnerException is MqttClientException || aggregateException.InnerException is ObjectDisposedException);
        }

        [Fact]
		public async Task when_subscribe_topic_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topicFilter = Guid.NewGuid ().ToString () + "/#";

			await client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce)
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topicFilter);

			client.Dispose ();
		}

		[Fact]
		public async Task when_subscribe_multiple_topics_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topicsToSubscribe = GetTestLoad();
			var topics = new List<string> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = Guid.NewGuid ().ToString ();

				tasks.Add (client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce));
				topics.Add (topicFilter);
			}

			await Task.WhenAll (tasks);

			Assert.True (client.IsConnected);

			await client.UnsubscribeAsync (topics.ToArray ());

			client.Dispose ();
		}

        [Theory]
        [InlineData("foo/#/#")]
        [InlineData("foo/bar#/")]
        [InlineData("foo/bar+/test")]
        [InlineData("foo/#/bar")]
        public async Task when_subscribing_invalid_topic_then_fails(string topicFilter)
        {
            var client = await GetClientAsync ();

            var ex = Assert.Throws<AggregateException> (() => client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce).Wait ());

            Assert.NotNull (ex);
            Assert.NotNull (ex.InnerException);
            Assert.True (ex.InnerException is MqttClientException);
            Assert.NotNull (ex.InnerException.InnerException);
            Assert.IsType<MqttException> (ex.InnerException.InnerException);
            Assert.Equal (string.Format (Resources.SubscribeFormatter_InvalidTopicFilter, topicFilter), ex.InnerException.InnerException.Message);
        }

        [Fact]
        public async Task when_subscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            var client = await GetClientAsync();

            var ex = Assert.Throws<AggregateException> (() => client.SubscribeAsync (string.Empty, MqttQualityOfService.AtMostOnce).Wait ());

            Assert.NotNull (ex);
            Assert.NotNull (ex.InnerException);
            Assert.True (ex.InnerException is MqttClientException);
            Assert.NotNull (ex.InnerException.InnerException);
            Assert.IsType<MqttProtocolViolationException> (ex.InnerException.InnerException);
        }

        [Fact]
		public async Task when_unsubscribe_topic_then_succeeds()
		{
			var client = await GetClientAsync ();
			var topicsToSubscribe = GetTestLoad ();
			var topics = new List<string> ();
			var tasks = new List<Task> ();

			for (var i = 1; i <= topicsToSubscribe; i++) {
				var topicFilter = Guid.NewGuid ().ToString ();

				tasks.Add (client.SubscribeAsync (topicFilter, MqttQualityOfService.AtMostOnce));
				topics.Add (topicFilter);
			}

			await Task.WhenAll (tasks);
			await client.UnsubscribeAsync (topics.ToArray())
				.ConfigureAwait(continueOnCapturedContext: false);

			Assert.True (client.IsConnected);

			client.Dispose ();
		}

        [Fact]
        public async Task when_unsubscribing_emtpy_topic_then_fails_with_protocol_violation()
        {
            var client = await GetClientAsync ();

            var ex = Assert.Throws<AggregateException> (() => client.UnsubscribeAsync (null).Wait ());

            Assert.NotNull (ex);
            Assert.NotNull (ex.InnerException);
            Assert.IsType<MqttClientException> (ex.InnerException);
            Assert.NotNull (ex.InnerException.InnerException);
            Assert.IsType<MqttProtocolViolationException> (ex.InnerException.InnerException);

        }
        void IDisposable.Dispose ()
		{
			if (server != null) {
				server.Stop ();
			}
		}
	}
}
