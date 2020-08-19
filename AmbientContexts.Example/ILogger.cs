using System;

namespace Architect.AmbientContexts.Example
{
	public interface ILogger : IDisposable
	{
		void WriteEntry(string message, string submessage = null);
	}
}
