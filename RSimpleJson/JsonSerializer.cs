using System;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

using RSimpleJson.Reflection;
using RSimpleJson.Objects;

namespace RSimpleJson
{
	public partial class JsonSerializer : IJsonSerializer
	{
		#region Variables & Properties
		private readonly Type includeAttrType = typeof(JsonIncludeAttribute);
		private readonly Type excludeAttrType = typeof(NonSerializedAttribute);

		protected static readonly string[] _iso8601Format = new string[]
		{
			"yyyy-MM-dd\\THH:mm:ss.FFFFFFF\\Z",
			"yyyy-MM-dd\\THH:mm:ss\\Z",
			"yyyy-MM-dd\\THH:mm:ssK"
		};

		protected CacheResolver _cacheResolver;

		public EncodeOptions Options { get; private set; }
		#endregion

		#region Public Methods
		public JsonSerializer()
		{
			SetOptions(EncodeOptions.All);
			_cacheResolver = new CacheResolver(new CacheResolver.MemberMapLoader(BuildMap));
		}

		public void SetOptions(EncodeOptions flags)
		{
			Options = flags;
		}

		public virtual object DeserializeObject(object value, Type type)
		{
			object obj = null;
			if (value == null)
			{
				return obj;
			}

			if (value is string)
			{
				string text = value as string;
				if (!string.IsNullOrEmpty(text) && (type == typeof(DateTime) || (ReflectionUtils.IsNullableType(type) && Nullable.GetUnderlyingType(type) == typeof(DateTime))))
				{
					obj = DateTime.ParseExact(text, _iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
				}
				else
				{
					obj = text;
				}
			}
			else if (value is bool)
			{
				obj = value;
			}
			else if (value is long)
			{
				if (type == typeof(DateTime))
				{
					DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
					obj = dateTime.AddMilliseconds((double)((long)value));
				}
				else
				{
					obj = value;
				}
			}
			else if (value is double)
			{
				if (type == typeof(DateTime))
				{
					DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
					obj = dateTime.AddMilliseconds((double)value);
				}
				else
				{
					obj = value;
				}
			}
			else if (value is IDictionary<string, object>)
			{
				DeserializeDictionary((IDictionary<string, object>)value, type, out obj);
			}
			else if (value is IList<object>)
			{
				DeserializeArray((IList<object>)value, type, out obj);
			}
			else if (type.IsEnum)
			{
				obj = Enum.ToObject(type, value);
			}
			else
			{
				obj = ((!typeof(IConvertible).IsAssignableFrom(type)) ? value : Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
			}

			if (ReflectionUtils.IsNullableType(type))
			{
				return ReflectionUtils.ToNullableType(obj, type);
			}

			return obj;
		}

		/// <summary>
		/// De-serialize a dictionary into a dictionary of the correct type or into an object
		/// </summary>
		/// <param name="dict">The json dictionary to retrieve the values from.</param>
		/// <param name="type">The type of the returned object.</param>
		/// <param name="obj">The object that is returned.</param>
		public virtual void DeserializeDictionary(IDictionary<string, object> dict, Type type, out object obj)
		{
			obj = null;
			if (ReflectionUtils.IsTypeDictionary(type))
			{
				Type keyType = type.GetGenericArguments()[0];
				Type valueType = type.GetGenericArguments()[1];
				IDictionary dictionary2 = (IDictionary)CacheResolver.GetNewInstance(type);
				foreach (KeyValuePair<string, object> current in dict)
				{
					Type objType = valueType;
					object data = current.Value;

					if (current.Value != null && current.Value.GetType() == typeof(JsonObject))
					{
						JsonObject dataObj = (JsonObject)current.Value;
						if (dataObj.ContainsKey("@type"))
						{
							objType = Type.GetType((string)dataObj["@type"]);
						}
					}

					dictionary2.Add(current.Key, this.DeserializeObject(data, objType));
				}
				obj = dictionary2;
			}
			else if (TryDeserializeCustomType(type, dict, out obj))
			{
				// The function should handle any conversions that are required.
			}
			else
			{
				obj = CacheResolver.GetNewInstance(type);
				Dictionary<string, CacheResolver.MemberMap> memberDict = _cacheResolver.LoadMaps(type);
				if (memberDict == null)
				{
					obj = dict;
				}
				else
				{
					foreach (KeyValuePair<string, CacheResolver.MemberMap> objMember in memberDict)
					{
						CacheResolver.MemberMap memberVal = objMember.Value;
						if (memberVal.Setter != null)
						{
							string key = objMember.Key;
							if (dict.ContainsKey(key))
							{
								Type objType = memberVal.Type;
								object data = dict[key];

								if (data != null && data is JsonObject)
								{
									JsonObject dataObj = (JsonObject)data;
									if (dataObj.ContainsKey("@type"))
									{
										objType = Type.GetType((string)dataObj["@type"]);
									}
								}

								object memberResult = this.DeserializeObject(data, objType);
								memberVal.Setter(obj, memberResult);
							}
						}
					}

					if (typeof(IJsonSerializable).IsAssignableFrom(type))
					{
						// TODO:
						// obj
					}
				}
			}
		}

		/// <summary>
		/// De-serialize an array into a list of the correct type/
		/// </summary>
		/// <param name="value">The json list to retrieve the values from.</param>
		/// <param name="type">The type of the returned object.</param>
		/// <param name="obj"></param>
		public virtual void DeserializeArray(IList<object> value, Type type, out object obj)
		{
			obj = null;
			JsonArray list = value as JsonArray;
			IList resultList = null;
			if (type.IsArray)
			{
				resultList = (IList)Activator.CreateInstance(type, new object[] { list.Count });
				for (int i = 0; i < list.Count; i++)
				{
					resultList[i] = this.DeserializeObject(resultList[i], type.GetElementType());
				}
			}
			else
			{
				if (ReflectionUtils.IsTypeGenericeCollectionInterface(type) || typeof(IList).IsAssignableFrom(type))
				{
					Type listType = type.GetGenericArguments()[0];
					resultList = (IList)CacheResolver.GetNewInstance(type);
					for (int i = 0; i < list.Count; i++)
					{
						object listValue = list[i];

						if (listValue != null && listValue is JsonObject)
						{
							JsonObject dataObj = (JsonObject)listValue;
							if (dataObj.ContainsKey("@type"))
							{
								Type objType = Type.GetType((string)dataObj["@type"]);
								resultList.Add(this.DeserializeObject(listValue, objType));
								continue;
							}
						}

						resultList.Add(this.DeserializeObject(listValue, listType));
					}
				}

				obj = resultList;
			}
		}

		public virtual bool SerializeNonPrimitiveObject(object input, out object output)
        {
            return TrySerializeKnownTypes(input, out output) || TrySerializeCustomType(input, out output) || TrySerializeUnknownTypes(input, out output);
        }
        #endregion

        #region Protected Methods	
        protected virtual void BuildMap(Type type, Dictionary<string, CacheResolver.MemberMap> memberMaps)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int i = 0;
            for (i = 0; i < fields.Length; i++)
            {
                FieldInfo fieldInfo = fields[i];

				// public variables are always added by default
				bool shouldInclude = fieldInfo.IsPublic;
				
				object[] attributes = fieldInfo.GetCustomAttributes(true);
				for (int j = 0; j < attributes.Length; j++)
				{
					Type attrType = attributes[j].GetType();

					if (excludeAttrType.IsAssignableFrom(attrType))
					{
						shouldInclude = false;
						break;
                    }
					else if (includeAttrType.IsAssignableFrom(attrType))
					{
						shouldInclude = true;
						break;
                    }
				}

				if (shouldInclude)
				{
					memberMaps.Add(fieldInfo.Name, new CacheResolver.MemberMap(fieldInfo));
				}
            }
			
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (i = 0; i < properties.Length; i++)
			{
				PropertyInfo propertyInfo = properties[i];

				if (propertyInfo.CanRead && propertyInfo.CanWrite)
				{
					// properties are excluded by default
					bool shouldInclude = false;

					// if we find the include attribute we add the property to the member map
					object[] attributes = propertyInfo.GetCustomAttributes(includeAttrType, true);
					if (attributes.Length > 0)
					{
						shouldInclude = true;
                    }

					if (shouldInclude)
					{
						memberMaps.Add(propertyInfo.Name, new CacheResolver.MemberMap(propertyInfo));
					}
				}
			}
		}

        protected virtual bool TrySerializeKnownTypes(object input, out object output)
        {
            bool result = true;
            if (input is DateTime)
            {
                output = ((DateTime)input).ToUniversalTime().ToString(_iso8601Format[0], CultureInfo.InvariantCulture);
            }
            else
            {
                if (input is Guid)
                {
                    output = ((Guid)input).ToString("D");
                }
                else
                {
                    if (input is Uri)
                    {
                        output = input.ToString();
                    }
                    else
                    {
                        if (input is Enum)
                        {
                            output = Convert.ToDouble((Enum)input, CultureInfo.InvariantCulture);
                        }
						else
						{
							output = null;
							result = false;
						}
                    }
                }
            }

            return result;
        }

		protected virtual bool TrySerializeCustomType(object input, out object output)
		{
			output = null;
			return false;
        }

		protected virtual bool TryDeserializeCustomType(Type type, IDictionary<string, object> input, out object output)
		{
			output = null;
			return false;
		}

		protected virtual bool TrySerializeUnknownTypes(object input, out object output)
        {
			if (input != null)
			{
				Type type = input.GetType();
				object[] root = type.GetCustomAttributes(false);

				for (int i = 0; i < root.Length; i++)
				{
					object objAttribute = root[i];
					if (objAttribute.GetType() == typeof(SerializableAttribute) || objAttribute.GetType() == typeof(JsonSerializableAttribute) 
						|| objAttribute.GetType() == typeof(System.Reflection.DefaultMemberAttribute)) // A solution for the Unity Vector structs
					{
						IDictionary<string, object> dictionary = new Dictionary<string, object>();
						Dictionary<string, CacheResolver.MemberMap> memberDict = _cacheResolver.LoadMaps(type);

						foreach (KeyValuePair<string, CacheResolver.MemberMap> current in memberDict)
						{
							if (current.Value.Getter != null)
							{
								dictionary.Add(current.Key, current.Value.Getter(input));
							}
						}

						// if this flag is set type won't be appended to the json
						if ((Options & EncodeOptions.AppendType) == EncodeOptions.AppendType)
						{
							SetObjectType(type, dictionary);
						}

						// append additional data to the json
						if(type.IsAssignableFrom(typeof(IJsonSerializable)) || typeof(IJsonSerializable).IsAssignableFrom(type))
						{
							((IJsonSerializable)input).AppendTo(dictionary);
						}

						output = dictionary;

						return true;
					}
				}

				JSON.Logger.LogWarning(input, "Type \"{0}\" is not marked as Serializable", type);
			}

			// The value was either null or the type was not label as serializable
			output = null;
			return true; // really dumb but is required
		}

		protected virtual void SetObjectType(Type type, IDictionary<string, object> dict)
		{
			dict.Add("@type", string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name));
		}
		#endregion
	}
}