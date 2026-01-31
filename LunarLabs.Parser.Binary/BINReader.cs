using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunarLabs.Parser.Binary;

public class BINReader
{
	private static DataNode ReadNode(BinaryReader reader, Dictionary<ushort, string> dic)
	{
		ushort key = reader.ReadUInt16();
		string name = dic[key];
		string value = null;
		ushort num = reader.ReadUInt16();
		if (num > 0)
		{
			byte[] bytes = reader.ReadBytes(num);
			value = Encoding.UTF8.GetString(bytes);
		}
		int num2 = reader.ReadInt32();
		DataNode dataNode = DataNode.CreateObject(name);
		dataNode.Value = value;
		while (num2 > 0)
		{
			DataNode node = ReadNode(reader, dic);
			dataNode.AddNode(node);
			num2--;
		}
		return dataNode;
	}

	public static DataNode ReadFromBytes(byte[] bytes)
	{
		Dictionary<ushort, string> dictionary = new Dictionary<ushort, string>();
		using BinaryReader binaryReader = new BinaryReader(new MemoryStream(bytes));
		for (int num = binaryReader.ReadInt32(); num > 0; num--)
		{
			ushort key = binaryReader.ReadUInt16();
			byte count = binaryReader.ReadByte();
			byte[] bytes2 = binaryReader.ReadBytes(count);
			string value = Encoding.UTF8.GetString(bytes2);
			dictionary[key] = value;
		}
		return ReadNode(binaryReader, dictionary);
	}
}
