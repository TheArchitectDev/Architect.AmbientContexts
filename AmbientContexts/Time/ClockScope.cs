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
	/// A default scope that uses <see cref="DateTime.Now"/> is registered by default. A different default can be registered on startup through <see cref="ClockScopeExtensions.UseClockScope(IServiceProvider, Func{DateTime})"/>.
	/// </para>
	/// <para>
	/// Outer code may construct a custom <see cref="ClockScope"/> inside a using statement, causing any code within the using block to see that instance.
	/// </para>
	/// </summary>
	public sealed class ClockScope : AmbientScope<ClockScope>
	{
		static ClockScope()
		{
			SetDefaultValue(() => DateTime.Now);
		}

		/// <summary>
		/// Returns the currently accessible <see cref="ClockScope"/>.
		/// The scope is configurable from the outside, such as from startup.
		/// </summary>
		public static ClockScope Current => GetAmbientScope() ?? throw new InvalidOperationException(
			$"{nameof(ClockScope)} was not configured. Call {nameof(ClockScopeExtensions)}.{nameof(ClockScopeExtensions.UseClockScope)} on startup.");

		public DateTime Now => this.NowSource();
		public DateTime Today => this.Now.Date;
		public DateTime UtcNow => this.Now.ToUniversalTime();

		private Func<DateTime> NowSource { get; }

		/// <summary>
		/// Establishes the given clock as the ambient one until the scope is disposed.
		/// </summary>
		public ClockScope(Func<DateTime> nowSource)
			: this(nowSource, AmbientScopeOption.ForceCreateNew)
		{
			this.Activate();
		}

		/// <summary>
		/// Private constructor.
		/// Does not activate this instance.
		/// </summary>
		private ClockScope(Func<DateTime> nowSource, AmbientScopeOption ambientScopeOption)
			: base(ambientScopeOption)
		{
			this.NowSource = nowSource ?? throw new ArgumentNullException(nameof(nowSource));
		}

		protected override void DisposeImplementation()
		{
			// Nothing to dispose
		}

		/// <summary>
		/// Sets the ubiquitous default scope, overwriting and disposing the previous one if necessary.
		/// </summary>
		/// <param name="nowSource">The clock to register. May be null to unregister.</param>
		internal static void SetDefaultValue(Func<DateTime> nowSource)
		{
			var newDefaultScope = nowSource is null
				? null
				: new ClockScope(nowSource, AmbientScopeOption.NoNesting);
			SetDefaultScope(newDefaultScope);
		}
	}
}
