using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Architect.AmbientContexts.Hosting;
using Microsoft.Extensions.Hosting;

namespace Architect.AmbientContexts.Defaults
{
	/// <summary>
	/// <para>
	/// Allows default scopes to be configured per context.
	/// </para>
	/// <para>
	/// When default scopes need to be associated with a context (such as with an <see cref="IHost"/>) rather than be static, they can be accessed through this type.
	/// </para>
	/// </summary>
	public class DefaultScopeContext : IDisposable
	{
		/// <summary>
		/// Returns the current context, if any.
		/// </summary>
		public static DefaultScopeContext? Current => CurrentValue?.Value;
		internal static AsyncLocal<DefaultScopeContext?>? CurrentValue { get; set; }

		/// <summary>
		/// The current context's default scopes, indexed by type.
		/// </summary>
		private Dictionary<Type, AmbientScope> ScopesByType { get; set; } = null!;

		/// <summary>
		/// Until we start reading, this is populated and <see cref="ScopesByType"/> is null.
		/// Once we start reading, this becomes null and <see cref="ScopesByType"/> becomes populated.
		/// </summary>
		private Dictionary<Type, AmbientScope>? ScopesByTypeBuilder { get; set; } = new Dictionary<Type, AmbientScope>();

		/// <summary>
		/// Returns the current context's default scope of type <typeparamref name="T"/>, if any.
		/// </summary>
		public T? GetDefaultScope<T>()
			where T : AmbientScope<T>
		{
			if (this.ScopesByTypeBuilder is not null) this.StartReading();

			var result = (T?)this.ScopesByType.GetValueOrDefault(typeof(T));
			if (result?.State == AmbientScopeState.Disposed) return null;
			return result;
		}

		/// <summary>
		/// Assigns the given <paramref name="scope"/> as the current context's default scope of type <typeparamref name="T"/>.
		/// </summary>
		public void SetDefaultScope<T>(T? scope)
			where T : AmbientScope<T>
		{
			AmbientScope? previousInstance;

			lock (this.ScopesByTypeBuilder ?? new object())
			{
				if (this.ScopesByTypeBuilder is null) throw new InvalidOperationException("Once default scopes are being retrieved, they can no longer be modified.");

				previousInstance = this.ScopesByTypeBuilder.GetValueOrDefault(typeof(T));

				if (scope is null)
				{
					this.ScopesByTypeBuilder.Remove(typeof(T));
				}
				else
				{
					scope.ConvertToDefaultScope();

					this.ScopesByTypeBuilder[typeof(T)] = scope;
				}
			}

			previousInstance?.Dispose();
		}

		private void StartReading()
		{
			if (this.ScopesByTypeBuilder is null) return;

			lock (this.ScopesByTypeBuilder)
			{
				this.ScopesByType = this.ScopesByTypeBuilder;
				this.ScopesByTypeBuilder = null;
			}
		}

		public void Dispose()
		{
			// Avoid dictionary modification after disposal
			this.StartReading();

			var exceptions = ImmutableList<Exception>.Empty;

			foreach (var scope in this.ScopesByType.Values)
			{
				try
				{
					scope.Dispose();
				}
				catch (Exception e)
				{
					exceptions = exceptions.Add(e);
				}
			}

			if (!exceptions.IsEmpty) throw new AggregateException(exceptions);
		}

		/// <summary>
		/// Creates an <see cref="IHost"/> wrapper around the given <paramref name="hostToWrap"/>.
		/// Whenever any code is run from the resulting <see cref="IHost"/>, it activates the current <see cref="DefaultScopeContext"/> as the ambient default.
		/// </summary>
		internal IHost CreateHostWrapper(IServiceProvider _, IHost hostToWrap)
		{
			var result = new AmbientContextProvidingHostWrapper(this, hostToWrap);
			return result;
		}
	}
}
