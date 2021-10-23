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
	public abstract partial class AmbientScope<TConcreteScope> : AmbientScope
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
		/// <para>
		/// Note that this property returns null if the scope has not been activated.
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
		/// <para>
		/// Note that this property always returns null if the scope has not been activated.
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

		public sealed override void Dispose() // Must NOT be virtual
		{
			// Perform our primary disposal immediately
			var isDisposing = this.BaseDisposeImplementation();

			if (!isDisposing) return;

			// Let the subclass perform its disposal
			this.DisposeImplementation();
		}

		/// <summary>
		/// Performs our base dispose logic.
		/// Returns whether this class is being disposed for the first time and dispose logic was performed.
		/// </summary>
		private protected bool BaseDisposeImplementation()
		{
			// If we have a physical parent, make sure it is not disposed
			if (this.State == AmbientScopeState.Active && this.PhysicalParentScope?.State == AmbientScopeState.Disposed)
				throw new InvalidOperationException("The ambient scope being disposed has a physical parent scope that was disposed before it.");

			this.ChangeState(AmbientScopeState.Disposed, out var previousState);

			switch (previousState)
			{
				case AmbientScopeState.Disposed:
					// Avoid repeated disposal
					return false;
				case AmbientScopeState.New:
				// Nothing to do
				case AmbientScopeState.Default:
					// Unset us as the default scope, if we were it
					Interlocked.CompareExchange(ref DefaultScopeValue, value: null, comparand: (TConcreteScope)this);
					break;
				case AmbientScopeState.Active:
					this.DeactivateCore();
					break;
				default:
					throw new NotImplementedException($"{nameof(this.BaseDisposeImplementation)} is not implemented for state {previousState}.");
			}

			return true;
		}

		/// <summary>
		/// <para>
		/// Allows custom dipose logic to be implemented without the chance of disrupting the base dispose logic.
		/// </para>
		/// </summary>
		protected abstract void DisposeImplementation();

		/// <summary>
		/// <para>
		/// Activates this scope as the ambient scope, pushing it onto the ambient stack. It will be popped from the ambient stack when it is deactivated, such as on disposal.
		/// </para>
		/// <para>
		/// This method should generally be called at the very end of the subclass' constructor.
		/// <strong>After a scope's constructor activates the scope, it must never throw, or there would be no disposable object to undo the activation!</strong>
		/// </para>
		/// </summary>
		protected void Activate()
		{
			var physicalParentScope = GetAmbientScope(considerDefaultScope: false);

			var isNested = physicalParentScope is not null || // There is a physical parent scope, or
				(DefaultScope is not null && !this.NoNestingIgnoresDefaultScope); // There is a default scope and we care

			if (this.ScopeOption == AmbientScopeOption.NoNesting && isNested)
				throw new InvalidOperationException($"{nameof(AmbientScopeOption)}.{nameof(AmbientScopeOption.NoNesting)} was specified, but an outer scope is present.");

			if (!this.TryChangeState(newState: AmbientScopeState.Active, expectedCurrentState: AmbientScopeState.New))
				throw new InvalidOperationException($"The {this} was not in a valid state to be activated.");

			this.PhysicalParentScope = physicalParentScope;
			this.EffectiveParentScope = this.ScopeOption == AmbientScopeOption.JoinExisting
				? this.PhysicalParentScope ?? DefaultScope
				: null;

			try
			{
				SetAmbientScope(this);
			}
			catch
			{
				this.ChangeState(AmbientScopeState.New);
				throw;
			}
		}

		/// <summary>
		/// <para>
		/// Deactivates this scope as the ambient scope, popping it off the ambient stack.
		/// </para>
		/// <para>
		/// Throws if the scope is not active.
		/// </para>
		/// </summary>
		protected void Deactivate()
		{
			if (!this.TryChangeState(newState: AmbientScopeState.New, expectedCurrentState: AmbientScopeState.Active))
				throw new InvalidOperationException($"The {this} was not in a valid state to be deactivated.");

			this.DeactivateCore();
		}

		/// <summary>
		/// Deactivates this scope as the ambient scope, popping it off the ambient stack.
		/// Checks only that the state is no longer <see cref="AmbientScopeState.Active"/>, as it should have been atomically updated by the caller.
		/// </summary>
		private void DeactivateCore()
		{
			if (this.State == AmbientScopeState.Active) throw new Exception("Update the state before invoking this method.");

			// Overwrite with our physical parent (which may be null) our position as the current ambient scope
			ReplaceAmbientScope(this, this.PhysicalParentScope);
		}

		/// <summary>
		/// <para>
		/// Returns the effective root scope from the current scope's perspective, which is either itself or the greatest accessible ancestor.
		/// </para>
		/// <para>
		/// Only a scope that uses <see cref="AmbientScopeOption.JoinExisting"/> can access its predecessor.
		/// As soon as a scope with another <see cref="AmbientScopeOption"/> is encountered, it termintes the effective chain.
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
			if (defaultScope is not null)
			{
				if (defaultScope.State != AmbientScopeState.New)
					throw new ArgumentException($"The {defaultScope} was not in a valid state to be made the default scope.");

				if (defaultScope.ScopeOption == AmbientScopeOption.JoinExisting)
					throw new ArgumentException($"The default scope must not use {nameof(AmbientScopeOption)}.{AmbientScopeOption.JoinExisting}.");

				if (defaultScope.EffectiveParentScope is not null || defaultScope.PhysicalParentScope is not null)
					throw new InvalidOperationException("The scope has a parent somehow, but the default scope must not have one.");

				defaultScope.ChangeState(AmbientScopeState.Default);
			}

			var previousInstance = Interlocked.Exchange(ref DefaultScopeValue, defaultScope);

			previousInstance?.Dispose();
		}
	}

	/// <summary>
	/// The non-generic base class for any <see cref="AmbientScope{TConcreteScope}"/>.
	/// </summary>
	public abstract class AmbientScope : IDisposable
	{
		/// <summary>
		/// Helps prevent inheritance by anything other than <see cref="AmbientScope{TConcreteScope}"/>.
		/// </summary>
		private protected AmbientScope()
		{
		}

		public abstract void Dispose();
	}
}
