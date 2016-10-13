using System.Net.Mqtt;
using System.Net.Mqtt.Sdk;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
	//Topic names based on Protocol Spec samples
	//http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc398718106
	public class TopicEvaluatorSpec
	{
		[Theory]
		[InlineData("foo/bar")]
		[InlineData("foo/bar/")]
		[InlineData("/foo/bar")]
		[InlineData("/")]
		public void when_evaluating_valid_topic_name_then_is_valid(string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.True (topicEvaluator.IsValidTopicName(topicName));
		}

		[Theory]
		[InlineData("foo/+")]
		[InlineData("#")]
		[InlineData("")]
		public void when_evaluating_invalid_topic_name_then_is_invalid(string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.False (topicEvaluator.IsValidTopicName(topicName));
		}

		[Theory]
		[InlineData("foo/bar")]
		[InlineData("foo/#")]
		[InlineData("#")]
		[InlineData("foo/bar/")]
		[InlineData("foo/+/+/bar")]
		[InlineData("+/bar/test")]
		[InlineData("/foo")]
		[InlineData("/")]
		public void when_evaluating_valid_topic_filter_then_is_valid(string topicFilter)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.True (topicEvaluator.IsValidTopicFilter(topicFilter));
		}

		[Theory]
		[InlineData("foo/#/#")]
		[InlineData("foo/bar#/")]
		[InlineData("foo/bar+/test")]
		[InlineData("")]
		[InlineData("foo/#/bar")]
		public void when_evaluating_invalid_topic_filter_then_is_invalid(string topicFilter)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.False (topicEvaluator.IsValidTopicFilter(topicFilter));
		}

		[Theory]
		[InlineData("foo/#")]
		[InlineData("#")]
		[InlineData("foo/+/+/bar")]
		[InlineData("+/bar/test")]
		public void when_evaluating_topic_filter_with_wildcards_and_configuration_does_not_allow_wildcards_then_is_invalid(string topicFilter)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration { 
				AllowWildcardsInTopicFilters = false });

			Assert.False (topicEvaluator.IsValidTopicFilter(topicFilter));
		}

		[Theory]
		[InlineData("sport/tennis/player1/#", "sport/tennis/player1")]
		[InlineData("sport/tennis/player1/#", "sport/tennis/player1/ranking")]
		[InlineData("sport/tennis/player1/#", "sport/tennis/player1/score/wimbledon")]
		[InlineData("sport/#", "sport")]
		[InlineData("#", "foo/bar")]
		[InlineData("#", "/")]
		[InlineData("#", "foo/bar/test")]
		[InlineData("#", "foo/bar/")]
		[InlineData("games/table tennis/players", "games/table tennis/players")]
		[InlineData("games/+/players", "games/table tennis/players")]
		[InlineData("#", "games/table tennis/players/ranking")]
		[InlineData("Accounts payable", "Accounts payable")]
		[InlineData("/", "/")]
		public void when_matching_valid_topic_name_with_multi_level_wildcard_topic_filter_then_matches(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.True (topicEvaluator.Matches (topicName, topicFilter));
		}

		[Theory]
		[InlineData("sport/tennis/+", "sport/tennis/player1")]
		[InlineData("sport/tennis/+", "sport/tennis/player2")]
		[InlineData("sport/+", "sport/")]
		[InlineData("+/+", "/finance")]
		[InlineData("/+", "/finance")]
		public void when_matching_valid_topic_name_with_single_level_wildcard_topic_filter_then_matches(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.True (topicEvaluator.Matches (topicName, topicFilter));
		}

		[Theory]
		[InlineData("+/tennis/#", "sport/tennis/player1/ranking/")]
		[InlineData("+/tennis/#", "sport/tennis/player1/ranking/player2")]
		[InlineData("+/tennis/#", "sport/tennis")]
		[InlineData("+/tennis/#", "sport/tennis/")]
		[InlineData("+/tennis/#", "games/tennis/player1/ranking/player2")]
		[InlineData("+/foo/+/bar/#", "sport/foo/players/bar")]
		[InlineData("+/foo/+/bar/#", "game/foo/ranking/bar/test/player1")]
		[InlineData("+/foo/+/bar/#", "game/foo/ranking/bar/test/player1/")]
		public void when_matching_valid_topic_name_with_mixed_wildcards_topic_filter_then_matches(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.True (topicEvaluator.Matches (topicName, topicFilter));
		}
		
		[Theory]
		[InlineData("$SYS/#", "$SYS/foo/bar")]
		[InlineData("$SYS/#", "$SYS/foo/bar/test")]
		[InlineData("$SYS/#", "$SYS")]
		[InlineData("$SYS/#", "$SYS/")]
		[InlineData("$SYS/monitor/+", "$SYS/monitor/Clients")]
		[InlineData("$/+/test", "$/foo/test")]
		public void when_matching_reserved_topic_names_then_matches(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.True (topicEvaluator.Matches (topicName, topicFilter));
		}

		[Theory]
		[InlineData("sport/tennis/+", "sport/tennis/player1/ranking")]
		[InlineData("sport/+", "sport")]
		[InlineData("+", "/finance")]
		public void when_matching_invalid_topic_name_with_single_level_wildcard_topic_filter_then_does_not_match(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.False (topicEvaluator.Matches (topicName, topicFilter));
		}

		[Theory]
		[InlineData("foo/bar/test", "FOO/BAR/TEST")]
		[InlineData("foo/bar/test", "foo/bar/Test")]
		[InlineData("FOO/BAR/TEST", "FOO/bar/TEST")]
		[InlineData("+/BAR/TEST", "FOO/BAR/TESt")]
		[InlineData("ACCOUNTS", "Accounts")]
		public void when_matching_topic_name_and_topic_filter_with_different_case_then_does_not_match(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.False (topicEvaluator.Matches (topicName, topicFilter));
		}

		[Theory]
		[InlineData("#", "$/test")]
		[InlineData("#", "$SYS/test/foo")]
		[InlineData("+/monitor/#", "$/monitor/Clients")]
		[InlineData("+/monitor/Clients", "$SYS/monitor/Clients")]
		public void when_matching_reserved_topic_names_with_starting_wildcards_then_does_not_match(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.False (topicEvaluator.Matches (topicName, topicFilter));
		}

		[Theory]
		[InlineData("foo/+")]
		[InlineData("#")]
		[InlineData("")]
		public void when_matching_with_invalid_topic_name_then_fails(string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.Throws<MqttException> (() => topicEvaluator.Matches (topicName, "#"));
		}

		[Theory]
		[InlineData("foo/#/#")]
		[InlineData("foo/bar#/")]
		[InlineData("foo/bar+/test")]
		[InlineData("")]
		[InlineData("foo/#/bar")]
		public void when_matching_with_invalid_topic_filter_then_fails(string topicFilter)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.Throws<MqttException> (() => topicEvaluator.Matches ("foo", topicFilter));
		}

		[Theory]
		[InlineData("test/foo", "test/foo/testClient")]
		[InlineData("test", "test/")]
		[InlineData("foo/bar/test/", "foo/bar/test")]
		[InlineData("test/foo/testClient", "test/foo")]
		[InlineData("test/", "test")]
		[InlineData("foo/bar/test", "foo/bar/test/")]
		public void when_matching_not_compatible_topics_then_does_not_match(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.False (topicEvaluator.Matches (topicName, topicFilter));
		}

		[Theory]
		[InlineData("test/foo", "test/foo")]
		[InlineData("test/", "test/")]
		[InlineData("foo/bar/test/", "foo/bar/test/")]
		[InlineData("test/foo/testClient", "test/foo/testClient")]
		[InlineData("test", "test")]
		[InlineData("foo/bar/test", "foo/bar/test")]
		public void when_matching_compatible_topics_then_matches(string topicFilter, string topicName)
		{
			var topicEvaluator = new MqttTopicEvaluator (new MqttConfiguration());

			Assert.True (topicEvaluator.Matches (topicName, topicFilter));
		}
	}
}
