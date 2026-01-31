using System;
using System.Text;

namespace LunarLabs.Parser.XML;

public class XMLReader
{
	private enum State
	{
		Next,
		TagOpen,
		TagClose,
		Prolog,
		Comment,
		AttributeName,
		AttributeQuote,
		AttributeValue,
		NextAttribute,
		Content,
		CData,
		CDataClose
	}

	private static bool CDataAt(string contents, int index)
	{
		string text = "![CDATA[";
		for (int i = 0; i < text.Length; i++)
		{
			int num = i + index;
			if (num >= contents.Length)
			{
				return false;
			}
			if (contents[num] != text[i])
			{
				return false;
			}
		}
		return true;
	}

	public static DataNode ReadFromString(string contents)
	{
		int index = 0;
		DataNode node = ReadNode(contents, ref index);
		DataNode dataNode = DataNode.CreateObject();
		dataNode.AddNode(node);
		return dataNode;
	}

	private static DataNode ReadNode(string contents, ref int index)
	{
		DataNode dataNode = null;
		State state = State.Next;
		State state2 = State.Next;
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		while (true)
		{
			bool flag = false;
			bool flag2 = state == State.Content || state == State.TagOpen || state == State.TagClose || state == State.AttributeName || state == State.AttributeValue || state == State.CData;
			char c;
			bool num;
			do
			{
				if (index >= contents.Length)
				{
					if (state == State.Next)
					{
						return null;
					}
					throw new Exception("XML parsing exception, unexpected end of data");
				}
				c = contents[index];
				num = char.IsWhiteSpace(c);
				index++;
			}
			while (num && !flag2);
			switch (state)
			{
			case State.Next:
				if (c == '<')
				{
					state = State.TagOpen;
					stringBuilder.Length = 0;
					break;
				}
				throw new Exception("XML parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected character");
			case State.TagOpen:
				switch (c)
				{
				case '?':
					if (contents[index - 2] == '<')
					{
						state = State.Prolog;
					}
					else
					{
						stringBuilder.Append(c);
					}
					break;
				case '!':
					if (index < contents.Length - 3 && contents[index - 2] == '<' && contents[index] == '-' && contents[index + 1] == '-')
					{
						state = State.Comment;
						state2 = State.Next;
					}
					else
					{
						stringBuilder.Append(c);
					}
					break;
				case '/':
					dataNode = DataNode.CreateObject(stringBuilder.ToString());
					state = State.TagClose;
					break;
				case '>':
					dataNode = DataNode.CreateObject(stringBuilder.ToString());
					state = State.Content;
					break;
				case ' ':
					dataNode = DataNode.CreateObject(stringBuilder.ToString());
					stringBuilder.Length = 0;
					state = State.AttributeName;
					break;
				default:
					stringBuilder.Append(c);
					break;
				}
				break;
			case State.TagClose:
				if (c == '>')
				{
					return dataNode;
				}
				break;
			case State.AttributeName:
				switch (c)
				{
				case '/':
					state = State.TagClose;
					break;
				case '=':
					state = State.AttributeQuote;
					break;
				default:
					if (stringBuilder.Length > 0 || !char.IsWhiteSpace(c))
					{
						stringBuilder.Append(c);
					}
					break;
				}
				break;
			case State.AttributeQuote:
				if (c == '"')
				{
					state = State.AttributeValue;
					stringBuilder2.Length = 0;
					break;
				}
				throw new Exception("XML parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected character");
			case State.AttributeValue:
				if (c == '"')
				{
					dataNode.AddField(stringBuilder.ToString(), stringBuilder2.ToString());
					stringBuilder2.Length = 0;
					state = State.NextAttribute;
				}
				else
				{
					stringBuilder2.Append(c);
				}
				break;
			case State.NextAttribute:
				switch (c)
				{
				case '/':
					break;
				case '>':
					if (contents[index - 2] == '/')
					{
						return dataNode;
					}
					state = State.Content;
					break;
				default:
					if (char.IsLetter(c))
					{
						stringBuilder.Length = 0;
						stringBuilder.Append(c);
						state = State.AttributeName;
						break;
					}
					throw new Exception("XML parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected character");
				}
				break;
			case State.Prolog:
				if (c == '>')
				{
					state = State.Next;
				}
				break;
			case State.Comment:
				if (c == '>' && contents[index - 2] == '-' && contents[index - 3] == '-')
				{
					state = state2;
				}
				break;
			case State.CData:
				if (c == ']' && contents[index - 2] == c && index < contents.Length && contents[index] == '>')
				{
					state = State.Content;
					stringBuilder2.Length--;
					index++;
				}
				else
				{
					stringBuilder2.Append(c);
				}
				break;
			case State.Content:
				if (c == '<')
				{
					if (CDataAt(contents, index))
					{
						state = State.CData;
						index += 8;
						break;
					}
					if (index < contents.Length && contents[index] == '/')
					{
						state = State.TagClose;
						dataNode.Value += stringBuilder2.ToString().Replace("&lt;", "<").Replace("&gt;", ">")
							.Replace("&amp;", "&")
							.Replace("&quot;", "\"")
							.Replace("&apos;", "'");
						break;
					}
					if (index < contents.Length - 3 && contents[index] == '!' && contents[index + 1] == '-' && contents[index + 2] == '-')
					{
						state = State.Comment;
						state2 = State.Content;
						index += 2;
						break;
					}
					index--;
					DataNode dataNode2 = ReadNode(contents, ref index);
					if (dataNode2 == null)
					{
						throw new Exception("XML parsing exception, unexpected end of data");
					}
					dataNode.AddNode(dataNode2);
				}
				else
				{
					stringBuilder2.Append(c);
				}
				break;
			}
		}
	}
}
