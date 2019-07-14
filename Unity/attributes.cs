using System;

namespace mtti.Inject
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class GetComponentAttribute : Attribute
	{
		public GetComponentAttribute() { }
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class GetComponentInChildrenAttribute : Attribute
	{
		public GetComponentInChildrenAttribute() { }
	}

    /// <summary>
    /// Get a component if it exists in the game object and add it if it doesn't.
    /// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class EnsureComponentAttribute : Attribute
	{
		public EnsureComponentAttribute() { }
	}
}
