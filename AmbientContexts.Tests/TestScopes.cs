using System;
using System.Threading.Tasks;

namespace Architect.AmbientContexts.Tests
{
	internal sealed class TestScope : AsyncAmbientScope<TestScope>
	{
		public const int DefaultIndex = -1;
		public static readonly TestScope DefaultScopeConstant = new TestScope(DefaultIndex, AmbientScopeOption.NoNesting, activate: false);

		public Action OnDispose { get; set; }
		public Func<ValueTask> OnDisposeAsync { get; set; }

		static TestScope()
		{
			SetDefaultScope(DefaultScopeConstant);
		}

		public int Index { get; }
		public int ParentIndex => Current.EffectiveParentScope.Index;

		public new TestScope PhysicalParentScope => base.PhysicalParentScope;
		public new TestScope EffectiveParentScope => base.EffectiveParentScope;
		public new TestScope GetEffectiveRootScope() => base.GetEffectiveRootScope();

		public TestScope(int index, AmbientScopeOption scopeOption, bool activate = true)
			: base(scopeOption)
		{
			this.Index = index;

			if (activate) this.Activate();
		}

		protected override void DisposeImplementation()
		{
			this.OnDispose?.Invoke();
		}

		protected override async ValueTask DisposeAsyncImplementation()
		{
			var task = this.OnDisposeAsync?.Invoke();
			if (task != null) await task.Value;
		}

		public static TestScope Current => GetAmbientScope();
		public static TestScope CurrentNondefault => GetAmbientScope(considerDefaultScope: false);
	}

	internal class ManuallyActivatedScope : AmbientScope<ManuallyActivatedScope>
	{
		public const int DefaultIndex = -1;

		static ManuallyActivatedScope()
		{
			SetDefaultScope(new ManuallyActivatedScope(DefaultIndex, AmbientScopeOption.NoNesting));
		}

		public int Index { get; }
		public int ParentIndex => Current.EffectiveParentScope.Index;
		protected override bool NoNestingIgnoresDefaultScope { get; }

		public new ManuallyActivatedScope PhysicalParentScope => base.PhysicalParentScope;
		public new ManuallyActivatedScope EffectiveParentScope => base.EffectiveParentScope;
		public new ManuallyActivatedScope GetEffectiveRootScope() => base.GetEffectiveRootScope();

		public ManuallyActivatedScope(int index, AmbientScopeOption scopeOption, bool noNestingIgnoresDefaultScope = false)
			: base(scopeOption)
		{
			this.Index = index;
			this.NoNestingIgnoresDefaultScope = noNestingIgnoresDefaultScope;
		}

		protected override void DisposeImplementation()
		{
		}

		public new void Activate() => base.Activate();
		public new void Deactivate() => base.Deactivate();

		public static ManuallyActivatedScope Current => GetAmbientScope();
		public static ManuallyActivatedScope CurrentNondefault => GetAmbientScope(considerDefaultScope: false);
	}

	/// <summary>
	/// Because we want to test some static behaviors, we will need various copies of a class like this.
	/// By subclassing, most of the code need not be duplicated.
	/// </summary>
	internal abstract class StaticTestScope<TSelf> : AmbientScope<TSelf>
		where TSelf : StaticTestScope<TSelf>
	{
		public static TSelf Current => GetAmbientScope();
		public static TSelf CurrentNondefault => GetAmbientScope(considerDefaultScope: false);

		public int Index { get; private set; }

		public bool IsCustomDisposed { get; private set; }

		protected override bool NoNestingIgnoresDefaultScope => true;

		public StaticTestScope()
			: this(AmbientScopeOption.NoNesting)
		{
		}

		public StaticTestScope(AmbientScopeOption option)
			: base(option)
		{
			this.Index = -1;
		}

		protected override void DisposeImplementation()
		{
			this.IsCustomDisposed = true;
		}

		public static void SetDefaultScope(int index)
		{
			var instance = Activator.CreateInstance<TSelf>();
			instance.Index = index;

			SetDefaultScope(instance);
		}

		public static new void SetDefaultScope(TSelf instance)
		{
			AmbientScope<TSelf>.SetDefaultScope(instance);
		}
	}
}
