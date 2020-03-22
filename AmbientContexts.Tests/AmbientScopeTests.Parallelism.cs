using System;
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
	}
}
