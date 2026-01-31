using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Client.Libs;

public class BetterDrawText : Script
{
	private static int currentTickId = 0;

	private static Dictionary<int, int> LastTimeCalled = new Dictionary<int, int>();

	public static float CustomOffsetX { get; set; }

	public static float CustomOffsetY { get; set; }

	public static float CustomOffsetW { get; set; }

	private static int MultipleHashCodes7(object t1, object t2, object t3, object t4, object t5, object t6, object t7)
	{
		return ((((((17 * 23 + t1.GetHashCode()) * 23 + t2.GetHashCode()) * 23 + t3.GetHashCode()) * 23 + t4.GetHashCode()) * 23 + t5.GetHashCode()) * 23 + t6.GetHashCode()) * 23 + t7.GetHashCode();
	}

	public static int GetTextHash(string text, float x, float y, float scale, TextJustification justification, Color text_color)
	{
		return MultipleHashCodes7(text, x + CustomOffsetX, y + CustomOffsetY, scale, justification, text_color, CustomOffsetW).GetHashCode();
	}

	public static void DrawTextThisFrame(string text, float x, float y, float scale, TextJustification justification, Color text_color, float min_x, float max_x, Font font)
	{
		int textHash = GetTextHash(text, x, y, scale, justification, text_color);
		int value = currentTickId;
		if (LastTimeCalled.ContainsKey(textHash))
		{
			LastTimeCalled[textHash] = value;
			return;
		}
		BaseScript.TriggerEvent("gtacnr:hud:sendNuiMessage", new object[1] { new
		{
			component = "drawtext",
			action = "draw",
			id = textHash,
			text = text,
			position = new
			{
				x = (x + CustomOffsetX) * 100f,
				y = (y + CustomOffsetY) * 100f
			},
			min_x = (min_x + CustomOffsetX) * 100f,
			max_x = (max_x + CustomOffsetW) * 100f,
			scale = (double)scale * 5.7,
			justification = justification,
			color = new int[4] { text_color.R, text_color.G, text_color.B, text_color.A },
			font = font
		}.Json() });
		LastTimeCalled.Add(textHash, value);
	}

	[Update]
	public async Coroutine CheckAllTexts()
	{
		await Coroutine.Wait(10uL);
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, int> item in LastTimeCalled)
		{
			if (item.Value + 1 < currentTickId)
			{
				list.Add(item.Key);
			}
		}
		foreach (int item2 in list)
		{
			LastTimeCalled.Remove(item2);
		}
		currentTickId = Game.FrameCount;
		if (list.Count != 0)
		{
			BaseScript.TriggerEvent("gtacnr:hud:sendNuiMessage", new object[1] { new
			{
				component = "drawtext",
				action = "clearm",
				ids = list
			}.Json() });
		}
	}

	[EventHandler("frozen_drawtext_prevention_internal")]
	private void Frozen_DrawText_Prevention_Internal(string json)
	{
		List<int> list = json.Unjson<List<int>>();
		List<int> list2 = new List<int>();
		foreach (int item in list)
		{
			if (!LastTimeCalled.ContainsKey(item))
			{
				list2.Add(item);
			}
		}
		if (list2.Count != 0)
		{
			BaseScript.TriggerEvent("gtacnr:hud:sendNuiMessage", new object[1] { new
			{
				component = "drawtext",
				action = "clearm",
				ids = list2
			}.Json() });
		}
	}
}
