using System;
using Xunit;

namespace Architect.AmbientContexts.Tests
{
	public sealed partial class AmbientScopeTests
	{
		[Fact]
		public void Construct_Regularly_ShouldResultInStateNew()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);
			Assert.Equal(AmbientScopeState.New, scope.State);
		}

		[Fact]
		public void Activate_Regularly_ShouldResultInStateActive()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);

			scope.Activate();

			Assert.Equal(AmbientScopeState.Active, scope.State);
		}

		[Fact]
		public void Deactivate_WithPotentialParentScope_ShouldResultInNonNullParents()
		{
			using var outerScope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);
			using var innerScope = new ManuallyActivatedScope(1, AmbientScopeOption.JoinExisting);

			outerScope.Activate();
			innerScope.Activate();

			Assert.NotNull(innerScope.PhysicalParentScope);
			Assert.NotNull(innerScope.EffectiveParentScope);
		}

		[Fact]
		public void Deactivate_FromNewState_ShouldThrow()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);

			Assert.Throws<InvalidOperationException>(() => scope.Deactivate());
		}

		[Fact]
		public void Deactivate_FromDisposedState_ShouldThrow()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);
			scope.Dispose();

			Assert.Throws<InvalidOperationException>(() => scope.Deactivate());
		}

		[Fact]
		public void Deactivate_FromActivateState_ShouldResultInStateNew()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);
			scope.Activate();

			scope.Deactivate();

			Assert.Equal(AmbientScopeState.New, scope.State);
		}

		[Fact]
		public void Construct_WithActivateFromConstructor_ShouldResultInStateActive()
		{
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			Assert.Equal(AmbientScopeState.Active, scope.State);
		}

		[Fact]
		public void Dispose_FromStateNew_ShouldResultInStateDisposed()
		{
			using var scope = new ManuallyActivatedScope(1, AmbientScopeOption.ForceCreateNew);

			scope.Dispose();

			Assert.Equal(AmbientScopeState.Disposed, scope.State);
		}

		[Fact]
		public void Dispose_FromStateActive_ShouldResultInStateDisposed()
		{
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			scope.Dispose();

			Assert.Equal(AmbientScopeState.Disposed, scope.State);
		}

		[Fact]
		public void Dispose_TwiceFromStateActive_ShouldResultInStateDisposed()
		{
			using var scope = new TestScope(1, AmbientScopeOption.ForceCreateNew);

			scope.Dispose();
			scope.Dispose();

			Assert.Equal(AmbientScopeState.Disposed, scope.State);
		}
	}
}
