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
		public async Task ConstructAndDispose_WithLifetimeOfTwoParallelUnitTestClasses_ShouldNotInterfereWithEachOther()
		{
			var tasks = new ConcurrentQueue<Task>();

			var utcNow = new DateTime(1234, 01, 02, 03, 00, 00, DateTimeKind.Utc);
			using var clockScope = new ClockScope(() => utcNow);

			Parallel.For(0, 20, i =>
			{
				if (i % 2 == 0)
				{
					using var testInstance = new TestClass1();

					// Will evaluate while testInstance and its ClockScope are still available
					testInstance.AssertNowDelayedAsync(TestClass1.FixedTime).GetAwaiter().GetResult();

					// Will evaluate after testInstance and its ClockScope have been disposed
					tasks.Enqueue(testInstance.AssertNowDelayedAsync(utcNow));
				}
				else
				{
					using var testInstance = new TestClass2();

					// Will evaluate while testInstance and its ClockScope are still available
					testInstance.AssertNowDelayedAsync(TestClass2.FixedTime).GetAwaiter().GetResult();

					// Will evaluate after testInstance and its ClockScope have been disposed
					tasks.Enqueue(testInstance.AssertNowDelayedAsync(utcNow));
				}
			});

			await Task.WhenAll(tasks);
		}

		private sealed class TestClass1 : IDisposable
		{
			public static readonly DateTime FixedTime = new DateTime(0001, 01, 01, 12, 00, 00, DateTimeKind.Utc);
			private ClockScope Scope { get; } = new ClockScope(() => FixedTime);

			public void Dispose()
			{
				this.Scope.Dispose();
			}

			public async Task AssertNowDelayedAsync(DateTime expectedValue)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(1));
				var now = Clock.UtcNow;
				Assert.Equal(expectedValue, now);
			}
		}

		private sealed class TestClass2 : IDisposable
		{
			public static readonly DateTime FixedTime = new DateTime(0002, 01, 01, 12, 00, 00, DateTimeKind.Utc);
			private ClockScope Scope { get; } = new ClockScope(() => FixedTime);

			public void Dispose()
			{
				this.Scope.Dispose();
			}

			public async Task AssertNowDelayedAsync(DateTime expectedValue)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(1));
				var now = Clock.UtcNow;
				Assert.Equal(expectedValue, now);
			}
		}
	}
}
