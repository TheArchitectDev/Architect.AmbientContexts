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
		internal static class DefaultKeeper<T>
			where T : AmbientScope<T>
		{
			public static T? DefaultScope { get; private set; }

			private static IServiceCollection? ServiceCollectionResponsibleForDefaultScope { get; set; }

			private static object RegistrationLock { get; } = new object();

			/// <summary>
			/// <para>
			/// Sets the default scope of type <typeparamref name="T"/> associated with the given DI container.
			/// </para>
			/// <para>
			/// The first container for which a default scope of type <typeparamref name="T"/> is registered gets a global, static default.
			/// If any further containers are used (like when running several integration tests), their default scopes are strictly associated with the container's <see cref="IHost"/>, and are only accessible from code run from it.
			/// </para>
			/// </summary>
			/// <param name="services">The DI container to associate the default scope with.</param>
			/// <param name="defaultScope">The default scope.</param>
			public static bool TrySetDefaultScopeForContainer(IServiceCollection services, T? defaultScope)
			{
				lock (RegistrationLock)
				{
					// Use the static approach as long as we are the only container registering any defaults
					if (ServiceCollectionResponsibleForDefaultScope is null || ServiceCollectionResponsibleForDefaultScope == services)
					{
						ServiceCollectionResponsibleForDefaultScope = services;
						DefaultScope = defaultScope;
						return true;
					}
					else
					{
						return false;
					}
				}
			}
		}

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
			// Attempt to use a static default scope
			if (!DefaultKeeper<T>.TrySetDefaultScopeForContainer(services, instance))
			{
				// But if that was occupied, use a host-associated one instead
				services.TryAddAmbientContextProvidingHostWrapper(out var defaultScopeContext);
				defaultScopeContext.SetDefaultScope(instance);
			}

			return services;
		}
	}
}
