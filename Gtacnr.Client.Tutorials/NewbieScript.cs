using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Localization;

namespace Gtacnr.Client.Tutorials;

public class NewbieScript : Script
{
	private bool controlsEnabled;

	private bool shouldDraw;

	public static NewbieScript instance;

	private readonly List<Tuple<Vector3, string>> textLabels = new List<Tuple<Vector3, string>>
	{
		Tuple.Create<Vector3, string>(new Vector3(-1033.3364f, -2735.138f, 20.1693f), "~HUD_COLOUR_G1~Discord\ndiscord.gg/cnr\n\n~q~Guide\ngtacnr.net/wiki"),
		Tuple.Create<Vector3, string>(new Vector3(-1029.5276f, -2722.565f, 20.0902f), "Read the ~r~RULES ~s~or face ~r~PENALTIES\n~b~" + ExternalLinks.Collection.Rules),
		Tuple.Create<Vector3, string>(new Vector3(-1037.6713f, -2732.7437f, 20.1693f), "Need a ~b~car ~s~legally? Come here!\nOr you could just carjack an NPC if you wish :)")
	};

	private readonly Vector3 carRentalTpLocation = new Vector3(-1037.6713f, -2732.7437f, 20.1693f);

	private bool rentalTpActivated;

	private bool newbieModeOn;

	private Vector3 qrPosition = new Vector3(5.6629f, -1070.0048f, 39.1466f);

	private Blip qrBlip;

	public NewbieScript()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		instance = this;
		AddQRCodeBlip();
	}

	private async Coroutine DrawTask()
	{
		if (!shouldDraw)
		{
			return;
		}
		foreach (Tuple<Vector3, string> textLabel in textLabels)
		{
			if (Math.Abs(textLabel.Item1.Z - ((Entity)Game.PlayerPed).Position.Z) < 5f)
			{
				Utils.Draw3DText(textLabel.Item2, textLabel.Item1);
			}
		}
	}

	private async Coroutine CheckTask()
	{
		await Script.Wait(500);
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = ((Vector3)(ref position)).DistanceToSquared(carRentalTpLocation);
		shouldDraw = num < 2500f;
		if (num > 2500f)
		{
			StopNewbieModeInternal();
		}
		else if (num < 1.44f)
		{
			EnableControls();
		}
		else
		{
			DisableControls();
		}
	}

	private void EnableControls()
	{
		if (!controlsEnabled)
		{
			Utils.DisplayHelpText("Press ~INPUT_CONTEXT~ to go to the ~y~car rental~s~.");
			Utils.AddInstructionalButton("rentCarTp", new InstructionalButton("Car Rental", 2, (Control)51));
			KeysScript.AttachListener((Control)51, OnKeyEvent, 50);
			controlsEnabled = true;
		}
	}

	private void DisableControls()
	{
		if (controlsEnabled)
		{
			controlsEnabled = false;
			KeysScript.DetachListener((Control)51, OnKeyEvent);
			Utils.RemoveInstructionalButton("rentCarTp");
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 51 && eventType == KeyEventType.JustPressed)
		{
			DisableControls();
			StopNewbieMode();
			GoToCarRental();
			return true;
		}
		return false;
	}

	private async void GoToCarRental()
	{
		if (rentalTpActivated)
		{
			Utils.PlayErrorSound();
			return;
		}
		rentalTpActivated = true;
		Utils.Freeze();
		Vehicle shuttle = await World.CreateVehicle(Model.op_Implicit("rentalbus"), new Vector3(-1063.1743f, -2709.7273f, 19.9235f), 226.7935f);
		shuttle.IsEngineRunning = true;
		API.SetEntityAsMissionEntity(((PoolObject)shuttle).Handle, true, true);
		Ped driver = null;
		try
		{
			using DisposableModel pedModel = new DisposableModel(Model.op_Implicit("s_m_m_autoshop_01"));
			await pedModel.Load();
			driver = new Ped(API.CreatePedInsideVehicle(((PoolObject)shuttle).Handle, 6, Model.op_Implicit(pedModel.Model), -1, true, true));
			driver.AlwaysKeepTask = true;
			driver.BlockPermanentEvents = true;
			API.SetDriverAbility(((PoolObject)driver).Handle, 1f);
			API.SetPedRandomComponentVariation(((PoolObject)driver).Handle, false);
			API.SetPedRandomProps(((PoolObject)driver).Handle);
			API.SetEntityAsMissionEntity(((PoolObject)driver).Handle, true, true);
			driver.Task.DriveTo(shuttle, new Vector3(-1035.5491f, -2726.725f, 19.9244f), 2f, 12f, 828);
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(-1030.7239f, -2732.8298f, 22.0376f);
		Vector3 val2 = default(Vector3);
		((Vector3)(ref val2))._002Ector(-1042.335f, -2721.5046f, 20.6245f);
		API.DestroyAllCams(true);
		int camera = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", val.X, val.Y, val.Z, 0f, 0f, 0f, 30f, false, 0);
		API.PointCamAtCoord(camera, val2.X, val2.Y, val2.Z);
		API.SetCamActive(camera, true);
		API.RenderScriptCams(true, false, 2000, true, true);
		await AntiEntitySpawnScript.RegisterEntity((Entity)(object)driver);
		await AntiEntitySpawnScript.RegisterEntity((Entity)(object)shuttle);
		await BaseScript.Delay(6000);
		await Utils.FadeOut(3000);
		_ = DateTime.Now;
		try
		{
			Utils.Unfreeze();
			API.SetCamActive(camera, false);
			API.DestroyCam(camera, false);
			API.RenderScriptCams(false, false, 0, true, false);
			((PoolObject)driver).Delete();
			((PoolObject)shuttle).Delete();
			if (!(await TriggerServerEventAsync<bool>("gtacnr:authorizeTeleportToRental", new object[0])))
			{
				await Utils.FadeIn();
				Utils.DisplayErrorMessage(81, 2, "You can still visit the car rental by walking to its location marked by a yellow car here in the airport.");
			}
			else
			{
				await BaseScript.Delay(5000);
				await Utils.TeleportToCoords(new Vector4(-902.1829f, -2335.6506f, 6.709f, 149.434f), Utils.TeleportFlags.None);
			}
		}
		catch (Exception exception2)
		{
			Print(exception2);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
		}
		finally
		{
			if (!API.IsScreenFadedIn())
			{
				Utils.FadeIn(500);
			}
		}
	}

	private void StartNewbieModeInternal()
	{
		if (!newbieModeOn)
		{
			base.Update += DrawTask;
			base.Update += CheckTask;
			newbieModeOn = true;
		}
	}

	private void StopNewbieModeInternal()
	{
		if (newbieModeOn)
		{
			base.Update -= DrawTask;
			base.Update -= CheckTask;
			newbieModeOn = false;
		}
	}

	public static void StartNewbieMode()
	{
		instance.StartNewbieModeInternal();
	}

	public static void StopNewbieMode()
	{
		instance.StopNewbieModeInternal();
	}

	private void AddQRCodeBlip()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (!Preferences.QrCodeBlipFound.Get())
		{
			qrBlip = World.CreateBlip(qrPosition);
			qrBlip.Sprite = (BlipSprite)465;
			qrBlip.Color = (BlipColor)81;
			Utils.SetBlipName(qrBlip, "?", "qr");
			qrBlip.Scale = 1f;
			qrBlip.IsShortRange = true;
			base.Update += QRCodeUpdateTask;
			base.Update += QRCodeDrawTask;
		}
	}

	private async Coroutine QRCodeUpdateTask()
	{
		await Script.Wait(500);
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(qrPosition) < 2.25f)
		{
			base.Update -= QRCodeUpdateTask;
			base.Update -= QRCodeDrawTask;
			((PoolObject)qrBlip).Delete();
			Preferences.QrCodeBlipFound.Set(value: true);
			Utils.DisplaySubtitle("You found a ~g~special offer~s~! Scan the ~y~QR Code ~s~on the building with your phone to obtain it!");
		}
	}

	private async Coroutine QRCodeDrawTask()
	{
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(0.7f, 0.7f, 0.7f);
		Color color = Color.FromUint(3684499584u);
		API.DrawMarker(32, qrPosition.X, qrPosition.Y, qrPosition.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, true, true, 2, false, (string)null, (string)null, false);
	}
}
