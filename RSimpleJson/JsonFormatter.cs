using System;
using System.Text;
using System.Collections.Generic;

namespace RSimpleJson
{
	/// <summary>
	/// Credit
	/// https://code.google.com/p/http-get-post-request/source/browse/trunk/JsonFormatter.cs?spec=svn8&r=8 
	/// http://stackoverflow.com/questions/4580397/json-formatter-in-c
	/// </summary>
	public class JsonFormatter
	{
		#region Variables & Properties
		protected const string Space = " ";
		protected const string Indent = "\t";

		protected int _indent = 0;
		protected bool isInString = false;
		protected char prevChar = '\0';
		#endregion

		#region Public Methods
		public JsonFormatter()
		{
		}

		public static string PrettyPrint(string input)
		{
			JsonFormatter formatter = new JsonFormatter();
			string result = formatter.Print(input);
			formatter.CleanUp();
			formatter = null;

			return result;
		}

		public virtual void CleanUp()
		{
			_indent = 0;
			isInString = false;
			prevChar = '\0';
		}

		public virtual string Print(string input)
		{
			StringBuilder output = new StringBuilder();
			char c;
			_indent = 0;

			for (int i = 0; i < input.Length; i++)
			{
				c = input[i];

				switch (c)
				{
					case '{':
					case '[':
						output.Append(c);
						if (!isInString)
						{
							_indent++;
							output.AppendLine();
							BuildIndents(_indent, output);
						}
						break;

					case '}':
					case ']':
						if (!isInString)
						{
							output.AppendLine();
							_indent--;
							BuildIndents(_indent, output);
						}
						output.Append(c);
						break;

					case ',':
						output.Append(c);

						if (!isInString)
						{
							output.AppendLine();
							BuildIndents(_indent, output);
						}
						break;

					case '"':
						output.Append(c);
						if (prevChar != '\\')
						{
							isInString = !isInString;
						}
						break;

					case ':':
						if (!isInString)
						{
							output.Append(c);
							output.Append(Space);
						}
						else
						{
							output.Append(c);
						}
						break;

					case ' ':
						if (isInString)
						{
							output.Append(c);
						}
						break;
						
					default:
						output.Append(c);
						break;
				}

				prevChar = c;
			}

			return output.ToString();
		}
		#endregion

		#region Protected Methods
		protected static void BuildIndents(int indents, StringBuilder output)
		{
			for (; indents > 0; indents--)
			{
				output.Append(Indent);
			}
		}
		#endregion
	}
}