using System;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.AmbientContexts.Defaults
{
	internal static class StaticDefaultKeeper<T>
		where T : AmbientScope<T>
	{
		/// <summary>
		/// The configured or automatic default scope.
		/// </summary>
		internal static T? DefaultScope;

		public static WeakReference<IServiceCollection?>? ServiceCollectionResponsibleForDefaultScope { get; set; }

		private static object RegistrationLock { get; } = new object();

		/// <summary>
		/// <para>
		/// Attempts to set the default scope of type <typeparamref name="T"/> associated with the given DI container, overwriting any existing default.
		/// </para>
		/// <para>
		/// Once a container is passed to this method, it returns false if it is ever invoked with a different container.
		/// </para>
		/// </summary>
		/// <param name="services">The DI container to associate the default scope with.</param>
		/// <param name="defaultScope">The default scope.</param>
		public static bool TrySetDefaultScopeForContainer(IServiceCollection services, T? defaultScope)
		{
			if (services is null) throw new ArgumentNullException(nameof(services));

			lock (RegistrationLock)
			{
				IServiceCollection? initialContainer = null;
				ServiceCollectionResponsibleForDefaultScope?.TryGetTarget(out initialContainer);

				// Use the static approach as long as we are the first container to register any defaults
				if (ServiceCollectionResponsibleForDefaultScope is null || initialContainer == services)
				{
					defaultScope?.ConvertToDefaultScope();

					DefaultScope = defaultScope;
					ServiceCollectionResponsibleForDefaultScope = new WeakReference<IServiceCollection?>(services);

					return true;
				}
				else
				{
					return false;
				}
			}
		}
	}
}
