using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Gtacnr;

public static class JsonExtensions
{
	private sealed class CustomContractResolver : DefaultContractResolver
	{
		protected override JsonConverter ResolveContractConverter(Type objectType)
		{
			if (objectType.IsGenericType && !objectType.IsGenericTypeDefinition)
			{
				object[] customAttributes = objectType.GetCustomAttributes(inherit: false);
				for (int i = 0; i < customAttributes.Length; i++)
				{
					if (customAttributes[i] is JsonConverterAttribute jsonConverterAttribute && jsonConverterAttribute != null && jsonConverterAttribute.ConverterType.IsGenericTypeDefinition)
					{
						return (JsonConverter)Activator.CreateInstance(jsonConverterAttribute.ConverterType.MakeGenericType(objectType.GetGenericArguments()), jsonConverterAttribute.ConverterParameters);
					}
				}
			}
			return base.ResolveContractConverter(objectType);
		}
	}

	private static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = new CustomContractResolver(),
		Converters = new List<JsonConverter>
		{
			new Vector3JsonConverter(),
			new Vector4JsonConverter()
		}
	};

	public static string Json(this object obj)
	{
		return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
	}

	public static T Unjson<T>(this string json)
	{
		return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);
	}

	public static string MinifyJson(this string json)
	{
		return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.None, jsonSerializerSettings);
	}

	public static string BeautifyJson(this string json)
	{
		return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented, jsonSerializerSettings);
	}
}
