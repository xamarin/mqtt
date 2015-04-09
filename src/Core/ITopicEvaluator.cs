namespace Hermes
{
	public interface ITopicEvaluator
	{
		bool IsValidTopicFilter (string topicFilter);

		bool IsValidTopicName (string topicName);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		bool Matches (string topicName, string topicFilter);
	}
}
