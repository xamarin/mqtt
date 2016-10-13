namespace System.Net.Mqtt.Sdk
{
	/// <summary>
	/// Represents an evaluator for MQTT topics
	/// according to the rules defined in the protocol specification
	/// </summary>
	/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180919">Topic Names and Topic Filters</a> 
	/// for more details on the topics specification
	public interface IMqttTopicEvaluator
	{
        /// <summary>
        /// Determines if a topic filter is valid according to the protocol specification
        /// </summary>
        /// <param name="topicFilter">Topic filter to evaluate</param>
        /// <returns>A boolean value that indicates if the topic filter is valid or not</returns>
		bool IsValidTopicFilter (string topicFilter);

        /// <summary>
        /// Determines if a topic name is valid according to the protocol specification
        /// </summary>
        /// <param name="topicName">Topic name to evaluate</param>
        /// <returns>A boolean value that indicates if the topic name is valid or not</returns>
		bool IsValidTopicName (string topicName);

        /// <summary>
        /// Evaluates if a topic name applies to a specific topic filter
        /// If a topic name matches a filter, it means that the Server will
        /// successfully dispatch incoming messages of that topic name
        /// to the subscribers of the topic filter
        /// </summary>
        /// <param name="topicName">Topic name to evaluate</param>
        /// <param name="topicFilter">Topic filter to evaluate</param>
        /// <returns>A boolean value that indicates if the topic name matches with the topic filter</returns>
        /// <exception cref="MqttException">MqttException</exception>
        bool Matches (string topicName, string topicFilter);
	}
}
