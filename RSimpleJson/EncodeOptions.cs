using System;

namespace RSimpleJson
{
	[Flags]
	public enum EncodeOptions
	{
		PrettyPrint,
		AppendType,
		All = PrettyPrint | AppendType,
		None
	}
}
