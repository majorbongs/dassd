using System;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Items;

public class BinocularsScript : Script
{
	private bool isUsingBinoculars;

	private Prop binocularsProp;

	private bool toolTaskAttached;

	private bool binocularsKeysAttached;

	private DateTime binocularsViewStartedT = DateTime.MinValue;

	private bool binocularViewEndInProgress;

	private const float MAX_FOV = 70f;

	private const float MIN_FOV = 3f;

	private const float ZOOM_SPEED = 5f;

	private const float SPEED_LR = 8f;

	private const float SPEED_UD = 8f;

	private float fov = 36.5f;

	private int? binocularsCam;

	private int? binocularsScaleform;

	private float initialCameraAngle;

	public static bool BinocularsViewTaskAttached { get; private set; }

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		if (itemId != "binoculars")
		{
			return;
		}
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition == null)
		{
			return;
		}
		if (isUsingBinoculars)
		{
			Utils.SendNotification("You are already using a ~r~" + itemDefinition.Name + "~s~.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (amount != 1f)
		{
			Utils.SendNotification("You can only use one ~r~" + itemDefinition.Name + " ~s~at a time.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (Game.PlayerPed.IsInVehicle())
		{
			Utils.SendNotification("You cannot use a ~r~" + itemDefinition.Name + " ~s~when you're in a vehicle.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (InventoryMenuScript.Cache != null)
		{
			InventoryEntry inventoryEntry = InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == "binoculars");
			if (inventoryEntry == null || inventoryEntry.Amount < 1f)
			{
				Utils.SendNotification("You don't have a ~r~" + itemDefinition.Name + "~s~.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
		}
		if (CuffedScript.IsCuffed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_USE_WHEN_CUFFED, itemDefinition.Name));
			Utils.PlayErrorSound();
			API.CancelEvent();
		}
		else
		{
			GiveBinoculars();
			API.CancelEvent();
		}
	}

	private async void GiveBinoculars()
	{
		if (!isUsingBinoculars)
		{
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			isUsingBinoculars = true;
			AttachToolTask();
			AttachBinocularsKeys();
			CreateProp();
		}
	}

	private void RemoveBinoculars()
	{
		if (isUsingBinoculars)
		{
			isUsingBinoculars = false;
			EndBinocularView(force: true);
			DetachToolTask();
			DetachBinocularsKeys();
			RemoveProp();
		}
	}

	private async void CreateProp()
	{
		InventoryItem? itemDefinition = Gtacnr.Data.Items.GetItemDefinition("binoculars");
		RemoveProp();
		binocularsProp = await World.CreateProp(Model.op_Implicit(API.GetHashKey(itemDefinition.Model)), ((Entity)Game.PlayerPed).Position, true, false);
		AntiEntitySpawnScript.RegisterEntity((Entity)(object)binocularsProp);
		int pedBoneIndex = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422);
		API.AttachEntityToEntity(((PoolObject)binocularsProp).Handle, ((PoolObject)Game.PlayerPed).Handle, pedBoneIndex, 0.125f, 0.035f, -0.035f, 0f, 0f, 0f, true, true, false, true, 1, true);
	}

	private void RemoveProp()
	{
		if (!((Entity)(object)binocularsProp == (Entity)null))
		{
			((PoolObject)binocularsProp).Delete();
		}
	}

	private void AttachToolTask()
	{
		if (isUsingBinoculars && !toolTaskAttached)
		{
			toolTaskAttached = true;
			base.Update += ToolTask;
		}
	}

	private void DetachToolTask()
	{
		if (!isUsingBinoculars && toolTaskAttached)
		{
			toolTaskAttached = false;
			base.Update -= ToolTask;
		}
	}

	private async Coroutine ToolTask()
	{
		API.DisableControlAction(2, 25, true);
		if (isUsingBinoculars && (((int)Game.PlayerPed.Weapons.Current.Hash != -1569615261 && (int)Game.PlayerPed.Weapons.Current.Hash != 966099553) || Game.PlayerPed.IsInMeleeCombat || CuffedScript.IsCuffed))
		{
			RemoveBinoculars();
		}
	}

	private async void InitializeBinocularView()
	{
		if (!BinocularsViewTaskAttached)
		{
			binocularsScaleform = API.RequestScaleformMovie("BINOCULARS");
			binocularsCam = API.CreateCam("DEFAULT_SCRIPTED_FLY_CAMERA", true);
			while (!API.HasScaleformMovieLoaded(binocularsScaleform.Value))
			{
				await BaseScript.Delay(100);
			}
			initialCameraAngle = Convert360To180(API.GetEntityHeading(((PoolObject)Game.PlayerPed).Handle));
			API.AttachCamToEntity(binocularsCam.Value, ((PoolObject)Game.PlayerPed).Handle, 0f, 0f, 1f, true);
			API.SetCamRot(binocularsCam.Value, 0f, 0f, initialCameraAngle, 2);
			API.SetCamFov(binocularsCam.Value, fov);
			API.RenderScriptCams(true, false, 0, true, false);
			API.PushScaleformMovieFunction(binocularsScaleform.Value, "SET_CAM_LOGO");
			API.PushScaleformMovieFunctionParameterInt(0);
			API.PopScaleformMovieFunctionVoid();
			API.TaskStartScenarioInPlace(((PoolObject)Game.PlayerPed).Handle, "WORLD_HUMAN_BINOCULARS", 0, true);
			RemoveProp();
			DetachBinocularsInstructionalButtons();
			base.Update += BinocularViewControlLoop;
			BinocularsViewTaskAttached = true;
			binocularsViewStartedT = DateTime.UtcNow;
		}
	}

	private async Coroutine BinocularViewControlLoop()
	{
		if (binocularsCam.HasValue && binocularsScaleform.HasValue)
		{
			HideHUDThisFrame();
			DisableBinocularControlsThisFrame();
			API.DrawScaleformMovie(binocularsScaleform.Value, 0.5f, 0.5f, 1.002f, 1.002f, 255, 255, 255, 255, 0);
			float zoomValue = 1f / 67f * (fov - 3f);
			AdjustCameraRotation(zoomValue);
			AdjustZoom();
			API.SetEntityLocallyInvisible(((PoolObject)Game.PlayerPed).Handle);
		}
	}

	private async void EndBinocularView(bool force = false)
	{
		if (!BinocularsViewTaskAttached)
		{
			return;
		}
		if (binocularsCam.HasValue)
		{
			API.RenderScriptCams(false, false, 0, true, false);
			API.DestroyCam(binocularsCam.Value, false);
			binocularsCam = null;
		}
		if (binocularsScaleform.HasValue)
		{
			int value = binocularsScaleform.Value;
			API.SetScaleformMovieAsNoLongerNeeded(ref value);
			binocularsScaleform = null;
		}
		if (!force)
		{
			API.ClearPedTasks(((PoolObject)Game.PlayerPed).Handle);
			if (binocularViewEndInProgress)
			{
				return;
			}
			try
			{
				binocularViewEndInProgress = true;
				await BaseScript.Delay(750);
				API.ClearPedTasksImmediately(((PoolObject)Game.PlayerPed).Handle);
				CreateProp();
				AttachBinocularsInstructionalButtons();
			}
			finally
			{
				binocularViewEndInProgress = false;
			}
			if (!BinocularsViewTaskAttached)
			{
				return;
			}
		}
		else
		{
			API.ClearPedTasksImmediately(((PoolObject)Game.PlayerPed).Handle);
		}
		base.Update -= BinocularViewControlLoop;
		BinocularsViewTaskAttached = false;
	}

	public static float Convert360To180(float angle)
	{
		return (angle + 180f) % 360f - 180f;
	}

	private static float GetAngleDeviation(float angle1, float angle2)
	{
		float num = (angle2 - angle1 + 180f) % 360f - 180f;
		return Math.Abs((num < -180f) ? (num + 360f) : num);
	}

	private void AdjustCameraRotation(float zoomValue)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		float disabledControlNormal = API.GetDisabledControlNormal(0, 1);
		float disabledControlNormal2 = API.GetDisabledControlNormal(0, 2);
		Vector3 camRot = API.GetCamRot(binocularsCam.Value, 2);
		if ((double)Math.Abs(disabledControlNormal) > 0.0 || (double)Math.Abs(disabledControlNormal2) > 0.0)
		{
			float num = camRot.Z + disabledControlNormal * -1f * 8f * (zoomValue + 0.1f);
			if (GetAngleDeviation(initialCameraAngle, num) > 45f)
			{
				num = camRot.Z;
			}
			float num2 = Math.Max(Math.Min(20f, camRot.X + disabledControlNormal2 * -1f * 8f * (zoomValue + 0.1f)), -89.5f);
			API.SetCamRot(binocularsCam.Value, num2, 0f, num, 2);
		}
	}

	private void AdjustZoom()
	{
		if (Game.IsControlJustPressed(0, (Control)241))
		{
			fov = Math.Max(fov - 5f, 3f);
		}
		if (Game.IsControlJustPressed(0, (Control)242))
		{
			fov = Math.Min(fov + 5f, 70f);
		}
		float camFov = API.GetCamFov(binocularsCam.Value);
		if (Math.Abs(fov - camFov) < 0.1f)
		{
			fov = camFov;
		}
		API.SetCamFov(binocularsCam.Value, camFov + (fov - camFov) * 0.05f);
	}

	private static void HideHUDThisFrame()
	{
		API.HideHelpTextThisFrame();
		API.HideHudAndRadarThisFrame();
		API.HideHudComponentThisFrame(1);
		API.HideHudComponentThisFrame(2);
		API.HideHudComponentThisFrame(3);
		API.HideHudComponentThisFrame(4);
		API.HideHudComponentThisFrame(11);
		API.HideHudComponentThisFrame(12);
		API.HideHudComponentThisFrame(15);
		API.HideHudComponentThisFrame(19);
	}

	private static void DisableBinocularControlsThisFrame()
	{
		API.DisableControlAction(2, 14, true);
		API.DisableControlAction(2, 15, true);
		API.DisableControlAction(2, 1, true);
		API.DisableControlAction(2, 2, true);
	}

	private void AttachBinocularsKeys()
	{
		if (!binocularsKeysAttached)
		{
			binocularsKeysAttached = true;
			AttachBinocularsInstructionalButtons();
			KeysScript.AttachListener((Control)173, OnKeyEvent, 50);
			KeysScript.AttachListener((Control)25, OnKeyEvent, 50);
		}
	}

	private void AttachBinocularsInstructionalButtons()
	{
		Utils.AddInstructionalButton("unequipBinoculars", new InstructionalButton("Unequip", 2, (Control)173));
		Utils.AddInstructionalButton("aimBinoculars", new InstructionalButton("Aim", 2, (Control)25));
	}

	private void DetachBinocularsKeys()
	{
		if (binocularsKeysAttached)
		{
			binocularsKeysAttached = false;
			DetachBinocularsInstructionalButtons();
			KeysScript.DetachListener((Control)173, OnKeyEvent);
			KeysScript.DetachListener((Control)25, OnKeyEvent);
		}
	}

	private void DetachBinocularsInstructionalButtons()
	{
		Utils.RemoveInstructionalButton("unequipBinoculars");
		Utils.RemoveInstructionalButton("aimBinoculars");
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		if ((int)control == 173 && eventType == KeyEventType.JustPressed)
		{
			RemoveBinoculars();
			return true;
		}
		if ((int)control == 25 && eventType == KeyEventType.JustPressed)
		{
			if (MenuController.IsAnyMenuOpen() || API.IsPedInAnyVehicle(((PoolObject)Game.PlayerPed).Handle, true) || !Gtacnr.Utils.CheckTimePassed(binocularsViewStartedT, TimeSpan.FromSeconds(0.5)))
			{
				return false;
			}
			InitializeBinocularView();
			return true;
		}
		if ((int)control == 25 && eventType == KeyEventType.JustReleased)
		{
			EndBinocularView();
			return true;
		}
		return false;
	}
}
