using System;
using Xunit;

namespace Architect.AmbientContexts.Tests.Time
{
	public sealed class ClockScopeTests
	{
		[Fact]
		public void Current_WithNoRegistration_ShouldReturnDefaultClockScope()
		{
			var result = ClockScope.Current;

			var expectedNow = DateTime.Now;
			var now = result.Now;

			var timeDifference = now - expectedNow;

			Assert.True(timeDifference >= TimeSpan.Zero && timeDifference < TimeSpan.FromSeconds(10));
		}

		[Fact]
		public void Now_WithNoRegistration_ShouldReturnExpectedResult()
		{
			var clockScope = ClockScope.Current;

			var expectedResult = DateTime.Now;
			var result = clockScope.Now;

			var timeDifference = result - expectedResult;

			Assert.True(timeDifference >= TimeSpan.Zero && timeDifference < TimeSpan.FromSeconds(10));
		}

		[Fact]
		public void UtcNow_WithNoRegistration_ShouldReturnExpectedResult()
		{
			var clockScope = ClockScope.Current;

			var expectedResult = DateTime.UtcNow;
			var result = clockScope.UtcNow;

			var timeDifference = result - expectedResult;

			Assert.True(timeDifference >= TimeSpan.Zero && timeDifference < TimeSpan.FromSeconds(10));
		}

		[Fact]
		public void Today_WithNoRegistration_ShouldReturnExpectedResult()
		{
			var clockScope = ClockScope.Current;

			var expectedResult = DateTime.Today;
			var result = clockScope.Today;

			Assert.Equal(expectedResult, result);
		}

#if NETCOREAPP3_1
		[Fact]
		public void Current_WithCustomScope_ShouldReturnExpectedResult()
		{
			using var scope = new ClockScope(() => DateTime.UnixEpoch);

			var result = ClockScope.Current;

			Assert.Equal(scope, result);
		}

		[Fact]
		public void Current_WithNestedCustomScopes_ShouldReturnExpectedResult()
		{
			using var outerScope = new ClockScope(() => DateTime.UnixEpoch);
			using var innerScope = new ClockScope(() => DateTime.UnixEpoch);

			var result = ClockScope.Current;

			Assert.Equal(innerScope, result);
		}
#endif

		[Fact]
		public void Now_WithCustomScope_ShouldReturnExpectedResult()
		{
			var expectedResult = new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local);

			using var scope = new ClockScope(() => expectedResult);

			var result = ClockScope.Current.Now;

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void Today_WithCustomScope_ShouldReturnExpectedResult()
		{
			using var scope = new ClockScope(() => new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local));

			var result = ClockScope.Current.Today;

			Assert.Equal(new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Local), result);
		}

		[Fact]
		public void UtcNow_WithCustomScope_ShouldReturnExpectedResult()
		{
			var expectedResult = new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local).ToUniversalTime();

			using var scope = new ClockScope(() => new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local));

			var result = ClockScope.Current.UtcNow;

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void UtcNow_WithCustomScope_ShouldMatchNow()
		{
			var localDateTime = new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local);
			var utcOffset = TimeZoneInfo.Local.GetUtcOffset(localDateTime);
			var expectedResult = localDateTime.ToUniversalTime();

			using var scope = new ClockScope(() => localDateTime);

			var now = ClockScope.Current.Now;
			var utcNow = ClockScope.Current.UtcNow;

			var offset = utcNow.Add(utcOffset) - now;

			Assert.Equal(TimeSpan.Zero, offset);
		}

		[Fact]
		public void Today_WithCustomScope_ShouldMatchNow()
		{
			var expectedResult = new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local).ToUniversalTime();

			using var scope = new ClockScope(() => new DateTime(2000, 01, 01, 12, 00, 00, DateTimeKind.Local));

			var now = ClockScope.Current.Now;
			var today = ClockScope.Current.Today;

			Assert.Equal(now.Date, today);
		}
	}
}
