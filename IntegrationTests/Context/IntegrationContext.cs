using System;
using Hermes;
using Xunit;

namespace IntegrationTests.Context
{
	public abstract class IntegrationContext : IUseFixture<IntegrationFixture>
	{
		protected IntegrationFixture fixture;

		public void SetFixture (IntegrationFixture data)
		{
			this.fixture = data;
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
