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
}
