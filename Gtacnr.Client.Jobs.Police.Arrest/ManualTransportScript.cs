using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Businesses.PoliceStations;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police.Arrest;

public class ManualTransportScript : Script
{
	private readonly Vector3 ARREST_MARKER_SIZE = new Vector3(3.5f, 3.5f, 0.75f);

	private readonly Color ARREST_MARKER_COLOR = 5618560;

	private List<Blip> manualArrestBlips;

	private Vector3 closestArrestLocation;

	private int suspectInCustodySvrId;

	private static List<Vector3> manualArrestLocations = InitializeArrestLocations();

	public static ManualTransportScript Instance { get; private set; }

	public ManualTransportScript()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Instance = this;
	}

	private static List<Vector3> InitializeArrestLocations()
	{
		List<Vector3> list = new List<Vector3>();
		foreach (KeyValuePair<string, Vector3[]> item in Gtacnr.Utils.LoadJson<Dictionary<string, Vector3[]>>("data/police/arrestDropOffs.json"))
		{
			string key = item.Key;
			if (!string.IsNullOrEmpty(key))
			{
				if (key.StartsWith("!"))
				{
					if (Gtacnr.Utils.IsResourceLoadedOrLoading(key.Substring(1)))
					{
						continue;
					}
				}
				else if (!Gtacnr.Utils.IsResourceLoadedOrLoading(key))
				{
					continue;
				}
			}
			list.AddRange(item.Value);
		}
		return list;
	}

	[Update]
	private async Coroutine TransportToJailTick()
	{
		await Script.Wait(1000);
		suspectInCustodySvrId = 0;
		closestArrestLocation = default(Vector3);
		if ((Entity)(object)Game.PlayerPed == (Entity)null || !Game.PlayerPed.IsInVehicle() || (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed || !Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice())
		{
			RemoveJailBlips();
			return;
		}
		foreach (Player player in ((BaseScript)this).Players)
		{
			Ped character = player.Character;
			if (character.IsInVehicle())
			{
				PlayerState playerState = LatentPlayers.Get(player.ServerId);
				if (playerState != null && playerState.IsInCustody && API.IsPedInVehicle(((PoolObject)character).Handle, ((PoolObject)Game.PlayerPed.CurrentVehicle).Handle, false))
				{
					suspectInCustodySvrId = player.ServerId;
					break;
				}
			}
		}
		if (suspectInCustodySvrId == 0)
		{
			RemoveJailBlips();
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed.CurrentVehicle).Position;
		float num = 9.99998E+11f;
		foreach (Vector3 manualArrestLocation in manualArrestLocations)
		{
			float num2 = ((Vector3)(ref position)).DistanceToSquared(manualArrestLocation);
			if (num2 < num)
			{
				num = num2;
				closestArrestLocation = manualArrestLocation;
			}
		}
		if (manualArrestBlips == null)
		{
			CreateJailBlips();
			PlayerState playerState2 = LatentPlayers.Get(suspectInCustodySvrId);
			Utils.DisplayHelpText();
			await InteractiveNotificationsScript.Show("Transport " + playerState2.ColorNameAndId + " to the closest ~y~jail~s~.", InteractiveNotificationType.Subtitle, OnAccepted, TimeSpan.FromSeconds(10.0), 1u, "Set GPS", "Set GPS (hold)", () => !Game.PlayerPed.IsInVehicle() || ((Entity)Game.PlayerPed).IsDead);
		}
		bool OnAccepted()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			if (closestArrestLocation == default(Vector3))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x50"));
				Print("No jail available.");
				return false;
			}
			foreach (Blip manualArrestBlip in manualArrestBlips)
			{
				((PoolObject)manualArrestBlip).Delete();
			}
			Blip item = GPSScript.SetDestination("Jail", closestArrestLocation, 0f, shortRange: false, (BlipSprite)253, (BlipColor)0);
			manualArrestBlips.Add(item);
			Utils.DisplayHelpText();
			return true;
		}
	}

	[Update]
	private async Coroutine ManualArrestTick()
	{
		if (manualArrestBlips == null || closestArrestLocation == default(Vector3) || suspectInCustodySvrId == 0 || (Entity)(object)Game.PlayerPed == (Entity)null || (Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null)
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed.CurrentVehicle).Position;
		float num = ((Vector3)(ref position)).DistanceToSquared(closestArrestLocation);
		if (!(num < 625f))
		{
			return;
		}
		Vector3 val = closestArrestLocation;
		Vector3 aRREST_MARKER_SIZE = ARREST_MARKER_SIZE;
		Color aRREST_MARKER_COLOR = ARREST_MARKER_COLOR;
		float z = 0f;
		if (API.GetGroundZFor_3dCoord(val.X, val.Y, val.Z, ref z, false))
		{
			val.Z = z;
		}
		API.DrawMarker(1, val.X, val.Y, val.Z, 0f, 0f, 0f, 0f, 0f, 0f, aRREST_MARKER_SIZE.X, aRREST_MARKER_SIZE.Y, aRREST_MARKER_SIZE.Z, (int)aRREST_MARKER_COLOR.R, (int)aRREST_MARKER_COLOR.G, (int)aRREST_MARKER_COLOR.B, (int)aRREST_MARKER_COLOR.A, false, true, 2, false, (string)null, (string)null, false);
		if (!(num < aRREST_MARKER_SIZE.X * aRREST_MARKER_SIZE.X) || !Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice())
		{
			return;
		}
		await Utils.FadeOut();
		if (!(await TriggerServerEventAsync<bool>("gtacnr:police:arrest", new object[2] { suspectInCustodySvrId, true })))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
		}
		else
		{
			DateTime t = DateTime.UtcNow;
			while (!Gtacnr.Utils.CheckTimePassed(t, 5000.0) && new Ped(API.GetPlayerPed(API.GetPlayerFromServerId(suspectInCustodySvrId))).IsInVehicle())
			{
				await Script.Wait(100);
			}
			suspectInCustodySvrId = 0;
			RemoveJailBlips();
		}
		await Utils.FadeIn();
	}

	private async void CreateJailBlips()
	{
		if (manualArrestBlips != null)
		{
			return;
		}
		manualArrestBlips = new List<Blip>();
		foreach (Vector3 manualArrestLocation in manualArrestLocations)
		{
			Blip val = World.CreateBlip(manualArrestLocation);
			val.Sprite = (BlipSprite)253;
			val.Color = (BlipColor)0;
			val.IsShortRange = false;
			Utils.SetBlipName(val, "Jail", "jail");
			val.IsFlashing = true;
			manualArrestBlips.Add(val);
		}
		await BaseScript.Delay(10000);
		foreach (Blip manualArrestBlip in manualArrestBlips)
		{
			manualArrestBlip.IsFlashing = false;
		}
	}

	private void RemoveJailBlips()
	{
		if (manualArrestBlips == null)
		{
			return;
		}
		foreach (Blip manualArrestBlip in manualArrestBlips)
		{
			((PoolObject)manualArrestBlip).Delete();
		}
		manualArrestBlips = null;
		GPSScript.ClearDestination();
	}

	public static bool IsSuspectCloseToFrontDesk(Ped ped)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Business business = PoliceStationsScript.CurrentStation?.ParentBusiness;
		if (business == null)
		{
			return false;
		}
		BusinessEmployee businessEmployee = business.Employees.FirstOrDefault((BusinessEmployee e) => e.Role == EmployeeRole.Cashier);
		if (businessEmployee == null)
		{
			return false;
		}
		Vector3 position = businessEmployee.Position;
		return ((Vector3)(ref position)).DistanceToSquared(((Entity)ped).Position) <= 8f;
	}

	public async void HandOver(int suspectInCustodySvrId)
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice())
		{
			await Utils.FadeOut();
			if (!(await TriggerServerEventAsync<bool>("gtacnr:police:arrest", new object[2] { suspectInCustodySvrId, true })))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			}
			await BaseScript.Delay(500);
			await Utils.FadeIn();
		}
	}
}
