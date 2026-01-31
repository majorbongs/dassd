using System;
using CitizenFX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gtacnr;

public sealed class Vector4JsonConverter : JsonConverter<Vector4>
{
	public override bool CanWrite => false;

	public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		if (reader.TokenType == JsonToken.Null)
		{
			return default(Vector4);
		}
		switch (reader.TokenType)
		{
		case JsonToken.StartArray:
		{
			JArray jArray = JArray.Load(reader);
			if (jArray.Count != 4)
			{
				throw new JsonSerializationException($"Expected an array with 4 elements for Vector4, but got {jArray.Count}.");
			}
			return new Vector4(jArray[0].ToObject<float>(serializer), jArray[1].ToObject<float>(serializer), jArray[2].ToObject<float>(serializer), jArray[3].ToObject<float>(serializer));
		}
		case JsonToken.StartObject:
		{
			JObject obj = JObject.Load(reader);
			float requiredFloat = GetRequiredFloat(obj, serializer, "x", "X");
			float requiredFloat2 = GetRequiredFloat(obj, serializer, "y", "Y");
			float requiredFloat3 = GetRequiredFloat(obj, serializer, "z", "Z");
			float requiredFloat4 = GetRequiredFloat(obj, serializer, "w", "W");
			return new Vector4(requiredFloat, requiredFloat2, requiredFloat3, requiredFloat4);
		}
		default:
			throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Vector4.");
		}
	}

	public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
	{
		throw new NotSupportedException("Vector4JsonConverter is a deserializer-only converter.");
	}

	private static float GetRequiredFloat(JObject obj, JsonSerializer serializer, params string[] candidateNames)
	{
		foreach (string propertyName in candidateNames)
		{
			if (obj.TryGetValue(propertyName, StringComparison.Ordinal, out var value))
			{
				return value.ToObject<float>(serializer);
			}
		}
		throw new JsonSerializationException("Missing required Vector4 component. Tried keys: " + string.Join(", ", candidateNames));
	}
}
