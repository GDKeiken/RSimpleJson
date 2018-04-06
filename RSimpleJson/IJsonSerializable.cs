using System.Collections.Generic;

namespace RSimpleJson
{
	public interface IJsonSerializable
	{
		object AppendTo(IDictionary<string, object> dictionary);
	}
}
