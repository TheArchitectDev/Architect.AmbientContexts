using System;
using System.Reflection;
using Xunit;

namespace Architect.AmbientContexts.Tests
{
	public sealed partial class AmbientScopeTests
	{
		[Fact]
		public void CurrentAmbientScope_ForDifferentScopes_ShouldReturnExpectedResult()
		{
			using var customizedScope = new CustomizedScope();
			using var noncustomizedScope = new NoncustomizedScope();

			var customizedScopeCurrentAmbientScopeStaticField = typeof(AmbientScope<CustomizedScope>).GetField("CurrentAmbientScope", BindingFlags.Static | BindingFlags.NonPublic) ??
				throw new Exception("Could not find the CurrentAmbientScope field.");

			var result = customizedScopeCurrentAmbientScopeStaticField.GetValue(obj: null);
			Assert.Null(result);

			customizedScope.Activate();

			result = customizedScopeCurrentAmbientScopeStaticField.GetValue(obj: null);
			Assert.NotNull(result); // Initialized now that a non-default scope has been activated

			customizedScope.Dispose();

			result = customizedScopeCurrentAmbientScopeStaticField.GetValue(obj: null);
			Assert.NotNull(result); // Remains intialized

			var noncustomizedScopeCurrentAmbientScopeStaticField = typeof(AmbientScope<NoncustomizedScope>).GetField("CurrentAmbientScope", BindingFlags.Static | BindingFlags.NonPublic) ??
				throw new Exception("Could not find the CurrentAmbientScope field.");

			result = noncustomizedScopeCurrentAmbientScopeStaticField.GetValue(obj: null);
			Assert.Null(result);
		}

		private sealed class CustomizedScope : AmbientScope<CustomizedScope>
		{
			public CustomizedScope()
				: base(AmbientScopeOption.NoNesting)
			{
			}

			protected override void DisposeImplementation()
			{
			}

			public new void Activate()
			{
				base.Activate();
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
