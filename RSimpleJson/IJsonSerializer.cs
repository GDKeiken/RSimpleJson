using System;

namespace RSimpleJson
{
	public interface IJsonSerializer
	{
		EncodeOptions Options { get; }

		void SetOptions(EncodeOptions flags);
		object DeserializeObject(object value, Type type);
        bool SerializeNonPrimitiveObject(object input, out object output);
    }
}
