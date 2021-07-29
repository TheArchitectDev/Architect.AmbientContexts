using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.AmbientContexts.Tools
{
	/// <summary>
	/// Helps register decorator implementations that wrap existing ones in the container.
	/// </summary>
	internal static class DecoratorRegistrationExtensions
	{
		/// <summary>
		/// Registers a <typeparamref name="TService"/> decorator on top of the previous registration of that type.
		/// </summary>
		/// <param name="decoratorFactory">Constructs a new instance based on the the instance to decorate and the <see cref="IServiceProvider"/>.</param>
		/// <param name="lifetime">If no lifetime is provided, the lifetime of the previous registration is used.</param>
		public static IServiceCollection AddDecorator<TService>(
			this IServiceCollection services,
			Func<IServiceProvider, TService, TService> decoratorFactory,
			ServiceLifetime? lifetime = null)
			where TService : class
		{
			// By convention, the last registration wins
			var previousRegistration = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(TService));

			if (previousRegistration is null)
				throw new InvalidOperationException($"Tried to register a decorator for type {typeof(TService).Name} when no such type was registered.");

			// Get a factory to produce the original implementation
			var decoratedServiceFactory = previousRegistration.ImplementationFactory;
			if (decoratedServiceFactory is null && previousRegistration.ImplementationInstance != null)
			{
				var implementationInstance = previousRegistration.ImplementationInstance;
				decoratedServiceFactory = _ => implementationInstance;
			}
			else if (decoratedServiceFactory is null && previousRegistration.ImplementationType != null)
			{
				var implementationType = previousRegistration.ImplementationType;
				decoratedServiceFactory = serviceProvider => ActivatorUtilities.CreateInstance(
					serviceProvider, implementationType, Array.Empty<object>());
			}

			if (decoratedServiceFactory is null) // Should be impossible
				throw new Exception($"Tried to register a decorator for type {typeof(TService).Name}, but the registration being wrapped specified no implementation at all.");

			var registration = new ServiceDescriptor(typeof(TService), CreateDecorator, lifetime ?? previousRegistration.Lifetime);

			services.Add(registration);

			return services;

			// Local function that creates the decorator instance
			TService CreateDecorator(IServiceProvider serviceProvider)
			{
				var decoratedInstance = (TService)decoratedServiceFactory(serviceProvider);
				var decorator = decoratorFactory(serviceProvider, decoratedInstance);
				return decorator;
			}
		}

		/// <summary>
		/// Registers a <typeparamref name="TService"/> decorator on top of the previous registration of that type.
		/// </summary>
		/// <param name="lifetime">If no lifetime is provided, the lifetime of the previous registration is used.</param>
		public static IServiceCollection AddDecorator<TService, TImplementation>(
			this IServiceCollection services,
			ServiceLifetime? lifetime = null)
			where TService : class
			where TImplementation : TService
		{
			return AddDecorator<TService>(
				services,
				(serviceProvider, decoratedInstance) => ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider, decoratedInstance),
				lifetime);
		}
	}
}
