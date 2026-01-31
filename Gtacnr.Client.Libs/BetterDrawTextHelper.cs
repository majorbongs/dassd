namespace Gtacnr.Client.Libs;

public static class BetterDrawTextHelper
{
	private static string text;

	private static float x_offset;

	private static float y_offset;

	private static float scale;

	private static TextJustification textJustification;

	private static Color color;

	private static float min_x;

	private static float max_x;

	private static Font font;

	public static void BeginTextCommandDisplayText(string text)
	{
		BetterDrawTextHelper.text = text;
		scale = 1f;
		textJustification = TextJustification.Left;
		color = new Color(205, 205, 205, byte.MaxValue);
		min_x = 0f;
		max_x = 1f;
		font = Font.Chalet;
		x_offset = 0f;
		y_offset = 0f;
	}

	public static void SetTextColour(byte r, byte g, byte b, byte a)
	{
		color.R = r;
		color.G = g;
		color.B = b;
		color.A = a;
	}

	public static void SetTextCentre(bool align)
	{
		if (align)
		{
			textJustification = TextJustification.Center;
		}
	}

	public static void SetTextJustification(TextJustification justifyType)
	{
		textJustification = justifyType;
	}

	public static void SetTextFont(Font font)
	{
		BetterDrawTextHelper.font = font;
	}

	public static void SetTextScale(float scale)
	{
		BetterDrawTextHelper.scale = scale;
	}

	public static void SetTextDirection(bool rtl)
	{
	}

	public static void SetTextWrap(float start, float end)
	{
		min_x = start;
		max_x = end;
	}

	public static void SetScriptGfxAlign(GfxAlign horizontalAlign, GfxAlign verticalAlign)
	{
		if (horizontalAlign == GfxAlign.Left)
		{
			x_offset = 0.015f;
		}
		if (verticalAlign == GfxAlign.Top)
		{
			y_offset = 0.015f;
		}
	}

	public static void EndTextCommandDisplayText(float x, float y)
	{
		BetterDrawText.DrawTextThisFrame(text, x + x_offset, y + y_offset, scale, textJustification, color, min_x + x_offset, max_x - x_offset, font);
	}
}
