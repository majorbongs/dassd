using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Crimes;

public class CrimeScript : Script
{
	private Blip crimeAreaBlip;

	private float wlRange;

	private List<Gtacnr.Model.Crime> crimesCommitted = new List<Gtacnr.Model.Crime>();

	private Menu crimesMenu;

	private int prevWL;

	public static CrimeScript Instance { get; private set; }

	public CrimeScript()
	{
		Instance = this;
	}

	public static void DeleteCrimeAreaBlip()
	{
		if (Instance.crimeAreaBlip != (Blip)null)
		{
			((PoolObject)Instance.crimeAreaBlip).Delete();
		}
	}

	protected override void OnStarted()
	{
		crimesMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_CRIME_COMMITTED_TITLE), LocalizationController.S(Entries.Businesses.MENU_CRIME_COMMITTED_SUBTITLE));
		MenuController.AddMenu(crimesMenu);
		Chat.AddSuggestion("/crimes", LocalizationController.S(Entries.Businesses.MENU_CRIME_COMMITTED_SUGGESTION));
		API.SetAudioFlag("PoliceScannerDisabled", true);
	}

	[Update]
	private async Coroutine UpdateRealWantedLevel()
	{
		await Script.Wait(2000);
		if (Gtacnr.Client.API.Crime.CachedWantedLevel.HasValue)
		{
			int value = Gtacnr.Client.API.Crime.CachedWantedLevel.Value;
			int num;
			if (crimeAreaBlip != (Blip)null)
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				num = ((((Vector3)(ref position)).DistanceToSquared2D(crimeAreaBlip.Position) <= wlRange * wlRange) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool flag = (byte)num != 0;
			bool flag2 = JurisdictionScript.IsPointOutOfJurisdiction(((Entity)Game.PlayerPed).Position);
			if (value == 5 && (prevWL < 5 || flag) && !CuffedScript.IsCuffed && !Game.PlayerPed.IsBeingStunned)
			{
				API.SetMaxWantedLevel(5);
				API.SetPlayerWantedLevel(API.PlayerId(), 5, false);
				API.SetPlayerWantedLevelNow(API.PlayerId(), false);
				API.SetDispatchTimeBetweenSpawnAttemptsMultiplier(12, 3f);
			}
			else if ((value < 5 && prevWL == 5) || !flag || flag2 || CuffedScript.IsCuffed || Game.PlayerPed.IsBeingStunned)
			{
				API.SetMaxWantedLevel(0);
				API.SetPlayerWantedLevel(API.PlayerId(), 0, false);
				API.SetPlayerWantedLevelNow(API.PlayerId(), false);
			}
			prevWL = Gtacnr.Client.API.Crime.CachedWantedLevel.Value;
		}
	}

	[EventHandler("gtacnr:crimes:crimeCommitted")]
	private async void OnCrimeCommitted(string crimeJson)
	{
		Gtacnr.Model.Crime crime = crimeJson.Unjson<Gtacnr.Model.Crime>();
		CrimeType? definition = Gtacnr.Data.Crimes.GetDefinition(crime.CrimeType);
		string colorStr = definition.ColorStr;
		string text = Utils.LocalizeCrimeSeverity(definition.CrimeSeverity).ToLowerInvariant();
		string text2 = Gtacnr.Utils.ResolveLocalization(definition.Description);
		string text3 = ((!definition.IsViolent) ? (colorStr + text) : ("~r~" + LocalizationController.S(Entries.Crime.CRIME_MODIFIER_VIOLENT, text)));
		Utils.DisplayHelpText(LocalizationController.S(Entries.Crime.COMMITTED_CRIME, text3, text2), playSound: false);
		API.FlashMinimapDisplay();
		crime.DateTime = DateTime.UtcNow;
		crimesCommitted.Add(crime);
		if (crimesCommitted.Count > 50)
		{
			crimesCommitted.RemoveAt(0);
		}
		if (crime.WantedLevelAfter != crime.WantedLevelBefore)
		{
			await BaseScript.Delay(5000);
			string text4 = null;
			if (crime.WantedLevelAfter >= 5)
			{
				text4 = LocalizationController.S(Entries.Crime.CRIME_MOST_WANTED);
			}
			else if (crime.WantedLevelAfter > 1 && crime.WantedLevelBefore < 2)
			{
				text4 = LocalizationController.S(Entries.Crime.CRIME_WARRANT_ISSUED);
			}
			if (!string.IsNullOrEmpty(text4))
			{
				Utils.DisplayHelpText(text4);
			}
		}
		if (crimeAreaBlip != (Blip)null)
		{
			((PoolObject)crimeAreaBlip).Delete();
		}
		wlRange = Constants.Crime.RANGE[crime.WantedLevelAfter - 1];
		crimeAreaBlip = World.CreateBlip(crime.Location, wlRange);
		crimeAreaBlip.IsShortRange = false;
		crimeAreaBlip.Sprite = (BlipSprite)(-1);
		crimeAreaBlip.Color = (BlipColor)1;
		crimeAreaBlip.Alpha = 64;
		Utils.SetBlipName(crimeAreaBlip, "Last Crime Location", "crime_location");
		API.SetBlipDisplay(((PoolObject)crimeAreaBlip).Handle, 8);
	}

	[EventHandler("gtacnr:crimes:wantedLevelChanged")]
	private void OnWantedLevelChanged(int oldLevel, int newLevel)
	{
		if (crimeAreaBlip != (Blip)null && newLevel == 0)
		{
			((PoolObject)crimeAreaBlip).Delete();
		}
	}

	[Update]
	private async Coroutine UpdateLastCrimeAreaBlip()
	{
		await Script.Wait(500);
		if (!(crimeAreaBlip != (Blip)null))
		{
			return;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			((PoolObject)crimeAreaBlip).Delete();
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared2D(crimeAreaBlip.Position) <= wlRange * wlRange)
		{
			if ((int)crimeAreaBlip.Color == 1)
			{
				crimeAreaBlip.Color = (BlipColor)54;
			}
			else if ((int)crimeAreaBlip.Color == 54)
			{
				crimeAreaBlip.Color = (BlipColor)1;
			}
		}
		else
		{
			((PoolObject)crimeAreaBlip).Delete();
		}
	}

	[Command("crimes")]
	private async Task CrimesCommand(string[] args)
	{
		try
		{
			int result;
			if (args.Length == 0 || (int)StaffLevelScript.StaffLevel < 100)
			{
				if (crimesCommitted.Count == 0)
				{
					Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You didn't commit any crime in this session.");
					return;
				}
				PopulateMenuWithCrimes(crimesMenu, crimesCommitted);
				crimesMenu.OpenMenu();
			}
			else if (!int.TryParse(args[0], out result))
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Invalid player ID.");
			}
			else
			{
				await OpenOtherPlayerCrimes(result, delegate(string errorMessage)
				{
					Chat.AddMessage(Gtacnr.Utils.Colors.Error, errorMessage);
				}, null);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	public async Task<bool> OpenOtherPlayerCrimes(int playerId, Action<string> showError, Menu parentMenu)
	{
		if (!ModeratorMenuScript.IsOnDuty)
		{
			showError("You must be on duty as a moderator to use this command.");
			return false;
		}
		if (LatentPlayers.Get(playerId) == null)
		{
			showError("Player not found.");
			return false;
		}
		string text = await TriggerServerEventAsync<string>("gtacnr:admin:fetchCrimes", new object[1] { playerId });
		if (text == null)
		{
			showError("Failed to fetch crimes for the specified player.");
			return false;
		}
		List<Gtacnr.Model.Crime> list = text.Unjson<List<Gtacnr.Model.Crime>>();
		if (list.Count == 0)
		{
			showError("Specified player didn't commit any crime in this session.");
			return false;
		}
		PopulateMenuWithCrimes(crimesMenu, list, parentMenu);
		MenuController.CloseAllMenus();
		crimesMenu.OpenMenu();
		return true;
	}

	private static void PopulateMenuWithCrimes(Menu menu, IEnumerable<Gtacnr.Model.Crime> crimes, Menu parentMenu = null)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		menu.ClearMenuItems();
		menu.ParentMenu = parentMenu;
		foreach (Gtacnr.Model.Crime item in crimes.OrderByDescending((Gtacnr.Model.Crime c) => c.DateTime))
		{
			CrimeType definition = Gtacnr.Data.Crimes.GetDefinition(item.CrimeType);
			string colorStr = definition.ColorStr;
			string text = Gtacnr.Utils.CalculateTimeAgo(item.DateTime);
			string locationName = Utils.GetLocationName(item.Location);
			MenuItem menuItem = new MenuItem(colorStr + Gtacnr.Utils.ResolveLocalization(definition.Description));
			menuItem.Description = colorStr + Utils.LocalizeCrimeSeverity(definition.CrimeSeverity).ToUpper() + "\n" + colorStr + "Time: ~s~" + text + "\n" + colorStr + "Location: ~s~" + locationName;
			MenuItem menuItem2 = menuItem;
			if (!string.IsNullOrEmpty(item.AffectedBusinessId) && BusinessScript.Businesses.TryGetValue(item.AffectedBusinessId, out Business value))
			{
				menuItem = menuItem2;
				menuItem.Description = menuItem.Description + "\n" + colorStr + "Store: ~s~" + value.Name;
			}
			if (!string.IsNullOrEmpty(item.InvolvedVehicleModelId))
			{
				int hashKey = API.GetHashKey(item.InvolvedVehicleModelId);
				string text2 = string.Concat(str2: Game.GetGXTEntry(Vehicle.GetModelDisplayName(Model.op_Implicit(hashKey))), str0: Game.GetGXTEntry(API.GetMakeNameFromVehicleModel((uint)hashKey)), str1: " ");
				menuItem = menuItem2;
				menuItem.Description = menuItem.Description + "\n" + colorStr + "Vehicle: ~s~" + text2;
			}
			if (item.AffectedPlayerId > 0)
			{
				menuItem2.Description += $"\n{colorStr}Player: ~s~{item.AffectedPlayerName} ({item.AffectedPlayerId})";
			}
			menuItem2.Label = colorStr + item.DamageValue.ToCurrencyString();
			menuItem2.Description += $"\n{colorStr}Wanted level: ~s~{item.WantedLevelBefore} -> {item.WantedLevelAfter}\n{colorStr}Fine/Bail: ~r~{item.FineBefore.ToCurrencyString()} ~s~-> ~r~{item.FineAfter.ToCurrencyString()}";
			menu.AddMenuItem(menuItem2);
		}
	}
}
