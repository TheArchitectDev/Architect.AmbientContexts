using System;
using System.Threading.Tasks;

namespace Architect.AmbientContexts
{
	/// <summary>
	/// <para>
	/// Asyncronously disposable.
	/// </para>
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
	public abstract class AsyncAmbientScope<TConcreteScope> : AmbientScope<TConcreteScope>, IAsyncDisposable
		where TConcreteScope : AmbientScope<TConcreteScope>
	{
		protected AsyncAmbientScope(AmbientScopeOption scopeOption)
			: base(scopeOption)
		{
		}

		public ValueTask DisposeAsync() // MUST NOT BE ASYNC, or popping us off the ambient stack will not be visible to the caller (i.e. we will be horribly broken)
		{
			// Perform our primary disposal immediately
			var isDisposing = this.BaseDisposeImplementation();

			if (!isDisposing) return new ValueTask();

			try
			{
				// Let the subclass perform its disposal
				var subclassDisposalTask = this.DisposeAsyncImplementation();

				// On immediate success, finish up
				if (subclassDisposalTask.IsCompleted)
				{
					this.UnsetParent();
					return subclassDisposalTask;
				}

				// Return a task that awaits and then finishes up
				return AwaitTaskAndUnsetParent(subclassDisposalTask);
			}
			catch
			{
				// On failure, finish up
				this.UnsetParent();
				throw;
			}

			// Local function that awaits the input task and then unsets the parent
			async ValueTask AwaitTaskAndUnsetParent(ValueTask task)
			{
				try
				{
					await task;
				}
				finally
				{
					this.UnsetParent();
				}
			}
		}

		/// <summary>
		/// <para>
		/// Allows custom dispose logic to be implemented without the chance of disrupting the base dispose logic.
		/// </para>
		/// <para>
		/// Override this method to implement a synchronous implementation manually.
		/// The default implementation calls the async one and synchronously awaits a result, which is usually fine for disposal.
		/// </para>
		/// </summary>
		protected override void DisposeImplementation()
		{
			var valueTask = this.DisposeAsyncImplementation();

			if (!valueTask.IsCompleted)
				valueTask.GetAwaiter().GetResult();
		}

		/// <summary>
		/// <para>
		/// Allows custom dispose logic to be implemented without the chance of disrupting the base dispose logic.
		/// </para>
		/// </summary>
		protected abstract ValueTask DisposeAsyncImplementation();
	}
}
