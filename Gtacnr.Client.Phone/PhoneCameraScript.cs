using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Localization;
using MenuAPI;

namespace Gtacnr.Client.Phone;

public class PhoneCameraScript : Script
{
	private enum CameraMode
	{
		Normal,
		Selfie
	}

	private static PhoneCameraScript instance;

	private bool suggestionShown;

	private bool isCameraOpen;

	private CameraMode cameraMode;

	private DateTime LastPhotoTime = DateTime.Now;

	private readonly TimeSpan PHOTO_COOLDOWN = TimeSpan.FromSeconds(2.0);

	public static void OpenCamera()
	{
		instance._OpenCamera();
	}

	public static void CloseCamera()
	{
		instance._CloseCamera();
	}

	public static void ShootPhoto()
	{
		instance._ShootPhoto();
	}

	public static void FlipCamera()
	{
		instance._FlipCamera();
	}

	public static bool IsCameraOpen()
	{
		return instance.isCameraOpen;
	}

	public PhoneCameraScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		KeysScript.AttachListener((Control)176, OnKeyEvent, 10);
		KeysScript.AttachListener((Control)27, OnKeyEvent, 10);
		KeysScript.AttachListener((Control)177, OnKeyEvent, 10);
		API.AddTextEntryByHash(2263944422u, LocalizationController.S(Entries.Imenu.IMENU_PHONE_CAMERA_UPLOAD));
		API.AddTextEntry("ERROR_UPLOAD", LocalizationController.S(Entries.Imenu.IMENU_PHONE_CAMERA_UPLOAD_CONFIRM, "https://forum.cfx.re/c/fivem-snapmatic"));
		API.CreateModelHide(0f, 0f, 0f, 99000f, (uint)API.GetHashKey("prop_prologue_phone"), true);
	}

	private void _OpenCamera()
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if (!isCameraOpen && (!Game.PlayerPed.IsInVehicle() || !((Entity)(object)Game.PlayerPed.CurrentVehicle.Driver == (Entity)(object)Game.PlayerPed)))
		{
			isCameraOpen = true;
			cameraMode = CameraMode.Normal;
			RefreshCamera();
			base.Update += UpdateTask;
			Utils.AddInstructionalButton("cameraShoot", new InstructionalButton(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CAMERA_SHOOT), 2, (Control)176));
			Utils.AddInstructionalButton("cameraFlip", new InstructionalButton(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CAMERA_FLIP), 2, (Control)27));
			Utils.AddInstructionalButton("cameraClose", new InstructionalButton(LocalizationController.S(Entries.Main.BTN_BACK), 2, (Control)177));
			_ = ((Entity)Game.PlayerPed).Position;
			API.CreateMobilePhone(4);
			API.CellCamActivate(true, true);
		}
	}

	private void _CloseCamera()
	{
		if (isCameraOpen)
		{
			isCameraOpen = false;
			base.Update -= UpdateTask;
			Utils.RemoveInstructionalButton("cameraShoot");
			Utils.RemoveInstructionalButton("cameraFlip");
			Utils.RemoveInstructionalButton("cameraClose");
			API.DestroyMobilePhone();
			API.CellCamActivate(false, false);
		}
	}

	private void _ShootPhoto()
	{
		if (isCameraOpen && Gtacnr.Utils.CheckTimePassed(LastPhotoTime, PHOTO_COOLDOWN))
		{
			API.BeginTakeHighQualityPhoto();
			API.SaveHighQualityPhoto(-1);
			API.FreeMemoryForHighQualityPhoto();
			LastPhotoTime = DateTime.Now;
			if (!suggestionShown)
			{
				suggestionShown = true;
				Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CAMERA_SUGGESTION));
			}
		}
	}

	private void _FlipCamera()
	{
		cameraMode = ((cameraMode == CameraMode.Normal) ? CameraMode.Selfie : CameraMode.Normal);
		RefreshCamera();
	}

	private void RefreshCamera()
	{
		Function.Call((Hash)2635073306796480568L, (InputArgument[])(object)new InputArgument[1] { InputArgument.op_Implicit(cameraMode == CameraMode.Selfie) });
	}

	private async Coroutine UpdateTask()
	{
		API.HideHudComponentThisFrame(7);
		API.HideHudComponentThisFrame(8);
		API.HideHudComponentThisFrame(9);
		API.HideHudComponentThisFrame(6);
		API.HideHudComponentThisFrame(19);
		API.HideHudAndRadarThisFrame();
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		if (eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard)
		{
			if ((int)control == 27)
			{
				if (MenuController.IsAnyMenuOpen() || Utils.IsOnScreenKeyboardActive || API.IsPauseMenuActive() || Utils.IsSwitchInProgress() || Utils.IsScreenFadingInProgress() || !SpawnScript.HasSpawned || CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed)
				{
					return false;
				}
				if (isCameraOpen)
				{
					_FlipCamera();
				}
				return true;
			}
			if ((int)control == 176 && isCameraOpen)
			{
				_ShootPhoto();
				return true;
			}
			if ((int)control == 177 && isCameraOpen)
			{
				_CloseCamera();
				return true;
			}
		}
		return false;
	}
}
