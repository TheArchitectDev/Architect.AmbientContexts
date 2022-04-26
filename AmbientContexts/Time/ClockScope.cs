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
	public sealed class ClockScope : AmbientScope<ClockScope>
	{
		static ClockScope()
		{
			SetDefaultScope(new ClockScope(utcNowSource: () => DateTime.UtcNow, AmbientScopeOption.NoNesting));
		}

		/// <summary>
		/// Returns the currently accessible <see cref="ClockScope"/>.
		/// A default is normally available, although it may be obscured by a local <see cref="ClockScope"/> instance.
		/// </summary>
		internal static ClockScope Current => GetAmbientScope()!;

		internal DateTime Now => this.UtcNowSource().ToLocalTime();
		internal DateTime Today => this.Now.Date;
		internal DateTime UtcNow => this.UtcNowSource();

		private Func<DateTime> UtcNowSource { get; }

		/// <summary>
		/// Establishes the given clock as the ambient one until the scope is disposed.
		/// </summary>
		/// <param name="utcNowSource">The clock to register, which produces the equivalent of <see cref="DateTime.UtcNow"/>, i.e. the UTC time.
		/// If it produces local times instead, they are interpreted correctly, but conversions may be lossy.
		/// Times of an unknown kind are assumed to be in UTC.</param>
		public ClockScope(Func<DateTime> utcNowSource)
			: this(utcNowSource, AmbientScopeOption.ForceCreateNew)
		{
			this.Activate();
		}

		/// <summary>
		/// Private constructor.
		/// Does not activate this instance.
		/// </summary>
		private ClockScope(Func<DateTime> utcNowSource, AmbientScopeOption ambientScopeOption)
			: base(ambientScopeOption)
		{
			this.UtcNowSource = utcNowSource ?? ThrowNullSource();

			// Workaround for compatibility with sources of local time
			if (this.UtcNowSource().Kind == DateTimeKind.Local)
				this.UtcNowSource = () => utcNowSource!().ToUniversalTime();

			static Func<DateTime> ThrowNullSource() => throw new ArgumentNullException(nameof(utcNowSource));
		}

		protected override void DisposeImplementation()
		{
			// Nothing to dispose
		}
	}
}
