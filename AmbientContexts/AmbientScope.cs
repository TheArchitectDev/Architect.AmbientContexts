using System;
using System.Runtime.Serialization;
using System.Threading;
using Architect.AmbientContexts.Defaults;
using Microsoft.Extensions.Hosting;

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

		static AmbientScope()
		{
			var dummyInstance = (TConcreteScope)FormatterServices.GetUninitializedObject(typeof(TConcreteScope));
			StaticDefaultKeeper<TConcreteScope>.DefaultScope = dummyInstance.CreateInitialDefaultScope();
		}

		/// <summary>
		/// <para>
		/// A default scope that is always present, acting as a ubiquitous bottom layer beneath any regular scopes.
		/// </para>
		/// <para>
		/// If the current call chain originates from any dependency inside an <see cref="IHost"/>, and a default scope was registered in its DI container, that is returned.
		/// Otherwise, the static default scope is returned.
		/// </para>
		/// </summary>
		protected static TConcreteScope? DefaultScope => DefaultScopeContext.Current?.GetDefaultScope<TConcreteScope>() ??
			StaticDefaultKeeper<TConcreteScope>.DefaultScope;

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
		/// Note that this property returns null if the scope has not been activated.
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

			try
			{
				// Let the subclass perform its disposal
				this.DisposeImplementation();
			}
			finally
			{
				// Finish up
				this.UnsetParent();
			}
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
					// Unset us as the static default scope, if we were it
					// If we were the default in an IHost-based DefaultScopeContext, do nothing, as it has its own mechanism of omitting disposed default scopes
					Interlocked.CompareExchange(ref StaticDefaultKeeper<TConcreteScope>.DefaultScope, value: null, comparand: (TConcreteScope)this);
					break;
				case AmbientScopeState.Active:
					this.DeactivateCore(unsetParent: false);
					break;
				default:
					throw new NotImplementedException($"{nameof(this.BaseDisposeImplementation)} is not implemented for state {previousState}.");
			}

			return true;
		}

		private protected void UnsetParent()
		{
			this.PhysicalParentScope = null;
			this.EffectiveParentScope = null;
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
		/// Checks only that the state is no longer active, as it should have been atomically updated by the caller.
		/// </summary>
		private void DeactivateCore(bool unsetParent = true)
		{
			if (this.State == AmbientScopeState.Active) throw new Exception("Update the state before invoking this method.");

			// Overwrite with our physical parent (which may be null) our position as the current ambient scope
			ReplaceAmbientScope(this, this.PhysicalParentScope);

			if (unsetParent) this.UnsetParent();
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
		/// Moves the current scope into <see cref="AmbientScopeState.Default"/>, throwing if it is unsuitable.
		/// </summary>
		internal void ConvertToDefaultScope()
		{
			if (this.State != AmbientScopeState.New)
				throw new ArgumentException($"The {this} was not in a valid state to be made the default scope.");

			if (this.ScopeOption == AmbientScopeOption.JoinExisting)
				throw new ArgumentException($"The default scope must not use {nameof(AmbientScopeOption)}.{AmbientScopeOption.JoinExisting}.");

			if (this.EffectiveParentScope != null || this.PhysicalParentScope != null)
				throw new InvalidOperationException("The scope has a parent somehow, but the default scope must not have one.");

			this.ChangeState(AmbientScopeState.Default);
		}

		// #TODO: If one container uses a different default, and the other wants the implicit default, the implicit default is currently gone.
		/// <summary>
		/// <para>
		/// Can be overridden to set the initial default scope.
		/// </para>
		/// <para>
		/// <em>In absence of static abstract members, this method has non-static. It must not reference any instance members.</em>
		/// </para>
		/// </summary>
		protected virtual TConcreteScope? CreateInitialDefaultScope()
		{
			return null;
		}
	}

	/// <summary>
	/// The non-generic base class for any <see cref="AmbientScope{TConcreteScope}"/>.
	/// </summary>
	public abstract class AmbientScope : IDisposable
	{
		/// <summary>
		/// Helps prevents inheritance from anything other than <see cref="AmbientScope{TConcreteScope}"/>.
		/// </summary>
		private protected AmbientScope()
		{
		}

		public abstract void Dispose();
	}
}
