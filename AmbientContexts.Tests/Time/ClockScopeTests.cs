using System;
using System.Linq;
using Xunit;

namespace Architect.AmbientContexts.Tests.Time
{
	public sealed class ClockScopeTests
	{
		[Theory]
		[InlineData(DateTimeKind.Utc)]
		[InlineData(DateTimeKind.Local)]
		[InlineData(DateTimeKind.Unspecified)]
		public void Construct_WithPinnedDateTime_ShouldMatchEquivalentFunc(DateTimeKind dateTimeKind)
		{
			using var comparisonInstance = new ClockScope(() => DateTime.SpecifyKind(DateTime.UnixEpoch, dateTimeKind));

			var expectedUtcNow = Clock.UtcNow;
			var expectedNow = Clock.Now;

			using var instance = new ClockScope(DateTime.SpecifyKind(DateTime.UnixEpoch, dateTimeKind));

			var utcNow = Clock.UtcNow;
			var now = Clock.Now;

			Assert.Equal(expectedUtcNow, utcNow);
			Assert.Equal(expectedNow, now);
		}

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

		/// <summary>
		/// When DST causes an hour on the clock to be repeated, UtcNow and Now should reflect this appropriately if UTC input is used.
		/// </summary>
		[Theory]
		[InlineData(DateTimeKind.Utc)]
		[InlineData(DateTimeKind.Unspecified)]
		public void UtcNow_WithCustomScopeAtDstRepeatedHourWithUtcOrUnspecifiedInput_ShouldReturnExpectedResult(DateTimeKind dateTimeKind)
		{
			var expectedResult = new DateTime(2022, 10, 30, 01, 30, 00, DateTimeKind.Local).ToUniversalTime().AddHours(2);
			expectedResult = DateTime.SpecifyKind(expectedResult, dateTimeKind);

			using var scope = new ClockScope(() => expectedResult);

			var utcNow = ClockScope.Current.UtcNow;
			var now = ClockScope.Current.Now;

			// This test was written with with CE(S)T in mind, so it can only be performed on a system whose clock is using CET
			if (!TimeZoneInfo.Local.IsAmbiguousTime(now) || !TimeZoneInfo.Local.GetAmbiguousTimeOffsets(now).SequenceEqual(new[] { TimeSpan.FromHours(1), TimeSpan.FromHours(2), }))
				return;

			Assert.Equal(expectedResult, utcNow);
			Assert.Equal("2022-10-30T02:30:00.0000000+01:00", now.ToString("O"));
		}

		/// <summary>
		/// When DST causes an hour on the clock to be repeated, UtcNow and Now cannot reflect this appropriately if local input is used.
		/// </summary>
		[Fact]
		public void UtcNow_WithCustomScopeAtDstRepeatedHourWithLocalInput_IsExpectedToReturnLossyResult()
		{
			// Local datetime does not realize that the hours at this date go from 01:30 to 02:30 to 02:30 again (DST repeated hour)
			// It cannot distinguish between the two duplicate hours, which renders it lossy
			using var scope = new ClockScope(() => new DateTime(2022, 10, 30, 01, 30, 00, DateTimeKind.Local).AddHours(2));

			var utcNow = ClockScope.Current.UtcNow;
			var now = ClockScope.Current.Now;

			// This test was written with with CE(S)T in mind, so it can only be performed on a system whose clock is using CET
			if (!TimeZoneInfo.Local.IsAmbiguousTime(now.AddHours(-1)) || !TimeZoneInfo.Local.GetAmbiguousTimeOffsets(now.AddHours(-1)).SequenceEqual(new[] { TimeSpan.FromHours(1), TimeSpan.FromHours(2), }))
				return;

			Assert.Equal(new DateTime(2022, 10, 30, 01, 30, 00, DateTimeKind.Local).ToUniversalTime().AddHours(3), utcNow); // Alas, an hour too far
			Assert.Equal("2022-10-30T03:30:00.0000000+01:00", now.ToString("O")); // Alas, an hour too far
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
