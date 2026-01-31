using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API.UI;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.API.Scripts;

public static class InteractiveNotificationsScript
{
	private static string defaultKeyboardMessage = "Accept";

	private static string defaultControllerMessage = "Accept (hold)";

	private static Control keyboardControl = (Control)246;

	private static Control controllerControl = (Control)303;

	private static Func<bool>? previousEventHandler = null;

	private static uint previousPriority = 0u;

	public static bool IsAnyNotificationInProgress => previousEventHandler != null;

	public static bool IsAnyNotificationWithHigherPriorityActive(uint priority)
	{
		return previousPriority > priority;
	}

	public static async Task Show(string? message, InteractiveNotificationType type, Func<bool> onAccept, TimeSpan? timeout = null, uint priority = 0u, string? keyboardMessage = null, string? controllerMessage = null, Func<bool>? notificationEndCondition = null, Color? notificationBgColor = null)
	{
		keyboardMessage = keyboardMessage ?? defaultKeyboardMessage;
		controllerMessage = controllerMessage ?? defaultControllerMessage;
		TimeSpan timeout2 = timeout ?? TimeSpan.FromSeconds(5.0);
		if (previousEventHandler == null || priority >= previousPriority)
		{
			RemovePreviousHandler();
			previousEventHandler = onAccept;
			previousPriority = priority;
			switch (type)
			{
			case InteractiveNotificationType.HelpText:
				Utils.DisplayHelpText(message, playSound: true, (int)timeout2.TotalMilliseconds);
				break;
			case InteractiveNotificationType.Notification:
				Utils.SendNotification(message, notificationBgColor);
				break;
			case InteractiveNotificationType.Subtitle:
				Utils.DisplaySubtitle(message, (int)timeout2.TotalMilliseconds);
				break;
			}
			EnableControls(keyboardMessage, controllerMessage);
			await WaitUnlessCondition(timeout2, () => notificationEndCondition?.Invoke() ?? false);
			if ((Delegate)previousEventHandler == (Delegate)onAccept)
			{
				Cleanup();
			}
		}
	}

	private static void Cleanup()
	{
		DisableControls();
		RemovePreviousHandler();
		previousPriority = 0u;
	}

	private static void RemovePreviousHandler()
	{
		if (previousEventHandler != null)
		{
			previousEventHandler = null;
		}
	}

	private static async Task WaitUnlessCondition(TimeSpan timeout, Func<bool> condition)
	{
		DateTime startTime = DateTime.UtcNow;
		while (!Gtacnr.Utils.CheckTimePassed(startTime, timeout) && !condition())
		{
			await BaseScript.Delay(100);
		}
	}

	private static void EnableControls(string keyboardMessage, string controllerMessage)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		KeysScript.AttachListener(keyboardControl, OnKeyEvent);
		KeysScript.AttachListener(controllerControl, OnKeyEvent);
		if (Utils.IsUsingKeyboard())
		{
			Utils.AddInstructionalButton("interactiveNotification", new InstructionalButton(keyboardMessage, 2, keyboardControl));
		}
		else
		{
			Utils.AddInstructionalButton("interactiveNotification", new InstructionalButton(controllerMessage, 2, controllerControl));
		}
	}

	private static void DisableControls()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		KeysScript.DetachListener(keyboardControl, OnKeyEvent);
		KeysScript.DetachListener(controllerControl, OnKeyEvent);
		Utils.RemoveInstructionalButton("interactiveNotification");
	}

	private static bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		bool num = control == keyboardControl && eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard;
		bool flag = control == controllerControl && eventType == KeyEventType.Held && inputType == InputType.Controller;
		if (num || flag)
		{
			bool result = previousEventHandler?.Invoke() ?? false;
			Cleanup();
			return result;
		}
		return false;
	}
}
