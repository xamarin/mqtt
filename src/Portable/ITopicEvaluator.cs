namespace Hermes
{
	public interface ITopicEvaluator
	{
		bool IsValidTopicFilter (string topicFilter);

		bool IsValidTopicName (string topicName);

		bool Matches (string topicName, string topicFilter);
	}
}
