using System;
using System.Drawing;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public static class ScreenTools
{
	public static SizeF ResolutionMaintainRatio
	{
		get
		{
			int width = Screen.Resolution.Width;
			int height = Screen.Resolution.Height;
			float num = (float)width / (float)height;
			return new SizeF(1080f * num, 1080f);
		}
	}

	public static Point SafezoneBounds
	{
		get
		{
			double num = Math.Round(Convert.ToDouble(API.GetSafeZoneSize()), 2);
			num = num * 100.0 - 90.0;
			num = 10.0 - num;
			int width = Screen.Resolution.Width;
			int height = Screen.Resolution.Height;
			float num2 = (float)width / (float)height * 5.4f;
			return new Point((int)Math.Round(num * (double)num2), (int)Math.Round(num * 5.400000095367432));
		}
	}

	public static bool IsMouseInBounds(Point topLeft, Size boxSize)
	{
		Game.EnableControlThisFrame(0, (Control)239);
		Game.EnableControlThisFrame(0, (Control)240);
		SizeF resolutionMaintainRatio = ResolutionMaintainRatio;
		int num = (int)Math.Round(API.GetDisabledControlNormal(0, 239) * resolutionMaintainRatio.Width);
		int num2 = (int)Math.Round(API.GetDisabledControlNormal(0, 240) * resolutionMaintainRatio.Height);
		bool num3 = num >= topLeft.X && num <= topLeft.X + boxSize.Width;
		bool flag = num2 > topLeft.Y && num2 < topLeft.Y + boxSize.Height;
		return num3 && flag;
	}

	public static bool IsMouseInBounds(PointF topLeft, SizeF boxSize)
	{
		Game.EnableControlThisFrame(0, (Control)239);
		Game.EnableControlThisFrame(0, (Control)240);
		SizeF resolutionMaintainRatio = ResolutionMaintainRatio;
		float num = API.GetDisabledControlNormal(0, 239) * resolutionMaintainRatio.Width;
		float num2 = API.GetDisabledControlNormal(0, 240) * resolutionMaintainRatio.Height;
		bool num3 = num >= topLeft.X && num <= topLeft.X + boxSize.Width;
		bool flag = num2 > topLeft.Y && num2 < topLeft.Y + boxSize.Height;
		return num3 && flag;
	}

	public static bool IsMouseInBounds(Point topLeft, Size boxSize, Point DrawOffset)
	{
		Game.EnableControlThisFrame(0, (Control)239);
		Game.EnableControlThisFrame(0, (Control)240);
		SizeF resolutionMaintainRatio = ResolutionMaintainRatio;
		int num = (int)Math.Round(API.GetDisabledControlNormal(0, 239) * resolutionMaintainRatio.Width);
		int num2 = (int)Math.Round(API.GetDisabledControlNormal(0, 240) * resolutionMaintainRatio.Height);
		num += DrawOffset.X;
		num2 += DrawOffset.Y;
		if (num >= topLeft.X && num <= topLeft.X + boxSize.Width)
		{
			if (num2 > topLeft.Y)
			{
				return num2 < topLeft.Y + boxSize.Height;
			}
			return false;
		}
		return false;
	}

	public static bool IsMouseInBounds(PointF topLeft, SizeF boxSize, PointF DrawOffset)
	{
		Game.EnableControlThisFrame(0, (Control)239);
		Game.EnableControlThisFrame(0, (Control)240);
		SizeF resolutionMaintainRatio = ResolutionMaintainRatio;
		float num = API.GetDisabledControlNormal(0, 239) * resolutionMaintainRatio.Width;
		float num2 = API.GetDisabledControlNormal(0, 240) * resolutionMaintainRatio.Height;
		num += DrawOffset.X;
		num2 += DrawOffset.Y;
		if (num >= topLeft.X && num <= topLeft.X + boxSize.Width)
		{
			if (num2 > topLeft.Y)
			{
				return num2 < topLeft.Y + boxSize.Height;
			}
			return false;
		}
		return false;
	}

	public static float GetTextWidth(string text, Font font, float scale)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected I4, but got Unknown
		API.SetTextEntryForWidth("CELL_EMAIL_BCON");
		UIResText.AddLongString(text);
		API.SetTextFont((int)font);
		API.SetTextScale(1f, scale);
		float textScreenWidth = API.GetTextScreenWidth(true);
		return ResolutionMaintainRatio.Width * textScreenWidth;
	}

	public static int GetLineCount(string text, Point position, Font font, float scale, int wrap)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected I4, but got Unknown
		API.SetTextGxtEntry("CELL_EMAIL_BCON");
		UIResText.AddLongStringForUtf8(text);
		SizeF resolutionMaintainRatio = ResolutionMaintainRatio;
		float num = (float)position.X / resolutionMaintainRatio.Width;
		float num2 = (float)position.Y / resolutionMaintainRatio.Height;
		API.SetTextFont((int)font);
		API.SetTextScale(1f, scale);
		if (wrap > 0)
		{
			float num3 = (float)position.X / resolutionMaintainRatio.Width + (float)wrap / resolutionMaintainRatio.Width;
			API.SetTextWrap(num, num3);
		}
		return API.GetTextScreenLineCount(num, num2);
	}

	public static int GetLineCount(string text, PointF position, Font font, float scale, float wrap)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected I4, but got Unknown
		API.SetTextGxtEntry("CELL_EMAIL_BCON");
		UIResText.AddLongStringForUtf8(text);
		SizeF resolutionMaintainRatio = ResolutionMaintainRatio;
		float num = position.X / resolutionMaintainRatio.Width;
		float num2 = position.Y / resolutionMaintainRatio.Height;
		API.SetTextFont((int)font);
		API.SetTextScale(1f, scale);
		if (wrap > 0f)
		{
			float num3 = position.X / resolutionMaintainRatio.Width + wrap / resolutionMaintainRatio.Width;
			API.SetTextWrap(num, num3);
		}
		return API.GetTextScreenLineCount(num, num2);
	}
}
