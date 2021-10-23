﻿using System;
using System.Threading;

namespace Architect.AmbientContexts
{
	public abstract partial class AmbientScope<TConcreteScope>
	{
		// Note that AsyncLocal does not cross AppDomain boundaries, which we consider desirable behavior

		/// <summary>
		/// <para>
		/// Contains the current async execution flow's AmbientScope.
		/// </para>
		/// <para>
		/// Lazily instantiated by <see cref="SetAmbientScope"/>, as an optimization for when no scopes or only default scopes are used.
		/// </para>
		/// </summary>
		private static AsyncLocal<TConcreteScope?>? CurrentAmbientScope;

		/// <summary>
		/// Makes the given scope the ambient one.
		/// Throws if it already is.
		/// </summary>
		/// <param name="newAmbientScope">The scope that is to be set as the ambient scope. May be null.</param>
		protected static void SetAmbientScope(AmbientScope<TConcreteScope>? newAmbientScope)
		{
			// Lazy instantiation works as an optimization for when no scopes or only default scopes are used
			if (CurrentAmbientScope is null)
				Interlocked.CompareExchange(ref CurrentAmbientScope, value: new AsyncLocal<TConcreteScope?>(), comparand: null);

			if (newAmbientScope is null)
			{
				CurrentAmbientScope.Value = null;
				return;
			}

			if (ReferenceEquals(newAmbientScope, CurrentAmbientScope.Value))
				ThrowAlreadyCurrent();

			CurrentAmbientScope.Value = (TConcreteScope)newAmbientScope;

			static void ThrowAlreadyCurrent() => throw new InvalidOperationException("The given scope was already the current ambient scope.");
		}

		/// <summary>
		/// <para>
		/// Removes the current ambient scope. If it has a physical parent, that becomes the ambient scope again.
		/// </para>
		/// <para>
		/// Throws if there is no current ambient scope.
		/// </para>
		/// </summary>
		protected static void RemoveAmbientScope()
		{
			var currentAmbientScope = CurrentAmbientScope?.Value;

			if (currentAmbientScope is null) ThrowNoAmbientScope();

			RemoveAmbientScope(currentAmbientScope!);

			static void ThrowNoAmbientScope() => throw new InvalidOperationException("Tried to remove the current ambient scope when there was none.");
		}

		/// <summary>
		/// Private implementation that removes the ambient scope known to be the current one.
		/// Must only be called with the current, non-null scope.
		/// </summary>
		private static void RemoveAmbientScope(AmbientScope<TConcreteScope> ambientScope)
		{
			System.Diagnostics.Debug.Assert(ambientScope is not null);
			System.Diagnostics.Debug.Assert(ambientScope == CurrentAmbientScope?.Value);

			ReplaceAmbientScope(ambientScope, ambientScope.PhysicalParentScope);
		}

		/// <summary>
		/// <para>
		/// Replaces the current ambient scope by the given new ambient scope or null.
		/// </para>
		/// <para>
		/// Throws if the current ambient scope is incorrect or the new ambient scope is already the current scope.
		/// </para>
		/// </summary>
		/// <param name="currentScope">The scope that is currently the ambient scope. Must be correct. May be null.</param>
		/// <param name="newAmbientScope">The scope that is to be set as the ambient scope. May be null.</param>
		protected static void ReplaceAmbientScope(AmbientScope<TConcreteScope> currentScope, AmbientScope<TConcreteScope>? newAmbientScope)
		{
			if (!ReferenceEquals(currentScope, CurrentAmbientScope?.Value))
				ThrowNotCurrent();

			SetAmbientScope(newAmbientScope);

			static void ThrowNotCurrent() => throw new InvalidOperationException("The supposed current scope was not the current ambient scope. Always dispose or deactivate in reverse order of activation.");
		}

		/// <summary>
		/// Returns the current ambient scope, or null if no ambient scope has been set up.
		/// </summary>
		/// <param name="considerDefaultScope">If true, the default scope may be returned. If false, if only the default scope is visible, null is returned.</param>
		protected static TConcreteScope? GetAmbientScope(bool considerDefaultScope = true)
		{
			var result = considerDefaultScope ? DefaultScope : null;

			// Use of AsyncLocal optimized away as long as it has not been touched
			if (CurrentAmbientScope is not null)
			{
				var ambientScope = CurrentAmbientScope.Value;

				// An async method that disposes an AmbientScope, like DisposeAsync(), will not propagate that change to its caller
				// The caller would see the disposed scope
				// To counteract this effect, navigate up through disposed scopes
				// Use the physical rather than the effective parent: the scope was disposed, so even if it used ForceCreateNew, its obscurement no longer applies
				while (ambientScope?.State == AmbientScopeState.Disposed)
					ambientScope = ambientScope.PhysicalParentScope;

				if (ambientScope is not null) result = ambientScope;
			}

			return result;
		}
	}
}
