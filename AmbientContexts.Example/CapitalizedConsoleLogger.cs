using System;

namespace Architect.AmbientContexts.Example
{
	internal sealed class CapitalizedConsoleLogger : ILogger
	{
		public void Dispose()
		{
			// Nothing to dispose
		}

		public void WriteEntry(string message, string submessage = null)
		{
			Console.WriteLine(message?.ToUpperInvariant());
			if (submessage is not null)
			{
				Console.Write('\t');
				Console.WriteLine(submessage.ToUpperInvariant());
			}
		}
	}
}
