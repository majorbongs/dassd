using System.Text;

namespace LunarLabs.Parser.JSON;

public static class JSONWriter
{
	private static void Append(DataNode node, DataNode parent, StringBuilder sb, bool genBounds = true)
	{
		if (node.Name != null && (parent == null || parent.Kind != NodeKind.Array))
		{
			sb.Append('"');
			sb.Append(node.Name);
			sb.Append("\" : ");
		}
		if (node.Value != null)
		{
			string value = node.Value;
			if (node.Kind == NodeKind.String)
			{
				value = EscapeJSON(value);
				sb.Append("\"");
				sb.Append(value);
				sb.Append('"');
			}
			else
			{
				sb.Append(value);
			}
			return;
		}
		if (node.Name != null || genBounds)
		{
			sb.Append((node.Kind == NodeKind.Array) ? '[' : '{');
		}
		if (node.Children != null)
		{
			int num = 0;
			foreach (DataNode child in node.Children)
			{
				if (num > 0)
				{
					sb.Append(',');
				}
				Append(child, node, sb, node.Kind == NodeKind.Array);
				num++;
			}
		}
		if (node.Name != null || genBounds)
		{
			sb.Append((node.Kind == NodeKind.Array) ? ']' : '}');
		}
	}

	public static string WriteToString(DataNode node)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (node.Name != null)
		{
			stringBuilder.Append('{');
		}
		Append(node, null, stringBuilder);
		if (node.Name != null)
		{
			stringBuilder.Append('}');
		}
		return stringBuilder.ToString();
	}

	public static string EscapeJSON(string s)
	{
		if (s == null || s.Length == 0)
		{
			return "";
		}
		char c = '\0';
		int length = s.Length;
		StringBuilder stringBuilder = new StringBuilder(length + 4);
		for (int i = 0; i < length; i++)
		{
			c = s[i];
			switch (c)
			{
			case '"':
			case '\\':
				stringBuilder.Append('\\');
				stringBuilder.Append(c);
				continue;
			case '/':
				stringBuilder.Append('\\');
				stringBuilder.Append(c);
				continue;
			case '\b':
				stringBuilder.Append("\\b");
				continue;
			case '\t':
				stringBuilder.Append("\\t");
				continue;
			case '\n':
				stringBuilder.Append("\\n");
				continue;
			case '\f':
				stringBuilder.Append("\\f");
				continue;
			case '\r':
				stringBuilder.Append("\\r");
				continue;
			}
			if (c < ' ')
			{
				string text = "000" + string.Format("X", c);
				stringBuilder.Append("\\u" + text.Substring(text.Length - 4));
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}
}
