using System;
using Hermes;
using Xunit;

namespace IntegrationTests.Context
{
	public abstract class IntegrationContext : IUseFixture<IntegrationFixture>
	{
		private readonly ushort keepAliveSecs;
		protected IntegrationFixture fixture;

		public IntegrationContext (ushort keepAliveSecs = 0)
		{
			this.keepAliveSecs = keepAliveSecs;
		}

		public void SetFixture (IntegrationFixture data)
		{
			this.fixture = data;
			this.fixture.Initialize (this.keepAliveSecs);
		}

		protected virtual Client GetClient()
		{
			var initializer = new ClientInitializer ("127.0.0.1");
			
			return initializer.Initialize (this.fixture.Configuration);
		}

		protected string GetClientId()
		{
			return string.Concat ("Client", Guid.NewGuid ().ToString ().Substring (0, 6));
		}
	}
}
