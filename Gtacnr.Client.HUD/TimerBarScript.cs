using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using NativeUI;

namespace Gtacnr.Client.HUD;

public class TimerBarScript : Script
{
	private bool isDrawTaskAttached;

	private static TimerBarScript instance;

	private static List<TimerBarBase> timerBars = new List<TimerBarBase>();

	public static IEnumerable<TimerBarBase> TimerBars => timerBars;

	public TimerBarScript()
	{
		instance = this;
	}

	public static bool AddTimerBar(TimerBarBase timerBar, int position = -1)
	{
		return instance.AddTimerBarInternal(timerBar, position);
	}

	public static bool RemoveTimerBar(TimerBarBase timerBar)
	{
		return instance.RemoveTimerBarInternal(timerBar);
	}

	private bool AddTimerBarInternal(TimerBarBase timerBar, int position = -1)
	{
		if (timerBars.Contains(timerBar))
		{
			return false;
		}
		timerBar.Label = timerBar.Label.AddFontTags();
		if (timerBar is TextTimerBar textTimerBar)
		{
			textTimerBar.Text = textTimerBar.Text.AddFontTags();
		}
		if (position == -1)
		{
			timerBars.Add(timerBar);
		}
		else
		{
			timerBars.Insert(position, timerBar);
		}
		AttachDrawTask();
		return true;
	}

	private bool RemoveTimerBarInternal(TimerBarBase timerBar)
	{
		if (!timerBars.Contains(timerBar))
		{
			return false;
		}
		if (timerBars.Remove(timerBar) && timerBars.Count == 0)
		{
			DetachDrawTask();
		}
		return true;
	}

	private void AttachDrawTask()
	{
		if (!isDrawTaskAttached)
		{
			isDrawTaskAttached = true;
			base.Update += DrawTask;
		}
	}

	private void DetachDrawTask()
	{
		if (isDrawTaskAttached)
		{
			isDrawTaskAttached = false;
			base.Update -= DrawTask;
		}
	}

	private async Coroutine DrawTask()
	{
		int num = 0;
		bool flag = MenuController.IsAnyMenuOpen() || InstructionalButtonsScript.AreInstructionsActive || API.IsHudComponentActive(9);
		bool num2 = API.IsHudComponentActive(7);
		bool flag2 = API.IsHudComponentActive(6);
		if (flag)
		{
			num += 13;
		}
		if (num2)
		{
			num += ((!flag) ? 22 : 9);
		}
		if (flag2)
		{
			num += ((!flag) ? 28 : 15);
		}
		foreach (TimerBarBase timerBar in timerBars)
		{
			timerBar.Draw(num);
			num += 10;
		}
	}

	public static T SetTimerBar<T>(T currentValue, T newValue) where T : TimerBarBase
	{
		if (!object.Equals(newValue, currentValue) && currentValue != null)
		{
			RemoveTimerBar(currentValue);
		}
		return newValue;
	}
}
