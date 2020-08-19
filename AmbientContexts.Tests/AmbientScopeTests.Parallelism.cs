using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Architect.AmbientContexts.Tests
{
	public sealed partial class AmbientScopeTests
	{
		[Fact]
		public void Current_FromParallel_ShouldSeeDefault()
		{
			Parallel.For(0, 100, index =>
			{
				Assert.Equal(TestScope.DefaultIndex, TestScope.Current.Index);
			});
		}

		[Fact]
		public void Construct_FromParallel_ShouldNotSeeEachOther()
		{
			using (new TestScope(Int32.MaxValue, AmbientScopeOption.ForceCreateNew))
			{
				Parallel.For(0, 2, index =>
				{
					if (index != 0) Thread.Sleep(20);

					Assert.Equal(Int32.MaxValue, TestScope.Current.Index); // Should see the outer scope

					using (new TestScope(index, AmbientScopeOption.JoinExisting))
					{
						Assert.Equal(index, TestScope.Current.Index); // Should see the own scope
						Assert.Equal(Int32.MaxValue, TestScope.Current.ParentIndex); // Should have the outer scope as the effective parent
						Thread.Sleep(60);
					}
				});
			}
		}

		[Fact]
		public void Construct_FromParallel_ShouldNotInterfereWithEachOther()
		{
			Parallel.For(0, 100, index =>
			{
				using (new TestScope(index, AmbientScopeOption.ForceCreateNew))
				{
					Assert.Equal(index, TestScope.Current.Index);
				}
			});
		}

		[Fact]
		public void Construct_FromParallelWithJoinExisting_ShouldSeeDefaultParent()
		{
			Parallel.For(0, 100, index =>
			{
				using (new TestScope(index, AmbientScopeOption.JoinExisting))
				{
					Assert.Equal(TestScope.DefaultIndex, TestScope.Current.ParentIndex);
				}
			});
		}

		[Fact]
		public void Construct_FromParallelWithJoinExistingAndOuterScope_ShouldSeeOuterScopeParent()
		{
			using (new TestScope(Int32.MaxValue, AmbientScopeOption.ForceCreateNew))
			{
				Parallel.For(0, 100, index =>
				{
					using (new TestScope(index, AmbientScopeOption.JoinExisting))
					{
						Assert.Equal(Int32.MaxValue, TestScope.Current.ParentIndex);
					}
				});
			}
		}

		[Fact]
		public void Construct_FromParallelWithForceCreateNewAndOuterScope_ShouldNotInterfereWithEachOther()
		{
			using (new TestScope(Int32.MaxValue, AmbientScopeOption.ForceCreateNew))
			{
				Parallel.For(0, 100, index =>
				{
					using (new TestScope(index, AmbientScopeOption.ForceCreateNew))
					{
						Assert.Equal(index, TestScope.Current.Index);
					}
				});
			}
		}

		[Fact]
		public Task ConstructAndDispose_WithLifetimeOfTwoParallelUnitTestClasses_ShouldNotInterfereWithEachOther()
		{
			var tasks = new ConcurrentQueue<Task>();

			Parallel.For(0, 20, i =>
			{
				if (i % 2 == 0)
				{
					using var testInstance = new TestClass1();
					tasks.Enqueue(testInstance.PerformAssertionAsync());
				}
				else
				{
					using var testInstance = new TestClass2();
					tasks.Enqueue(testInstance.PerformAssertionAsync());
				}
			});

			return Task.WhenAll(tasks);
		}

		private sealed class TestClass1 : IDisposable
		{
			private static readonly DateTime FixedTime = new DateTime(0001, 01, 01, 12, 00, 00, DateTimeKind.Local);
			private ClockScope Scope { get; } = new ClockScope(() => FixedTime);

			public void Dispose()
			{
				this.Scope.Dispose();
			}

			public async Task PerformAssertionAsync()
			{
				await Task.Delay(TimeSpan.FromMilliseconds(1));
				var now = Clock.Now;
				Assert.Equal(FixedTime, now);
			}
		}

		private sealed class TestClass2 : IDisposable
		{
			private static readonly DateTime FixedTime = new DateTime(0002, 01, 01, 12, 00, 00, DateTimeKind.Local);
			private ClockScope Scope { get; } = new ClockScope(() => FixedTime);

			public void Dispose()
			{
				this.Scope.Dispose();
			}

			public async Task PerformAssertionAsync()
			{
				await Task.Delay(TimeSpan.FromMilliseconds(1));
				var now = Clock.Now;
				Assert.Equal(FixedTime, now);
			}
		}
	}
}
