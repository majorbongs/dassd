using System.Linq;
using System.Text;

namespace LunarLabs.Parser.XML;

public class XMLWriter
{
	public static string WriteToString(DataNode node, bool expand = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		WriteNode(stringBuilder, node, 0, expand);
		return stringBuilder.ToString();
	}

	private static void WriteNode(StringBuilder buffer, DataNode node, int tabs, bool expand)
	{
		for (int i = 0; i < tabs; i++)
		{
			buffer.Append('\t');
		}
		buffer.Append('<');
		buffer.Append(node.Name);
		int num = 0;
		int num2 = 0;
		foreach (DataNode child in node.Children)
		{
			if (expand || child.Children.Any())
			{
				num++;
				continue;
			}
			buffer.Append(' ');
			buffer.Append(child.Name);
			buffer.Append('=');
			buffer.Append('"');
			buffer.Append(child.Value);
			buffer.Append('"');
			num2++;
		}
		if (num2 > 0)
		{
			buffer.Append(' ');
		}
		int num3;
		if (num2 == node.ChildCount)
		{
			num3 = ((node.Value == null) ? 1 : 0);
			if (num3 != 0)
			{
				buffer.Append('/');
			}
		}
		else
		{
			num3 = 0;
		}
		buffer.Append('>');
		if (num3 != 0)
		{
			if (!expand)
			{
				buffer.AppendLine();
			}
			return;
		}
		if (num2 < node.ChildCount)
		{
			buffer.AppendLine();
			foreach (DataNode child2 in node.Children)
			{
				if (expand || child2.Children.Any())
				{
					WriteNode(buffer, child2, tabs + 1, expand);
				}
			}
			for (int j = 0; j < tabs; j++)
			{
				buffer.Append('\t');
			}
		}
		if (node.Value != null)
		{
			buffer.Append(node.Value);
		}
		buffer.Append('<');
		buffer.Append('/');
		buffer.Append(node.Name);
		buffer.Append('>');
		buffer.AppendLine();
	}
}
