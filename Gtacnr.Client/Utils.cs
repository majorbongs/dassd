using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Jobs;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Client.Vehicles.Fuel;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client;

public static class Utils
{
	public struct MinimapInfo
	{
		public float X { get; set; }

		public float Y { get; set; }

		public float Width { get; set; }

		public float Height { get; set; }

		public float Bottom { get; set; }

		public float Left { get; set; }

		public float Top { get; set; }

		public float Right { get; set; }

		public float UnitX { get; set; }

		public float UnitY { get; set; }
	}

	[Flags]
	public enum TeleportFlags
	{
		None = 0,
		TeleportVehicle = 1,
		FindGroundHeight = 2,
		PlaceOnGround = 4,
		VisualEffects = 8
	}

	private static Random random = new Random();

	private static List<PrefabAppearance> faces = Gtacnr.Utils.LoadJson<List<PrefabAppearance>>("data/faces.json");

	public static Dictionary<int, KeyValuePair<string, string>> HairOverlays = new Dictionary<int, KeyValuePair<string, string>>
	{
		{
			0,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a")
		},
		{
			1,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			2,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			3,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_003_a")
		},
		{
			4,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			5,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			6,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			7,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			8,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_008_a")
		},
		{
			9,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			10,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			11,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			12,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			13,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			14,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_long_a")
		},
		{
			15,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_long_a")
		},
		{
			16,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z")
		},
		{
			17,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a")
		},
		{
			18,
			new KeyValuePair<string, string>("mpbusiness_overlays", "FM_Bus_M_Hair_000_a")
		},
		{
			19,
			new KeyValuePair<string, string>("mpbusiness_overlays", "FM_Bus_M_Hair_001_a")
		},
		{
			20,
			new KeyValuePair<string, string>("mphipster_overlays", "FM_Hip_M_Hair_000_a")
		},
		{
			21,
			new KeyValuePair<string, string>("mphipster_overlays", "FM_Hip_M_Hair_001_a")
		},
		{
			22,
			new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a")
		}
	};

	public static readonly int FreemodeMale = Game.GenerateHash("mp_m_freemode_01");

	public static readonly int FreemodeFemale = Game.GenerateHash("mp_f_freemode_01");

	private static readonly Dictionary<string, Apparel> tempApparels = new Dictionary<string, Apparel>();

	public static string MENU_ARROW = "›";

	public static bool IsOnScreenKeyboardActive = false;

	private static string lastHelpText;

	private static string[]? _draw3DSingleString = new string[1];

	private static MinimapInfo? storedMinimapInfo = null;

	private static bool isMinimapVisible;

	private static bool isScreenFadedOut;

	private static readonly HashSet<WeaponHash> nonWeapons = new HashSet<WeaponHash>
	{
		(WeaponHash)(-1569615261),
		(WeaponHash)1198879012,
		(WeaponHash)(-1951375401),
		(WeaponHash)101631238,
		(WeaponHash)126349499,
		(WeaponHash)(-72657034),
		(WeaponHash)966099553
	};

	private static readonly string[] seat_windows_bones = new string[4] { "window_lf", "window_rf", "window_lr", "window_rr" };

	public static WeaponHash[] WeaponHashes;

	public static bool IsFrozen { get; private set; }

	public static bool IsInvisible { get; private set; }

	public static bool IsTeleporting { get; private set; }

	public static void ClearPedDamage(Ped ped, bool blood = true, bool wetness = true, bool dirt = true)
	{
		if (blood)
		{
			API.ClearPedBloodDamage(((PoolObject)ped).Handle);
		}
		if (wetness)
		{
			API.ClearPedWetness(((PoolObject)ped).Handle);
		}
		if (dirt)
		{
			API.ClearPedEnvDirt(((PoolObject)ped).Handle);
		}
		API.ResetPedVisibleDamage(((PoolObject)ped).Handle);
	}

	public static void ApplyAppearance(Ped ped, Appearance appearance)
	{
		int mother = appearance.Heritage.Mother;
		int father = appearance.Heritage.Father;
		float shapeMix = appearance.Heritage.ShapeMix;
		float skinMix = appearance.Heritage.SkinMix;
		API.SetPedHeadBlendData(((PoolObject)ped).Handle, mother, father, 0, mother, father, 0, shapeMix, skinMix, 0f, true);
		foreach (FaceFeature faceFeature in appearance.FaceFeatures)
		{
			API.SetPedFaceFeature(((PoolObject)ped).Handle, faceFeature.Index, faceFeature.Scale);
		}
		foreach (ComponentVariation componentVariation in appearance.ComponentVariations)
		{
			API.SetPedComponentVariation(((PoolObject)ped).Handle, componentVariation.Index, componentVariation.Drawable, componentVariation.Texture, componentVariation.Palette);
		}
		foreach (HeadOverlay headOverlay in appearance.HeadOverlays)
		{
			int num = ((headOverlay.Index != 8) ? 1 : 2);
			API.SetPedHeadOverlay(((PoolObject)ped).Handle, headOverlay.Index, headOverlay.Overlay, headOverlay.Opacity);
			API.SetPedHeadOverlayColor(((PoolObject)ped).Handle, headOverlay.Index, num, headOverlay.Color, 0);
		}
		API.SetPedHairColor(((PoolObject)ped).Handle, appearance.HairColor, 0);
		API.SetPedEyeColor(((PoolObject)ped).Handle, appearance.EyeColor);
	}

	public static PrefabAppearance GetRandomPrefabFace(Sex sex)
	{
		List<PrefabAppearance> list = faces.Where((PrefabAppearance f) => f.Id.EndsWith((sex == Sex.Male) ? "m" : "f")).ToList();
		int index = random.Next(list.Count);
		return list[index];
	}

	public static PrefabAppearance GetRandomPrefabFace(Ped ped)
	{
		if (((Entity)ped).Model.Hash != FreemodeMale)
		{
			return GetRandomPrefabFace(Sex.Female);
		}
		return GetRandomPrefabFace(Sex.Male);
	}

	public static bool IsFreemodePed(Ped ped)
	{
		if (((Entity)ped).Model.Hash != FreemodeMale)
		{
			return ((Entity)ped).Model.Hash == FreemodeFemale;
		}
		return true;
	}

	public static Sex GetFreemodePedSex(Ped ped)
	{
		if (((Entity)ped).Model.Hash != FreemodeMale)
		{
			if (((Entity)ped).Model.Hash != FreemodeFemale)
			{
				return (Sex)(-1);
			}
			return Sex.Female;
		}
		return Sex.Male;
	}

	public static Model GetFreemodePedModelFromSex(Sex sex)
	{
		return Model.op_Implicit(sex switch
		{
			Sex.Female => FreemodeFemale, 
			Sex.Male => FreemodeMale, 
			_ => -1, 
		});
	}

	public static void StoreCurrentOutfit(string tempId = "")
	{
		if (tempId == null)
		{
			tempId = "";
		}
		tempApparels[tempId] = new Apparel(Clothes.CurrentApparel);
	}

	public static void RestoreOutfit(string tempId = "")
	{
		if (tempId == null)
		{
			tempId = "";
		}
		Apparel apparel = tempApparels.TryGetRefOrNull(tempId);
		if (apparel != null)
		{
			Clothes.CurrentApparel = apparel;
			tempApparels.Remove(tempId);
		}
	}

	public static Apparel? GetTempApparel(string tempId = "")
	{
		if (tempId == null)
		{
			tempId = "";
		}
		return tempApparels.TryGetRefOrNull(tempId);
	}

	public static void PreviewClothingItem(ClothingItem clothingItem)
	{
		Clothes.CurrentApparel.Add(clothingItem);
	}

	public static async Task<string> GetUserInput(string title, string text, string placeholder, int maxInputLength, string type = "text", string defaultText = null)
	{
		try
		{
			if (IsOnScreenKeyboardActive)
			{
				return null;
			}
			IsOnScreenKeyboardActive = true;
			return await NuiDialogScript.Show(new NuiDialog(title, text, placeholder)
			{
				MaxLength = maxInputLength,
				InputType = type,
				DefaultText = defaultText
			});
		}
		finally
		{
			IsOnScreenKeyboardActive = false;
		}
	}

	public static bool IsUsingKeyboard()
	{
		return API.IsInputDisabled(2);
	}

	public static async Task<bool> IsControlHeld(int index, Control control, int time = 500)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (Game.IsControlJustPressed(index, control))
		{
			DateTime pressTimestamp = DateTime.UtcNow;
			while (Game.IsControlPressed(index, control))
			{
				await Delay();
				if (Gtacnr.Utils.CheckTimePassed(pressTimestamp, time))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static async Task<bool> IsControlHeld(int index, int control, int time = 500)
	{
		return await IsControlHeld(index, (Control)control, time);
	}

	public static string AddFontTags(this string s)
	{
		switch (LocalizationController.CurrentLanguage)
		{
		case "vi-VN":
		case "tr-TR":
		case "sv-SE":
		case "no-NO":
		case "lv-LV":
		case "cs-CZ":
		case "pl-PL":
		case "ru-RU":
			return "<font face='Fire Sans'>" + s + "</font>";
		default:
			return s;
		}
	}

	public static async void DisplaySubtitle(string subtitle, int time = 5000)
	{
		if (!string.IsNullOrWhiteSpace(subtitle) && HideHUDScript.EnableMessages)
		{
			Debug.WriteLine("Mission: " + subtitle.GTAUIToF8());
			while (API.IsScreenFadedOut())
			{
				await Delay();
			}
			API.ClearPrints();
			API.SetTextEntry_2("STRING");
			API.AddTextComponentString(subtitle.AddFontTags());
			API.DrawSubtitleTimed(time, true);
		}
	}

	public static async void DisplayHelpText(string text = null, bool playSound = true, int clearAfter = 0)
	{
		if (!HideHUDScript.EnableMessages)
		{
			return;
		}
		if (string.IsNullOrEmpty(text))
		{
			API.SetTextComponentFormat("STRING");
			API.AddTextComponentString("_");
			API.DisplayHelpTextFromStringLabel(0, false, false, 1);
			return;
		}
		Debug.WriteLine("Help: " + text.GTAUIToF8());
		while (API.IsScreenFadedOut() || API.IsPlayerSwitchInProgress())
		{
			await Delay();
		}
		API.AddTextEntry("gtacnrHelpText", text.AddFontTags());
		API.BeginTextCommandDisplayHelp("gtacnrHelpText");
		API.EndTextCommandDisplayHelp(0, false, playSound, -1);
		lastHelpText = text;
		if (clearAfter > 0)
		{
			await Delay(clearAfter);
			if (lastHelpText == text)
			{
				DisplayHelpText();
			}
		}
	}

	public static async void SendNotification(string text, Color? color = null)
	{
		if (HideHUDScript.EnableMessages)
		{
			Debug.WriteLine("Notification: " + text.GTAUIToF8());
			while (API.IsScreenFadedOut() || IsSwitchInProgress())
			{
				await Delay();
			}
			if (!color.HasValue)
			{
				color = new Color(0, 0, 0);
			}
			BaseScript.TriggerEvent("gtacnr:hud:sendNotification", new object[4]
			{
				text,
				color.Value.R,
				color.Value.G,
				color.Value.B
			});
		}
	}

	public static void DisplayErrorMessage(int code = 0, int subCode = -1, string message = null, bool playSound = true)
	{
		if (code == 0)
		{
			DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR) + " " + (message ?? LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8)), playSound: false);
		}
		else if (subCode == -1)
		{
			DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x{code:X2}") + " " + message, playSound: false);
		}
		else
		{
			DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x{code:X2}-{subCode}") + " " + message, playSound: false);
		}
		if (playSound)
		{
			PlayErrorSound();
		}
	}

	public static void DisplayError(ResponseCode responseCode, string message = "", [CallerMemberName] string member = "")
	{
		if ((int)StaffLevelScript.StaffLevel > 0)
		{
			string description = Gtacnr.Utils.GetDescription(responseCode);
			Debug.WriteLine($"^1{member}()^0: {(int)responseCode} - {responseCode} - {description}");
			message = message + " " + LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8);
		}
		DisplayHelpText((LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"{(int)responseCode}") + "\n~s~" + message).Trim(), playSound: false, 4000);
		PlayErrorSound();
	}

	public static string GTAUIToF8(this string input)
	{
		return input.Replace("~r~", "^1").Replace("~g~", "^2").Replace("~y~", "^3")
			.Replace("~o~", "^3")
			.Replace("~b~", "^4")
			.Replace("~p~", "^6")
			.Replace("~w~", "^0")
			.Replace("~s~", "^0")
			.Replace("~n~", "\n")
			.Replace("<C>", "")
			.Replace("</C>", "");
	}

	public static string GTAUIToCnRChat(this string input)
	{
		return input.Replace("~r~", "{red}").Replace("~b~", "{blue}").Replace("~g~", "{green}")
			.Replace("~y~", "{yellow}")
			.Replace("~p~", "{purple}")
			.Replace("~q~", "{pink}")
			.Replace("~o~", "{orange}")
			.Replace("~c~", "{grey}")
			.Replace("~s~", "{white}")
			.Replace("~w~", "{white}");
	}

	public static string CnRChatToGTAUI(this string input)
	{
		return input.Replace("{red}", "~r~").Replace("{blue}", "~b~").Replace("{green}", "~g~")
			.Replace("{yellow}", "~y~")
			.Replace("{purple}", "~p~")
			.Replace("{pink}", "~q~")
			.Replace("{orange}", "~o~")
			.Replace("{grey}", "~c~")
			.Replace("{white}", "~s~")
			.Replace("{white}", "~w~");
	}

	public static void AddInstructionalButton(string key, InstructionalButton button)
	{
		InstructionalButtonsScript.AddInstructionalButton(key, button);
	}

	public static void RemoveInstructionalButton(string key)
	{
		InstructionalButtonsScript.RemoveInstructionalButton(key);
	}

	public static void ClearAllInstructionalButtons()
	{
		InstructionalButtonsScript.ClearInstructionalButtons();
	}

	public static async Task<bool> ShowConfirm(string message, string title = null, TimeSpan? forceReadTime = null, int buttonsAll = 20, int buttonsDisabled = 16)
	{
		if (Game.PlayerPed.IsInVehicle() && (int)Game.PlayerPed.SeatIndex == -1)
		{
			Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
			DateTime t2 = DateTime.UtcNow;
			TimeSpan max = TimeSpan.FromSeconds(5.0);
			API.TaskVehicleTempAction(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)vehicle).Handle, 6, (int)max.TotalMilliseconds);
			while (API.GetEntitySpeed(((PoolObject)vehicle).Handle) > 1f && !Gtacnr.Utils.CheckTimePassed(t2, max.TotalMilliseconds))
			{
				Game.DisableAllControlsThisFrame(2);
				await Delay();
			}
			API.ClearVehicleTasks(((PoolObject)vehicle).Handle);
		}
		Game.PlaySound("EXIT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
		if (title == null)
		{
			title = LocalizationController.S(Entries.Main.ALERT);
		}
		API.SetTextEntry("STRING");
		API.AddTextEntry("CONFIRM_TITLE", title.AddFontTags());
		API.AddTextEntry("CONFIRM_CONTENT", message.AddFontTags());
		DateTime t3 = DateTime.UtcNow;
		if (!forceReadTime.HasValue)
		{
			forceReadTime = TimeSpan.FromSeconds(1.0);
		}
		do
		{
			await Delay();
			bool flag = !forceReadTime.HasValue || Gtacnr.Utils.CheckTimePassed(t3, forceReadTime.Value);
			API.DrawFrontendAlert("CONFIRM_TITLE", "CONFIRM_CONTENT", flag ? buttonsAll : buttonsDisabled, 0, "", 0, 0, 0, "FM_NXT_RAC", "1", true, 0);
			if (flag && (API.IsControlJustReleased(13, 201) || API.IsControlJustReleased(13, 203)))
			{
				Game.PlaySound("OK", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				return true;
			}
		}
		while (!API.IsControlJustReleased(13, 202));
		Game.PlaySound("OK", "HUD_FRONTEND_DEFAULT_SOUNDSET");
		return false;
	}

	public static async Task ShowAlert(string message, string title = null, TimeSpan? forceReadTime = null)
	{
		if (Game.PlayerPed.IsInVehicle() && (int)Game.PlayerPed.SeatIndex == -1)
		{
			Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
			DateTime t2 = DateTime.UtcNow;
			TimeSpan max = TimeSpan.FromSeconds(5.0);
			API.TaskVehicleTempAction(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)vehicle).Handle, 6, (int)max.TotalMilliseconds);
			while (API.GetEntitySpeed(((PoolObject)vehicle).Handle) > 1f && !Gtacnr.Utils.CheckTimePassed(t2, max.TotalMilliseconds))
			{
				Game.DisableAllControlsThisFrame(2);
				await Delay();
			}
			API.ClearVehicleTasks(((PoolObject)vehicle).Handle);
		}
		Game.PlaySound("EXIT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
		if (title == null)
		{
			title = LocalizationController.S(Entries.Main.ALERT);
		}
		API.SetTextEntry("STRING");
		API.AddTextEntry("ALERT_TITLE", title.AddFontTags());
		API.AddTextEntry("ALERT_CONTENT", message.AddFontTags());
		DateTime t3 = DateTime.UtcNow;
		if (!forceReadTime.HasValue)
		{
			forceReadTime = TimeSpan.FromSeconds(2.0);
		}
		bool flag;
		do
		{
			await Delay();
			flag = !forceReadTime.HasValue || Gtacnr.Utils.CheckTimePassed(t3, forceReadTime.Value);
			API.DrawFrontendAlert("ALERT_TITLE", "ALERT_CONTENT", flag ? 2 : 0, 0, "", 0, 0, 0, "FM_NXT_RAC", "1", true, 0);
		}
		while (!flag || (!API.IsControlJustReleased(13, 201) && !API.IsControlJustReleased(13, 202)));
		Game.PlaySound("OK", "HUD_FRONTEND_DEFAULT_SOUNDSET");
	}

	public static void PlaySelectSound()
	{
		API.PlaySoundFrontend(0, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
	}

	public static void PlayContinueSound()
	{
		API.PlaySoundFrontend(0, "CONTINUE", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
	}

	public static void PlayPurchaseSound()
	{
		API.PlaySoundFrontend(0, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", false);
	}

	public static void PlayErrorSound()
	{
		API.PlaySoundFrontend(0, "ERROR", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
	}

	public static void PlayNavSound()
	{
		API.PlaySoundFrontend(0, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", false);
	}

	public static async Task ShakeGamepad(int duration = 500, int intensity = 200)
	{
		if (!IsUsingKeyboard())
		{
			API.SetPadShake(0, duration, intensity);
			await Delay(duration);
			API.StopPadShake(0);
		}
	}

	public static MenuItem GetSpacerMenuItem(string title, string description = null)
	{
		string text = "~h~";
		int length = title.Length;
		int num = 80 - length;
		for (int i = 0; i < num / 2 - length / 2; i++)
		{
			text += " ";
		}
		text += title;
		return new MenuItem(text, description ?? "")
		{
			Enabled = false,
			PlaySelectSound = false,
			PlayErrorSound = false
		};
	}

	public static void Draw2DText(string text, Vector2 position, Color? color = null, float scale = 1f, int font = 4, Alignment justification = (Alignment)2, bool drawOutline = true, Color? shadowColor = null, int shadowLength = 0)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected I4, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		if (API.IsHudPreferenceSwitchedOn() && Hud.IsVisible && !API.IsPlayerSwitchInProgress() && API.IsScreenFadedIn() && !API.IsPauseMenuActive() && !API.IsFrontendFading() && !API.IsPauseMenuRestarting() && !API.IsHudHidden())
		{
			API.SetTextFont(font);
			API.SetTextScale(1f, scale);
			if ((int)justification == 2)
			{
				API.SetTextWrap(0f, position.X);
			}
			API.SetTextJustification((int)justification);
			if (!color.HasValue)
			{
				color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, 128);
			}
			Color value = color.Value;
			API.SetTextColour((int)value.R, (int)value.G, (int)value.B, (int)value.A);
			if (drawOutline)
			{
				API.SetTextOutline();
			}
			if (shadowColor.HasValue)
			{
				Color value2 = shadowColor.Value;
				API.SetTextDropshadow(shadowLength, (int)value2.R, (int)value2.G, (int)value2.B, (int)value2.A);
				API.SetTextDropShadow();
			}
			API.BeginTextCommandDisplayText("STRING");
			API.AddTextComponentSubstringPlayerName(text);
			API.EndTextCommandDisplayText(position.X, position.Y);
		}
	}

	public static void Draw3DText(string[] textParts, Vector3 position, Color? color = null, float scale = 1f, int font = 4, bool wrap = false, Action? customTextCommands = null)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		Vector2 zero = Vector2.Zero;
		if (!API.World3dToScreen2d(position.X, position.Y, position.Z, ref zero.X, ref zero.Y))
		{
			return;
		}
		Vector3 gameplayCamCoords = API.GetGameplayCamCoords();
		float distanceBetweenCoords = API.GetDistanceBetweenCoords(gameplayCamCoords.X, gameplayCamCoords.Y, gameplayCamCoords.Z, position.X, position.Y, position.Z, true);
		float num = 1f / API.GetGameplayCamFov() * 100f;
		float num2 = (float)(4.0 / (double)(distanceBetweenCoords * num)) * scale;
		if (num2 < 0.25f)
		{
			num2 = 0.25f;
		}
		else if (num2 > 1f)
		{
			num2 = 1f;
		}
		if (!color.HasValue)
		{
			color = new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		Color value = color.Value;
		API.SetTextScale(1f, num2);
		API.SetTextFont(font);
		API.SetTextColour((int)value.R, (int)value.G, (int)value.B, (int)value.A);
		API.SetTextOutline();
		API.SetTextCentre(true);
		if (wrap)
		{
			API.SetTextWrap(0.4f, 0.6f);
		}
		if (customTextCommands == null)
		{
			API.SetTextEntry("jamyfafi");
			for (int i = 0; i < textParts.Length; i++)
			{
				API.AddTextComponentString(textParts[i]);
			}
		}
		else
		{
			customTextCommands();
		}
		API.DrawText(zero.X, zero.Y);
	}

	public static void Draw3DText(string text, Vector3 position, Color? color = null, float scale = 1f, int font = 4, bool wrap = false)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		_draw3DSingleString[0] = text;
		Draw3DText(_draw3DSingleString, position, color, scale, font, wrap);
	}

	public static void Draw3DText(Action customTextCommands, Vector3 position, Color? color = null, float scale = 1f, int font = 4, bool wrap = false)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		Draw3DText(_draw3DSingleString, position, color, scale, font, wrap, customTextCommands);
	}

	public static MinimapInfo GetMinimapAnchor()
	{
		bool num = API.IsMinimapRendering() != isMinimapVisible || API.IsScreenFadedOut() != isScreenFadedOut;
		isMinimapVisible = API.IsMinimapRendering();
		isScreenFadedOut = API.IsScreenFadedOut();
		if (!num && storedMinimapInfo.HasValue)
		{
			return storedMinimapInfo.Value;
		}
		float safeZoneSize = API.GetSafeZoneSize();
		float num2 = 0.05f;
		float num3 = 0.05f;
		float aspectRatio = API.GetAspectRatio(false);
		int num4 = 0;
		int num5 = 0;
		API.GetActiveScreenResolution(ref num4, ref num5);
		float num6 = 1f / (float)num4;
		float num7 = 1f / (float)num5;
		MinimapInfo minimapInfo = default(MinimapInfo);
		minimapInfo.Width = num6 * ((float)num4 / (4f * aspectRatio));
		minimapInfo.Height = num7 * ((float)num5 / 5.674f);
		minimapInfo.Left = num6 * ((float)num4 * (num2 * (Math.Abs(safeZoneSize - 1f) * 10f)));
		minimapInfo.Bottom = 1f - num7 * ((float)num5 * (num3 * (Math.Abs(safeZoneSize - 1f) * 10f)));
		minimapInfo.Right = minimapInfo.Left + minimapInfo.Width;
		minimapInfo.Top = minimapInfo.Bottom - minimapInfo.Height;
		minimapInfo.X = minimapInfo.Left;
		minimapInfo.Y = minimapInfo.Top;
		minimapInfo.UnitX = num6;
		minimapInfo.UnitY = num7;
		if (!isMinimapVisible)
		{
			minimapInfo.Y = 1f;
			minimapInfo.Height = 0f;
		}
		if (isScreenFadedOut)
		{
			minimapInfo.Y = 0f;
			minimapInfo.Height = 0f;
		}
		storedMinimapInfo = minimapInfo;
		return minimapInfo;
	}

	public static MenuItem AddErrorMenuItem(this Menu menu, Exception e = null)
	{
		MenuItem menuItem = new MenuItem(LocalizationController.S(Entries.Main.MENU_ERROR_ITEM), (e == null) ? (LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8) + ".") : ("~y~" + e.GetType().Name + "~n~~s~" + LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8) + "."))
		{
			ItemData = e
		};
		menu.AddMenuItem(menuItem);
		return menuItem;
	}

	public static MenuItem AddLoadingMenuItem(this Menu menu)
	{
		MenuItem menuItem = new MenuItem(LocalizationController.S(Entries.Main.LOADING) + "...")
		{
			Enabled = false
		};
		menu.AddMenuItem(menuItem);
		return menuItem;
	}

	public static string ToMenuItemDescription(this ItemRarity rarity)
	{
		return rarity switch
		{
			ItemRarity.Uncommon => "~y~" + LocalizationController.S(Entries.Businesses.ITEM_RARITY_UNCOMMON) + "~s~", 
			ItemRarity.Rare => "~p~" + LocalizationController.S(Entries.Businesses.ITEM_RARITY_RARE) + "~s~", 
			ItemRarity.VeryRare => "~p~" + LocalizationController.S(Entries.Businesses.ITEM_RARITY_VERY_RARE) + "~s~", 
			ItemRarity.Legendary => "~b~" + LocalizationController.S(Entries.Businesses.ITEM_RARITY_LEGENDARY) + "~s~", 
			ItemRarity.Unique => "~b~" + LocalizationController.S(Entries.Businesses.ITEM_RARITY_UNIQUE) + "~s~", 
			_ => "", 
		};
	}

	public static string LocalizeCrimeSeverity(CrimeSeverity crimeSeverity)
	{
		switch (crimeSeverity)
		{
		case CrimeSeverity.Undefined:
			return LocalizationController.S("crime_severity_undefined");
		case CrimeSeverity.Misdemeanor:
			return LocalizationController.S("crime_severity_misdemeanor");
		case CrimeSeverity.Felony:
			return LocalizationController.S("crime_severity_felony");
		case CrimeSeverity.MajorFelony:
			return LocalizationController.S("crime_severity_major_felony");
		default:
		{
			global::_003CPrivateImplementationDetails_003E.ThrowInvalidOperationException();
			string result = default(string);
			return result;
		}
		}
	}

	public static void SwapMenuItems(this Menu menu, MenuItem itemA, MenuItem itemB)
	{
		List<MenuItem> menuItems = menu.GetMenuItems();
		menu.ClearMenuItems();
		int num = menuItems.IndexOf(itemA);
		int num2 = menuItems.IndexOf(itemB);
		if (num < 0 || num2 < 0)
		{
			return;
		}
		menuItems.Swap(num, num2);
		foreach (MenuItem item in menuItems)
		{
			menu.AddMenuItem(item);
		}
	}

	public static void SwapMenuItems(this Menu menu, int idxA, int idxB)
	{
		List<MenuItem> menuItems = menu.GetMenuItems();
		menu.ClearMenuItems();
		if (idxA < 0 || idxB < 0 || idxA >= menuItems.Count || idxB >= menuItems.Count)
		{
			return;
		}
		menuItems.Swap(idxA, idxB);
		foreach (MenuItem item in menuItems)
		{
			menu.AddMenuItem(item);
		}
	}

	public static string ReplaceDelimitedString(this string menuText, params string[] stringParams)
	{
		int startIndex = 0;
		for (int i = 0; i < stringParams.Length; i++)
		{
			string newValue = $"{'\u200b'}{stringParams[i]}{'\u200c'}";
			int num = menuText.IndexOf('\u200b', startIndex);
			int num2 = menuText.IndexOf('\u200c', num) + 1;
			if (num == -1 || num2 == -1)
			{
				break;
			}
			string oldValue = menuText.Substring(num, num2 - num);
			menuText = menuText.Replace(oldValue, newValue);
			startIndex = num2;
		}
		return menuText;
	}

	public static T GetPreference<T>(string key, T defaultValue = default(T))
	{
		if (API.GetResourceKvpInt(key + "_isSet") < 1)
		{
			return defaultValue;
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)API.GetResourceKvpInt(key);
		}
		if (typeof(T) == typeof(bool))
		{
			return (T)(object)(API.GetResourceKvpInt(key) == 1);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)API.GetResourceKvpFloat(key);
		}
		if (typeof(T) == typeof(string))
		{
			return (T)(object)API.GetResourceKvpString(key);
		}
		if (typeof(T).IsEnum)
		{
			return (T)(object)API.GetResourceKvpInt(key);
		}
		return API.GetResourceKvpString(key).Unjson<T>();
	}

	public static void SetPreference<T>(string key, T value)
	{
		if (typeof(T) == typeof(int))
		{
			API.SetResourceKvpInt(key, (int)(object)value);
		}
		else if (typeof(T) == typeof(bool))
		{
			API.SetResourceKvpInt(key, ((bool)(object)value) ? 1 : 0);
		}
		else if (typeof(T) == typeof(float))
		{
			API.SetResourceKvpFloat(key, (float)(object)value);
		}
		else if (typeof(T) == typeof(string))
		{
			API.SetResourceKvp(key, (string)(object)value);
		}
		else if (typeof(T).IsEnum)
		{
			API.SetResourceKvpInt(key, (int)(object)value);
		}
		else
		{
			API.SetResourceKvp(key, value.Json());
		}
		API.SetResourceKvpInt(key + "_isSet", 1);
	}

	public static void ResetPreference(string key)
	{
		API.DeleteResourceKvp(key);
		API.SetResourceKvpInt(key + "_isSet", 0);
	}

	public static T GetExternalPreference<T>(string res, string key, T defaultValue = default(T))
	{
		if (API.GetExternalKvpInt(res, key + "_isSet") < 1)
		{
			return defaultValue;
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)API.GetExternalKvpInt(res, key);
		}
		if (typeof(T) == typeof(bool))
		{
			return (T)(object)(API.GetExternalKvpInt(res, key) == 1);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)API.GetExternalKvpFloat(res, key);
		}
		if (typeof(T) == typeof(string))
		{
			return (T)(object)API.GetExternalKvpString(res, key);
		}
		return API.GetExternalKvpString(res, key).Unjson<T>();
	}

	public static async Task Delay(int time = 0)
	{
		await BaseScript.Delay(time);
	}

	public static async Task<bool> ChangeModel(this Player player, PedHash hash)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return await player.ChangeModel(new Model(hash));
	}

	public static async Task<Ped> CreateLocalPed(Model model, Vector3 position, float rotation)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		using DisposableModel pedModel = new DisposableModel(model);
		await pedModel.Load();
		return new Ped(API.CreatePed(0, Model.op_Implicit(pedModel.Model), position.X, position.Y, position.Z, rotation, false, true))
		{
			PositionNoOffset = position
		};
	}

	public static void Freeze(bool freeze = true, bool freezeCamControls = true)
	{
		IsFrozen = freeze;
		int num = API.PlayerId();
		int playerPed = API.GetPlayerPed(num);
		API.SetPlayerControl(num, !freeze, (!freezeCamControls) ? 256 : 0);
		API.FreezeEntityPosition(playerPed, freeze);
		API.SetEntityInvincible(playerPed, freeze);
		for (int i = 21; i <= 25; i++)
		{
			API.DisableControlAction(0, i, freeze);
		}
		for (int j = 30; j <= 36; j++)
		{
			API.DisableControlAction(0, j, freeze);
		}
		if (freeze && !API.IsPedFatallyInjured(playerPed))
		{
			API.ClearPedTasksImmediately(playerPed);
		}
	}

	public static void Unfreeze()
	{
		Freeze(freeze: false);
	}

	public static void Hide(bool hide = true)
	{
		IsInvisible = hide;
		int playerPed = API.GetPlayerPed(API.PlayerId());
		API.SetEntityVisible(playerPed, !hide, true);
		API.SetEntityCollision(playerPed, !hide, !hide);
	}

	public static void Unhide()
	{
		Hide(hide: false);
	}

	public static async Task SwitchOut()
	{
		API.SwitchOutPlayer(API.PlayerPedId(), 0, 1);
		while (API.GetPlayerSwitchState() != 5)
		{
			await BaseScript.Delay(0);
		}
	}

	public static async Task SwitchIn()
	{
		API.SwitchInPlayer(API.PlayerPedId());
		while (API.IsPlayerSwitchInProgress())
		{
			await BaseScript.Delay(0);
		}
	}

	public static bool IsSwitchInProgress()
	{
		return API.IsPlayerSwitchInProgress();
	}

	public static async Task FadeOut(int time = 1000)
	{
		if (!API.IsScreenFadedOut())
		{
			API.FreezeEntityPosition(API.PlayerPedId(), true);
			API.DoScreenFadeOut(time);
			while (API.IsScreenFadingOut())
			{
				await BaseScript.Delay(0);
			}
		}
	}

	public static async Task FadeIn(int time = 1000)
	{
		if (!API.IsScreenFadedIn())
		{
			API.DoScreenFadeIn(time);
			while (API.IsScreenFadingIn())
			{
				await BaseScript.Delay(0);
			}
			API.FreezeEntityPosition(API.PlayerPedId(), false);
		}
	}

	public static bool IsScreenFadingInProgress()
	{
		if (!API.IsScreenFadingIn())
		{
			return API.IsScreenFadingOut();
		}
		return true;
	}

	public static void Blur(int time = 500)
	{
		API.TriggerScreenblurFadeIn((float)time);
	}

	public static void Unblur(int time = 500)
	{
		API.TriggerScreenblurFadeOut((float)time);
	}

	public static bool IsBlurred()
	{
		return API.IsScreenblurFadeRunning();
	}

	public static void RemoveAllAttachedProps()
	{
		Prop[] allProps = World.GetAllProps();
		foreach (Prop val in allProps)
		{
			if (((Entity)val).IsAttachedTo((Entity)(object)Game.PlayerPed))
			{
				((PoolObject)val).Delete();
			}
		}
	}

	public static bool IsAnActualWeapon(WeaponHash weaponHash)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return !nonWeapons.Contains(weaponHash);
	}

	public static string GetDeathCauseString(int causeHash)
	{
		WeaponDefinition weaponDefinitionByHash = Gtacnr.Data.Items.GetWeaponDefinitionByHash((uint)causeHash);
		string result = $"Unknown (0x{causeHash:X})";
		if (weaponDefinitionByHash != null)
		{
			result = weaponDefinitionByHash.Name;
		}
		else
		{
			switch ((uint)causeHash)
			{
			case 2741846334u:
				result = "Vehicle";
				break;
			case 539292904u:
				result = "Explosion";
				break;
			case 3750660587u:
				result = "Fire";
				break;
			case 4284007675u:
				result = "Drowning";
				break;
			case 1936677264u:
				result = "Drowning in Vehicle";
				break;
			case 3452007600u:
				result = "Fall";
				break;
			case 3425972830u:
				result = "Water Cannon";
				break;
			case 4194021054u:
				result = "Animal";
				break;
			case 148160082u:
				result = "Cougar";
				break;
			case 4294966297u:
				result = "Drug Overdose";
				break;
			}
		}
		return result;
	}

	public static async Task<bool> LoadAnimDictionary(string dictionary)
	{
		do
		{
			if (!API.DoesAnimDictExist(dictionary))
			{
				Debug.WriteLine("^1Warning: requested anim dictionary `" + dictionary + "` was not found");
				return false;
			}
			API.RequestAnimDict(dictionary);
			await Delay(10);
		}
		while (!API.HasAnimDictLoaded(dictionary));
		return true;
	}

	public static async Task WaitUntilAccountDataLoaded()
	{
		string uid = null;
		do
		{
			await Delay(250);
			PlayerState playerState = LatentPlayers.Get(Game.Player);
			if (playerState != null && !string.IsNullOrWhiteSpace(playerState.Uid))
			{
				uid = playerState.Uid;
			}
		}
		while (uid == null);
	}

	public static async Task TeleportToCoords(Vector3 position, float heading = -1f, TeleportFlags flags = TeleportFlags.None, int additionalDelayTime = 0)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		if (((Entity)Game.PlayerPed).IsAlive && API.IsPedClimbing(((PoolObject)Game.PlayerPed).Handle))
		{
			Game.PlayerPed.Task.ClearAllImmediately();
		}
		IsTeleporting = true;
		Entity entity = (Entity)(object)Game.PlayerPed;
		if (flags.HasFlag(TeleportFlags.TeleportVehicle) && (Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver == (Entity)(object)Game.PlayerPed)
		{
			entity = (Entity)(object)Game.PlayerPed.CurrentVehicle;
		}
		bool wasPedVisible = ((Entity)Game.PlayerPed).IsVisible;
		if (flags.HasFlag(TeleportFlags.VisualEffects))
		{
			if (!API.IsScreenFadedOut())
			{
				await FadeOut(500);
			}
			if (wasPedVisible)
			{
				API.NetworkFadeOutEntity(((PoolObject)entity).Handle, true, false);
			}
			if (((Entity)Game.PlayerPed).IsAlive && API.IsPedClimbing(((PoolObject)Game.PlayerPed).Handle))
			{
				Game.PlayerPed.Task.ClearAllImmediately();
			}
		}
		entity.IsPositionFrozen = true;
		DateTime t;
		if (flags.HasFlag(TeleportFlags.FindGroundHeight))
		{
			bool found = false;
			for (int i = 0; i < 40; i++)
			{
				float newZ = 25f * (float)i;
				if (i % 2 == 0)
				{
					newZ = 1000f - newZ;
				}
				API.RequestCollisionAtCoord(position.X, position.Y, newZ);
				API.NewLoadSceneStart(position.X, position.Y, newZ, position.X, position.Y, newZ, 50f, 0);
				t = DateTime.UtcNow;
				while (API.IsNetworkLoadingScene() && !Gtacnr.Utils.CheckTimePassed(t, 1000.0))
				{
					await Delay();
				}
				AntiTeleportScript.JustTeleported();
				entity.PositionNoOffset = new Vector3(position.X, position.Y, newZ);
				while ((!API.HasCollisionLoadedAroundEntity(((PoolObject)entity).Handle) || API.IsEntityWaitingForWorldCollision(((PoolObject)entity).Handle)) && !Gtacnr.Utils.CheckTimePassed(t, 2000.0))
				{
					await Delay();
				}
				if (API.GetGroundZFor_3dCoord(position.X, position.Y, newZ, ref newZ, false))
				{
					position.Z = newZ;
					found = true;
					break;
				}
			}
			if (!found)
			{
				API.GetNthClosestVehicleNode(position.X, position.Y, position.Z, 0, ref position, 0, 0, 0);
			}
		}
		AntiTeleportScript.JustTeleported();
		entity.PositionNoOffset = position;
		API.RequestCollisionAtCoord(position.X, position.Y, position.Z);
		t = DateTime.UtcNow;
		while ((!API.HasCollisionLoadedAroundEntity(((PoolObject)entity).Handle) || API.IsEntityWaitingForWorldCollision(((PoolObject)entity).Handle)) && !Gtacnr.Utils.CheckTimePassed(t, 10000.0))
		{
			await Delay(10);
		}
		if (flags.HasFlag(TeleportFlags.PlaceOnGround))
		{
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z + 0.5f, ref z, false))
			{
				position.Z = z;
				AntiTeleportScript.JustTeleported();
				entity.Position = position;
			}
			Vehicle val = (Vehicle)(object)((entity is Vehicle) ? entity : null);
			if (val != null)
			{
				val.PlaceOnGround();
			}
		}
		if (heading != -1f)
		{
			entity.Heading = heading;
		}
		API.SetGameplayCamRelativePitch(0f, 1f);
		API.SetGameplayCamRelativeHeading(0f);
		entity.IsPositionFrozen = false;
		if (flags.HasFlag(TeleportFlags.VisualEffects))
		{
			if (additionalDelayTime > 0)
			{
				await Delay(additionalDelayTime);
			}
			if (wasPedVisible)
			{
				API.NetworkFadeInEntity(((PoolObject)entity).Handle, true);
			}
			if (API.IsScreenFadedOut())
			{
				await FadeIn(500);
			}
		}
		IsTeleporting = false;
	}

	public static async Task TeleportToCoords(Vector4 positionAndHeading, TeleportFlags flags = TeleportFlags.None)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		await TeleportToCoords(positionAndHeading.XYZ(), positionAndHeading.W, flags);
	}

	public static async void UpdateWeatherParticles(string weather)
	{
		await Delay(10);
		if (weather.ToUpper() == "XMAS")
		{
			API.RequestScriptAudioBank("ICE_FOOTSTEPS", false);
			API.RequestScriptAudioBank("SNOW_FOOTSTEPS", false);
			if (!API.HasNamedPtfxAssetLoaded("core_snow"))
			{
				API.RequestNamedPtfxAsset("core_snow");
				while (!API.HasNamedPtfxAssetLoaded("core_snow"))
				{
					await Delay();
				}
			}
			API.UseParticleFxAssetNextCall("core_snow");
			API.SetForceVehicleTrails(true);
			API.SetForcePedFootstepsTracks(true);
			API.WaterOverrideSetStrength(3f);
		}
		else
		{
			API.SetForceVehicleTrails(false);
			API.SetForcePedFootstepsTracks(false);
			API.RemoveNamedPtfxAsset("core_snow");
		}
	}

	public static async Task<Vehicle?> CreateStoredVehicle(StoredVehicle storedVehicle, Vector3 position, float heading)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		_ = 2;
		try
		{
			using DisposableModel vehModel = new DisposableModel(Model.op_Implicit(storedVehicle.Model));
			await vehModel.Load();
			Vehicle vehicle = await World.CreateVehicle(vehModel.Model, position, heading);
			if ((Entity)(object)vehicle == (Entity)null)
			{
				DisplayErrorMessage(0, -1, LocalizationController.S(Entries.Vehicles.ERROR_CREATING_VEHICLE) + " " + LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8));
				Debug.WriteLine("Warning! Unable to created stored vehicle id `" + storedVehicle.Id + "` because World.CreateVehicle returned null. This could be a ^3temporary ^0download problem, please try again before reporting.");
				return null;
			}
			if (storedVehicle.ModData != null)
			{
				API.SetVehicleModKit(((PoolObject)vehicle).Handle, 0);
				vehicle.Mods.InstallModKit();
				vehicle.Mods.PrimaryColor = (VehicleColor)storedVehicle.ModData.PrimaryColor;
				vehicle.Mods.SecondaryColor = (VehicleColor)storedVehicle.ModData.SecondaryColor;
				vehicle.Mods.TrimColor = (VehicleColor)storedVehicle.ModData.TrimColor;
				vehicle.Mods.DashboardColor = (VehicleColor)storedVehicle.ModData.DashboardColor;
				vehicle.Mods.PearlescentColor = (VehicleColor)storedVehicle.ModData.PearlescentColor;
				if (storedVehicle.ModData.Livery >= 0)
				{
					vehicle.Mods.Livery = storedVehicle.ModData.Livery;
				}
				if (!string.IsNullOrEmpty(storedVehicle.LicensePlate))
				{
					vehicle.Mods.LicensePlate = storedVehicle.LicensePlate;
				}
				else
				{
					vehicle.Mods.LicensePlate = "ERROR";
				}
				bool vehicleModVariation = API.GetVehicleModVariation(((PoolObject)vehicle).Handle, 23);
				bool flag = false;
				foreach (KeyValuePair<int, int> modInfo in storedVehicle.ModData.Mods)
				{
					VehicleModPricingInfo vehicleModPricingInfo = SellToPlayersScript.VehicleMods.FirstOrDefault((VehicleModPricingInfo i) => i.Id == modInfo.Key);
					if (vehicleModPricingInfo != null)
					{
						if (vehicleModPricingInfo.BasePrice < 0)
						{
							flag = true;
						}
						else
						{
							API.SetVehicleMod(((PoolObject)vehicle).Handle, modInfo.Key, modInfo.Value, vehicleModVariation);
						}
					}
				}
				vehicle.Mods.WindowTint = (VehicleWindowTint)storedVehicle.ModData.WindowTint;
				if (flag)
				{
					SendNotification("<C>~y~Notice:</C> ~s~some vehicle mods such as ~y~mounted guns ~s~have been disabled in version ~b~0.3.23~s~.");
				}
				if (((Entity)vehicle).Model == Model.op_Implicit("bspot"))
				{
					API.SetVehicleExtra(((PoolObject)vehicle).Handle, 9, false);
				}
			}
			VehicleModRestrictionsScript.RemoveRestrictedMods(vehicle);
			if (storedVehicle.HealthData != null)
			{
				vehicle.EngineHealth = storedVehicle.HealthData.EngineHealth;
				vehicle.BodyHealth = storedVehicle.HealthData.BodyHealth;
				if (storedVehicle.HealthData.BodyHealth < 950f)
				{
					int num = 10 - storedVehicle.HealthData.BodyHealth.ToInt() / 100;
					Vector3 val = default(Vector3);
					for (int num2 = 0; num2 < num; num2++)
					{
						((Vector3)(ref val))._002Ector((float)Gtacnr.Utils.GetRandomDouble(-1.8, 1.8), (float)Gtacnr.Utils.GetRandomDouble(-2.0, 5.0), (float)Gtacnr.Utils.GetRandomDouble(-0.1, 0.8));
						float num3 = (1000f - storedVehicle.HealthData.BodyHealth) / 5f;
						vehicle.Deform(val, num3, 1f);
						if (num2 < 3 && storedVehicle.HealthData.BodyHealth < 850f)
						{
							VehicleWindowIndex val2 = new List<VehicleWindowIndex>
							{
								(VehicleWindowIndex)0,
								(VehicleWindowIndex)1,
								(VehicleWindowIndex)2,
								(VehicleWindowIndex)3
							}.Random();
							vehicle.Windows[val2].Smash();
						}
					}
				}
				vehicle.PetrolTankHealth = storedVehicle.HealthData.PetrolTankHealth;
				if (storedVehicle.HealthData.WheelHealth != null)
				{
					for (int num4 = 0; num4 < storedVehicle.HealthData.WheelHealth.Length; num4++)
					{
						if (storedVehicle.HealthData.WheelHealth[num4] < 1f)
						{
							API.SetVehicleTyreBurst(((PoolObject)vehicle).Handle, num4, true, 600f);
						}
					}
				}
				vehicle.DirtLevel = storedVehicle.HealthData.DirtLevel;
				if (storedVehicle.HealthData.Fuel < 0.075f)
				{
					storedVehicle.HealthData.Fuel = 0.075f;
				}
				GasScript.InitializeGas(vehicle, storedVehicle.HealthData.Fuel);
			}
			DateTime t = DateTime.UtcNow;
			bool success = true;
			while (!API.NetworkGetEntityIsNetworked(((PoolObject)vehicle).Handle) || !API.NetworkDoesNetworkIdExist(API.NetworkGetNetworkIdFromEntity(((PoolObject)vehicle).Handle)))
			{
				if (Gtacnr.Utils.CheckTimePassed(t, TimeSpan.FromSeconds(10.0)))
				{
					success = false;
					break;
				}
				await Delay(50);
			}
			if (!success)
			{
				DisplayErrorMessage(0, -1, LocalizationController.S(Entries.Vehicles.ERROR_CREATING_VEHICLE) + " " + LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8));
				Debug.WriteLine("[Utils] Unable to create stored vehicle " + storedVehicle.Id + " (sync timeout)");
				if (vehicle.Exists())
				{
					((PoolObject)vehicle).Delete();
				}
				return null;
			}
			int num5 = API.NetworkGetNetworkIdFromEntity(((PoolObject)vehicle).Handle);
			API.SetNetworkIdExistsOnAllMachines(num5, true);
			API.SetEntityAsMissionEntity(((PoolObject)vehicle).Handle, true, true);
			storedVehicle.NetworkId = num5;
			return vehicle;
		}
		catch (ArgumentException ex)
		{
			DisplayErrorMessage(0, -1, LocalizationController.S(Entries.Vehicles.ERROR_CREATING_VEHICLE) + " " + LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8));
			Debug.WriteLine($"Warning! Unable to created stored vehicle id `{storedVehicle.Id}` because its model `0x{storedVehicle.Model:X}` doesn't exist.");
			Debug.WriteLine(ex.ToString());
			return null;
		}
		catch (Exception ex2)
		{
			DisplayErrorMessage(0, -1, LocalizationController.S(Entries.Vehicles.ERROR_CREATING_VEHICLE) + " " + LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8));
			Debug.WriteLine(ex2.ToString());
			return null;
		}
	}

	public static VehicleHealthData? GetVehicleHealthData(Vehicle vehicle)
	{
		if ((Entity)(object)vehicle == (Entity)null)
		{
			return null;
		}
		float fuel = LatentVehicleStateScript.Get(((Entity)vehicle).NetworkId)?.Fuel ?? 0f;
		float[] array = new float[6];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (API.IsVehicleTyreBurst(((PoolObject)vehicle).Handle, i, false) ? 0f : 1f);
		}
		return new VehicleHealthData
		{
			EngineHealth = vehicle.EngineHealth,
			BodyHealth = vehicle.BodyHealth,
			PetrolTankHealth = vehicle.PetrolTankHealth,
			WheelHealth = array,
			DirtLevel = vehicle.DirtLevel,
			Fuel = fuel
		};
	}

	public static string GetVehicleModelName(int modelHash)
	{
		return Game.GetGXTEntry(Vehicle.GetModelDisplayName(Model.op_Implicit(modelHash)));
	}

	public static string GetVehicleMakeName(int modelHash)
	{
		return Game.GetGXTEntry(API.GetMakeNameFromVehicleModel((uint)modelHash));
	}

	public static string GetVehicleFullName(int modelHash)
	{
		string vehicleMakeName = GetVehicleMakeName(modelHash);
		string vehicleModelName = GetVehicleModelName(modelHash);
		if (!string.IsNullOrEmpty(vehicleMakeName))
		{
			return vehicleMakeName + " " + vehicleModelName;
		}
		return vehicleModelName;
	}

	public static string GetVehicleFullName(string modelId)
	{
		return GetVehicleFullName(API.GetHashKey(modelId));
	}

	public static Vector3 GetVehicleFuelCapPos(Vehicle vehicle, int side = -1)
	{
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		string[] obj = new string[5] { "petrolcap", "petroltank", "petroltank_r", "petroltank_l", "wheel_lr" };
		EntityBone val = null;
		string[] array = obj;
		foreach (string text in array)
		{
			int entityBoneIndexByName = API.GetEntityBoneIndexByName(((PoolObject)vehicle).Handle, text);
			val = ((Entity)vehicle).Bones[entityBoneIndexByName];
			if (val.IsValid)
			{
				if (text == "petroltank_r")
				{
					side = 1;
				}
				break;
			}
		}
		if (val == (EntityBone)null)
		{
			return ((Entity)vehicle).Position + ((Entity)vehicle).RightVector * 1.5f * (float)side;
		}
		Vector3 position = val.Position;
		position += ((Entity)vehicle).RightVector * 0.5f * (float)side;
		API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref position.Z, false);
		return position;
	}

	public static float GetVehicleTankCapacityL(Vehicle vehicle)
	{
		float num = ((!GasScript.CapacityOverrides.ContainsKey(((Entity)vehicle).Model.Hash)) ? API.GetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fPetrolTankVolume") : GasScript.CapacityOverrides[((Entity)vehicle).Model.Hash]);
		if (num <= 0f)
		{
			num = 65f;
		}
		return num;
	}

	public static float GetVehicleTankCapacity(Vehicle vehicle)
	{
		return GetVehicleTankCapacityL(vehicle) * GasScript.GALLONS_PER_LITER;
	}

	public static float GetVehicleFuel(Vehicle vehicle)
	{
		float vehicleTankCapacityL = GetVehicleTankCapacityL(vehicle);
		VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)vehicle).NetworkId);
		float num;
		if (vehicleState != null)
		{
			float fuel = vehicleState.Fuel;
			num = vehicleTankCapacityL * fuel;
		}
		else
		{
			num = vehicle.FuelLevel;
		}
		return num * GasScript.GALLONS_PER_LITER;
	}

	public static float GetVehicleRefuelAmount(Vehicle vehicle)
	{
		float vehicleTankCapacityL = GetVehicleTankCapacityL(vehicle);
		VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)vehicle).NetworkId);
		float num;
		if (vehicleState != null)
		{
			float fuel = vehicleState.Fuel;
			num = vehicleTankCapacityL - vehicleTankCapacityL * fuel;
		}
		else
		{
			num = vehicleTankCapacityL - vehicle.FuelLevel;
		}
		return num * GasScript.GALLONS_PER_LITER;
	}

	public static VehicleWindowIndex GetVehicleWindowBySeat(VehicleSeat seat)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		if ((int)seat != -1)
		{
			if ((int)seat != 0)
			{
				if ((int)seat != 1)
				{
					if ((int)seat == 2)
					{
						return (VehicleWindowIndex)3;
					}
					return (VehicleWindowIndex)(-1);
				}
				return (VehicleWindowIndex)2;
			}
			return (VehicleWindowIndex)1;
		}
		return (VehicleWindowIndex)0;
	}

	private static VehicleWindowIndex SeatWindowFromBone(string bone)
	{
		return (VehicleWindowIndex)(bone switch
		{
			"window_lf" => 0, 
			"window_rf" => 1, 
			"window_lr" => 2, 
			"window_rr" => 3, 
			_ => -1, 
		});
	}

	public static VehicleWindowIndex ClosestSeatWindow(this Vehicle vehicle, Vector3 check_position)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		string bone = "";
		float? num = null;
		string[] array = seat_windows_bones;
		foreach (string text in array)
		{
			int entityBoneIndexByName = API.GetEntityBoneIndexByName(((PoolObject)vehicle).Handle, text);
			if (entityBoneIndexByName >= 0)
			{
				Vector3 worldPositionOfEntityBone = API.GetWorldPositionOfEntityBone(((PoolObject)vehicle).Handle, entityBoneIndexByName);
				float num2 = Vector3.DistanceSquared(check_position, worldPositionOfEntityBone);
				if (!num.HasValue || num2 < num)
				{
					num = num2;
					bone = text;
				}
			}
		}
		return SeatWindowFromBone(bone);
	}

	public static List<int> GetPlayerPassengersInVehicle(Vehicle vehicle)
	{
		Ped[] passengers = vehicle.Passengers;
		List<int> list = new List<int>(passengers.Length);
		Ped[] array = passengers;
		foreach (Ped val in array)
		{
			if (val.IsPlayer)
			{
				list.Add(API.GetPlayerServerId(API.NetworkGetPlayerIndexFromPed(((PoolObject)val).Handle)));
			}
		}
		return list;
	}

	public static int GetStatInt(string key)
	{
		int result = 0;
		API.StatGetInt((uint)API.GetHashKey(key), ref result, -1);
		return result;
	}

	public static void SetStatInt(string key, int value)
	{
		API.StatSetInt((uint)API.GetHashKey(key), value, true);
	}

	public static float GetStatFloat(string key)
	{
		float result = 0f;
		API.StatGetFloat((uint)API.GetHashKey(key), ref result, -1);
		return result;
	}

	public static void SetStatFloat(string key, float value)
	{
		API.StatSetFloat((uint)API.GetHashKey(key), value, true);
	}

	public static string GetLocationName(Vector3 coords)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return API.GetLabelText(API.GetNameOfZone(coords.X, coords.Y, coords.Z));
	}

	public static void SetBlipName(Blip blip, string name, string? label = null)
	{
		if (NullableExtensions.IsStringNullOrEmpty(label))
		{
			label = Gtacnr.Utils.GenerateAsciiString(8);
		}
		label = label.ToUpperInvariant();
		API.AddTextEntry(label, name);
		API.BeginTextCommandSetBlipName(label);
		API.EndTextCommandSetBlipName(((PoolObject)blip).Handle);
	}

	public static Vector3[][] GetBoundingBoxPolyMatrix(Vector3[] box)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3[12][]
		{
			(Vector3[])(object)new Vector3[3]
			{
				box[2],
				box[1],
				box[0]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[3],
				box[2],
				box[0]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[4],
				box[5],
				box[6]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[4],
				box[6],
				box[7]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[2],
				box[3],
				box[6]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[7],
				box[6],
				box[3]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[0],
				box[1],
				box[4]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[5],
				box[4],
				box[1]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[1],
				box[2],
				box[5]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[2],
				box[6],
				box[5]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[4],
				box[7],
				box[3]
			},
			(Vector3[])(object)new Vector3[3]
			{
				box[4],
				box[3],
				box[0]
			}
		};
	}

	public static Vector3[][] GetBoundingBoxEdgeMatrix(Vector3[] box)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3[12][]
		{
			(Vector3[])(object)new Vector3[2]
			{
				box[0],
				box[1]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[1],
				box[2]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[2],
				box[3]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[3],
				box[0]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[4],
				box[5]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[5],
				box[6]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[6],
				box[7]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[7],
				box[4]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[0],
				box[4]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[1],
				box[5]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[2],
				box[6]
			},
			(Vector3[])(object)new Vector3[2]
			{
				box[3],
				box[7]
			}
		};
	}

	public static void DrawPolyMatrix(Vector3[][] polyCollection, int r, int g, int b, int a)
	{
		foreach (Vector3[] obj in polyCollection)
		{
			float x = obj[0].X;
			float y = obj[0].Y;
			float z = obj[0].Z;
			float x2 = obj[1].X;
			float y2 = obj[1].Y;
			float z2 = obj[1].Z;
			float x3 = obj[2].X;
			float y3 = obj[2].Y;
			float z3 = obj[2].Z;
			API.DrawPoly(x, y, z, x2, y2, z2, x3, y3, z3, r, g, b, a);
		}
	}

	public static void DrawEdgeMatrix(Vector3[][] linesCollection, int r, int g, int b, int a)
	{
		foreach (Vector3[] obj in linesCollection)
		{
			float x = obj[0].X;
			float y = obj[0].Y;
			float z = obj[0].Z;
			float x2 = obj[1].X;
			float y2 = obj[1].Y;
			float z2 = obj[1].Z;
			API.DrawLine(x, y, z, x2, y2, z2, r, g, b, a);
		}
	}

	public static void DrawBoundingBox(Vector3[] box, int r, int g, int b, int a)
	{
		Vector3[][] boundingBoxPolyMatrix = GetBoundingBoxPolyMatrix(box);
		Vector3[][] boundingBoxEdgeMatrix = GetBoundingBoxEdgeMatrix(box);
		DrawPolyMatrix(boundingBoxPolyMatrix, r, g, b, a);
		DrawEdgeMatrix(boundingBoxEdgeMatrix, 255, 255, 255, 255);
	}

	public static Vector3[] GetEntityBoundingBox(int entity)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		Vector3 zero = Vector3.Zero;
		Vector3 zero2 = Vector3.Zero;
		API.GetModelDimensions((uint)API.GetEntityModel(entity), ref zero, ref zero2);
		return (Vector3[])(object)new Vector3[8]
		{
			API.GetOffsetFromEntityInWorldCoords(entity, zero.X - 0.001f, zero.Y - 0.001f, zero.Z - 0.001f),
			API.GetOffsetFromEntityInWorldCoords(entity, zero2.X + 0.001f, zero.Y - 0.001f, zero.Z - 0.001f),
			API.GetOffsetFromEntityInWorldCoords(entity, zero2.X + 0.001f, zero2.Y + 0.001f, zero.Z - 0.001f),
			API.GetOffsetFromEntityInWorldCoords(entity, zero.X - 0.001f, zero2.Y + 0.001f, zero.Z - 0.001f),
			API.GetOffsetFromEntityInWorldCoords(entity, zero.X - 0.001f, zero.Y - 0.001f, zero2.Z + 0.001f),
			API.GetOffsetFromEntityInWorldCoords(entity, zero2.X + 0.001f, zero.Y - 0.001f, zero2.Z + 0.001f),
			API.GetOffsetFromEntityInWorldCoords(entity, zero2.X + 0.001f, zero2.Y + 0.001f, zero2.Z + 0.001f),
			API.GetOffsetFromEntityInWorldCoords(entity, zero.X - 0.001f, zero2.Y + 0.001f, zero2.Z + 0.001f)
		};
	}

	public static void DrawEntityBoundingBox(Entity ent, int r, int g, int b, int a)
	{
		DrawBoundingBox(GetEntityBoundingBox(((PoolObject)ent).Handle), r, g, b, a);
	}

	public static async Task<bool> GetNetworkControlOfEntity(Entity entity)
	{
		int attempts = 10;
		while (!API.NetworkHasControlOfEntity(((PoolObject)entity).Handle))
		{
			API.NetworkRequestControlOfEntity(((PoolObject)entity).Handle);
			API.NetworkRequestControlOfNetworkId(entity.NetworkId);
			attempts--;
			if (attempts == 0)
			{
				return false;
			}
			await Delay(100);
		}
		return true;
	}

	public static async Task<bool> MakeEntityNetworked(Entity entity, TimeSpan? timeout = null)
	{
		if (!timeout.HasValue)
		{
			timeout = TimeSpan.FromSeconds(10.0);
		}
		if (!API.NetworkGetEntityIsNetworked(((PoolObject)entity).Handle))
		{
			API.NetworkRegisterEntityAsNetworked(((PoolObject)entity).Handle);
		}
		DateTime t = DateTime.UtcNow;
		while (!API.NetworkGetEntityIsNetworked(((PoolObject)entity).Handle) || !API.NetworkDoesNetworkIdExist(API.NetworkGetNetworkIdFromEntity(((PoolObject)entity).Handle)))
		{
			if (Gtacnr.Utils.CheckTimePassed(t, timeout.Value))
			{
				return false;
			}
			await Delay(50);
		}
		return true;
	}

	public static bool GetPedConfigFlagEx(int pedId, PedConfigFlag flag)
	{
		return API.GetPedConfigFlag(pedId, (int)flag, false);
	}

	public static bool GetPedConfigFlagEx(Ped ped, PedConfigFlag flag)
	{
		return API.GetPedConfigFlag(((PoolObject)ped).Handle, (int)flag, false);
	}

	public static void SetPedConfigFlagEx(int pedId, PedConfigFlag flag, bool value)
	{
		API.SetPedConfigFlag(pedId, (int)flag, value);
	}

	public static void SetPedConfigFlagEx(Ped ped, PedConfigFlag flag, bool value)
	{
		API.SetPedConfigFlag(((PoolObject)ped).Handle, (int)flag, value);
	}

	public static bool GetIsTaskActiveEx(int pedId, TaskTypeIndex taskType)
	{
		return API.GetIsTaskActive(pedId, (int)taskType);
	}

	public static bool GetIsTaskActiveEx(Ped ped, TaskTypeIndex taskType)
	{
		return API.GetIsTaskActive(((PoolObject)ped).Handle, (int)taskType);
	}

	public static async void PlaySoundFromEntityFromAudioBank(int soundId, string audioName, int entity, string audioRef, bool isNetwork, string audioBankName)
	{
		TimeSpan timeout = TimeSpan.FromSeconds(10.0);
		DateTime startT = DateTime.UtcNow;
		bool flag;
		while (!(flag = API.RequestScriptAudioBank(audioBankName, false)) || Gtacnr.Utils.CheckTimePassed(startT, timeout))
		{
			await Delay();
		}
		if (flag)
		{
			API.PlaySoundFromEntity(soundId, audioName, entity, audioRef, isNetwork, 0);
		}
		else
		{
			Debug.WriteLine("Failed to load audio bank: '" + audioBankName + "'");
		}
	}

	public static async void PlaySoundFrontendFromAudioBank(int soundId, string audioName, string audioRef, string audioBankName)
	{
		TimeSpan timeout = TimeSpan.FromSeconds(10.0);
		DateTime startT = DateTime.UtcNow;
		bool flag;
		while (!(flag = API.RequestScriptAudioBank(audioBankName, false)) || Gtacnr.Utils.CheckTimePassed(startT, timeout))
		{
			await Delay();
		}
		if (flag)
		{
			API.PlaySoundFrontend(soundId, audioName, audioRef, false);
		}
		else
		{
			Debug.WriteLine("Failed to load audio bank: '" + audioBankName + "'");
		}
	}

	public static string FormatDistanceString(float meters)
	{
		if (API.GetProfileSetting(227) != 1)
		{
			if (!(meters >= 304.8f))
			{
				return $"{meters.ToFeet():0.#}ft";
			}
			return $"{meters.ToMiles():0.##}mi";
		}
		if (!(meters >= 1000f))
		{
			return $"{meters:0.#}m";
		}
		return $"{meters.ToKm():0.##}km";
	}

	static Utils()
	{
		WeaponHash[] array = new WeaponHash[100];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		array[46] = (WeaponHash)API.GetHashKey("weapon_combatshotgun");
		array[56] = (WeaponHash)API.GetHashKey("weapon_specialrifle");
		array[69] = (WeaponHash)API.GetHashKey("weapon_boltrifle");
		array[70] = (WeaponHash)API.GetHashKey("weapon_winchester");
		array[71] = (WeaponHash)API.GetHashKey("weapon_springfield");
		WeaponHashes = (WeaponHash[])(object)array;
	}
}
