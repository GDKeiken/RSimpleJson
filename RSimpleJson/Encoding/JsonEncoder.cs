using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RSimpleJson.Encoding
{
	internal class JsonEncoder : IDisposable
	{
		#region Variables & Properties
		private StringBuilder builder;
		private IJsonSerializer _jsonSerializer;
		#endregion

		#region Public Methods
		public static string Encode(IJsonSerializer jsonSerializer, object obj)
		{
			if (jsonSerializer != null)
			{
				using (JsonEncoder instance = new JsonEncoder(jsonSerializer))
				{
					instance.EncodeValue(obj, false);
					return instance.builder.ToString();
				}
			}

			return string.Empty;
		}

		public void Dispose()
		{
			builder.Clear();
			_jsonSerializer = null;
		}
		#endregion

		#region Private Methods
		private JsonEncoder(IJsonSerializer jsonSerializer)
		{
			_jsonSerializer = jsonSerializer;
			builder = new StringBuilder();
		}

		private static bool IsNumeric(object value)
		{
			return value is sbyte || value is byte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal;
		}

		private bool EncodeValue(object value, bool forceTypeHint)
		{
			//Array asArray;
			//IList asList;
			//IDictionary asDict;
			//string asString;
			bool flag = true;

			if (value == null)
			{
				builder.Append("null");
			}
			else if (value is string)
			{
				EncodeString((string)value);
			}
			else if (value is bool)
			{
				builder.Append((!(bool)value) ? "false" : "true");
			}
			else if (value is Enum)
			{
				flag = EncodeString(value.ToString());
			}
			else if (value is IDictionary<string, object>)
			{
				IDictionary<string, object> dictionary = (IDictionary<string, object>)value;
				flag = EncodeObject(dictionary.Keys, dictionary.Values, forceTypeHint);
			}
			else
			{
				if (value is IDictionary<string, string>)
				{
					IDictionary<string, string> dictionary = (IDictionary<string, string>)value;
					flag = EncodeObject(dictionary.Keys, dictionary.Values, forceTypeHint);
				}
				else
				{
					if (value is IDictionary)
					{
						IDictionary dictionary = (IDictionary)value;
						flag = EncodeObject(dictionary.Keys, dictionary.Values, forceTypeHint);
					}
					else
					{
						if (value is IEnumerable)
						{
							flag = EncodeArray((IEnumerable)value, forceTypeHint);
						}
						else
						{
							flag = EncodeOther(value, forceTypeHint);
                        }
                    }
                }
            }

			return flag;
		}

		private bool EncodeObject(IEnumerable keys, IEnumerable values, bool forceTypeHint)
		{
			builder.Append("{");
			IEnumerator enumerator = keys.GetEnumerator();
			IEnumerator enumerator2 = values.GetEnumerator();
			bool flag = true;
			while (enumerator.MoveNext() && enumerator2.MoveNext())
			{
				object current = enumerator.Current;
				object current2 = enumerator2.Current;

				if (!flag)
				{
					builder.Append(",");
				}

				if (current is string)
				{
					EncodeString((string)current);
				}
				else
				{
					if (!EncodeValue(current2, forceTypeHint))
					{
						return false;
					}
				}

				builder.Append(":");

				if (!EncodeValue(current2, forceTypeHint))
				{
					return false;
				}

				flag = false;
			}

			builder.Append("}");
			return true;
		}

		protected bool EncodeArray(IEnumerable anArray, bool forceTypeHint)
		{
			builder.Append("[");
			bool flag = true;
			IEnumerator enumerator = anArray.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object current = enumerator.Current;
					if (!flag)
					{
						builder.Append(",");
					}
					if (!EncodeValue(current, forceTypeHint))
					{
						return false;
					}
					flag = false;
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
			builder.Append("]");
			return true;
		}

		private bool EncodeOther(object value, bool forceTypeHint)
		{
			bool flag = true;
			if (IsNumeric(value))
			{
				builder.Append(value.ToString());
			}
			else
			{
				object result;
				flag = _jsonSerializer.SerializeNonPrimitiveObject(value, out result);
				if (flag)
				{
					flag = EncodeValue(result, forceTypeHint);
				}
			}

			return flag;
        }

		private bool EncodeString(string value)
		{
			builder.Append('\"');

			char[] charArray = value.ToCharArray();
			for (int i = 0; i <charArray.Length; i++)
			{
				char c = charArray[i];
				switch (c)
				{
					case '"':
						builder.Append("\\\"");
						break;

					case '\\':
						builder.Append("\\\\");
						break;

					case '\b':
						builder.Append("\\b");
						break;

					case '\f':
						builder.Append("\\f");
						break;

					case '\n':
						builder.Append("\\n");
						break;

					case '\r':
						builder.Append("\\r");
						break;

					case '\t':
						builder.Append("\\t");
						break;

					default:
						int codepoint = Convert.ToInt32(c);
						if ((codepoint >= 32) && (codepoint <= 126))
						{
							builder.Append(c);
						}
						else
						{
							builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
						}
						break;
				}
			}

			builder.Append('\"');
			return true;
		}
		#endregion
	}
}
