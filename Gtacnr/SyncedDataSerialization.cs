using System;
using System.Collections.Generic;
using System.IO;
using CitizenFX.Core;

namespace Gtacnr;

public static class SyncedDataSerialization
{
	public static byte[] Serialize(string data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data ?? "");
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(bool data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(byte data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(ushort data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(int data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(float data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(long data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(Vector3 data)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data.X);
		binaryWriter.Write(data.Y);
		binaryWriter.Write(data.Z);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(Color data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(data.R);
		binaryWriter.Write(data.G);
		binaryWriter.Write(data.B);
		return memoryStream.ToArray();
	}

	public static byte[] Serialize<TField>(Dictionary<TField, Tuple<uint, byte[]>> dict) where TField : Enum
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		foreach (KeyValuePair<TField, Tuple<uint, byte[]>> item2 in dict)
		{
			binaryWriter.Write((byte)(object)item2.Key);
			binaryWriter.Write(item2.Value.Item1);
			byte[] item = item2.Value.Item2;
			binaryWriter.Write(item.Length);
			binaryWriter.Write(item);
		}
		return memoryStream.ToArray();
	}

	public static byte[] Serialize(Dictionary<int, byte[]> dict)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		foreach (KeyValuePair<int, byte[]> item in dict)
		{
			binaryWriter.Write(item.Key);
			byte[] value = item.Value;
			binaryWriter.Write(value.Length);
			binaryWriter.Write(value);
		}
		return memoryStream.ToArray();
	}

	public static string DeserializeString(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return binaryReader.ReadString();
	}

	public static bool DeserializeBool(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return binaryReader.ReadBoolean();
	}

	public static byte DeserializeByte(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return binaryReader.ReadByte();
	}

	public static ushort DeserializeUInt16(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return binaryReader.ReadUInt16();
	}

	public static int DeserializeInt32(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return binaryReader.ReadInt32();
	}

	public static long DeserializeInt64(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return binaryReader.ReadInt64();
	}

	public static float DeserializeFloat(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return binaryReader.ReadSingle();
	}

	public static Vector3 DeserializePosition(byte[] data)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
	}

	public static Guid DeserializeGuid(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		return new Guid(binaryReader.ReadBytes(16));
	}

	public static Dictionary<TField, Tuple<uint, byte[]>> DeserializeVersionAndDataDictionary<TField>(byte[] data) where TField : Enum
	{
		using MemoryStream memoryStream = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(memoryStream);
		Dictionary<TField, Tuple<uint, byte[]>> dictionary = new Dictionary<TField, Tuple<uint, byte[]>>();
		long length = memoryStream.Length;
		while (memoryStream.Position != length)
		{
			TField key = (TField)(object)binaryReader.ReadByte();
			uint item = binaryReader.ReadUInt32();
			int count = binaryReader.ReadInt32();
			byte[] item2 = binaryReader.ReadBytes(count);
			dictionary[key] = new Tuple<uint, byte[]>(item, item2);
		}
		return dictionary;
	}

	public static Dictionary<int, byte[]> DeserializeBatchedDataDictionary(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(memoryStream);
		Dictionary<int, byte[]> dictionary = new Dictionary<int, byte[]>();
		long length = memoryStream.Length;
		while (memoryStream.Position != length)
		{
			int key = binaryReader.ReadInt32();
			int count = binaryReader.ReadInt32();
			byte[] value = binaryReader.ReadBytes(count);
			dictionary[key] = value;
		}
		return dictionary;
	}
}
