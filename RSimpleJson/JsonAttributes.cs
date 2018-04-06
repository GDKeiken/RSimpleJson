using System;

namespace RSimpleJson
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public sealed class JsonIncludeAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
	/// <summary>
	/// Indicates that the class is json serializable.
	/// </summary>
	public sealed class JsonSerializableAttribute : Attribute
	{
		/// <summary>
		/// Indicates that the class is json serializable.
		/// </summary>
		public JsonSerializableAttribute()
		{
		}
	}
}