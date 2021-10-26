using System;
using System.Threading;
using System.Threading.Tasks;
using Architect.AmbientContexts.Defaults;
using Microsoft.Extensions.Hosting;

namespace Architect.AmbientContexts.Hosting
{
	/// <summary>
	/// <para>
	/// An <see cref="IHost"/> wrapper that selectively provides ambient context default scopes.
	/// </para>
	/// <para>
	/// With the Ambient Context pattern, it is sometimes desirable to register <em>custom default scopes</em> as part of the Dependency Injection (DI) registrations.
	/// This poses a potential problem: default scopes tend to be static, whereas there could be <em>multiple</em> <see cref="IServiceProvider"/>s (DI containers), generally managed by their respective <see cref="IHost"/>s.
	/// </para>
	/// <para>
	/// Rather than making custom default scopes <em>static</em>, certain scopes may associate them with the <see cref="IHost"/> they are registered on.
	/// When a particular <see cref="IHost"/>'s dependencies are registered, a customized default scope may be associated with it.
	/// </para>
	/// <para>
	/// This class wraps the regular <see cref="IHost"/>. It ensures that any code called from that <see cref="IHost"/>'s services can see the corresponding default scopes.
	/// Effectively, this achieves the ability to have <em>different</em> default scopes for parallel <see cref="IHost"/>s.
	/// </para>
	/// <para>
	/// This approach relies on the precept that, when DI is used, all relevant call chains start in a service resolved from the <see cref="IHost"/>.
	/// </para>
	/// </summary>
	internal class AmbientContextProvidingHostWrapper : IHost
	{
		static AmbientContextProvidingHostWrapper()
		{
			DefaultScopeContext.CurrentValue = new AsyncLocal<DefaultScopeContext?>();
		}

		public IServiceProvider Services
		{
			get
			{
				DefaultScopeContext.CurrentValue!.Value = this.DefaultScopeContext;

				return this.WrappedHost.Services;
			}
		}

		private DefaultScopeContext DefaultScopeContext { get; }

		private IHost WrappedHost { get; }

		public AmbientContextProvidingHostWrapper(DefaultScopeContext defaultScopeContext, IHost wrappedHost)
		{
			this.DefaultScopeContext = defaultScopeContext ?? throw new ArgumentNullException(nameof(defaultScopeContext));
			this.WrappedHost = wrappedHost ?? throw new ArgumentNullException(nameof(wrappedHost));

			DefaultScopeContext.CurrentValue!.Value = this.DefaultScopeContext;
		}

		public void Dispose()
		{
			try
			{
				DefaultScopeContext.CurrentValue!.Value = null;
				this.DefaultScopeContext.Dispose();
			}
			finally
			{
				this.WrappedHost.Dispose();
			}
		}

		public Task StartAsync(CancellationToken cancellationToken = default) // Must NOT use async keyword, to affect caller with AsyncLocal
		{
			DefaultScopeContext.CurrentValue!.Value = this.DefaultScopeContext;

			return this.WrappedHost.StartAsync(cancellationToken);
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				await this.WrappedHost.StopAsync(cancellationToken);
			}
			finally
			{
				DefaultScopeContext.CurrentValue!.Value = this.DefaultScopeContext;
			}
		}
	}
}
