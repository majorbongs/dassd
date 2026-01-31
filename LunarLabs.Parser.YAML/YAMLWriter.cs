using System.Text;

namespace LunarLabs.Parser.YAML;

public class YAMLWriter
{
	public static string WriteToString(DataNode node)
	{
		StringBuilder stringBuilder = new StringBuilder();
		WriteNode(stringBuilder, node, 0);
		return stringBuilder.ToString();
	}

	private static void WriteNode(StringBuilder buffer, DataNode node, int idents)
	{
		for (int i = 0; i < idents; i++)
		{
			buffer.Append(' ');
		}
		if (node.Name != null && node.Name != "")
		{
			buffer.Append(node.Name);
			buffer.Append(':');
			buffer.Append(' ');
			if (node.Value != null)
			{
				buffer.Append(node.Value);
			}
			buffer.AppendLine();
		}
		foreach (DataNode child in node.Children)
		{
			WriteNode(buffer, child, idents + 1);
		}
	}
}
