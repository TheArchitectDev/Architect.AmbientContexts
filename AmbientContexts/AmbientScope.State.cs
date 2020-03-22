using System.Threading;

namespace Architect.AmbientContexts
{
	public abstract partial class AmbientScope<TConcreteScope>
	{
		internal AmbientScopeState State => (AmbientScopeState)this._state;
		private int _state = (int)AmbientScopeState.New;

		private void ChangeState(AmbientScopeState newState)
		{
			this._state = (int)newState;
		}

		private void ChangeState(AmbientScopeState newState, out AmbientScopeState previousState)
		{
			previousState = (AmbientScopeState)Interlocked.Exchange(ref this._state, (int)newState);
		}
	}
}
