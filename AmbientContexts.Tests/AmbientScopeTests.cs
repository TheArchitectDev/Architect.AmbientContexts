using System;
using Xunit;

namespace Architect.AmbientContexts.Tests
{
	public sealed partial class AmbientScopeTests
	{
		[Fact]
		public void Construct_Regularly_ShouldNotRegisterAmbientScope()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting);
			Assert.Null(ManuallyActivatedScope.CurrentNondefault);
		}

		[Fact]
		public void Activate_Regularly_ShouldRegisterAmbientScope()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			scope.Activate();

			Assert.Equal(scope, ManuallyActivatedScope.CurrentNondefault);
		}

		[Fact]
		public void Activate_Twice_ShouldThrow()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			scope.Activate();

			Assert.ThrowsAny<InvalidOperationException>(() => scope.Activate());
		}

		[Fact]
		public void Activate_AfterDisposal_ShouldThrow()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			scope.Dispose();

			Assert.ThrowsAny<InvalidOperationException>(() => scope.Activate());
		}

		[Fact]
		public void Activate_Default_ShouldThrow()
		{
			Assert.ThrowsAny<InvalidOperationException>(() => ManuallyActivatedScope.Current.Activate());
		}

		[Fact]
		public void Activate_WithNoNestingAndIgnoredDefaultScope_ShouldSucceed()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			scope.Activate();
		}

		[Fact]
		public void Activate_WithNoNestingAndDefaultScope_ShouldThrow()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting);
			Assert.ThrowsAny<InvalidOperationException>(() => scope.Activate());
		}

		[Fact]
		public void Activate_WithForceCreateNewAndDefaultScope_ShouldSucceed()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);
			scope.Activate();
		}

		[Fact]
		public void Activate_WithJoinExistingAndDefaultScope_ShouldSucceed()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.JoinExisting);
			scope.Activate();
		}

		[Fact]
		public void Dispose_FromActiveWithUnderlyingScope_ShouldRevealExpectedScope()
		{
			using var scope1 = new TestScope(1, AmbientScopeOption.JoinExisting);
			using var scope2 = new TestScope(2, AmbientScopeOption.JoinExisting);
			scope2.Dispose();

			Assert.Equal(scope1, TestScope.Current);
		}

		[Fact]
		public void Dispose_FromActiveWithDefaultScope_ShouldRevealExpectedScope()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);
			scope.Dispose();

			Assert.Equal(TestScope.DefaultScopeConstant, TestScope.Current);
		}

		[Fact]
		public void Dispose_Regularly_ShouldInvokeDisposeImplementation()
		{
			var wasInvoked = false;

			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDispose = () => wasInvoked = true
			};

			scope.Dispose();

			Assert.True(wasInvoked);
		}

		[Fact]
		public void Dispose_SecondTime_ShouldNotInvokeDisposeImplementation()
		{
			var wasInvoked = false;

			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDispose = () => wasInvoked = true
			};

			scope.Dispose();
			wasInvoked = false;
			scope.Dispose();

			Assert.False(wasInvoked);
		}

		[Fact]
		public void Dispose_WithExceptionInSubclassDispose_ShouldStillPopScopeOffAmbientStack()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDispose = () => throw new TimeoutException(),
			};

			try
			{
				scope.Dispose();
			}
			catch (TimeoutException)
			{
				// Ignore our own exception
			}

			Assert.Equal(TestScope.DefaultScopeConstant, TestScope.Current);
		}

		[Fact]
		public void PhysicalParentScope_WithNoNesting_ShouldReturnNull()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			scope.Activate();

			var parent = scope.PhysicalParentScope;

			Assert.Null(parent);
		}

		[Fact]
		public void EffectiveParentScope_WithNoNesting_ShouldReturnNull()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			scope.Activate();

			var parent = scope.EffectiveParentScope;

			Assert.Null(parent);
		}

		[Fact]
		public void PhysicalParentScope_WithForceCreateNewAndDefault_ShouldReturnNull()
		{
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			var parent = scope.PhysicalParentScope;

			Assert.Null(parent);
		}

		[Fact]
		public void EffectiveParentScope_WithForceCreateNewAndDefault_ShouldReturnNull()
		{
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			var parent = scope.EffectiveParentScope;

			Assert.Null(parent);
		}

		[Fact]
		public void PhysicalParentScope_WithForceCreateNewAndUnderlyingScope_ShouldReturnUnderlyingScope()
		{
			using var underlyingScope = new TestScope(1, AmbientScopeOption.ForceCreateNew);
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			var parent = scope.PhysicalParentScope;

			Assert.Equal(underlyingScope, parent);
		}

		[Fact]
		public void EffectiveParentScope_WithForceCreateNewAndUnderlyingScope_ShouldReturnNull()
		{
			using var underlyingScope = new TestScope(1, AmbientScopeOption.ForceCreateNew);
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			var parent = scope.EffectiveParentScope;

			Assert.Null(parent);
		}

		[Fact]
		public void PhysicalParentScope_WithJoinExistingAndDefault_ShouldReturnNull()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);

			var parent = scope.PhysicalParentScope;

			Assert.Null(parent);
		}

		[Fact]
		public void EffectiveParentScope_WithJoinExistingAndDefault_ShouldReturnDefault()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);

			var parent = scope.EffectiveParentScope;

			Assert.Equal(TestScope.DefaultScopeConstant, parent);
		}

		[Fact]
		public void PhysicalParentScope_WithJoinExistingAndUnderlyingScope_ShouldReturnUnderlyingScope()
		{
			using var underlyingScope = new TestScope(1, AmbientScopeOption.JoinExisting);
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);

			var parent = scope.PhysicalParentScope;

			Assert.Equal(underlyingScope, parent);
		}

		[Fact]
		public void EffectiveParentScope_WithJoinExistingAndUnderlyingScope_ShouldReturnUnderlyingScope()
		{
			using var underlyingScope = new TestScope(1, AmbientScopeOption.JoinExisting);
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);

			var parent = scope.EffectiveParentScope;

			Assert.Equal(underlyingScope, parent);
		}

		[Fact]
		public void GetEffectiveRootScope_WithNoNesting_ShouldReturnSelf()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			scope.Activate();

			var parent = scope.GetEffectiveRootScope();

			Assert.Equal(scope, parent);
		}

		[Fact]
		public void GetEffectiveRootScope_WithForceCreateNewAndDefault_ShouldReturnSelf()
		{
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			var parent = scope.GetEffectiveRootScope();

			Assert.Equal(scope, parent);
		}

		[Fact]
		public void GetEffectiveRootScope_WithForceCreateNewAndTransparentUnderlyingScope_ShouldReturnSelf()
		{
			using var underlyingScope = new TestScope(1, AmbientScopeOption.JoinExisting);
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			var parent = scope.GetEffectiveRootScope();

			Assert.Equal(scope, parent);
		}

		[Fact]
		public void GetEffectiveRootScope_WithJoinExistingAndDefault_ShouldReturnDefault()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);

			var parent = scope.GetEffectiveRootScope();

			Assert.Equal(TestScope.DefaultScopeConstant, parent);
		}

		[Fact]
		public void GetEffectiveRootScope_WithJoinExistingAndOpaqueUnderlyingScope_ShouldReturnUnderlyingScope()
		{
			using var underlyingScope = new TestScope(1, AmbientScopeOption.ForceCreateNew);
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);

			var parent = scope.GetEffectiveRootScope();

			Assert.Equal(underlyingScope, parent);
		}

		[Fact]
		public void GetEffectiveRootScope_WithJoinExistingAndTransparentUnderlyingScope_ShouldReturnDefault()
		{
			using var underlyingScope = new TestScope(1, AmbientScopeOption.JoinExisting);
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);

			var parent = scope.GetEffectiveRootScope();

			Assert.Equal(TestScope.DefaultScopeConstant, parent);
		}

		[Fact]
		public void GetEffectiveRootScope_WithJoinExistingAndNoNestingUnderlyingScope_ShouldReturnUnderlyingScope()
		{
			using var underlyingScope = new ManuallyActivatedScope(1, AmbientScopeOption.NoNesting, noNestingIgnoresDefaultScope: true);
			underlyingScope.Activate();

			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.JoinExisting);
			scope.Activate();

			var parent = scope.GetEffectiveRootScope();

			Assert.Equal(underlyingScope, parent);
		}
	}
}

