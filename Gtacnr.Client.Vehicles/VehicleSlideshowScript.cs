using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Sync;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Vehicles;

public class VehicleSlideshowScript : Script
{
	private Vector4 VEHICLE_COORDS = new Vector4(-669.8419f, -226.0024f, 36.5275f, 113.4949f);

	private Vector3 CAMERA_COORDS_1 = new Vector3(-674.8621f, -225.255f, 37.5f);

	private Vector3 CAMERA_COORDS_2 = new Vector3(-664.0055f, -227.5824f, 37.5f);

	private bool isInSlideshow;

	private DealershipSupply currentSupply;

	private MembershipTier membershipTier;

	private int cameraHandle;

	private int cameraPosition;

	private List<string> shownModels = new List<string>();

	private bool stopRequested;

	private static VehicleSlideshowScript instance;

	public static bool IsInSlideshow => instance.isInSlideshow;

	public VehicleSlideshowScript()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		instance = this;
	}

	[EventHandler("gtacnr:vehicles:slideshow:start")]
	private async void OnVehicleSlideshowStart(string tier)
	{
		if (!isInSlideshow)
		{
			if (tier.ToLowerInvariant() == "silver")
			{
				membershipTier = MembershipTier.Silver;
			}
			else if (tier.ToLowerInvariant() == "gold")
			{
				membershipTier = MembershipTier.Gold;
			}
			isInSlideshow = true;
			await Slideshow();
			isInSlideshow = false;
		}
	}

	[Command("stop-slideshow")]
	private void OnVehicleSlideshowStop()
	{
		stopRequested = true;
	}

	private async Coroutine Slideshow()
	{
		await PrepareSlideshow();
		await Utils.FadeOut();
		foreach (Business item in DealershipScript.Dealerships.OrderBy((Business biz) => biz.Dealership.Type))
		{
			if (stopRequested)
			{
				break;
			}
			foreach (DealershipSupply item2 in item.Dealership.Supplies.OrderBy((DealershipSupply sup) => Utils.GetVehicleFullName(sup.ModelData.Id)))
			{
				if (stopRequested)
				{
					break;
				}
				if (item2.ModelData != null && !item2.ModelData.WasRecalled && !item2.Unlisted && item2.ModelData.MembershipTier == membershipTier && !shownModels.Contains(item2.ModelData.Id))
				{
					try
					{
						shownModels.Add(item2.ModelData.Id);
						Vehicle vehicle = await PreviewVehicle(item2);
						await Utils.FadeIn(500);
						await BaseScript.Delay(2000);
						FlipCamera();
						await BaseScript.Delay(1000);
						await Utils.FadeOut(500);
						((PoolObject)vehicle).Delete();
						FlipCamera();
					}
					catch (Exception exception)
					{
						Print(exception);
					}
				}
			}
		}
		await FinalizeSlideshow();
	}

	private async Coroutine PrepareSlideshow()
	{
		TimeSyncScript.OverrideTime = new GameTime(16, 0);
		await Utils.TeleportToCoords(VEHICLE_COORDS.XYZ());
		if (!HideHUDScript.ScreenshotMode)
		{
			HideHUDScript.ScreenshotMode = true;
		}
		API.DestroyAllCams(true);
		cameraPosition = 1;
		cameraHandle = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", CAMERA_COORDS_1.X, CAMERA_COORDS_1.Y, CAMERA_COORDS_1.Z, 0f, 0f, 0f, 45f, false, 0);
		API.SetCamActive(cameraHandle, true);
		API.PointCamAtCoord(cameraHandle, VEHICLE_COORDS.X, VEHICLE_COORDS.Y, VEHICLE_COORDS.Z);
		API.RenderScriptCams(true, false, 2000, true, true);
		base.Update += DrawTask;
	}

	private async Coroutine FinalizeSlideshow()
	{
		TimeSyncScript.OverrideTime = null;
		if (cameraHandle != 0)
		{
			API.SetCamActive(cameraHandle, false);
			API.DestroyCam(cameraHandle, false);
			API.RenderScriptCams(false, false, 0, true, false);
			cameraHandle = 0;
		}
		base.Update -= DrawTask;
		shownModels.Clear();
		HideHUDScript.ScreenshotMode = false;
		await Utils.FadeIn(500);
	}

	private async Task<Vehicle> PreviewVehicle(DealershipSupply supply)
	{
		currentSupply = supply;
		Debug.WriteLine("Previewing: " + supply.ModelData.Id);
		using DisposableModel disposable = new DisposableModel(Model.op_Implicit(supply.ModelData.Id))
		{
			TimeOut = TimeSpan.FromSeconds(5.0)
		};
		await disposable.Load();
		Vehicle val = await World.CreateVehicle(disposable.Model, VEHICLE_COORDS.XYZ(), VEHICLE_COORDS.W);
		Game.PlayerPed.SetIntoVehicle(val, (VehicleSeat)(-1));
		if (val.Mods.LiveryCount > 0)
		{
			val.Mods.Livery = 0;
		}
		return val;
	}

	private void FlipCamera()
	{
		if (cameraPosition == 1)
		{
			cameraPosition = 2;
			API.SetCamCoord(cameraHandle, CAMERA_COORDS_2.X, CAMERA_COORDS_2.Y, CAMERA_COORDS_2.Z);
		}
		else if (cameraPosition == 2)
		{
			cameraPosition = 1;
			API.SetCamCoord(cameraHandle, CAMERA_COORDS_1.X, CAMERA_COORDS_1.Y, CAMERA_COORDS_1.Z);
		}
		API.PointCamAtCoord(cameraHandle, VEHICLE_COORDS.X, VEHICLE_COORDS.Y, VEHICLE_COORDS.Z);
	}

	private async Coroutine DrawTask()
	{
		try
		{
			string vehicleFullName = Utils.GetVehicleFullName(currentSupply.ModelData.Id);
			Color value = currentSupply.ModelData.MembershipTier switch
			{
				MembershipTier.Gold => new Color(130, 114, 31, byte.MaxValue), 
				MembershipTier.Silver => new Color(210, 210, 210, byte.MaxValue), 
				_ => default(Color), 
			};
			Utils.Draw2DText(vehicleFullName ?? "", new Vector2(0.03f, 0.86f), value, 1.75f, 1, (Alignment)1, drawOutline: true, new Color(0, 0, 0, byte.MaxValue), 12);
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}
}
