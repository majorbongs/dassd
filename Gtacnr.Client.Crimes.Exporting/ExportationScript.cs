using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Vehicles;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Crimes.Exporting;

public class ExportationScript : Script
{
	private readonly Control KEYBOARD_CONTROL = (Control)246;

	private readonly Control GAMEPAD_CONTROL = (Control)303;

	private readonly Vector3 EXPORT_LOCATION = new Vector3(460.5544f, -3026.0212f, 5.6283f);

	private readonly float EXPORT_RADIUS = 3.5f;

	private readonly Vector3 EXPORT_MARKER_SIZE = new Vector3(3.5f, 3.5f, 0.75f);

	private readonly Color EXPORT_MARKER_COLOR = 3137339520u;

	private readonly BlipSprite BLIP_SPRITE = (BlipSprite)68;

	private readonly BlipColor BLIP_COLOR;

	private bool canSetGPS;

	private bool canExportCurrentVehicle;

	private bool isCloseToExport;

	private bool isInExportMarker;

	private Blip exportBlip;

	private int vehicleOfTheDayModelHash;

	private int vehicleOfTheDayPrize;

	private readonly List<Vehicle> nearbyVehiclesOfTheDay = new List<Vehicle>();

	private const float VEHICLE_OF_THE_DAY_MARKER_SIZE = 0.7f;

	private readonly Color VehicleOfTheDayMarkerColor = 2060153984;

	private bool isSellingVehicle;

	private bool IsCurrentJobDisallowed
	{
		get
		{
			if (!Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
			{
				return string.IsNullOrEmpty(Gtacnr.Client.API.Jobs.CachedJob);
			}
			return true;
		}
	}

	public ExportationScript()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		ShuffleSeatScript.SeatShuffled = (EventHandler<VehicleEventArgs>)Delegate.Combine(ShuffleSeatScript.SeatShuffled, new EventHandler<VehicleEventArgs>(OnEnteredVehicle));
		VehicleEvents.LeftVehicle += OnLeftVehicle;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	protected override void OnStarted()
	{
		BaseScript.TriggerServerEvent("gtacnr:vehicles:getBonusVehicleInfo", new object[0]);
		CreateBlips();
		AddChatSuggestions();
	}

	private void CreateBlips()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Blip obj = World.CreateBlip(EXPORT_LOCATION);
		obj.Sprite = BLIP_SPRITE;
		obj.Color = BLIP_COLOR;
		Utils.SetBlipName(obj, "Car Export", "export");
		obj.IsShortRange = true;
	}

	private void AddChatSuggestions()
	{
		Chat.AddSuggestion("/mwveh", "Shows information about the current most wanted vehicle.");
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		Vehicle vehicle = e.Vehicle;
		VehicleSeat seat = e.Seat;
		VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)vehicle).NetworkId);
		if (IsCurrentJobDisallowed || !string.IsNullOrEmpty(vehicleState?.PersonalVehicleId) || DealershipScript.IsInDealership || API.GetEntityPopulationType(((PoolObject)vehicle).Handle) >= 6 || (int)seat != -1)
		{
			return;
		}
		int value = await TriggerServerEventAsync<int>("gtacnr:vehicles:getExportValue", new object[0]);
		if (value <= 0)
		{
			return;
		}
		canExportCurrentVehicle = true;
		string mwveh = "";
		if (vehicleOfTheDayModelHash == ((Entity)vehicle).Model.Hash)
		{
			string locationName = Utils.GetLocationName(((Entity)Game.PlayerPed).Position);
			BaseScript.TriggerServerEvent("gtacnr:vehicles:enteredWantedVehicle", new object[1] { locationName });
			mwveh = "~b~Most wanted vehicle! ~s~";
		}
		string ctrls = "";
		if (!isCloseToExport)
		{
			ctrls = ((!Utils.IsUsingKeyboard()) ? "Hold ~INPUT_REPLAY_SCREENSHOT~ to set the GPS." : "Press ~INPUT_MP_TEXT_CHAT_TEAM~ to set the GPS.");
		}
		await BaseScript.Delay(1000);
		if (isCloseToExport)
		{
			Utils.DisplayHelpText(mwveh + "You can export this vehicle for ~g~" + value.ToCurrencyString() + "~s~.");
			return;
		}
		CreateExportBlip();
		canSetGPS = true;
		bool wasAccepted = false;
		await InteractiveNotificationsScript.Show(mwveh + "You can export this vehicle for ~g~" + value.ToCurrencyString() + "~s~. " + ctrls, InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Set GPS", "Set GPS (hold)", () => !canSetGPS);
		if (!wasAccepted && exportBlip != (Blip)null)
		{
			exportBlip.IsShortRange = true;
		}
		bool OnAccepted()
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			Utils.DisplayHelpText();
			Blip obj = exportBlip;
			if (obj != null)
			{
				((PoolObject)obj).Delete();
			}
			exportBlip = GPSScript.SetDestination("Vehicle Export Dock", EXPORT_LOCATION, 0f, shortRange: false, BLIP_SPRITE, BLIP_COLOR);
			API.SetBlipDisplay(((PoolObject)exportBlip).Handle, 5);
			Utils.DisplaySubtitle("Go to ~y~the docks ~s~and bring this ~b~vehicle ~s~to the ~r~exporters~s~.", 10000);
			wasAccepted = true;
			return true;
		}
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		canSetGPS = false;
		canExportCurrentVehicle = false;
		isInExportMarker = false;
		DestroyExportBlip();
		DisableSellInstructionalButtons();
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		if (!IsCurrentJobDisallowed)
		{
			base.Update += UpdateTick;
			base.Update += FindNearbyVehiclesOfTheDayTick;
		}
	}

	private async Coroutine UpdateTick()
	{
		await Script.Wait(500);
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		bool flag = isCloseToExport;
		bool flag2 = isInExportMarker;
		isCloseToExport = false;
		isInExportMarker = false;
		if (canExportCurrentVehicle)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num = ((Vector3)(ref position)).DistanceToSquared(EXPORT_LOCATION);
			isCloseToExport = num <= 2500f;
			isInExportMarker = num <= EXPORT_RADIUS * EXPORT_RADIUS;
		}
		bool isCurrentJobDisallowed = IsCurrentJobDisallowed;
		if (isCloseToExport && !flag && !isCurrentJobDisallowed)
		{
			base.Update += DrawExportMarker;
		}
		else if (!isCloseToExport && flag)
		{
			base.Update -= DrawExportMarker;
		}
		if (isInExportMarker && !flag2 && !isCurrentJobDisallowed)
		{
			EnableSellInstructionalButtons();
			if (Utils.IsUsingKeyboard())
			{
				KeysScript.AttachListener(KEYBOARD_CONTROL, OnKeyEvent);
			}
			else
			{
				KeysScript.AttachListener(GAMEPAD_CONTROL, OnKeyEvent);
			}
		}
		else if (!isInExportMarker && flag2)
		{
			DisableSellInstructionalButtons();
			KeysScript.DetachListener(KEYBOARD_CONTROL, OnKeyEvent);
			KeysScript.DetachListener(GAMEPAD_CONTROL, OnKeyEvent);
		}
	}

	private async Coroutine DrawExportMarker()
	{
		Vector3 eXPORT_LOCATION = EXPORT_LOCATION;
		Vector3 eXPORT_MARKER_SIZE = EXPORT_MARKER_SIZE;
		Color eXPORT_MARKER_COLOR = EXPORT_MARKER_COLOR;
		float z = 0f;
		if (API.GetGroundZFor_3dCoord(eXPORT_LOCATION.X, eXPORT_LOCATION.Y, eXPORT_LOCATION.Z, ref z, false))
		{
			eXPORT_LOCATION.Z = z;
		}
		API.DrawMarker(1, eXPORT_LOCATION.X, eXPORT_LOCATION.Y, eXPORT_LOCATION.Z, 0f, 0f, 0f, 0f, 0f, 0f, eXPORT_MARKER_SIZE.X, eXPORT_MARKER_SIZE.Y, eXPORT_MARKER_SIZE.Z, (int)eXPORT_MARKER_COLOR.R, (int)eXPORT_MARKER_COLOR.G, (int)eXPORT_MARKER_COLOR.B, (int)eXPORT_MARKER_COLOR.A, false, true, 2, false, (string)null, (string)null, false);
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (!isInExportMarker)
		{
			return false;
		}
		bool flag = Utils.IsUsingKeyboard();
		if (!(control == KEYBOARD_CONTROL && eventType == KeyEventType.JustPressed && flag) && (control != GAMEPAD_CONTROL || eventType != KeyEventType.Held || flag))
		{
			return false;
		}
		TrySellVehicle();
		return true;
	}

	private async Coroutine FindNearbyVehiclesOfTheDayTick()
	{
		await Script.Wait(1000);
		bool flag = nearbyVehiclesOfTheDay.Count > 0;
		nearbyVehiclesOfTheDay.Clear();
		if (vehicleOfTheDayModelHash != 0)
		{
			Vehicle[] allVehicles = World.GetAllVehicles();
			foreach (Vehicle val in allVehicles)
			{
				if (((Entity)val).Model.Hash == vehicleOfTheDayModelHash)
				{
					Ped driver = val.Driver;
					if (driver != null && driver.IsPlayer && string.IsNullOrEmpty(LatentVehicleStateScript.Get(((Entity)val).NetworkId)?.PersonalVehicleId))
					{
						nearbyVehiclesOfTheDay.Add(val);
					}
				}
			}
		}
		if (nearbyVehiclesOfTheDay.Count > 0 && !flag && !IsCurrentJobDisallowed)
		{
			base.Update += DrawNearbyVehiclesOfTheDayTick;
		}
		else if (nearbyVehiclesOfTheDay.Count == 0 && flag)
		{
			base.Update -= DrawNearbyVehiclesOfTheDayTick;
		}
	}

	private async Coroutine DrawNearbyVehiclesOfTheDayTick()
	{
		Color vehicleOfTheDayMarkerColor = VehicleOfTheDayMarkerColor;
		foreach (Vehicle item in nearbyVehiclesOfTheDay)
		{
			Vector3 position = ((Entity)item).Position;
			position.Z += 2f;
			API.DrawMarker(0, position.X, position.Y, position.Z, 0f, 0f, 0f, 0f, 0f, 0f, 0.7f, 0.7f, 0.7f, (int)vehicleOfTheDayMarkerColor.R, (int)vehicleOfTheDayMarkerColor.G, (int)vehicleOfTheDayMarkerColor.B, (int)vehicleOfTheDayMarkerColor.A, true, true, 2, false, (string)null, (string)null, false);
		}
	}

	[EventHandler("gtacnr:vehicles:announceVehicleOfTheDay")]
	private void OnAnnounceVehicleOfTheDay(int modelHash, int prize, bool bonusIncreased)
	{
		if (!IsCurrentJobDisallowed)
		{
			vehicleOfTheDayModelHash = modelHash;
			vehicleOfTheDayPrize = prize;
			string vehicleFullName = Utils.GetVehicleFullName(modelHash);
			Utils.SendNotification(bonusIncreased ? ("Today's most wanted vehicle is still ~y~" + vehicleFullName + "~s~! Reward increased: ~g~" + prize.ToCurrencyString() + "~s~. Type ~y~/mwveh ~s~to show it again.") : ("Today's most wanted vehicle is ~y~" + vehicleFullName + "~s~. Reward: ~g~" + prize.ToCurrencyString() + "~s~. Type ~y~/mwveh ~s~to show it again."), Gtacnr.Utils.Colors.HudBlueDark);
			Game.PlaySound("Event_Message_Purple", "GTAO_FM_Events_Soundset");
		}
	}

	[EventHandler("gtacnr:vehicles:vehicleOfTheDaySold")]
	private async void OnVehicleOfTheDaySold(int winnerId, int modelHash, int prize)
	{
		vehicleOfTheDayModelHash = 0;
		vehicleOfTheDayPrize = 0;
		string vehicleFullName = Utils.GetVehicleFullName(modelHash);
		PlayerState arg = LatentPlayers.Get(winnerId);
		Utils.SendNotification($"<C>{arg}</C> exported today's most wanted vehicle <C>({vehicleFullName})</C> for <C>{prize.ToCurrencyString()}</C>.", Gtacnr.Utils.Colors.HudGreenDark);
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		bool flag = e.PreviousJobEnum.IsPublicService();
		bool flag2 = e.CurrentJobEnum.IsPublicService();
		if (flag && !flag2)
		{
			BaseScript.TriggerServerEvent("gtacnr:vehicles:getBonusVehicleInfo", new object[0]);
			base.Update += UpdateTick;
			base.Update += FindNearbyVehiclesOfTheDayTick;
		}
		else if (!flag && flag2)
		{
			base.Update -= UpdateTick;
			base.Update -= FindNearbyVehiclesOfTheDayTick;
		}
	}

	[EventHandler("gtacnr:vehicles:setBonusVehicleInfo")]
	private void SetBonusVehicleInfo(int modelHash, int prize)
	{
		vehicleOfTheDayModelHash = modelHash;
		vehicleOfTheDayPrize = prize;
	}

	[Command("mwveh")]
	private async void MostWantedVehicleCommand(string[] args)
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			Utils.SendNotification("Public officers and emergency workers can't illegally export vehicles.");
			return;
		}
		string text = "";
		if (vehicleOfTheDayModelHash != 0)
		{
			string vehicleFullName = Utils.GetVehicleFullName(vehicleOfTheDayModelHash);
			text = text + "Today's most wanted vehicle is ~b~" + vehicleFullName + " ~s~(~g~" + vehicleOfTheDayPrize.ToCurrencyString() + "~s~).";
		}
		if (!string.IsNullOrEmpty(text))
		{
			Utils.SendNotification(text);
		}
		else
		{
			Utils.SendNotification("There's currently no most wanted vehicle. Please, wait until 5:00am.");
		}
		Utils.PlaySelectSound();
	}

	private void SetExportGPS()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		canSetGPS = false;
		if (exportBlip != (Blip)null)
		{
			((PoolObject)exportBlip).Delete();
		}
		exportBlip = GPSScript.SetDestination("Car Export", EXPORT_LOCATION, 0f, shortRange: false, BLIP_SPRITE, BLIP_COLOR);
		API.SetBlipDisplay(((PoolObject)exportBlip).Handle, 5);
		Utils.DisplaySubtitle("Go to ~y~the docks ~s~and bring this ~b~vehicle ~s~to the ~r~exporters~s~.", 10000);
	}

	private async void CreateExportBlip()
	{
		if (!(exportBlip != (Blip)null))
		{
			exportBlip = World.CreateBlip(EXPORT_LOCATION);
			exportBlip.Sprite = BLIP_SPRITE;
			exportBlip.Color = BLIP_COLOR;
			exportBlip.IsFlashing = true;
			Utils.SetBlipName(exportBlip, "Car Export", "export");
			API.SetBlipDisplay(((PoolObject)exportBlip).Handle, 5);
			await BaseScript.Delay(10000);
			if (exportBlip != (Blip)null && ((PoolObject)exportBlip).Exists())
			{
				exportBlip.IsFlashing = false;
			}
		}
	}

	private void DestroyExportBlip()
	{
		if (!(exportBlip == (Blip)null))
		{
			((PoolObject)exportBlip).Delete();
			exportBlip = null;
			GPSScript.ClearDestination("Car Export");
		}
	}

	private async void TrySellVehicle()
	{
		if (isSellingVehicle)
		{
			return;
		}
		Vehicle vehicleToSell = Game.PlayerPed.CurrentVehicle;
		isSellingVehicle = true;
		DisableSellInstructionalButtons();
		await Utils.FadeOut();
		try
		{
			int value = await TriggerServerEventAsync<int>("gtacnr:vehicles:getExportValue", new object[0]);
			switch (await TriggerServerEventAsync<int>("gtacnr:vehicles:export", new object[0]))
			{
			case 1:
			{
				string vehicleFullName = Utils.GetVehicleFullName(Model.op_Implicit(((Entity)vehicleToSell).Model));
				Utils.DisplayHelpText("You exported a ~b~" + vehicleFullName + " ~s~for ~g~" + value.ToCurrencyString() + "~s~.");
				DestroyExportBlip();
				((PoolObject)vehicleToSell).Delete();
				goto end_IL_0128;
			}
			case 5:
				Utils.DisplayHelpText("This vehicle is ~r~too damaged ~s~to be exported!");
				break;
			case 6:
				Utils.DisplayHelpText("This vehicle is ~r~not wanted ~s~for exportation!");
				break;
			case 9:
				Utils.DisplayHelpText("~r~You cannot illegally export vehicles on this job!");
				break;
			case 10:
				Utils.DisplayHelpText("~b~Cops are nearby! ~r~Get rid ~s~of them before exporting the vehicle.");
				break;
			case 8:
				Utils.DisplayHelpText("~r~You cannot export a personal vehicle!");
				break;
			case 7:
				Utils.DisplayHelpText("~r~You cannot export this vehicle!");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
				break;
			case 4:
				break;
			}
			await BaseScript.Delay(3000);
			if ((int)Game.PlayerPed.SeatIndex == -1 && isInExportMarker)
			{
				EnableSellInstructionalButtons();
			}
			end_IL_0128:;
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		isSellingVehicle = false;
		await Utils.FadeIn();
	}

	private void EnableSellInstructionalButtons()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (Utils.IsUsingKeyboard())
		{
			Utils.AddInstructionalButton("export", new InstructionalButton("Export", 2, KEYBOARD_CONTROL));
		}
		else
		{
			Utils.AddInstructionalButton("export", new InstructionalButton("Export (hold)", 2, GAMEPAD_CONTROL));
		}
	}

	private void DisableSellInstructionalButtons()
	{
		Utils.RemoveInstructionalButton("export");
	}
}
