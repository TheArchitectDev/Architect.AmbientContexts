using System;
using System.Linq;
using Architect.AmbientContexts.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architect.AmbientContexts.Defaults
{
	/// <summary>
	/// Provides extension methods for registering default scopes.
	/// </summary>
	public static class DefaultScopeRegistrationExtensions
	{
		/// <summary>
		/// <para>
		/// Registers the given <paramref name="instance"/> as the default scope of type <typeparamref name="T"/>.
		/// </para>
		/// <para>
		/// The first container that registers a default scope of type <typeparamref name="T"/> during the application lifetime gets a global, static default.
		/// If any further containers register one (like when running several integration tests), their default scopes are strictly associated with the container's <see cref="IHost"/>, and are only accessible from code run from it.
		/// The latter has worse performance, but provides isolation between containers.
		/// </para>
		/// <para>
		/// With multiple containers, a default scope is accessible to methods of any service resolved from the current <see cref="IHost"/>, including any call chain originating from there.
		/// This includes non-service methods that were ultimately invoked from such a service.
		/// (However, <em>manually</em> building the <see cref="IServiceCollection"/> into an <see cref="IServiceProvider"/>, without use of an <see cref="IHost"/>, will not grant access to the default scope.)
		/// This approach relies on the precept that, when DI is used, all relevant call chains start in a service resolved from the <see cref="IHost"/>.
		/// </para>
		/// </summary>
		public static IServiceCollection AddDefaultScope<T>(this IServiceCollection services, T? instance)
			where T : AmbientScope<T>
		{
			// #TODO: Test with webhost, and if that throws, add a hint to the error message
			if (!services.Any(descriptor => descriptor.ServiceType == typeof(IHost)))
				throw new NotSupportedException("Ambient context with default scopes is only supported on DI containers containing an IHost (usually the result of using a HostBuilder).");

			// Prefer a static default for performance
			if (!StaticDefaultKeeper<T>.TrySetDefaultScopeForContainer(services, instance))
			{
				// Fall back to a per-IHost default if necessary
				services.TryAddAmbientContextProvidingHostWrapper(out var defaultScopeContext);
				defaultScopeContext.SetDefaultScope(instance);
			}

			return services;
		}
	}
}
