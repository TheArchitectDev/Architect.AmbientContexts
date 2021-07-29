using System;
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
		/// Registers the given <paramref name="instance"/> as the default scope of type <typeparamref name="T"/> <em>for the current container</em>.
		/// </para>
		/// <para>
		/// The default scope applies to methods of any service resolved from the current <see cref="IHost"/>, including any call chain originating from there.
		/// This includes non-service methods that were ultimately invoked from such a service.
		/// (However, <em>manually</em> building the <see cref="IServiceCollection"/> into an <see cref="IServiceProvider"/>, without use of an <see cref="IHost"/>, will not grant access to the default scope.)
		/// </para>
		/// <para>
		/// This approach relies on the precept that, when DI is used, all relevant call chains start in a service resolved from the <see cref="IHost"/>.
		/// </para>
		/// </summary>
		public static IServiceCollection AddDefaultScope<T>(this IServiceCollection services, T? instance)
			where T : AmbientScope<T>
		{
			// Ensure that we have a mechanism to access default scopes associated with the host
			services.TryAddAmbientContextProvidingHostWrapper(out var defaultScopeContext);
			
			defaultScopeContext.SetDefaultScope(instance);

			return services;
		}
	}
}
