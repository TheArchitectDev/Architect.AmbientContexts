﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
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
		public static DefaultScopeContext? Current => CurrentValue.Value;
		internal static AsyncLocal<DefaultScopeContext> CurrentValue { get; } = new AsyncLocal<DefaultScopeContext>();
		
		/// <summary>
		/// The current context's default scopes, indexed by type.
		/// </summary>
		private Dictionary<Type, AmbientScope> ScopesByType { get; } = new Dictionary<Type, AmbientScope>();

		/// <summary>
		/// Returns the current context's default scope of type <typeparamref name="T"/>, if any.
		/// </summary>
		public T? GetDefaultScope<T>()
			where T : AmbientScope
		{
			var result = (T?)this.ScopesByType.GetValueOrDefault(typeof(T));
			return result;
		}

		/// <summary>
		/// Assigns the given <paramref name="scope"/> as the current context's default scope of type <typeparamref name="T"/>.
		/// </summary>
		public void SetDefaultScope<T>(T? scope)
			where T : AmbientScope<T>
		{
			lock (this.ScopesByType)
			{
				var previousInstance = this.ScopesByType.GetValueOrDefault(typeof(T));

				if (scope is null)
				{
					this.ScopesByType.Remove(typeof(T));
				}
				else
				{
					scope.ConvertToDefaultScope();

					this.ScopesByType[typeof(T)] = scope;
				}

				previousInstance?.Dispose();
			}
		}

		public void Dispose()
		{
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
	}
}
