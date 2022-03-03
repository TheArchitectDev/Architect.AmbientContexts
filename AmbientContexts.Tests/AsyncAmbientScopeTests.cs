using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xunit;

#if NETCOREAPP3_1

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
		public Task ConstructAndDispose_WithLifetimeOfTwoParallelUnitTestClasses_ShouldNotInterfereWithEachOther()
		{
			var tasks = new ConcurrentQueue<Task>();

			Parallel.For(0, 20, i =>
			{
				tasks.Enqueue(PerformCore(useNumberOne: i % 2 == 0));
			});

			return Task.WhenAll(tasks);

			// Local function that performs the core of the test
			static async Task PerformCore(bool useNumberOne)
			{
				if (useNumberOne)
				{
					await using var testInstance = new TestClass1();
					await testInstance.PerformAssertionAsync();
				}
				else
				{
					await using var testInstance = new TestClass2();
					await testInstance.PerformAssertionAsync();
				}
			}
		}

		private sealed class TestClass1 : IAsyncDisposable
		{
			private TestScope Scope { get; } = new TestScope(index: 1, AmbientScopeOption.ForceCreateNew);

			public ValueTask DisposeAsync()
			{
				return this.Scope.DisposeAsync();
			}

			public async Task PerformAssertionAsync()
			{
				await Task.Delay(TimeSpan.FromMilliseconds(1));
				var result = TestScope.Current.Index;
				Assert.Equal(1, result);
			}
		}

		private sealed class TestClass2 : IAsyncDisposable
		{
			private TestScope Scope { get; } = new TestScope(index: 2, AmbientScopeOption.ForceCreateNew);

			public ValueTask DisposeAsync()
			{
				return this.Scope.DisposeAsync();
			}

			public async Task PerformAssertionAsync()
			{
				await Task.Delay(TimeSpan.FromMilliseconds(1));
				var result = TestScope.Current.Index;
				Assert.Equal(2, result);
			}
		}
	}
}

#endif