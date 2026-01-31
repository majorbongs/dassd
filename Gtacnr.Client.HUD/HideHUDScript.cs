using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Vehicles;

namespace Gtacnr.Client.HUD;

public class HideHUDScript : Script
{
	public static bool EnableGamerTags = true;

	public static bool EnableChat = true;

	public static bool EnableHUD = true;

	public static bool EnableMessages = true;

	public static bool EnableReticle = true;

	public static bool _showPlayerNameTagsOnBlips = false;

	public static bool ShowPlayerNameTagsOnBlips
	{
		get
		{
			return _showPlayerNameTagsOnBlips;
		}
		set
		{
			_showPlayerNameTagsOnBlips = value;
			API.DisplayPlayerNameTagsOnBlips(_showPlayerNameTagsOnBlips);
		}
	}

	public static bool ScreenshotMode
	{
		get
		{
			if (!EnableGamerTags && !EnableChat && !EnableHUD)
			{
				return !EnableMessages;
			}
			return false;
		}
		set
		{
			if (value && !VehicleSlideshowScript.IsInSlideshow)
			{
				Utils.DisplayHelpText("You enabled ~p~immersive mode~s~. Press ~INPUT_DROP_WEAPON~ to disable it.", playSound: false, 4000);
			}
			else
			{
				EnableMessages = true;
				Utils.DisplayHelpText("You disabled ~p~immersive mode~s~.", playSound: false, 2000);
			}
			EnableGamerTags = !value;
			EnableChat = !value;
			EnableMessages = !value;
			EnableHUD = !value;
			Utils.PlayContinueSound();
			BaseScript.TriggerEvent("gtacnr:screenshotModeChanged", new object[0]);
			BaseScript.TriggerEvent("gtacnr:chat:toggle", new object[1] { EnableChat });
			BaseScript.TriggerEvent("gtacnr:hud:toggle", new object[1] { EnableHUD });
			API.DisplayRadar(EnableHUD);
		}
	}

	protected override void OnStarted()
	{
		KeysScript.AttachListener((Control)56, OnKeyEvent, 1000);
		KeysScript.AttachListener((Control)57, OnKeyEvent, 1000);
		EnableReticle = Preferences.ReticleEnabled.Get();
		ShowPlayerNameTagsOnBlips = Preferences.ShowPlayerNameTagsOnBlips.Get();
	}

	public static void ToggleChat(bool toggle, bool showMessage = true)
	{
		EnableChat = toggle;
		BaseScript.TriggerEvent("gtacnr:screenshotModeChanged", new object[0]);
		BaseScript.TriggerEvent("gtacnr:chat:toggle", new object[1] { EnableChat });
		if (showMessage)
		{
			if (!EnableChat)
			{
				Utils.DisplayHelpText("You've ~p~hidden the chat~s~. Press ~INPUT_DROP_AMMO~ to restore it.", playSound: false, 4000);
			}
			else
			{
				Utils.DisplayHelpText("You've ~p~restored the chat~s~. Press ~INPUT_DROP_AMMO~ to hide it.", playSound: false, 2000);
			}
			Utils.PlayContinueSound();
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		if (eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard)
		{
			if ((int)control == 56)
			{
				ScreenshotMode = !ScreenshotMode;
			}
			else if ((int)control == 57)
			{
				ToggleChat(!EnableChat);
			}
			return true;
		}
		return false;
	}

	[Update]
	private async Coroutine ControlsTick()
	{
		if (!EnableHUD)
		{
			API.HideAreaAndVehicleNameThisFrame();
		}
		if (!EnableReticle)
		{
			API.HideHudComponentThisFrame(14);
		}
	}
}
