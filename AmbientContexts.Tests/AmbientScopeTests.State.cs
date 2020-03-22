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
