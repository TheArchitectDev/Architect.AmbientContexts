using System;
using Architect.AmbientContexts.Defaults;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.AmbientContexts
{
	public static class ClockScopeExtensions
	{
		/// <summary>
		/// <para>
		/// Configures the default behavior of the <see cref="Clock"/> class.
		/// </para>
		/// <para>
		/// If this method is not invoked, <see cref="Clock"/> is available based on <see cref="DateTime.Now"/>.
		/// </para>
		/// </summary>
		public static IServiceCollection AddClockScope(this IServiceCollection services, Func<DateTime> defaultNowSource)
		{
			services.AddDefaultScope(new ClockScope(defaultNowSource, AmbientScopeOption.NoNesting));
			return services;
		}
	}
}
