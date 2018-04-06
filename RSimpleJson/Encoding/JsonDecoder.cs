using System;
using System.IO;
using System.Text;
using System.Globalization;

using RSimpleJson.Objects;

namespace RSimpleJson.Encoding
{
	internal sealed class JsonDecoder : IDisposable
	{
		#region Enums
		public enum Token
		{
			None,
			OpenBrace,
			CloseBrace,
			OpenBracket,
			CloseBracket,
			Colon,
			Comma,
			String,
			Number,
			True,
			False,
			Null
		}
		#endregion

		#region Variables & Properties
		//private static readonly char[] floatingPointCharacters = new char[] { '.', 'e' };

		private const string WhiteSpace = " \t\n\r";
		private const string WordBreak = " \t\n\r{}[],:\"";

		private StringReader json;

		private char PeekChar
		{
			get
			{
				var peek = json.Peek();
				return peek == -1 ? '\0' : Convert.ToChar(peek);
			}
		}

		private char NextChar { get { return Convert.ToChar(json.Read()); } }

		private string NextWord
		{
			get
			{
				StringBuilder word = new StringBuilder();

				while (WordBreak.IndexOf(PeekChar) == -1)
				{
					word.Append(NextChar);

					if (json.Peek() == -1)
					{
						break;
					}
				}

				return word.ToString();
			}
		}

		private Token NextToken
		{
			get
			{
				ConsumeWhiteSpace();

				if (json.Peek() == -1)
				{
					return Token.None;
				}

				switch (PeekChar)
				{
					case '{':
						return Token.OpenBrace;

					case '}':
						json.Read();
						return Token.CloseBrace;

					case '[':
						return Token.OpenBracket;

					case ']':
						json.Read();
						return Token.CloseBracket;

					case ',':
						json.Read();
						return Token.Comma;

					case '"':
						return Token.String;

					case ':':
						return Token.Colon;

					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case '-':
						return Token.Number;
				}

				switch (NextWord)
				{
					case "false":
						return Token.False;

					case "true":
						return Token.True;

					case "null":
						return Token.Null;
				}

				return Token.None;
			}
		}
		#endregion

		#region Public Methods
		public static object Decode(string jsonString)
		{
			using (JsonDecoder instance = new JsonDecoder(jsonString))
			{
				return instance.DecodeValue();
			}
		}

		public void Dispose()
		{
			json.Dispose();
			json = null;
		}
		#endregion

		#region Private Methods
		private JsonDecoder(string jsonString)
		{
			json = new StringReader(jsonString);
		}

		#region Static Methods
		private static object ParseNumber(IConvertible value)
		{
			object result;
			bool success = true;

			if (value is string)
			{
				string str = (string)value;
                if (str.IndexOf(".", StringComparison.OrdinalIgnoreCase) != -1 || str.IndexOf("e", StringComparison.OrdinalIgnoreCase) != -1)
				{
					double num;
					success = double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
					result = num;
				}
				else
				{
					long num2;
					success = long.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out num2);
					result = num2;
				}
			}
			else
			{
				result = value;
			}

			if (!success)
			{
				JSON.Logger.LogError(value, "Failed to parse string to number, returning 0");
				result = 0;
			}

			return result;
		}
		#endregion
		
		private object DecodeObject()
		{
			JsonObject jsonObject = new JsonObject();
			
			json.Read(); // skip openning brace
			while (true)
			{
				switch (NextToken)
				{
					case Token.None:
						return null;

					case Token.Comma:
						continue;

					case Token.CloseBrace:
						return jsonObject;

					default:
						// Key
						string key = DecodeString();
						if (key == null)
						{
							return null;
						}

						// :
						if (NextToken != Token.Colon)
						{
							return null;
						}
						json.Read();

						// Value
						object value = DecodeValue();
                        jsonObject.Add(key, value);
						break;
				}
			}
		}

		private object DecodeArray()
		{
			JsonArray jsonArray = new JsonArray();

			// Ditch opening bracket.
			json.Read();

			// [
			var parsing = true;
			while (parsing)
			{
				Token nextToken = NextToken;

				switch (nextToken)
				{
					case Token.None:
						return null;

					case Token.Comma:
						continue;

					case Token.CloseBracket:
						parsing = false;
						break;

					default:
						jsonArray.Add(DecodeByToken(nextToken));
						break;
				}
			}

			return jsonArray;
		}

		private object DecodeValue()
		{
			Token nextToken = NextToken;
			return DecodeByToken(nextToken);
		}

		private object DecodeByToken(Token token)
		{
			switch (token)
			{
				case Token.String:
					return DecodeString();

				case Token.Number:
					return DecodeNumber();

				case Token.OpenBrace:
					return DecodeObject();

				case Token.OpenBracket:
					return DecodeArray();

				case Token.True:
					return true;

				case Token.False:
					return false;

				case Token.Null:
					return null;

				default:
					return null;
			}
		}

		private string DecodeString()
		{
			var stringBuilder = new StringBuilder();
			char c;

			// ditch opening quote
			json.Read();

			bool parsing = true;
			while (parsing)
			{
				if (json.Peek() == -1)
				{
					parsing = false;
					break;
				}

				c = NextChar;
				switch (c)
				{
					case '"':
						parsing = false;
						break;

					case '\\':
						if (json.Peek() == -1)
						{
							parsing = false;
							break;
						}

						c = NextChar;
						switch (c)
						{
							case '"':
							case '\\':
							case '/':
								stringBuilder.Append(c);
								break;

							case 'b':
								stringBuilder.Append('\b');
								break;

							case 'f':
								stringBuilder.Append('\f');
								break;

							case 'n':
								stringBuilder.Append('\n');
								break;

							case 'r':
								stringBuilder.Append('\r');
								break;

							case 't':
								stringBuilder.Append('\t');
								break;

							case 'u':
								var hex = new StringBuilder();

								for (int i = 0; i < 4; i++)
								{
									hex.Append(NextChar);
								}

								stringBuilder.Append((char)Convert.ToInt32(hex.ToString(), 16));
								break;
						}
						break;

					default:
						stringBuilder.Append(c);
						break;
				}
			}

			return stringBuilder.ToString();
		}

		private object DecodeNumber()
		{
			return ParseNumber(NextWord);
		}

		private void ConsumeWhiteSpace()
		{
			while (WhiteSpace.IndexOf(PeekChar) != -1)
			{
				json.Read();

				if (json.Peek() == -1)
				{
					break;
				}
			}
		}
		#endregion
	}
}
