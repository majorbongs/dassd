using System;
using System.Drawing;
using System.Text;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIResText : Text
{
	public Alignment TextAlignment { get; set; }

	public float Wrap { get; set; }

	[Obsolete("Use UIResText.Wrap instead.", true)]
	public SizeF WordWrap
	{
		get
		{
			return new SizeF(Wrap, 0f);
		}
		set
		{
			Wrap = value.Width;
		}
	}

	public UIResText(string caption, PointF position, float scale)
		: base(caption, position, scale)
	{
		TextAlignment = (Alignment)1;
	}

	public UIResText(string caption, PointF position, float scale, Color color)
		: base(caption, position, scale, color)
	{
		TextAlignment = (Alignment)1;
	}

	public UIResText(string caption, PointF position, float scale, Color color, Font font, Alignment justify)
		: base(caption, position, scale, color, font, (Alignment)1)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		TextAlignment = justify;
	}

	public static void AddLongString(string str)
	{
		if (Encoding.UTF8.GetByteCount(str) == str.Length)
		{
			AddLongStringForAscii(str);
		}
		else
		{
			AddLongStringForUtf8(str);
		}
	}

	private static void AddLongStringForAscii(string input)
	{
		for (int i = 0; i < input.Length; i += 99)
		{
			API.AddTextComponentString(input.Substring(i, Math.Min(99, input.Length - i)));
		}
	}

	internal static void AddLongStringForUtf8(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return;
		}
		Encoding uTF = Encoding.UTF8;
		if (uTF.GetByteCount(input) < 99)
		{
			API.AddTextComponentString(input);
			return;
		}
		int num = 0;
		for (int i = 0; i < input.Length; i++)
		{
			int num2 = i - num;
			if (uTF.GetByteCount(input.Substring(num, num2)) > 99)
			{
				API.AddTextComponentString(input.Substring(num, num2 - 1));
				i--;
				num = num + num2 - 1;
			}
		}
		API.AddTextComponentString(input.Substring(num, input.Length - num));
	}

	[Obsolete("Use ScreenTools.GetTextWidth instead.", true)]
	public static float MeasureStringWidth(string str, Font font, float scale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ScreenTools.GetTextWidth(str, font, scale);
	}

	[Obsolete("Use ScreenTools.GetTextWidth instead.", true)]
	public static float MeasureStringWidthNoConvert(string str, Font font, float scale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return ScreenTools.GetTextWidth(str, font, scale);
	}

	public override void Draw(SizeF offset)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected I4, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Invalid comparison between Unknown and I4
		int width = Screen.Resolution.Width;
		int height = Screen.Resolution.Height;
		float num = (float)width / (float)height;
		float num2 = 1080f * num;
		float num3 = ((Text)this).Position.X / num2;
		float num4 = ((Text)this).Position.Y / 1080f;
		API.SetTextFont((int)((Text)this).Font);
		API.SetTextScale(1f, ((Text)this).Scale);
		API.SetTextColour((int)((Text)this).Color.R, (int)((Text)this).Color.G, (int)((Text)this).Color.B, (int)((Text)this).Color.A);
		if (((Text)this).Shadow)
		{
			API.SetTextDropShadow();
		}
		if (((Text)this).Outline)
		{
			API.SetTextOutline();
		}
		Alignment textAlignment = TextAlignment;
		if ((int)textAlignment != 0)
		{
			if ((int)textAlignment == 2)
			{
				API.SetTextRightJustify(true);
				API.SetTextWrap(0f, num3);
			}
		}
		else
		{
			API.SetTextCentre(true);
		}
		if (Wrap != 0f)
		{
			float num5 = (((Text)this).Position.X + Wrap) / num2;
			API.SetTextWrap(num3, num5);
		}
		API.SetTextEntry("jamyfafi");
		AddLongString(((Text)this).Caption);
		API.DrawText(num3, num4);
	}
}
