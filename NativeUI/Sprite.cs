using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class Sprite
{
	public PointF Position;

	public SizeF Size;

	public Color Color;

	public bool Visible;

	public float Heading;

	public string TextureName;

	private string _textureDict;

	public string TextureDict
	{
		get
		{
			return _textureDict;
		}
		set
		{
			_textureDict = value;
		}
	}

	public Sprite(string textureDict, string textureName, PointF position, SizeF size, float heading, Color color)
	{
		TextureDict = textureDict;
		TextureName = textureName;
		Position = position;
		Size = size;
		Heading = heading;
		Color = color;
		Visible = true;
	}

	public Sprite(string textureDict, string textureName, PointF position, SizeF size)
		: this(textureDict, textureName, position, size, 0f, Color.FromArgb(255, 255, 255, 255))
	{
	}

	public void Draw()
	{
		if (Visible)
		{
			if (!API.HasStreamedTextureDictLoaded(TextureDict))
			{
				API.RequestStreamedTextureDict(TextureDict, true);
			}
			int width = Screen.Resolution.Width;
			int height = Screen.Resolution.Height;
			float num = (float)width / (float)height;
			float num2 = 1080f * num;
			float num3 = Size.Width / num2;
			float num4 = Size.Height / 1080f;
			float num5 = Position.X / num2 + num3 * 0.5f;
			float num6 = Position.Y / 1080f + num4 * 0.5f;
			API.DrawSprite(TextureDict, TextureName, num5, num6, num3, num4, Heading, (int)Color.R, (int)Color.G, (int)Color.B, (int)Color.A);
		}
	}

	public static void Draw(string dict, string name, int xpos, int ypos, int boxWidth, int boxHeight, float rotation, Color color)
	{
		if (!API.HasStreamedTextureDictLoaded(dict))
		{
			API.RequestStreamedTextureDict(dict, true);
		}
		int width = Screen.Resolution.Width;
		int height = Screen.Resolution.Height;
		float num = (float)width / (float)height;
		float num2 = 1080f * num;
		float num3 = (float)boxWidth / num2;
		float num4 = (float)boxHeight / 1080f;
		float num5 = (float)xpos / num2 + num3 * 0.5f;
		float num6 = (float)ypos / 1080f + num4 * 0.5f;
		API.DrawSprite(dict, name, num5, num6, num3, num4, rotation, (int)color.R, (int)color.G, (int)color.B, (int)color.A);
	}

	public static string WriteFileFromResources(Assembly yourAssembly, string fullResourceName)
	{
		string tempFileName = Path.GetTempFileName();
		return WriteFileFromResources(yourAssembly, fullResourceName, tempFileName);
	}

	public static string WriteFileFromResources(Assembly yourAssembly, string fullResourceName, string savePath)
	{
		using (Stream stream = yourAssembly.GetManifestResourceStream(fullResourceName))
		{
			if (stream != null)
			{
				byte[] buffer = new byte[stream.Length];
				stream.Read(buffer, 0, Convert.ToInt32(stream.Length));
				using FileStream fileStream = File.Create(savePath);
				fileStream.Write(buffer, 0, Convert.ToInt32(stream.Length));
				fileStream.Close();
			}
		}
		return Path.GetFullPath(savePath);
	}
}
