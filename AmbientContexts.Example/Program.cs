using System;
using Architect.AmbientContexts.Defaults;
using Microsoft.Extensions.Hosting;

namespace Architect.AmbientContexts.Example
{
	internal static class Program
	{
		/// <summary>
		/// Demonstrates how the included <see cref="ClockScope"/> and a custom example scope can be registered and used.
		/// </summary>
		private static void Main()
		{
			DemonstrateClockScope();
			DemonstrateLogScope();

			Console.ReadKey(intercept: true);
		}

		/// <summary>
		/// Demonstrates use of the included <see cref="ClockScope"/> class.
		/// </summary>
		private static void DemonstrateClockScope()
		{
			Console.WriteLine("Demonstrating ClockScope:");
			Console.WriteLine();

			{
				var now = Clock.Now;
				Console.WriteLine($"The clock tells {now:yyyy-MM-dd HH:mm:ss}. We determined this without any injected dependencies.");
				Console.WriteLine();
			}

			using (new ClockScope(() => new DateTime(2000, 01, 01)))
			{
				var now = Clock.Now;
				Console.WriteLine($"The clock tells {now:yyyy-MM-dd HH:mm:ss}. Even without dependency injection, we were in control.");
				Console.WriteLine();
			}
		}

		/// <summary>
		/// Demonstrates use of the custom <see cref="LogScope"/> class implemented just for this example.
		/// This example, including the <see cref="LogScope"/> class, is intended to demonstrate how you can create and use your own type that uses the Ambient Context pattern.
		/// </summary>
		private static void DemonstrateLogScope()
		{
			Console.WriteLine("Demonstrating LogScope:");
			Console.WriteLine();

			try
			{
				// For demonstration purposes: This will throw a NullReferenceException, which we catch
				LogScope.Current.WriteEntry("ERROR: How did we log when no LogScope was registered?");
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("Without registration, LogScope.Current is null.");
				Console.WriteLine();
			}

			// Setup normally happens in Startup.ConfigureServices()
			var hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(services =>
				services.AddDefaultScope(new LogScope(AmbientScopeOption.NoNesting, new[] { new ConsoleLogger() }, isDefaultScope: true)));
			using var host = hostBuilder.Build();
			host.Start();

			{
				LogScope.Current.WriteEntry("This should write to the default log scope, which writes to the console.");
				Console.WriteLine();
			}

			using (new LogScope(AmbientScopeOption.ForceCreateNew, new[] { new CapitalizedConsoleLogger() }))
			{
				LogScope.Current.WriteEntry("This should write ONLY to the capitalized console logger, with the default logger obscured away.");
				Console.WriteLine();
			}

			using (new LogScope(AmbientScopeOption.ForceCreateNew, new[] { new CapitalizedConsoleLogger(), new CapitalizedConsoleLogger() }))
			{
				LogScope.Current.WriteEntry("This should write to TWO capitalized console loggers, with the default logger obscured away.");
				Console.WriteLine();
			}

			using (new LogScope(AmbientScopeOption.JoinExisting, new[] { new CapitalizedConsoleLogger() }))
			{
				LogScope.Current.WriteEntry("This should write to both the default and the capitalized console loggers.");
				Console.WriteLine();
			}

			using (new LogScope(AmbientScopeOption.ForceCreateNew, Array.Empty<ILogger>()))
			{
				LogScope.Current.WriteEntry("This should write to NO loggers at all, with the default logger obscured away.");
				Console.WriteLine();
			}

			{
				LogScope.Current.WriteEntry("This should write to the default log scope again.");
				Console.WriteLine();
			}
		}
	}
}
