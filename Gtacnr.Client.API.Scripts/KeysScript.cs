using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;

namespace Gtacnr.Client.API.Scripts;

public class KeysScript : Script
{
	public static readonly TimeSpan HOLD_TIME = TimeSpan.FromMilliseconds(400.0);

	public static readonly TimeSpan DOUBLE_PRESS_TIME = TimeSpan.FromMilliseconds(500.0);

	private static Dictionary<Control, List<KeyEventHandler>> handlers = new Dictionary<Control, List<KeyEventHandler>>();

	private static Dictionary<KeyEventHandler, int> handlerPriority = new Dictionary<KeyEventHandler, int>();

	private static Dictionary<Control, DateTime> lastPressTime = new Dictionary<Control, DateTime>();

	public static void AttachListener(Control control, KeyEventHandler handler, int priority = 0)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (!handlers.ContainsKey(control))
		{
			handlers[control] = new List<KeyEventHandler>();
		}
		if (!handlers[control].Contains(handler))
		{
			handlers[control].Add(handler);
		}
		handlerPriority[handler] = priority;
		if (handlers[control].Count > 1)
		{
			handlers[control] = handlers[control].OrderByDescending((KeyEventHandler h) => handlerPriority.ContainsKey(h) ? handlerPriority[h] : 0).ToList();
		}
	}

	public static void AttachListener(IEnumerable<Control> controls, KeyEventHandler handler, int priority = 0)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		foreach (Control control in controls)
		{
			AttachListener(control, handler, priority);
		}
	}

	public static void DetachListener(Control control, KeyEventHandler handler)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (!handlers.ContainsKey(control))
		{
			return;
		}
		if (handlers[control].Contains(handler))
		{
			handlers[control].Remove(handler);
			if (handlerPriority.ContainsKey(handler))
			{
				handlerPriority.Remove(handler);
			}
		}
		if (handlers[control].Count == 0)
		{
			handlers.Remove(control);
		}
	}

	[Update]
	private async Coroutine DetectKeysTask()
	{
		foreach (Control item in handlers.Keys.ToList())
		{
			Control control = item;
			InputType inputType = ((!Utils.IsUsingKeyboard()) ? InputType.Controller : InputType.Keyboard);
			DateTime pressTimestamp;
			if (Game.IsControlJustPressed(2, control))
			{
				FireEvent(KeyEventType.JustPressed);
				pressTimestamp = DateTime.UtcNow;
				CheckHoldGesture();
				if (!lastPressTime.ContainsKey(control))
				{
					lastPressTime[control] = DateTime.MinValue;
				}
				if (!Gtacnr.Utils.CheckTimePassed(lastPressTime[control], DOUBLE_PRESS_TIME))
				{
					lastPressTime[control] = DateTime.MinValue;
					FireEvent(KeyEventType.DoublePressed);
				}
				else
				{
					lastPressTime[control] = DateTime.UtcNow;
				}
			}
			else if (Game.IsControlJustReleased(2, control))
			{
				FireEvent(KeyEventType.JustReleased);
			}
			async void CheckHoldGesture()
			{
				while (Game.IsControlPressed(2, control))
				{
					await BaseScript.Delay(0);
					if (Gtacnr.Utils.CheckTimePassed(pressTimestamp, HOLD_TIME))
					{
						FireEvent(KeyEventType.Held);
						break;
					}
				}
			}
			void FireEvent(KeyEventType eventType)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				//IL_0041: Unknown result type (might be due to invalid IL or missing references)
				//IL_004b: Expected I4, but got Unknown
				foreach (KeyEventHandler item2 in handlers[control])
				{
					if (item2(control, eventType, inputType))
					{
						BaseScript.TriggerEvent("gtacnr:keys:event", new object[3]
						{
							(int)control,
							(int)eventType,
							(int)inputType
						});
						break;
					}
				}
			}
		}
	}
}
