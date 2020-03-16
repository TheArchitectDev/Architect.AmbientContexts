namespace Architect.AmbientContexts
{
	/// <summary>
	/// Defines how an ambient scope relates to a potential outer scope.
	/// </summary>
	public enum AmbientScopeOption : byte
	{
		/// <summary>
		/// Join the current ambient scope if one exists. That scope becomes the new scope's parent.
		/// </summary>
		JoinExisting,
		/// <summary>
		/// Obscure any potential current ambient scope for as long as the new scope is in effect.
		/// </summary>
		ForceCreateNew,
		/// <summary>
		/// Assume that no current ambient scope exists. Throw otherwise.
		/// </summary>
		NoNesting,
	}
}
