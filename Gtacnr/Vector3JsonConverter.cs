using System;
using CitizenFX.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gtacnr;

public sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
	public override bool CanWrite => false;

	public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if (reader.TokenType == JsonToken.Null)
		{
			return default(Vector3);
		}
		switch (reader.TokenType)
		{
		case JsonToken.StartArray:
		{
			JArray jArray = JArray.Load(reader);
			if (jArray.Count != 3)
			{
				throw new JsonSerializationException($"Expected an array with 3 elements for Vector3, but got {jArray.Count}.");
			}
			return new Vector3(jArray[0].ToObject<float>(serializer), jArray[1].ToObject<float>(serializer), jArray[2].ToObject<float>(serializer));
		}
		case JsonToken.StartObject:
		{
			JObject obj = JObject.Load(reader);
			float requiredFloat = GetRequiredFloat(obj, serializer, "x", "X");
			float requiredFloat2 = GetRequiredFloat(obj, serializer, "y", "Y");
			float requiredFloat3 = GetRequiredFloat(obj, serializer, "z", "Z");
			return new Vector3(requiredFloat, requiredFloat2, requiredFloat3);
		}
		default:
			throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing Vector3.");
		}
	}

	public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
	{
		throw new NotSupportedException("Vector3JsonConverter is a deserializer-only converter.");
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
		throw new JsonSerializationException("Missing required Vector3 component. Tried keys: " + string.Join(", ", candidateNames));
	}
}
