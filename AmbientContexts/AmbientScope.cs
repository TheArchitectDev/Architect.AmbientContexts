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
	/// <typeparam name="TConcreteScope">The inheriting class.</typeparam>
	public abstract partial class AmbientScope<TConcreteScope> : IDisposable
		where TConcreteScope : AmbientScope<TConcreteScope>
	{
		public override string ToString() => this.GetType().Name;

		/// <summary>
		/// A default, global root scope that is always present, acting as a ubiquitous bottom layer beneath any regular scopes.
		/// </summary>
		protected static TConcreteScope? DefaultScope => DefaultScopeValue;
		private static TConcreteScope? DefaultScopeValue;

		/// <summary>
		/// <para>
		/// The physical parent scope, even if it is being obscured because of the <see cref="AmbientScopeOption"/>.
		/// </para>
		/// <para>
		/// Null if there is no parent or if the effective parent is the default scope.
		/// </para>
		/// </summary>
		protected TConcreteScope? PhysicalParentScope { get; private set; }

		/// <summary>
		/// <para>
		/// If this scope uses <see cref="AmbientScopeOption.JoinExisting"/>, this contains the parent scope, if there is one. Note that the effective parent may be the default scope.
		/// </para>
		/// <para>
		/// Null otherwise.
		/// </para>
		/// </summary>
		protected TConcreteScope? EffectiveParentScope { get; private set; }

		protected AmbientScopeOption ScopeOption { get; }

		/// <summary>
		/// If true, when checking for nesting because <see cref="AmbientScopeOption.NoNesting"/> is specified, the default scope is ignored.
		/// Defaults to false.
		/// </summary>
		protected virtual bool NoNestingIgnoresDefaultScope => false;

		protected AmbientScope(AmbientScopeOption scopeOption)
		{
			if (!(this is TConcreteScope)) throw new ArgumentException("The generic type parameter must be the type of the concrete AmbientScope subclass itself.");
			if (!Enum.IsDefined(typeof(AmbientScopeOption), scopeOption)) throw new ArgumentException($"Undefined {nameof(AmbientScopeOption)}: {scopeOption}.");

			this.ScopeOption = scopeOption;
		}

		public void Dispose() // Must NOT be virtual
		{
			// If we have a physical parent, make sure it is not disposed
			if (this.State == AmbientScopeState.Active && this.PhysicalParentScope?.State == AmbientScopeState.Disposed)
				throw new InvalidOperationException("The ambient scope being disposed has a physical parent scope that was disposed before it.");

			try
			{
				this.DisposeImplementation();
			}
			finally
			{
				this.BaseDisposeImplementation();
			}
		}

		/// <summary>
		/// Performs our base dispose logic.
		/// Skips repeated execution.
		/// </summary>
		private void BaseDisposeImplementation()
		{
			this.ChangeState(AmbientScopeState.Disposed, out var previousState);

			switch (previousState)
			{
				case AmbientScopeState.Disposed:
					// Avoid repeated disposal
					return;
				case AmbientScopeState.New:
					// Nothing to do
				case AmbientScopeState.Default:
					// Unset us as the default scope, if we were it
					Interlocked.CompareExchange(ref DefaultScopeValue, value: null, comparand: (TConcreteScope)this);
					break;
				case AmbientScopeState.Active:
					// Overwrite with our physical parent (which may be null) our position as the current ambient scope
					ReplaceAmbientScope(this, this.PhysicalParentScope);
					break;
				default:
					throw new NotImplementedException($"{nameof(this.BaseDisposeImplementation)} is not implemented for state {previousState}.");
			}
		}

		/// <summary>
		/// Allows custom dipose logic to be implemented without the chance of disrupting the base dispose logic.
		/// </summary>
		protected abstract void DisposeImplementation();

		/// <summary>
		/// <para>
		/// Activates this scope as the ambient scope, pushing it onto the ambient stack. It will be popped from the ambient stack when it is disposed.
		/// </para>
		/// <para>
		/// This method should generally be called at the very end of the subclass' constructor.
		/// After activation, no constructor may throw, or there is nothing to dispose to undo the activation!
		/// </para>
		/// </summary>
		protected void Activate()
		{
			if (this.State != AmbientScopeState.New)
				throw new InvalidOperationException($"The {this} was not in a valid state to be activated.");

			var physicalParentScope = GetAmbientScope(considerDefaultScope: false);

			var isNested = physicalParentScope != null || // There is a physical parent scope, or
				(DefaultScope != null && !this.NoNestingIgnoresDefaultScope); // There is a default scope and we care

			if (this.ScopeOption == AmbientScopeOption.NoNesting && isNested)
				throw new InvalidOperationException($"{nameof(AmbientScopeOption)}.{nameof(AmbientScopeOption.NoNesting)} was specified, but an outer scope is present.");

			this.PhysicalParentScope = physicalParentScope;
			this.EffectiveParentScope = this.ScopeOption == AmbientScopeOption.JoinExisting
				? this.PhysicalParentScope ?? DefaultScope
				: null;

			SetAmbientScope(this);

			this.ChangeState(AmbientScopeState.Active);
		}

		/// <summary>
		/// <para>
		/// Returns the effective root scope from the current scope's perspective, which is either itself or the greatest accessible ancestor.
		/// </para>
		/// <para>
		/// An ancestor is accessible if it uses <see cref="AmbientScopeOption.JoinExisting"/>.
		/// See also <see cref="EffectiveParentScope"/>.
		/// </para>
		/// </summary>
		protected TConcreteScope GetEffectiveRootScope()
		{
			// Recurse into the effective parent until there is none
			return this.EffectiveParentScope?.GetEffectiveRootScope() ?? (TConcreteScope)this;
		}

		/// <summary>
		/// <para>
		/// Sets a default, global root scope that is always present, acting as a ubiquitous bottom layer beneath any regular scopes.
		/// </para>
		/// <para>
		/// This method overwrites and disposes the potential previous value.
		/// </para>
		/// <para>
		/// If the default scope is disposed while it is still the default scope, it will remove itself from the position.
		/// </para>
		/// </summary>
		/// <param name="defaultScope">The default scope to register. May be null to unset.</param>
		protected static void SetDefaultScope(TConcreteScope? defaultScope)
		{
			if (defaultScope != null)
			{
				if (defaultScope.State != AmbientScopeState.New)
					throw new ArgumentException($"The {defaultScope} was not in a valid state to be made the default scope.");

				if (defaultScope.ScopeOption == AmbientScopeOption.JoinExisting)
					throw new ArgumentException($"The default scope must not use {nameof(AmbientScopeOption)}.{AmbientScopeOption.JoinExisting}.");

				if (defaultScope.EffectiveParentScope != null || defaultScope.PhysicalParentScope != null)
					throw new InvalidOperationException("The scope has a parent somehow, but the default scope must not have one.");
			}

			defaultScope?.ChangeState(AmbientScopeState.Default);

			var previousInstance = Interlocked.Exchange(ref DefaultScopeValue, defaultScope);

			previousInstance?.Dispose();
		}
	}
}
