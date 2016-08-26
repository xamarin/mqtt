namespace System.Net.Mqtt
{
	public interface IMqttTopicEvaluator
	{
		bool IsValidTopicFilter (string topicFilter);

		bool IsValidTopicName (string topicName);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		bool Matches (string topicName, string topicFilter);
	}
}
