using System.Drawing;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIResRectangle : Rectangle
{
	public UIResRectangle()
	{
	}

	public UIResRectangle(PointF pos, SizeF size)
		: base(pos, size)
	{
	}

	public UIResRectangle(PointF pos, SizeF size, Color color)
		: base(pos, size, color)
	{
	}

	public override void Draw(SizeF offset)
	{
		if (((Rectangle)this).Enabled)
		{
			int width = Screen.Resolution.Width;
			int height = Screen.Resolution.Height;
			float num = (float)width / (float)height;
			float num2 = 1080f * num;
			float num3 = ((Rectangle)this).Size.Width / num2;
			float num4 = ((Rectangle)this).Size.Height / 1080f;
			float num5 = (((Rectangle)this).Position.X + offset.Width) / num2 + num3 * 0.5f;
			float num6 = (((Rectangle)this).Position.Y + offset.Height) / 1080f + num4 * 0.5f;
			API.DrawRect(num5, num6, num3, num4, (int)((Rectangle)this).Color.R, (int)((Rectangle)this).Color.G, (int)((Rectangle)this).Color.B, (int)((Rectangle)this).Color.A);
		}
	}

	public static void Draw(float xPos, float yPos, int boxWidth, int boxHeight, Color color)
	{
		int width = Screen.Resolution.Width;
		int height = Screen.Resolution.Height;
		float num = (float)width / (float)height;
		float num2 = 1080f * num;
		float num3 = (float)boxWidth / num2;
		float num4 = (float)boxHeight / 1080f;
		float num5 = xPos / num2 + num3 * 0.5f;
		float num6 = yPos / 1080f + num4 * 0.5f;
		API.DrawRect(num5, num6, num3, num4, (int)color.R, (int)color.G, (int)color.B, (int)color.A);
	}
}
