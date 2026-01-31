using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Vehicles;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Crimes.Exporting;

public class ScrappingScript : Script
{
	private readonly int KEYBOARD_CONTROL = 246;

	private readonly int GAMEPAD_CONTROL = 303;

	private readonly int HOLD_TIME = 500;

	private readonly float ACTION_RADIUS = 3.5f;

	private readonly Vector3 MARKER_SIZE = new Vector3(3.5f, 3.5f, 0.75f);

	private readonly Color MARKER_COLOR = 3137339520u;

	private readonly BlipSprite BLIP_SPRITE = (BlipSprite)527;

	private readonly BlipColor BLIP_COLOR;

	private List<Scrapyard> scrapyards = Gtacnr.Utils.LoadJson<List<Scrapyard>>("data/vehicles/scrapyards.json");

	private List<VehicleSellInfo> scrapValues = Gtacnr.Utils.LoadJson<List<VehicleSellInfo>>("data/vehicles/scrappableVehicles.json");

	private Scrapyard closestScrapyard;

	private Blip scrapBlip;

	private bool isJobDisallowed;

	private bool canScrapCurrentVehicle;

	private bool isCloseToScrapyard;

	private bool canSetGPS;

	private bool isInScrapMarker;

	private bool sellInstructionShown;

	private bool isSellingVehicle;

	protected override void OnStarted()
	{
		AttachEventHandlers();
		CreateBlips();
	}

	private void AttachEventHandlers()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		ShuffleSeatScript.SeatShuffled = (EventHandler<VehicleEventArgs>)Delegate.Combine(ShuffleSeatScript.SeatShuffled, new EventHandler<VehicleEventArgs>(OnEnteredVehicle));
		VehicleEvents.LeftVehicle += OnLeftVehicle;
	}

	private void CreateBlips()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		foreach (Scrapyard scrapyard in scrapyards)
		{
			Blip obj = World.CreateBlip(scrapyard.Position);
			obj.Sprite = BLIP_SPRITE;
			obj.Color = BLIP_COLOR;
			Utils.SetBlipName(obj, "Scrapyard", "scrapyard");
			obj.Scale = 1f;
			obj.IsShortRange = true;
		}
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		Vehicle vehicle = e.Vehicle;
		VehicleSeat seat = e.Seat;
		VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)vehicle).NetworkId);
		if (isJobDisallowed || closestScrapyard == null || (int)seat != -1 || !string.IsNullOrEmpty(vehicleState?.PersonalVehicleId) || DealershipScript.IsInDealership || API.GetEntityPopulationType(((PoolObject)vehicle).Handle) >= 6)
		{
			return;
		}
		VehicleSellInfo vehicleSellInfo = scrapValues.FirstOrDefault((VehicleSellInfo s) => Model.op_Implicit(API.GetHashKey(s.Model)) == ((Entity)e.Vehicle).Model);
		if (vehicleSellInfo == null)
		{
			return;
		}
		int value = Convert.ToInt32((float)vehicleSellInfo.Price * closestScrapyard.Multiplier);
		if (value <= 0)
		{
			return;
		}
		canScrapCurrentVehicle = true;
		string ctrls = "";
		if (!isCloseToScrapyard)
		{
			ctrls = ((!Utils.IsUsingKeyboard()) ? "Hold ~INPUT_REPLAY_SCREENSHOT~ to set the GPS." : "Press ~INPUT_MP_TEXT_CHAT_TEAM~ to set the GPS.");
		}
		await BaseScript.Delay(1000);
		Utils.DisplayHelpText("You can scrap this vehicle for ~g~" + value.ToCurrencyString() + "~s~. " + ctrls);
		if (!isCloseToScrapyard)
		{
			EnableGPSInstructionalButtons();
			canSetGPS = true;
			CreateScrapyardBlip();
			await BaseScript.Delay(10000);
			if (canSetGPS)
			{
				scrapBlip.IsShortRange = true;
			}
			Utils.DisplayHelpText();
			DisableGPSInstructionalButtons();
			canSetGPS = false;
		}
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		canSetGPS = false;
		canScrapCurrentVehicle = false;
		isInScrapMarker = false;
		DestroyScrapyardBlip();
		DisableGPSInstructionalButtons();
		DisableSellInstructionalButtons();
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(100);
		isCloseToScrapyard = false;
		isJobDisallowed = Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService();
		if ((int)Game.PlayerPed.SeatIndex == -1 && !isJobDisallowed)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num = ((Vector3)(ref position)).DistanceToSquared2D(closestScrapyard.Position);
			isCloseToScrapyard = num <= 2500f;
			isInScrapMarker = num <= ACTION_RADIUS * ACTION_RADIUS;
		}
	}

	[Update]
	private async Coroutine ClosestScrapyardTask()
	{
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 99980000f;
		foreach (Scrapyard scrapyard in scrapyards)
		{
			float num2 = ((Vector3)(ref position)).DistanceToSquared(scrapyard.Position);
			if (num2 < num)
			{
				num = num2;
				closestScrapyard = scrapyard;
			}
		}
		await Script.Wait(2000);
	}

	[Update]
	private async Coroutine SellTask()
	{
		if (!canScrapCurrentVehicle || !isCloseToScrapyard || isJobDisallowed || closestScrapyard == null)
		{
			return;
		}
		Vector3 position = closestScrapyard.Position;
		Vector3 mARKER_SIZE = MARKER_SIZE;
		Color mARKER_COLOR = MARKER_COLOR;
		float z = 0f;
		if (API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref z, false))
		{
			position.Z = z;
		}
		API.DrawMarker(1, position.X, position.Y, position.Z, 0f, 0f, 0f, 0f, 0f, 0f, mARKER_SIZE.X, mARKER_SIZE.Y, mARKER_SIZE.Z, (int)mARKER_COLOR.R, (int)mARKER_COLOR.G, (int)mARKER_COLOR.B, (int)mARKER_COLOR.A, false, true, 2, false, (string)null, (string)null, false);
		if (isInScrapMarker)
		{
			if (!sellInstructionShown)
			{
				sellInstructionShown = true;
				EnableSellInstructionalButtons();
				int amount = Convert.ToInt32((float)scrapValues.FirstOrDefault((VehicleSellInfo s) => Model.op_Implicit(API.GetHashKey(s.Model)) == ((Entity)Game.PlayerPed.CurrentVehicle).Model).Price * closestScrapyard.Multiplier);
				Utils.DisplaySubtitle("Scrap this vehicle for ~g~" + amount.ToCurrencyString() + "~s~.");
			}
		}
		else if (sellInstructionShown)
		{
			sellInstructionShown = false;
			DisableSellInstructionalButtons();
		}
	}

	[Update]
	private async Coroutine ControlsTask()
	{
		if (isJobDisallowed)
		{
			return;
		}
		bool pressed = false;
		if (Utils.IsUsingKeyboard())
		{
			pressed = API.IsControlJustPressed(2, KEYBOARD_CONTROL);
		}
		else if (API.IsControlJustPressed(2, GAMEPAD_CONTROL))
		{
			DateTime gamePadPressTimestamp = DateTime.UtcNow;
			while (API.IsControlPressed(2, GAMEPAD_CONTROL))
			{
				await Script.Yield();
				if (Gtacnr.Utils.CheckTimePassed(gamePadPressTimestamp, HOLD_TIME))
				{
					pressed = true;
					break;
				}
			}
		}
		if (pressed)
		{
			if (canScrapCurrentVehicle && isCloseToScrapyard && isInScrapMarker)
			{
				TrySellVehicle();
			}
			else if (canSetGPS)
			{
				SetScrapyardGPS();
				Utils.DisplayHelpText();
			}
		}
	}

	private void SetScrapyardGPS()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (closestScrapyard != null)
		{
			DisableGPSInstructionalButtons();
			canSetGPS = false;
			if (scrapBlip != (Blip)null)
			{
				((PoolObject)scrapBlip).Delete();
			}
			scrapBlip = GPSScript.SetDestination("Scrapyard", closestScrapyard.Position, 0f, shortRange: false, BLIP_SPRITE, BLIP_COLOR);
			API.SetBlipDisplay(((PoolObject)scrapBlip).Handle, 5);
			Utils.DisplaySubtitle("Go to the ~y~Scrapyard ~s~to get ~g~paid ~s~for the ~b~parts ~s~and ~b~materials~s~.", 10000);
		}
	}

	private async void CreateScrapyardBlip()
	{
		if (!(scrapBlip != (Blip)null) && closestScrapyard != null)
		{
			scrapBlip = World.CreateBlip(closestScrapyard.Position);
			scrapBlip.Sprite = BLIP_SPRITE;
			scrapBlip.Color = BLIP_COLOR;
			scrapBlip.IsFlashing = true;
			Utils.SetBlipName(scrapBlip, "Scrapyard", "scrapyard");
			API.SetBlipDisplay(((PoolObject)scrapBlip).Handle, 5);
			await BaseScript.Delay(10000);
			if (scrapBlip != (Blip)null && ((PoolObject)scrapBlip).Exists())
			{
				scrapBlip.IsFlashing = false;
			}
		}
	}

	private void DestroyScrapyardBlip()
	{
		if (!(scrapBlip == (Blip)null))
		{
			((PoolObject)scrapBlip).Delete();
			scrapBlip = null;
			GPSScript.ClearDestination("Scrapyard");
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
			VehicleSellInfo vehicleSellInfo = scrapValues.FirstOrDefault((VehicleSellInfo s) => Model.op_Implicit(API.GetHashKey(s.Model)) == ((Entity)vehicleToSell).Model);
			int value = Convert.ToInt32((float)vehicleSellInfo.Price * closestScrapyard.Multiplier);
			switch (await TriggerServerEventAsync<int>("gtacnr:vehicles:scrap", new object[1] { scrapyards.IndexOf(closestScrapyard) }))
			{
			case 1:
			{
				DestroyScrapyardBlip();
				string localizedName = vehicleToSell.LocalizedName;
				((PoolObject)vehicleToSell).Delete();
				Utils.DisplayHelpText("You sold a ~b~" + localizedName + " ~s~for ~g~" + value.ToCurrencyString() + "~s~.");
				goto end_IL_00cc;
			}
			case 6:
				Utils.DisplayHelpText("This vehicle is ~r~not wanted ~s~for scrap!");
				break;
			case 9:
				Utils.DisplayHelpText("~r~You cannot scrap stolen vehicles on this job!");
				break;
			case 8:
				Utils.DisplayHelpText("~r~You cannot scrap a personal vehicle!");
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
			if ((int)Game.PlayerPed.SeatIndex == -1 && isInScrapMarker)
			{
				EnableSellInstructionalButtons();
			}
			end_IL_00cc:;
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		isSellingVehicle = false;
		await Utils.FadeIn();
	}

	private void EnableGPSInstructionalButtons()
	{
		if (Utils.IsUsingKeyboard())
		{
			Utils.AddInstructionalButton("exportGps", new InstructionalButton("Set GPS", 2, KEYBOARD_CONTROL));
		}
		else
		{
			Utils.AddInstructionalButton("exportGps", new InstructionalButton("Set GPS (hold)", 2, GAMEPAD_CONTROL));
		}
	}

	private void DisableGPSInstructionalButtons()
	{
		Utils.RemoveInstructionalButton("exportGps");
	}

	private void EnableSellInstructionalButtons()
	{
		if (Utils.IsUsingKeyboard())
		{
			Utils.AddInstructionalButton("export", new InstructionalButton("Sell", 2, KEYBOARD_CONTROL));
		}
		else
		{
			Utils.AddInstructionalButton("export", new InstructionalButton("Sell (hold)", 2, GAMEPAD_CONTROL));
		}
	}

	private void DisableSellInstructionalButtons()
	{
		Utils.RemoveInstructionalButton("export");
	}
}
