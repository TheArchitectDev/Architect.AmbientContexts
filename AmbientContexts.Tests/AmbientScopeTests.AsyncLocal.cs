using System;
using System.Reflection;
using Xunit;

namespace Architect.AmbientContexts.Tests
{
	public sealed partial class AmbientScopeTests
	{
		[Fact]
		public void WasCurrentScopeEverAssigned_ForDifferentScopes_ShouldReturnExpectedResult()
		{
			_ = new CustomizedScope();
			_ = new NoncustomizedScope();

			var customizedScopeWasCurrentScopeTouchedPropertyGetter = typeof(AmbientScope<CustomizedScope>).GetProperty("WasCurrentScopeEverAssigned", BindingFlags.Static | BindingFlags.NonPublic)?.GetMethod ??
				throw new Exception("Could not find the WasCurrentScopeEverAssigned property getter.");

			var result = (bool)customizedScopeWasCurrentScopeTouchedPropertyGetter.Invoke(obj: null, parameters: null);

			Assert.True(result);

			var noncustomizedScopeWasCurrentScopeTouchedPropertyGetter = typeof(AmbientScope<NoncustomizedScope>).GetProperty("WasCurrentScopeEverAssigned", BindingFlags.Static | BindingFlags.NonPublic)?.GetMethod ??
				throw new Exception("Could not find the WasCurrentScopeEverAssigned property getter.");

			result = (bool)noncustomizedScopeWasCurrentScopeTouchedPropertyGetter.Invoke(obj: null, parameters: null);

			Assert.False(result);
		}

		private sealed class CustomizedScope : AmbientScope<CustomizedScope>
		{
			public CustomizedScope()
				: base(AmbientScopeOption.NoNesting)
			{
				SetAmbientScope(this);
			}

			protected override void DisposeImplementation()
			{
			}
		}

		private sealed class NoncustomizedScope : AmbientScope<NoncustomizedScope>
		{
			public NoncustomizedScope()
				: base(AmbientScopeOption.NoNesting)
			{
			}

			protected override void DisposeImplementation()
			{
			}
		}
	}
}
