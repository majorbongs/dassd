using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.API;
using Gtacnr.Client.Libs;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client;

public class MainScript : Script
{
	private static int camera;

	private static List<Character> characters = new List<Character>();

	private static DateTime serverDateTimeGetT;

	private static DateTime serverDateTime;

	private static MainScript instance;

	public static IEnumerable<Character> Characters => characters;

	public static Character SelectedCharacter { get; set; }

	public static bool LocalizationLoaded { get; private set; } = false;

	public static DateTime ServerDateTime
	{
		get
		{
			return serverDateTime;
		}
		set
		{
			serverDateTime = value;
			serverDateTimeGetT = DateTime.Now;
		}
	}

	public static TimeSpan TimeElapsedSinceServerDateTime => DateTime.Now - serverDateTimeGetT;

	public static DateTime EstimatedServerTime => ServerDateTime + TimeElapsedSinceServerDateTime;

	public static bool HardcoreMode { get; private set; } = false;

	public MainScript()
	{
		instance = this;
		HardcoreMode = (dynamic)((BaseScript)this).GlobalState.Get("Hardcore");
		LoadLocalization();
		BaseScript.TriggerServerEvent("gtacnr:reauth", new object[0]);
	}

	public static IEnumerable<Player> GetPlayerList()
	{
		return ((IEnumerable<Player>)((BaseScript)instance).Players).ToList();
	}

	protected override async void OnStarted()
	{
		LoadServerDateTime();
		Utils.Unblur();
		API.SetNuiFocus(false, false);
		await Game.Player.ChangeModel(Model.op_Implicit((PedHash)API.GetHashKey("player_zero")));
		SetDefaultCamera();
		API.DisableIdleCamera(true);
		if (!API.IsScreenFadedOut())
		{
			await Utils.FadeOut(1);
		}
		LoadingPrompt.Show("Loading account data", (LoadingSpinnerType)5);
		await ReloadCharacters();
		if (characters.Count == 1)
		{
			LoadingPrompt.Show("Loading character", (LoadingSpinnerType)5);
			BaseScript.TriggerEvent("gtacnr:characters:selectCharacter", new object[1] { 0 });
			BaseScript.TriggerServerEvent("gtacnr:characters:characterSelected", new object[1] { 0 });
		}
		else if (characters.Count > 1)
		{
			API.ShutdownLoadingScreen();
			API.ShutdownLoadingScreenNui();
			LoadingPrompt.Show("Loading characters", (LoadingSpinnerType)5);
			BaseScript.TriggerEvent("gtacnr:characters:showSelectionHud", new object[0]);
		}
		else
		{
			API.ShutdownLoadingScreen();
			API.ShutdownLoadingScreenNui();
			LoadingPrompt.Show("Loading character creator", (LoadingSpinnerType)5);
			await BaseScript.Delay(1000);
			characters = new List<Character>();
			BaseScript.TriggerEvent("gtacnr:characters:enterCreationMode", new object[0]);
		}
	}

	public static async Task ReloadCharacters()
	{
		characters.Clear();
		int attempts = 1;
		List<Character> list;
		while (true)
		{
			list = await Gtacnr.Client.API.Characters.GetAll();
			if (list != null)
			{
				break;
			}
			if (attempts > 10)
			{
				BaseScript.TriggerServerEvent("gtacnr:unableToSpawn", new object[0]);
				return;
			}
			Debug.WriteLine($"Loading character... (attempt {attempts}/10)");
			await BaseScript.Delay(2000);
			attempts++;
		}
		characters.AddRange(list);
	}

	[Command("unblur")]
	private void UnblurCommand()
	{
		Utils.Unblur();
	}

	private async void TestAnimCommand(string[] args)
	{
		await Utils.LoadAnimDictionary(args[0]);
		Game.PlayerPed.Task.PlayAnimation(args[0], args[1]);
	}

	public static void SetDefaultCamera()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(-350f, -2500f, 300f);
		API.DestroyAllCams(false);
		camera = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", val.X, val.Y, val.Z, 0f, 0f, 0f, 70f, false, 0);
		API.SetCamActive(camera, true);
		API.RenderScriptCams(true, false, 2000, true, true);
		API.PointCamAtCoord(camera, -300f, 0f, 270f);
		API.SetEntityCoords(API.PlayerPedId(), val.X, val.Y, val.Z - 10f, false, false, false, false);
		Utils.Freeze();
	}

	public static void DestroyDefaultCamera()
	{
		API.SetCamActive(camera, false);
		API.DestroyCam(camera, false);
		API.RenderScriptCams(false, false, 0, true, false);
	}

	private async void LoadServerDateTime()
	{
		try
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:serverDateTime", new object[0]);
			ServerDateTime = Gtacnr.Utils.ParseDateTime(text);
			Print("Server date and time: " + ServerDateTime.ToMysqlDateTime());
			BaseScript.TriggerEvent("gtacnr:serverDateChanged", new object[1] { text });
		}
		catch (Exception exception)
		{
			Print("^1ERROR: Unable to obatin server's date and time. Features like discounts and time-limited offers might not work as intended!");
			Print(exception);
		}
	}

	private async void LoadLocalization()
	{
		if (LocalizationLoaded)
		{
			return;
		}
		string text = await TriggerServerEventAsync<string>("gtacnr:getLocale", new object[0]);
		if (string.IsNullOrEmpty(text))
		{
			text = LocalizationController.GetLocaleFromGTALanguage();
		}
		Font defaultHeaderFont = MenuController.DefaultHeaderFont;
		Font defaultTextFont = MenuController.DefaultTextFont;
		MenuController.TextDirection defaultTextDirection = MenuController.DefaultTextDirection;
		switch (text)
		{
		case "ar-001":
			MenuController.DefaultHeaderFont = Font.AbdoLine;
			MenuController.DefaultTextFont = Font.Janna;
			MenuController.DefaultTextDirection = MenuController.TextDirection.RTL;
			break;
		case "zh-Hant":
			MenuController.DefaultHeaderFont = Font.BoDangXingKai;
			break;
		case "zh-Hans":
			MenuController.DefaultHeaderFont = Font.STXingKai;
			break;
		default:
			if (text.In("vi-VN", "tr-TR", "sv-SE", "no-NO", "lv-LV", "cs-CZ", "pl-PL", "ru-RU"))
			{
				MenuController.DefaultHeaderFont = Font.Ephesis;
				MenuController.DefaultTextFont = Font.NotoSansDisplay;
			}
			break;
		}
		foreach (Menu menu in MenuController.Menus)
		{
			if (menu.HeaderFont == defaultHeaderFont)
			{
				menu.HeaderFont = MenuController.DefaultHeaderFont;
			}
			if (menu.TextFont == defaultTextFont)
			{
				menu.TextFont = MenuController.DefaultTextFont;
			}
			if (menu.TextDirection == defaultTextDirection)
			{
				menu.TextDirection = MenuController.DefaultTextDirection;
			}
		}
		if (LocalizationController.CurrentLanguage != text)
		{
			if (LocalizationController.CurrentLanguage != null)
			{
				LocalizationController.Unload(LocalizationController.CurrentLanguage);
			}
			LocalizationController.CurrentLanguage = text;
			LocalizationController.Load(text);
		}
		LocalizationLoaded = true;
		Preferences.PreferredLanguage.Set(LocalizationController.CurrentLanguage);
		Debug.WriteLine("Language loaded: " + LocalizationController.CurrentLanguage);
	}
}
