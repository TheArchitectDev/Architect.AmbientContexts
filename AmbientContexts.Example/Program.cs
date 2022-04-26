using System;
using System.Threading;

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
			DemonstrateIdGeneratorScope();

			Console.ReadKey(intercept: true);
		}

		/// <summary>
		/// Demonstrates the use of the included <see cref="ClockScope"/> class.
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
		/// Demosntrates the use of a custom <see cref="IdGeneratorScope"/> type implemented just for this example.
		/// This example, including the <see cref="IdGeneratorScope"/> type, is intended to demonstrate how you can create and use your own type that uses the Ambient Context pattern.
		/// </summary>
		public static void DemonstrateIdGeneratorScope()
		{
			Console.WriteLine("Demonstrating IdGeneratorScope:");
			Console.WriteLine();

			Console.WriteLine("The default scope should generate random IDs:");
			Console.WriteLine(IdGeneratorScope.Current.GenerateId());
			Console.WriteLine(IdGeneratorScope.Current.GenerateId());
			Console.WriteLine();

			var previousId = 0L;
			using (new IdGeneratorScope(generatorFunction: () => Interlocked.Increment(ref previousId)))
			{
				Console.WriteLine("The default scope is now obscured behind a local scope that generates incremental IDs:");
				Console.WriteLine(IdGeneratorScope.Current.GenerateId());
				Console.WriteLine(IdGeneratorScope.Current.GenerateId());
				Console.WriteLine();

				using (new IdGeneratorScope(generatorFunction: () => -1L))
				{
					Console.WriteLine("A nested local scope now obscures the previous one, always generating a value of -1:");
					Console.WriteLine(IdGeneratorScope.Current.GenerateId());
					Console.WriteLine(IdGeneratorScope.Current.GenerateId());
					Console.WriteLine();
				}

				Console.WriteLine("With the nested scope disposed, we should reach the previous local scope, generating incremental IDs:");
				Console.WriteLine(IdGeneratorScope.Current.GenerateId());
				Console.WriteLine(IdGeneratorScope.Current.GenerateId());
				Console.WriteLine();
			}

			Console.WriteLine("With the local scope disposed, we should now reach the default scope once more:");
			Console.WriteLine(IdGeneratorScope.Current.GenerateId());
			Console.WriteLine(IdGeneratorScope.Current.GenerateId());
			Console.WriteLine();
		}
	}
}
