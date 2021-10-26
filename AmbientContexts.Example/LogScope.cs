using System;
using System.Collections.Generic;
using System.Linq;

namespace Architect.AmbientContexts.Example
{
	/// <summary>
	/// Provides logging functionality through the Ambient Context IoC pattern.
	/// Calling code can log from anywhere.
	/// </summary>
	public sealed class LogScope : AmbientScope<LogScope>
	{
		/// <summary>
		/// Returns the currently available scope.
		/// If no explicit scope was declared, the default scope is returned.
		/// If nothing was registered, an exception is thrown.
		/// </summary>
		public static LogScope Current => GetAmbientScope();

		private List<ILogger> Loggers { get; }

		/// <summary>
		/// Constructs a new instance that defers to the given loggers.
		/// </summary>
		/// <param name="scopeOption">Allows joining any existing scope (i.e. adding additional logging mechanisms) or obscuring it (i.e. temporarily replacing the logging mechanisms).</param>
		/// <param name="loggers">The loggers to defer to.</param>
		public LogScope(AmbientScopeOption scopeOption, IEnumerable<ILogger> loggers, bool isDefaultScope = false)
			: base(scopeOption)
		{
			if (loggers is null) throw new ArgumentNullException(nameof(loggers));

			// Make a defensive copy to prevent outside mutation
			var loggerList = loggers.ToList();

			// If we are joining the existing scope (rather than obscuring it) and there is one, insert the existing loggers before our own
			if (scopeOption == AmbientScopeOption.JoinExisting && GetAmbientScope() != null)
				loggerList.InsertRange(0, GetAmbientScope().Loggers);

			this.Loggers = loggerList;

			// If we are not the default scope, activate us, making us available to the currente flow of execution
			// Note that the default scope is ubiquitous and thus is never activated
			if (!isDefaultScope) this.Activate();
		}
		
		protected override void DisposeImplementation()
		{
			// Dispose our own disposable resources
			foreach (var logger in this.Loggers)
				logger.Dispose();
		}

		/// <summary>
		/// Writes a log entry using the loggers thar are active in the current scope.
		/// </summary>
		public void WriteEntry(string message, string submessage = null)
		{
			foreach (var logger in this.Loggers)
				logger.WriteEntry(message, submessage);
		}
	}
}
