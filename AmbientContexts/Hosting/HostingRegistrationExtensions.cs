using System.Linq;
using System.Runtime.CompilerServices;
using Architect.AmbientContexts.Defaults;
using Architect.AmbientContexts.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architect.AmbientContexts.Hosting
{
	/// <summary>
	/// Provides extensions methods related to the Ambient Context pattern and Microsoft.Extensions.Hosting.
	/// </summary>
	public static class HostingRegistrationExtensions
	{
		/// <summary>
		/// Used to track the <see cref="DefaultScopeContext"/> registered per <see cref="IServiceCollection"/>.
		/// </summary>
		private static ConditionalWeakTable<IServiceCollection, DefaultScopeContext> DefaultScopeContextsByServiceCollection { get; }
			= new ConditionalWeakTable<IServiceCollection, DefaultScopeContext>();

		/// <summary>
		/// Attempts to wrap the <see cref="IHost"/> in an <see cref="AmbientContextProvidingHostWrapper"/>.
		/// This allows <see cref="DefaultScopeContext"/> to work based on association with an <see cref="IHost"/>.
		/// </summary>
		/// <param name="avoidNestedWrappers">If true (recommended), the registration is careful: if this was invoked for the current <see cref="IServiceCollection"/> before, it is ignored.
		/// The risk is that other code registers a different <see cref="IHost"/> altogether.
		/// If false, instead the registration is ignored only if the <em>last</em> <see cref="IHost"/> registration was the result of this method.
		/// This introduces a different risk: that this method wraps the original <see cref="IHost"/>, another method wraps that, and this method wraps that once more.</param>
		public static IServiceCollection TryAddAmbientContextProvidingHostWrapper(this IServiceCollection services, bool avoidNestedWrappers = true)
		{
			return TryAddAmbientContextProvidingHostWrapper(services, out _, avoidNestedWrappers);
		}

		/// <summary>
		/// Attempts to wrap the <see cref="IHost"/> in an <see cref="AmbientContextProvidingHostWrapper"/>.
		/// This allows <see cref="DefaultScopeContext"/> to work based on association with an <see cref="IHost"/>.
		/// </summary>
		/// <param name="context">Outputs the <see cref="DefaultScopeContext"/> that default scopes for the <see cref="IHost"/> can be registered to.</param>
		/// <param name="avoidNestedWrappers">If true (recommended), the registration is careful: if this was invoked for the current <see cref="IServiceCollection"/> before, it is ignored.
		/// The risk is that other code registers a different <see cref="IHost"/> altogether.
		/// If false, instead the registration is ignored only if the <em>last</em> <see cref="IHost"/> registration was the result of this method.
		/// This introduces a different risk: that this method wraps the original <see cref="IHost"/>, another method wraps that, and this method wraps that once more.</param>
		public static IServiceCollection TryAddAmbientContextProvidingHostWrapper(this IServiceCollection services, out DefaultScopeContext context, bool avoidNestedWrappers = true)
		{
			var shouldRegister = !DefaultScopeContextsByServiceCollection.TryGetValue(services, out context!);

			if (!avoidNestedWrappers)
			{
				// More aggressive determination
				var currentIHostDescriptor = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IHost));
				var hasPriorRegistration = currentIHostDescriptor?.ImplementationInstance?.GetType() == typeof(DefaultScopeContext);
				shouldRegister = !hasPriorRegistration;
			}

			if (shouldRegister)
			{
				if (context is null) context = new DefaultScopeContext();

				DefaultScopeContextsByServiceCollection.AddOrUpdate(services, context);

				var theContext = context; // Needed because a lambda cannot capture out params
				services.AddDecorator<IHost>((serviceProvider, wrappedHost) => new AmbientContextProvidingHostWrapper(theContext, wrappedHost));
			}

			return services;
		}
	}
}
