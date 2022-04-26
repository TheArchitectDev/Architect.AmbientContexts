using System;
using System.Security.Cryptography;

namespace Architect.AmbientContexts.Example
{
	/// <summary>
	/// Provides ID generation functionality through the Ambient Context IoC pattern.
	/// Calling code can generate IDs from anywhere.
	/// The default scope generates random IDs.
	/// </summary>
	public sealed class IdGeneratorScope : AmbientScope<IdGeneratorScope>
	{
		static IdGeneratorScope()
		{
			var defaultScope = new IdGeneratorScope(generatorFunction: GenerateRandomId, isDefaultScope: true);
			SetDefaultScope(defaultScope);
		}

		/// <summary>
		/// Returns the currently available scope.
		/// If no explicit scope was declared, the default scope is returned.
		/// </summary>
		public static IdGeneratorScope Current => GetAmbientScope();

		private Func<long> GeneratorFunction { get; }

		public IdGeneratorScope(Func<long> generatorFunction)
			: this(generatorFunction, isDefaultScope: false)
		{
		}

		private IdGeneratorScope(Func<long> generatorFunction, bool isDefaultScope)
			: base(AmbientScopeOption.ForceCreateNew)
		{
			this.GeneratorFunction = generatorFunction ?? throw new ArgumentNullException(nameof(generatorFunction));

			// If we are not the default scope, activate us, making us available to the currente flow of execution
			// Note that the default scope is ubiquitous and thus is never activated
			if (!isDefaultScope) this.Activate();

			// The constructor should complete directly after Activate, to avoid throwing exceptions after activation
		}

		protected override void DisposeImplementation()
		{
			// Dispose our own disposable resources, if any
		}

		public long GenerateId()
		{
			var result = this.GeneratorFunction();
			return result;
		}

		private static long GenerateRandomId()
		{
			Span<byte> bytes = stackalloc byte[8];
			RandomNumberGenerator.Fill(bytes);
			var result = BitConverter.ToUInt64(bytes);
			result &= (UInt64.MaxValue >> 1); // Unset the most significant bit, to ensure a positive Int64 value
			return (long)result;
		}
	}
}
