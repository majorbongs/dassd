namespace LunarLabs.Parser;

internal static class ParserUtils
{
	public static void GetColumnAndLine(string text, int offset, out int col, out int line)
	{
		line = 1;
		col = 0;
		for (int i = 0; i <= offset && i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				col = 0;
				line++;
			}
			col++;
		}
	}

	public static string GetOffsetError(string text, int offset)
	{
		GetColumnAndLine(text, offset, out var col, out var line);
		return $"at line {line}, column {col}";
	}

	public static bool IsNumeric(this string text)
	{
		double result;
		return double.TryParse(text, out result);
	}
}
