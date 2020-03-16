using System;
using System.Threading;

namespace Architect.AmbientContexts
{
	/// <summary>
	/// <para>
	/// This abstract base allows a subclass to implement the Ambient Context pattern, similar to how TransactionScope works.
	/// </para>
	/// <para>
	/// A scope is created with the help of a using statement. From any code within that using, the scope can be statically accessed.
	/// </para>
	/// <para>
	/// Scopes can optionally offer nesting (e.g. nested TransactionScopes), as well as the ability to temporarily obsure the parent scope (e.g. TransactionScope's Suppress option).
	/// </para>
	/// <para>
	/// Optionally, a ubiquitous default scope can be registered, accessible when no other scope is obscuring it.
	/// </para>
	/// </summary>
	public abstract partial class AmbientScope<TConcreteScope> : IDisposable
		where TConcreteScope : AmbientScope<TConcreteScope>
	{
		/// <summary>
		/// <para>
		/// Initialize once to use a default, global root scope that is always present.
		/// </para>
		/// <para>
		/// When an instance is assigned to this, it is removed from its position as the current AsyncLocal, and instead becomes the global root scope.
		/// </para>
		/// </summary>
		protected static TConcreteScope DefaultScope
		{
			get => DefaultScopeValue;
			set
			{
				if (value is null) throw new ArgumentNullException();
				if (value.ScopeOption == AmbientScopeOption.JoinExisting)
					throw new ArgumentException($"The default scope must not use {nameof(AmbientScopeOption)}.{AmbientScopeOption.JoinExisting}.");

				if (value.EffectiveParentScope != null || value.PhysicalParentScope != null)
					throw new InvalidOperationException("The scope has a parent somehow, but the default scope must not have a parent.");
				if (!ReferenceEquals(value, CurrentAmbientScope.Value))
					throw new InvalidOperationException("Another ambient scope was created between creation and registration of the default scope.");

				RemoveAmbientScope(value); // Do not keep the default in the AsyncLocal, as it is global rather than local

				var previousInstance = DefaultScopeValue;
				DefaultScopeValue = value;

				// On the potential previous default, call the custom dispose implementation without invoking the base dispose
				// The base dispose would remove it from the AsyncLocal, but as a default scope, it was already removed from there
				previousInstance?.DisposeImplementation();
			}
		}
		private static TConcreteScope DefaultScopeValue;

		/// <summary>
		/// <para>
		/// The physical parent scope, even if it is being obscured.
		/// </para>
		/// <para>
		/// Null if there is no parent or if the effective parent is the DefaultScope.
		/// </para>
		/// </summary>
		protected TConcreteScope PhysicalParentScope { get; }

		/// <summary>
		/// <para>
		/// If this scope uses JoinExisting, this contains the potential parent scope. Note that the effective parent may be the DefaultScope.
		/// </para>
		/// <para>
		/// Null otherwise.
		/// </para>
		/// </summary>
		protected TConcreteScope EffectiveParentScope { get; }

		protected AmbientScopeOption ScopeOption { get; }

		private int _disposeCount;

		protected AmbientScope(AmbientScopeOption scopeOption)
		{
			if (!(this is TConcreteScope)) throw new ArgumentException("The generic type parameter must be the type of the concrete AmbientScope subclass itself.");
			if (!Enum.IsDefined(typeof(AmbientScopeOption), scopeOption)) throw new ArgumentException($"Undefined {nameof(AmbientScopeOption)}: {scopeOption}.");

			this.ScopeOption = scopeOption;

			this.PhysicalParentScope = (TConcreteScope)GetAmbientScope();

			if (scopeOption == AmbientScopeOption.NoNesting && (this.PhysicalParentScope != null || DefaultScope != null))
				throw new InvalidOperationException($"{nameof(AmbientScopeOption)}.{nameof(AmbientScopeOption.NoNesting)} was used, but an outer scope is present.");

			this.EffectiveParentScope = scopeOption == AmbientScopeOption.JoinExisting
				? this.PhysicalParentScope ?? DefaultScope
				: null;

			SetAmbientScope(this);
		}

		public void Dispose() // Must NOT be virtual
		{
			// Dispose only once, using interlocked just in case
			if (Interlocked.CompareExchange(ref this._disposeCount, value: 1, comparand: 0) != 0)
				throw new InvalidOperationException("Dispose must only be called once."); // Exception is necessary to prevent implementor error related to setting DefaultScope

			try
			{
				this.DisposeImplementation();
			}
			finally
			{
				// If we have a parent, make sure it is not disposed
				if (this.PhysicalParentScope?._disposeCount > 0)
					throw new InvalidOperationException("The ambient scope being disposed has a physical parent scope that was disposed before it.");

				// Fill our position as the current ambient scope with our parent (which may be null)
				ReplaceAmbientScope(this, this.PhysicalParentScope);
			}
		}

		/// <summary>
		/// Allows custom dipose logic to be implemented without the chance of disrupting the base dispose logic.
		/// </summary>
		protected abstract void DisposeImplementation();

		/// <summary>
		/// <para>
		/// Returns the effective root scope from the current scope's perspective, which is either itself or the greatest accessible ancestor.
		/// </para>
		/// <para>
		/// Iteration stops when a scope uses the ForceCreateNew or NoNesting option, or when no further parent exists.
		/// </para>
		/// </summary>
		protected TConcreteScope GetEffectiveRootScope()
		{
			// Recurse into the effective parent until there is none
			return this.EffectiveParentScope?.GetEffectiveRootScope() ?? (TConcreteScope)this;
		}
	}
}
