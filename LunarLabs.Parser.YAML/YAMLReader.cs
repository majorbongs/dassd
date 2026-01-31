using System;
using System.Text;

namespace LunarLabs.Parser.YAML;

public class YAMLReader
{
	private enum State
	{
		Header,
		Comment,
		Next,
		Name,
		NewLine,
		Idents,
		Child,
		Content
	}

	public static DataNode ReadFromString(string contents)
	{
		string[] lines = contents.Split('\n');
		int index = 0;
		DataNode dataNode = DataNode.CreateArray();
		ReadNodes(lines, ref index, 0, dataNode);
		return dataNode;
	}

	private static void ReadNodes(string[] lines, ref int index, int baseIndents, DataNode parent)
	{
		int num = -1;
		DataNode dataNode = null;
		while (true)
		{
			if (index >= lines.Length)
			{
				return;
			}
			int num2 = 0;
			string text = lines[index].TrimEnd();
			if (text.StartsWith("---"))
			{
				index++;
				continue;
			}
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == ' ')
				{
					num2++;
					continue;
				}
				text = text.Substring(i);
				break;
			}
			if (num2 < baseIndents)
			{
				return;
			}
			index++;
			num2 -= baseIndents;
			if (num == -1)
			{
				num = num2;
			}
			else if (num2 != num)
			{
				throw new Exception("YAML parsing exception, unexpected ammount of identation");
			}
			string[] array = text.Split(':');
			if (array.Length != 2)
			{
				break;
			}
			string name = array[0].Trim();
			string text2 = array[1].Trim();
			if (text2.StartsWith("&"))
			{
				text2 = null;
			}
			if (text2 == ">" || text2 == "|")
			{
				bool flag = text2 == "|";
				StringBuilder stringBuilder = new StringBuilder();
				while (index < lines.Length)
				{
					string text3 = lines[index].TrimStart();
					if (text3.Contains(":"))
					{
						break;
					}
					stringBuilder.Append(text3);
					if (flag)
					{
						stringBuilder.Append('\n');
					}
					index++;
				}
				stringBuilder.Append('\n');
				text2 = stringBuilder.ToString();
			}
			if (!string.IsNullOrEmpty(text2))
			{
				parent.AddField(name, text2);
				continue;
			}
			dataNode = DataNode.CreateObject(name);
			parent.AddNode(dataNode);
			ReadNodes(lines, ref index, baseIndents + 1, dataNode);
		}
		throw new Exception("YAML parsing exception, bad formed line");
	}
}
