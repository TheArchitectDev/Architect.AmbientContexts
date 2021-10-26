using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.AmbientContexts.Tests.Time
{
	public sealed class ClockScopeExtensionsTests
	{
		[Fact]
		public async Task AddClockScope_WithCustomScope_ShouldSetExpectedAmbientClockScope()
		{
			// Unused, but ensures that the hosts being tested below are never the first to register a default
			var unusedHostBuilder = new HostBuilder();
			unusedHostBuilder.ConfigureServices(services => services.AddClockScope(() => new DateTime(999)));
			using var unusedHost = unusedHostBuilder.Build();

			var hostBuilder1 = new HostBuilder();
			hostBuilder1.ConfigureServices(services => services
				.AddClockScope(() => new DateTime(10)) // Will be overwritten
				.AddClockScope(() => new DateTime(1))); // Overwrites
			using var host1 = hostBuilder1.Build();

			Assert.Equal(1, ClockScope.Current.Now.Ticks);

			var hostBuilder2 = new HostBuilder();
			hostBuilder2.ConfigureServices(services => services
				.AddClockScope(() => new DateTime(10)) // Will be overwritten
				.AddClockScope(() => new DateTime(2))); // Overwrites
			using var host2 = hostBuilder2.Build();

			Assert.Equal(2, ClockScope.Current.Now.Ticks);
			host2.Start();
			Assert.Equal(2, ClockScope.Current.Now.Ticks);
			await host2.StopAsync();
			host2.Dispose();
			Assert.NotEqual(2, ClockScope.Current.Now.Ticks);

			// We can make no further assumptions about what is now ambient, because the first AddClockScope() ever (in any test method) has determined the static default
			// This property is hard to get around: The initial container may affect the static default and get built; after this, if a second container comes along, we can no longer change the first
		}
	}
}
