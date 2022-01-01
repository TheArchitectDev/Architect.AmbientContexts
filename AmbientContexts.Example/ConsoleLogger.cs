using System;

namespace Architect.AmbientContexts.Example
{
	internal sealed class ConsoleLogger : ILogger
	{
		public void Dispose()
		{
			// Nothing to dispose
		}

		public void WriteEntry(string message, string submessage = null)
		{
			Console.WriteLine(message);
			if (submessage is not null)
			{
				Console.Write('\t');
				Console.WriteLine(submessage);
			}
		}
	}
}
