using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LunarLabs.Parser.CSV;

public class CSVWriter
{
	public static string WriteToString(DataNode node)
	{
		StringBuilder stringBuilder = new StringBuilder();
		DataNode dataNode = node.Children.FirstOrDefault();
		int num = 0;
		List<string> list = new List<string>();
		foreach (DataNode child in dataNode.Children)
		{
			if (num > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(child.Name);
			list.Add(child.Name);
			num++;
		}
		stringBuilder.AppendLine();
		foreach (DataNode child2 in node.Children)
		{
			num = 0;
			using (List<string>.Enumerator enumerator2 = list.GetEnumerator())
			{
				for (; enumerator2.MoveNext(); num++)
				{
					string current3 = enumerator2.Current;
					DataNode nodeByName = child2.GetNodeByName(current3);
					if (num > 0)
					{
						stringBuilder.Append(',');
					}
					if (nodeByName == null)
					{
						continue;
					}
					int num2;
					if (!nodeByName.Value.Contains(','))
					{
						num2 = (nodeByName.Value.Contains('\n') ? 1 : 0);
						if (num2 == 0)
						{
							goto IL_00ed;
						}
					}
					else
					{
						num2 = 1;
					}
					stringBuilder.Append('"');
					goto IL_00ed;
					IL_00ed:
					stringBuilder.Append(nodeByName.Value);
					if (num2 != 0)
					{
						stringBuilder.Append('"');
					}
				}
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}
}
