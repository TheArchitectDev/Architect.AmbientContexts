﻿using System;
using Xunit;

namespace Architect.AmbientContexts.Tests
{
	public sealed partial class AmbientScopeTests
	{
		[Fact]
		public void SetDefaultScope_Regularly_ShouldNotMakeScopeAmbient()
		{
			using var scope = new StaticTestScope1();

			StaticTestScope1.SetDefaultScope(scope);

			Assert.NotEqual(StaticTestScope1.CurrentNondefault, scope);
		}

		private sealed class StaticTestScope1 : StaticTestScope<StaticTestScope1>
		{
		}

		[Fact]
		public void SetDefaultScope_Regularly_ShouldNotCallCustomDispose()
		{
			using var scope = new StaticTestScope2();

			StaticTestScope2.SetDefaultScope(scope);

			Assert.False(scope.IsCustomDisposed);
		}

		private sealed class StaticTestScope2 : StaticTestScope<StaticTestScope2>
		{
		}

		[Fact]
		public void SetDefaultScope_ReplacingPreviousScopeByNew_ShouldCallCustomDisposeOnPrevious()
		{
			using var previousScope = new StaticTestScope3();
			StaticTestScope3.SetDefaultScope(previousScope);

			using var newScope = new StaticTestScope3();
			StaticTestScope3.SetDefaultScope(newScope);

			Assert.True(previousScope.IsCustomDisposed);
		}

		private sealed class StaticTestScope3 : StaticTestScope<StaticTestScope3>
		{
		}

		[Fact]
		public void SetDefaultScope_ReplacingPreviousScopeByNull_ShouldCallCustomDisposeOnPrevious()
		{
			using var previousScope = new StaticTestScope4();
			StaticTestScope4.SetDefaultScope(previousScope);

			StaticTestScope4.SetDefaultScope(null);

			Assert.True(previousScope.IsCustomDisposed);
		}

		private sealed class StaticTestScope4 : StaticTestScope<StaticTestScope4>
		{
		}

		[Fact]
		public void SetDefaultScope_WithJoinExistingOption_ShouldThrow()
		{
			using var scope = new StaticTestScope5();

			Assert.ThrowsAny<ArgumentException>(() =>
				StaticTestScope5.SetDefaultScope(scope));
		}

		private sealed class StaticTestScope5 : StaticTestScope<StaticTestScope5>
		{
			public StaticTestScope5()
				: base(AmbientScopeOption.JoinExisting)
			{
			}
		}

		[Fact]
		public void SetDefaultScope_WithException_ShouldLeaveExistingDefaultScope()
		{
			using var previousScope = new StaticTestScope6(throwsWhenMadeDefault: false);
			StaticTestScope6.SetDefaultScope(previousScope);

			using var newScope = new StaticTestScope6(throwsWhenMadeDefault: true);
			Assert.ThrowsAny<ArgumentException>(() =>
				StaticTestScope6.SetDefaultScope(newScope));

			Assert.Equal(previousScope, StaticTestScope6.Current);
		}

		private sealed class StaticTestScope6 : StaticTestScope<StaticTestScope6>
		{
			public StaticTestScope6(bool throwsWhenMadeDefault)
				: base(throwsWhenMadeDefault ? AmbientScopeOption.JoinExisting : AmbientScopeOption.NoNesting)
			{
			}
		}

		[Fact]
		public void Dispose_WhileTheDefaultScope_ShouldUnsetDefaultScope()
		{
			using (var scope = new StaticTestScope7())
			{
				StaticTestScope7.SetDefaultScope(scope);
			}

			Assert.Null(StaticTestScope7.Current);
		}

		private sealed class StaticTestScope7 : StaticTestScope<StaticTestScope7>
		{
		}

		[Fact]
		public void Dispose_WhileNoLongerTheDefaultScope_ShouldNotTouchDefaultScope()
		{
			using var newDefaultScope = new StaticTestScope8();

			using (var previousDefaultScope = new StaticTestScope8())
			{
				StaticTestScope8.SetDefaultScope(previousDefaultScope);
				StaticTestScope8.SetDefaultScope(newDefaultScope);
			}

			Assert.Equal(newDefaultScope, StaticTestScope8.Current);
		}

		private sealed class StaticTestScope8 : StaticTestScope<StaticTestScope8>
		{
		}
	}
}
