using System;
using Microsoft.Extensions.Hosting;
using Xunit;

#if NETCOREAPP3_1

namespace Architect.AmbientContexts.Tests.Time
{
	public sealed class ClockScopeExtensionsTests
	{
		[Fact]
		public void UseClockScope_WithCustomScope_SetsExpectedAmbientClockScope()
		{
			var defaultClockScope = ClockScope.Current;

			var hostBuilder = new HostBuilder();
			using (var host = hostBuilder.Build())
				host.Services.UseClockScope(() => DateTime.Now); // Use the same clock as the default to avoid breaking other tests, since we are setting a static property

			var result = ClockScope.Current;

			Assert.NotEqual(defaultClockScope, result);
		}
	}
}

#endif