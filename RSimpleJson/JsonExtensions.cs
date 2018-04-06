namespace RSimpleJson
{
	public static class JsonExtensions
	{
		public static string ToJson(this object source, EncodeOptions flags = EncodeOptions.All)
		{
			if (JSON.CurrentJsonSerializer == null)
			{
				return string.Empty;
			}

			JSON.CurrentJsonSerializer.SetOptions(flags);
			string json = JSON.Serialize(source);
			JSON.CurrentJsonSerializer.SetOptions(EncodeOptions.None);

			return json;
		}
	}
}
