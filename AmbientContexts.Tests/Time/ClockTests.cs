using System;
using Xunit;

namespace Architect.AmbientContexts.Tests.Time
{
	public sealed class ClockTests
	{
		[Fact]
		public void Now_WithoutCustomScope_ShouldMatchResultOfClockScopeNow()
		{
			var expectedResult = ClockScope.Current.Now;
			var result = Clock.Now;

			var offset = result - expectedResult;

			Assert.True(offset >= TimeSpan.Zero && offset <= TimeSpan.FromSeconds(10));
		}

		[Fact]
		public void UtcNow_WithoutCustomScope_ShouldMatchResultOfClockScopeUtcNow()
		{
			var expectedResult = ClockScope.Current.UtcNow;
			var result = Clock.UtcNow;

			var offset = result - expectedResult;

			Assert.True(offset >= TimeSpan.Zero && offset <= TimeSpan.FromSeconds(10));
		}

		[Fact]
		public void Today_WithoutCustomScope_ShouldMatchResultOfClockScopeToday()
		{
			var expectedResult = ClockScope.Current.Today;
			var result = Clock.Today;

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void Now_WithCustomScope_ShouldMatchResultOfClockScopeNow()
		{
			using (new ClockScope(() => new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local)))
			{
				var expectedResult = ClockScope.Current.Now;
				var result = Clock.Now;

				Assert.Equal(expectedResult, result);
			}
		}

		[Fact]
		public void UtcNow_WithCustomScope_ShouldMatchResultOfClockScopeUtcNow()
		{
			using (new ClockScope(() => new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local)))
			{
				var expectedResult = ClockScope.Current.UtcNow;
				var result = Clock.UtcNow;

				Assert.Equal(expectedResult, result);
			}
		}

		[Fact]
		public void Today_WithCustomScope_ShouldMatchResultOfClockScopeUtcToday()
		{
			using (new ClockScope(() => new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local)))
			{
				var expectedResult = ClockScope.Current.Today;
				var result = Clock.Today;

				Assert.Equal(expectedResult, result);
			}
		}

		[Fact]
		public void Now_WithNestedCustomScopes_ShouldMatchResultOfClockScopeNow()
		{
			using (new ClockScope(() => new DateTime(2000, 01, 01)))
			using (new ClockScope(() => DateTime.UnixEpoch))
			{
				var expectedResult = ClockScope.Current.Now;
				var result = Clock.Now;

				Assert.Equal(expectedResult, result);
			}
		}
	}
}
