using System;
using System.Globalization;
using System.Text;

namespace LunarLabs.Parser.JSON;

public class JSONReader
{
	private enum State
	{
		Type,
		Name,
		Colon,
		Value,
		Next
	}

	private enum InputMode
	{
		None,
		Text,
		Number
	}

	public static DataNode ReadFromString(string contents)
	{
		int index = 0;
		return ReadNode(contents, ref index, null);
	}

	private static void ReadString(string target, string contents, ref int index)
	{
		index--;
		for (int i = 0; i < target.Length; i++)
		{
			if (index >= contents.Length)
			{
				throw new Exception("JSON parsing exception, unexpected end of data");
			}
			if (contents[index] != target[i])
			{
				throw new Exception("JSON parsing exception, unexpected character");
			}
			index++;
		}
	}

	private static DataNode ReadNode(string contents, ref int index, string name)
	{
		DataNode dataNode = null;
		State state = State.Type;
		InputMode inputMode = InputMode.None;
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		int num = index;
		bool flag = false;
		while (true)
		{
			bool flag2 = false;
			char c;
			bool flag3;
			do
			{
				if (index >= contents.Length)
				{
					if (state == State.Next)
					{
						return dataNode;
					}
					throw new Exception("JSON parsing exception, unexpected end of data");
				}
				c = contents[index];
				flag3 = char.IsWhiteSpace(c);
				if (!flag3)
				{
					num = index;
				}
				index++;
			}
			while (inputMode == InputMode.None && flag3);
			switch (state)
			{
			case State.Type:
				switch (c)
				{
				case '{':
					dataNode = DataNode.CreateObject(name);
					state = State.Name;
					break;
				case '[':
					dataNode = DataNode.CreateArray(name);
					state = State.Value;
					break;
				default:
					throw new Exception("JSON parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected character");
				}
				break;
			case State.Name:
				if (c == '}' && dataNode.Kind == NodeKind.Object)
				{
					return dataNode;
				}
				if (c == '"')
				{
					if (inputMode == InputMode.None)
					{
						inputMode = InputMode.Text;
						stringBuilder.Length = 0;
					}
					else
					{
						inputMode = InputMode.None;
						state = State.Colon;
					}
				}
				else
				{
					if (inputMode != InputMode.Text)
					{
						throw new Exception("JSON parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected character");
					}
					stringBuilder.Append(c);
				}
				break;
			case State.Colon:
				if (c == ':')
				{
					state = State.Value;
					break;
				}
				throw new Exception("JSON parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", expected collon");
			case State.Value:
				if (c == '\\' && !flag)
				{
					flag = true;
					break;
				}
				if (flag)
				{
					flag = false;
					switch (c)
					{
					case 'n':
						stringBuilder2.Append('\n');
						goto end_IL_0067;
					case 'r':
						stringBuilder2.Append('\r');
						goto end_IL_0067;
					case 't':
						stringBuilder2.Append('\t');
						goto end_IL_0067;
					case 'b':
						stringBuilder2.Append('\b');
						goto end_IL_0067;
					case 'f':
						stringBuilder2.Append('\f');
						goto end_IL_0067;
					case 'u':
					{
						string text = "";
						for (int i = 0; i < 4; i++)
						{
							if (index >= contents.Length)
							{
								throw new Exception("JSON parsing exception, unexpected end of data");
							}
							text += contents[index];
							index++;
						}
						c = (char)ushort.Parse(text, NumberStyles.HexNumber);
						break;
					}
					}
					stringBuilder2.Append(c);
					break;
				}
				if (c == 'n' && inputMode == InputMode.None)
				{
					ReadString("null", contents, ref index);
					dataNode.AddField((stringBuilder.Length == 0) ? null : stringBuilder.ToString(), null);
					state = State.Next;
					break;
				}
				if (c == 'f' && inputMode == InputMode.None)
				{
					ReadString("false", contents, ref index);
					dataNode.AddField((stringBuilder.Length == 0) ? null : stringBuilder.ToString(), false);
					state = State.Next;
					break;
				}
				if (c == 't' && inputMode == InputMode.None)
				{
					ReadString("true", contents, ref index);
					dataNode.AddField((stringBuilder.Length == 0) ? null : stringBuilder.ToString(), true);
					state = State.Next;
					break;
				}
				if (c == ']' && inputMode == InputMode.None && dataNode.Kind == NodeKind.Array)
				{
					return dataNode;
				}
				switch (c)
				{
				case '"':
				{
					if (inputMode == InputMode.None)
					{
						inputMode = InputMode.Text;
						stringBuilder2.Length = 0;
						break;
					}
					string text3 = stringBuilder2.ToString();
					object value;
					if (inputMode == InputMode.Number)
					{
						text3.Contains("e");
						value = text3;
					}
					else
					{
						value = text3;
					}
					inputMode = InputMode.None;
					dataNode.AddField((stringBuilder.Length == 0) ? null : stringBuilder.ToString(), value);
					state = State.Next;
					break;
				}
				case '[':
				case '{':
				{
					if (inputMode == InputMode.Text)
					{
						stringBuilder2.Append(c);
						break;
					}
					index = num;
					DataNode node = ReadNode(contents, ref index, (stringBuilder.Length == 0) ? null : stringBuilder.ToString());
					dataNode.AddNode(node);
					state = State.Next;
					break;
				}
				default:
					if (inputMode == InputMode.Text)
					{
						stringBuilder2.Append(c);
						break;
					}
					if (char.IsNumber(c) || c == '.' || c == 'e' || c == 'E' || c == '-' || c == '+')
					{
						if (inputMode != InputMode.Number)
						{
							stringBuilder2.Length = 0;
							inputMode = InputMode.Number;
						}
						if (c == 'E')
						{
							c = 'e';
						}
						stringBuilder2.Append(c);
						break;
					}
					if (inputMode == InputMode.Number)
					{
						inputMode = InputMode.None;
						string text2 = stringBuilder2.ToString();
						if (text2.Contains("e"))
						{
							double num2 = double.Parse(text2, NumberStyles.Any, CultureInfo.InvariantCulture);
							dataNode.AddField((stringBuilder.Length == 0) ? null : stringBuilder.ToString(), num2);
						}
						else
						{
							decimal num3 = decimal.Parse(text2, NumberStyles.Any, CultureInfo.InvariantCulture);
							dataNode.AddField((stringBuilder.Length == 0) ? null : stringBuilder.ToString(), num3);
						}
						state = State.Next;
						if (c == ',' || c == ']' || c == '}')
						{
							index = num;
						}
						break;
					}
					throw new Exception("JSON parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected character");
				}
				break;
			case State.Next:
				{
					switch (c)
					{
					case ',':
						state = ((dataNode.Kind != NodeKind.Array) ? State.Name : State.Value);
						break;
					case '}':
						if (dataNode.Kind != NodeKind.Object)
						{
							throw new Exception("JSON parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected }");
						}
						return dataNode;
					case ']':
						if (dataNode.Kind != NodeKind.Array)
						{
							throw new Exception("JSON parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", unexpected ]");
						}
						return dataNode;
					default:
						throw new Exception("JSON parsing exception at " + ParserUtils.GetOffsetError(contents, index) + ", expected collon");
					}
					break;
				}
				end_IL_0067:
				break;
			}
		}
	}
}
