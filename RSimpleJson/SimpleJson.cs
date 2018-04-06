using System;

using RSimpleJson.Objects;
using RSimpleJson.Encoding;

namespace RSimpleJson
{
    public class JSON
    {
		#region Inner Classes
		public static class Logger
		{
			public delegate void LogDelegate(string output, object obj);

			public static event LogDelegate LogWarningEvent;
			public static event LogDelegate LogErrorEvent;

			internal static void LogError(object obj, string format, params object[] args)
			{
				LogErrorEvent?.Invoke(string.Format(format, args), obj);
			}

			internal static void LogWarning(object obj, string format, params object[] args)
			{
				LogWarningEvent?.Invoke(string.Format(format, args), obj);
			}

			public static void ClearEvents()
			{
				LogWarningEvent = null;
				LogErrorEvent = null;
            }
		}
		#endregion

		#region Variables & Properties
		private static IJsonSerializer _currentJsonSerializer = null;
        private static JsonSerializer _defaultJsonSerializer = null;

        public static IJsonSerializer CurrentJsonSerializer
		{
			get
			{
				return _currentJsonSerializer;
			}
			set
			{
				_currentJsonSerializer = value;
			}
		}

        public static JsonSerializer DefaultJsonSerializer
        {
            get
            {
                if (_defaultJsonSerializer == null)
                {
                    _defaultJsonSerializer = new JsonSerializer();
                }

                return _defaultJsonSerializer;
            }
        }
		#endregion

		#region Public Methods
		/// <summary>
		/// Try to deserialize the json into a json dictionary
		/// </summary>
		/// <returns><c>true</c>, if deserialize object was successfull, <c>false</c> if the process failed.</returns>
		/// <param name="json">Json.</param>
		/// <param name="obj">Object.</param>
		public static bool TryDeserialize(string json, out object obj)
		{
			obj = (!string.IsNullOrEmpty(json)) ? JsonDecoder.Decode(json) : null;

			return (obj != null);
		}

		/// <summary>
		/// Decode the specified json string into a dictionary.
		/// </summary>
		/// <param name="json">The json string to decode.</param>
		public static object Deserialize(string json)
		{
			object result = null;
			if (!TryDeserialize(json, out result))
			{
				Logger.LogError(json, "Something went wrong deserializing the json and null was returned. Check the format of the json file.");
			}

			return result;
		}

		/// <summary>
		/// Deserializes the specified json string into an object.
		/// </summary>
		/// <param name="json">The json string to decode.</param>
		/// <param name="rootElement">The element of the json file that contains the data of the object.</param>
		/// <typeparam name="T">The type of the object that will be returned.</typeparam>
		public static T DeserializeStringAsObject<T>(string json, string rootElement = null) where T : new()
		{
			Type type = typeof(T);
			object jsonObj = Deserialize(json);
			if (type == null || 
				(jsonObj != null && jsonObj.GetType().IsAssignableFrom(type)))
			{
				return (T)jsonObj;
			}

			return (T)DeserializeObject(jsonObj, type, rootElement);
		}

		/// <summary>
		/// Deserializes the object to the correct object type.
		/// </summary>
		/// <typeparam name="T">The type of the object that will be returned.</typeparam>
		/// <param name="jsonObject">The json dictionary to convert to an object</param>
		/// <param name="rootElement">The element of the json dictionary that contains the data of the object.</param>
		/// <returns>The object.</returns>
		public static T DeserializeObject<T>(object jsonObject, string rootElement = null) where T : new()
		{
			return (T)DeserializeObject(jsonObject, typeof(T), rootElement);
        }

		/// <summary>
		/// Deserialize a json array or object into the desired type
		/// </summary>
		/// <param name="jsonObject">The json object or array</param>
		/// <param name="objectType">The object type to be returned</param>
		/// <param name="rootElement">The element of the json dictionary that contains the data of the object.</param>
		/// <returns>The object.</returns>
		public static object DeserializeObject(object jsonObject, Type objectType, string rootElement = null)
		{
			if (CurrentJsonSerializer == null || 
				objectType == null || 
				(jsonObject != null && jsonObject.GetType().IsAssignableFrom(objectType)))
			{
				return jsonObject;
			}

			if (rootElement != null)
			{
				if (jsonObject is JsonObject)
				{
					JsonObject dictionary = jsonObject as JsonObject;
					if (dictionary.ContainsKey(rootElement))
					{
						jsonObject = dictionary[rootElement];
					}
					else
					{
						Logger.LogWarning(null, "A rootElement was requested ({0}) but does not exist in the decoded Dictionary", rootElement);
					}
				}
				else
				{
					Logger.LogWarning(jsonObject, "A rootElement was requested ({0}) but the decoded object is not a Dictionary. It is a {1}", rootElement, jsonObject.GetType());
				}
			}

			return CurrentJsonSerializer.DeserializeObject(jsonObject, objectType);
		}

        /// <summary>
        /// Serialize the specified object into a json string.
        /// </summary>
        /// <param name="obj">Object to convert to a json string.</param>
        public static string Serialize(object obj)
        {
			string result = JsonEncoder.Encode(CurrentJsonSerializer, obj);

			if (!string.IsNullOrEmpty(result) && (CurrentJsonSerializer.Options & EncodeOptions.PrettyPrint) == EncodeOptions.PrettyPrint)
			{
				return JsonFormatter.PrettyPrint(result);
			}

            return result;
        }
        #endregion

        #region Private Methods
        #endregion
    }
}
