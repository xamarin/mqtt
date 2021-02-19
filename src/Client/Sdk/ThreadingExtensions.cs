using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal static class ThreadingExtensions
	{
		static readonly ITracer tracer = Tracer.Get(typeof(ThreadingExtensions));

		internal static async void FireAndForget(this Task task)
		{
			try
			{
				await task.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				tracer.Error(ex);
			}
		}
	}
}
