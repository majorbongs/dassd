using System.Collections.Generic;
using System.Text;

namespace LunarLabs.Parser.CSV;

public class CSVReader
{
	private enum State
	{
		Header,
		Content
	}

	public static DataNode ReadFromString(string contents)
	{
		DataNode dataNode = DataNode.CreateArray();
		List<string> list = new List<string>();
		int num = 0;
		State state = State.Header;
		int num2 = 0;
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder();
		DataNode dataNode2 = null;
		while (num < contents.Length)
		{
			char c = contents[num];
			num++;
			switch (state)
			{
			case State.Header:
				if (c == ',' || c == '\n')
				{
					list.Add(stringBuilder.ToString().Trim());
					stringBuilder.Length = 0;
				}
				switch (c)
				{
				case '\n':
					state = State.Content;
					break;
				default:
					stringBuilder.Append(c);
					break;
				case ',':
					break;
				}
				break;
			case State.Content:
				if (!flag && (c == ',' || c == '\n'))
				{
					if (num2 < list.Count)
					{
						dataNode2.AddField(list[num2], stringBuilder.ToString());
					}
					stringBuilder.Length = 0;
					num2++;
					if (c == '\n')
					{
						num2 = 0;
						dataNode2 = null;
					}
					break;
				}
				if (c == '"')
				{
					if (!flag || num >= contents.Length || contents[num] != '"')
					{
						flag = !flag;
						break;
					}
					num++;
				}
				if (dataNode2 == null)
				{
					dataNode2 = DataNode.CreateObject();
					dataNode.AddNode(dataNode2);
				}
				stringBuilder.Append(c);
				break;
			}
		}
		if (dataNode2 != null && num2 < list.Count)
		{
			dataNode2.AddField(list[num2], stringBuilder.ToString());
		}
		return dataNode;
	}
}
