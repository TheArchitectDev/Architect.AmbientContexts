using System;

// ReSharper disable once CheckNamespace
namespace Architect.AmbientContexts
{
	/// <summary>
	/// <para>
	/// Provides access to a clock through the Ambient Context pattern.
	/// </para>
	/// <para>
	/// This type provides a lightweight Inversion of Control (IoC) mechanism.
	/// The mechanism optimizes accessiblity (through a static property) at the cost of transparency, making it suitable for obvious, ubiquitous, rarely-changing dependencies.
	/// </para>
	/// <para>
	/// A default scope that uses <see cref="DateTime.UtcNow"/> is registered by default.
	/// </para>
	/// <para>
	/// Outer code may construct a custom <see cref="ClockScope"/> inside a using statement, causing any code within the using block to see that instance.
	/// </para>
	/// </summary>
	public static class Clock
	{
		/// <summary>
		/// Returns a <see cref="DateTime"/> that is set to the current date and time according to the current clock, expressed as the local time.
		/// </summary>
		public static DateTime Now => ClockScope.Current.Now;

		/// <summary>
		/// Returns a <see cref="DateTime"/> that is set to the current date according to the current clock, expressed as the local time.
		/// </summary>
		public static DateTime Today => ClockScope.Current.Today;

		/// <summary>
		/// Returns a <see cref="DateTime"/> that is set to the current date and time according to the current clock, expressed as the Coordinated Universal Time (UTC).
		/// </summary>
		public static DateTime UtcNow => ClockScope.Current.UtcNow;
	}
}
