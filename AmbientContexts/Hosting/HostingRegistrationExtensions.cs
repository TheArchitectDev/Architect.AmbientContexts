using System;
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
	internal static class HostingRegistrationExtensions
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
		public static IServiceCollection TryAddAmbientContextProvidingHostWrapper(this IServiceCollection services)
		{
			return TryAddAmbientContextProvidingHostWrapper(services, out _);
		}

		/// <summary>
		/// Attempts to wrap the <see cref="IHost"/> in an <see cref="AmbientContextProvidingHostWrapper"/>.
		/// This allows <see cref="DefaultScopeContext"/> to work based on association with an <see cref="IHost"/>.
		/// </summary>
		/// <param name="context">Outputs the <see cref="DefaultScopeContext"/> that default scopes for the <see cref="IHost"/> can be registered to.</param>
		public static IServiceCollection TryAddAmbientContextProvidingHostWrapper(this IServiceCollection services, out DefaultScopeContext context)
		{
			var shouldRegister = !DefaultScopeContextsByServiceCollection.TryGetValue(services, out context!);

			if (context is not null)
			{
				var currentIHostDescriptor = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IHost));
				var hasPriorRegistration = ReferenceEquals(currentIHostDescriptor?.ImplementationFactory, (Func<IServiceProvider, IHost, IHost>)context.CreateHostWrapper);
				shouldRegister = !hasPriorRegistration;
			}

			if (shouldRegister)
			{
				if (context is null) context = new DefaultScopeContext();

				DefaultScopeContextsByServiceCollection.AddOrUpdate(services, context);

				var theContext = context; // Needed because a lambda cannot capture out params
				services.AddDecorator<IHost>(context.CreateHostWrapper);
			}

			return services;
		}
	}
}
