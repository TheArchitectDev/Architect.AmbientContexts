using System;
using System.Threading.Tasks;
using Xunit;

namespace Architect.AmbientContexts.Tests
{
	public sealed class AsyncAmbientScopeTests
	{
		[Fact]
		public async Task DisposeAsync_FromActiveWithUnderlyingScope_ShouldRevealExpectedScope()
		{
			await using var scope1 = new TestScope(1, AmbientScopeOption.JoinExisting);
			await using var scope2 = new TestScope(2, AmbientScopeOption.JoinExisting);
			await scope2.DisposeAsync();

			Assert.Equal(scope1, TestScope.Current);
		}

		[Fact]
		public async Task DisposeAsync_FromActiveWithDefaultScope_ShouldRevealExpectedScope()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting);
			await scope.DisposeAsync();

			Assert.Equal(TestScope.DefaultScopeConstant, TestScope.Current);
		}

		[Fact]
		public async Task DisposeAsync_Regularly_ShouldInvokeDisposeAsyncImplementation()
		{
			var wasInvoked = false;

			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDisposeAsync = () => { wasInvoked = true; return new ValueTask(); },
			};

			await scope.DisposeAsync();

			Assert.True(wasInvoked);
		}

		[Fact]
		public async Task DisposeAsync_Regularly_ShouldNotInvokeSynchronousDisposeImplementation()
		{
			var wasInvoked = false;

			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDispose = () => wasInvoked = true,
			};

			await scope.DisposeAsync();

			Assert.False(wasInvoked);
		}

		[Fact]
		public async Task DisposeAsync_SecondTime_ShouldNotInvokeAsyncDisposeImplementation()
		{
			var wasInvoked = false;

			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDisposeAsync = () => { wasInvoked = true; return new ValueTask(); },
			};

			await scope.DisposeAsync();
			wasInvoked = false;
			await scope.DisposeAsync();

			Assert.False(wasInvoked);
		}

		[Fact]
		public async Task DisposeAsync_WithExceptionInSubclassDispose_ShouldStillPopScopeOffAmbientStack()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDisposeAsync = () => throw new TimeoutException(),
			};

			try
			{
				await scope.DisposeAsync();
			}
			catch (TimeoutException)
			{
				// Ignore our own exception
			}

			Assert.Equal(TestScope.DefaultScopeConstant, TestScope.Current);
		}

		[Fact]
		public async Task DisposeAsync_WithExceptionInSubclassDispose_ShouldStillUnsetParent()
		{
			using var scope = new TestScope(1, AmbientScopeOption.JoinExisting)
			{
				OnDisposeAsync = () => throw new TimeoutException(),
			};

			try
			{
				await scope.DisposeAsync();
			}
			catch (TimeoutException)
			{
				// Ignore our own exception
			}

			Assert.Null(scope.PhysicalParentScope);
			Assert.Null(scope.EffectiveParentScope);
		}
	}
}
