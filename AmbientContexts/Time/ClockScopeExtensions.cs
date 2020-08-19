using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.AmbientContexts
{
	public static class ClockScopeExtensions
	{
		#region Configuration

		/// <summary>
		/// <para>
		/// Enables static, injection-free access to the registered clock through the <see cref="ClockScope"/> class.
		/// </para>
		/// <para>
		/// This overwrites the clock that is exposed by default.
		/// </para>
		/// </summary>
		public static IApplicationBuilder UseClockScope(this IApplicationBuilder applicationBuilder, Func<DateTime> nowSource)
		{
			UseClockScope(applicationBuilder.ApplicationServices, nowSource);
			return applicationBuilder;
		}
		
		/// <summary>
		/// <para>
		/// Enables static, injection-free access to the registered clock through the <see cref="ClockScope"/> class.
		/// </para>
		/// <para>
		/// This overwrites the clock that is exposed by default.
		/// </para>
		/// </summary>
		public static IHost UseClockScope(this IHost host, Func<DateTime> nowSource)
		{
			UseClockScope(host.Services, nowSource);
			return host;
		}

		/// <summary>
		/// <para>
		/// Enables static, injection-free access to the registered clock through the <see cref="ClockScope"/> class.
		/// </para>
		/// <para>
		/// This overwrites the clock that is exposed by default.
		/// </para>
		/// </summary>
		public static IServiceProvider UseClockScope(this IServiceProvider serviceProvider, Func<DateTime> nowSource)
		{
			ClockScope.SetDefaultValue(nowSource ?? throw new ArgumentNullException(nameof(nowSource)));
			return serviceProvider;
		}

		#endregion
	}
}
