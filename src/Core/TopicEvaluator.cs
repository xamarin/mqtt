using System.Linq;

namespace System.Net.Mqtt
{
	internal class TopicEvaluator : ITopicEvaluator
	{
		readonly ProtocolConfiguration configuration;

		public TopicEvaluator (ProtocolConfiguration configuration)
		{
			this.configuration = configuration;
		}

		public bool IsValidTopicFilter (string topicFilter)
		{
			if (!this.configuration.AllowWildcardsInTopicFilters) {
				if (topicFilter.Contains (Protocol.SingleLevelTopicWildcard) ||
					topicFilter.Contains (Protocol.MultiLevelTopicWildcard))
					return false;

			}

			if (string.IsNullOrEmpty (topicFilter))
				return false;

			if (topicFilter.Length > 65536)
				return false;

			var topicFilterParts = topicFilter.Split ('/');

			if(topicFilterParts.Count(s => s == "#") > 1)
				return false;

			if (topicFilterParts.Any (s => s.Length > 1 && s.Contains ("#")))
				return false;

			if (topicFilterParts.Any (s => s.Length > 1 && s.Contains ("+")))
				return false;

			if(topicFilterParts.Any(s => s == "#") && topicFilter.IndexOf("#") < topicFilter.Length - 1)
				return false;

			return true;
		}

		public bool IsValidTopicName (string topicName)
		{
			return !string.IsNullOrEmpty (topicName) &&
				topicName.Length <= 65536 &&
				!topicName.Contains ("#") &&
				!topicName.Contains ("+");
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public bool Matches (string topicName, string topicFilter)
		{
			if (!this.IsValidTopicName (topicName)) { 
				var message = string.Format(Properties.Resources.TopicEvaluator_InvalidTopicName, topicName);

				throw new ProtocolException (message);
			}

			if (!this.IsValidTopicFilter (topicFilter)) { 
				var message = string.Format(Properties.Resources.TopicEvaluator_InvalidTopicFilter, topicFilter);

				throw new ProtocolException (message);
			}

			var topicFilterParts = topicFilter.Split ('/');
			var topicNameParts = topicName.Split ('/');

			if (topicNameParts.Length > topicFilterParts.Length && topicFilterParts[topicFilterParts.Length - 1] != "#")
				return false;

			if (topicFilterParts.Length - topicNameParts.Length > 1)
				return false;

			if (topicFilterParts.Length - topicNameParts.Length == 1 && topicFilterParts[topicFilterParts.Length -1] != "#")
				return false;

			if ((topicFilterParts[0] == "#" || topicFilterParts[0] == "+") && topicNameParts[0].StartsWith ("$"))
				return false;

			var matches = true;

			for (var i = 0; i < topicFilterParts.Length; i++) {
				var topicFilterPart = topicFilterParts[i];

				if (topicFilterPart == "#") {
					matches = true;
					break;
				}

				if (topicFilterPart == "+") {
					if (i == topicFilterParts.Length - 1 && topicNameParts.Length > topicFilterParts.Length) {
						matches = false;
						break;
					}

					continue;
				}

				if (topicFilterPart != topicNameParts[i]) {
					matches = false;
					break;
				}
			}

			return matches;
		}
	}
}
