using Hermes.Storage;

namespace Hermes
{
	public static class Extensions
	{
		public static bool Matches(this ClientSubscription subscription, string topic)
		{
			return true;
		}
	}
}
