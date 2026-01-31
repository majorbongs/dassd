using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunarLabs.Parser.Binary;

public class BINWriter
{
	private static void GenDic(Dictionary<string, ushort> dic, DataNode node)
	{
		if (!dic.ContainsKey(node.Name))
		{
			dic[node.Name] = (ushort)(dic.Count + 1);
		}
		foreach (DataNode child in node.Children)
		{
			GenDic(dic, child);
		}
	}

	private static void WriteNode(BinaryWriter writer, Dictionary<string, ushort> dic, DataNode node)
	{
		ushort value = dic[node.Name];
		writer.Write(value);
		byte[] array = (string.IsNullOrEmpty(node.Value) ? null : Encoding.UTF8.GetBytes(node.Value));
		ushort value2 = (byte)((array != null) ? ((uint)array.Length) : 0u);
		writer.Write(value2);
		if (array != null)
		{
			writer.Write(array);
		}
		int childCount = node.ChildCount;
		writer.Write(childCount);
		foreach (DataNode child in node.Children)
		{
			WriteNode(writer, dic, child);
		}
	}

	public static byte[] SaveToBytes(DataNode root)
	{
		Dictionary<string, ushort> dictionary = new Dictionary<string, ushort>();
		GenDic(dictionary, root);
		using MemoryStream memoryStream = new MemoryStream(1024);
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			int count = dictionary.Count;
			binaryWriter.Write(count);
			foreach (KeyValuePair<string, ushort> item in dictionary)
			{
				binaryWriter.Write(item.Value);
				byte[] bytes = Encoding.UTF8.GetBytes(item.Key);
				byte value = (byte)bytes.Length;
				binaryWriter.Write(value);
				binaryWriter.Write(bytes);
			}
			WriteNode(binaryWriter, dictionary, root);
		}
		return memoryStream.ToArray();
	}
}
