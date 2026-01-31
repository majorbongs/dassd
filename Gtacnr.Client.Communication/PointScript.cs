using System;
using System.Collections.Generic;
using System.Drawing;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Client.Weapons;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Communication;

public class PointScript : Script
{
	private readonly int MAX_PINGS = 5;

	private readonly int PING_DURATION = 15000;

	private bool isPointing;

	private List<PingInfo> pings = new List<PingInfo>();

	private int myPingCount;

	private DateTime lastPingT;

	private WeaponHash prevWeapon;

	private static PointScript instance;

	public PointScript()
	{
		instance = this;
	}

	public static void StartPointing()
	{
		instance._StartPointing();
	}

	public static void StopPointing()
	{
		instance._StopPointing();
	}

	protected override void OnStarted()
	{
		KeysScript.AttachListener((Control)29, OnKeyEvent);
		((BaseScript)this).Exports.Add("SetPointingState", (Delegate)(Action<bool>)delegate(bool state)
		{
			isPointing = state;
		});
		API.RegisterKeyMapping("ping", "Ping position", "", "");
		API.RegisterKeyMapping("ping_caution", "Ping caution", "", "");
	}

	private static Color PingTypeToColor(PingType pingType)
	{
		return pingType switch
		{
			PingType.Pointer => new Color(109, 180, 227, byte.MaxValue), 
			PingType.Caution => new Color(224, 50, 50, byte.MaxValue), 
			PingType.Revive => new Color(138, 0, 196, byte.MaxValue), 
			_ => throw new NotImplementedException(), 
		};
	}

	private static BlipColor PingTypeToBlipColor(PingType pingType)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return (BlipColor)(pingType switch
		{
			PingType.Pointer => 3, 
			PingType.Caution => 1, 
			PingType.Revive => 50, 
			_ => throw new NotImplementedException(), 
		});
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (inputType != InputType.Keyboard)
		{
			return false;
		}
		if (eventType == KeyEventType.Held && CanPoint() && !isPointing)
		{
			prevWeapon = Game.PlayerPed.Weapons.Current.Hash;
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			_StartPointing();
			return true;
		}
		if (eventType == KeyEventType.JustPressed && isPointing)
		{
			_StopPointing();
			return false;
		}
		return false;
	}

	private bool CanPoint()
	{
		if (!Game.PlayerPed.IsInVehicle() && !((Entity)Game.PlayerPed).IsDead && !API.IsPlayerSwitchInProgress() && !Game.IsPaused && API.IsScreenFadedIn() && !Game.PlayerPed.IsInParachuteFreeFall && !Game.PlayerPed.IsFalling && !Game.PlayerPed.IsBeingStunned && !Game.PlayerPed.IsSwimming && !Game.PlayerPed.IsSwimmingUnderWater && !Game.PlayerPed.IsDiving && !CuffedScript.IsInCustody && !CuffedScript.IsBeingCuffedOrUncuffed && (Entity)(object)EnterVehicleScript.TargetVehicle == (Entity)null)
		{
			return !MenuController.IsAnyMenuOpen();
		}
		return false;
	}

	private async void _StartPointing()
	{
		if (isPointing)
		{
			return;
		}
		isPointing = true;
		((dynamic)((BaseScript)this).Exports["fingerpoint"]).startPointing();
		Utils.AddInstructionalButton("pingPoint", new InstructionalButton("Ping", 2, (Control)24));
		Utils.AddInstructionalButton("pingCaution", new InstructionalButton("Caution", 2, (Control)25));
		while (isPointing)
		{
			await BaseScript.Delay(0);
			if (!CanPoint() || (int)Game.PlayerPed.Weapons.Current.Hash != -1569615261)
			{
				_StopPointing();
			}
		}
	}

	private void _StopPointing()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if (isPointing)
		{
			isPointing = false;
			((dynamic)((BaseScript)this).Exports["fingerpoint"]).stopPointing();
			Utils.RemoveInstructionalButton("pingPoint");
			Utils.RemoveInstructionalButton("pingCaution");
			WeaponBehaviorScript.QuickSwitchToWeapon(prevWeapon, playSound: false);
		}
	}

	[Update]
	private async Coroutine DrawTask()
	{
		if (isPointing && Utils.IsUsingKeyboard())
		{
			if (Game.IsControlJustPressed(2, (Control)24))
			{
				PingAtCameraCoords(PingType.Pointer);
			}
			else if (Game.IsControlJustPressed(2, (Control)25))
			{
				PingAtCameraCoords(PingType.Caution);
			}
		}
		foreach (PingInfo ping in pings)
		{
			if (ping.Label != null)
			{
				float distance = World.GetDistance(((Entity)Game.PlayerPed).Position, ping.Position);
				Color value = PingTypeToColor(ping.Type);
				System.Drawing.Color color = new Color(value.R, value.G, value.B, 128).ToSystemColor();
				World.DrawMarker((MarkerType)20, ping.Position + new Vector3(0f, 0f, 0.4f), Vector3.Zero, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.5f), color, true, true, false, (string)null, (string)null, false);
				Utils.Draw3DText(ping.Label + $"~w~\n{ping.Author}\n{distance:0.0}m", ping.Position, value, 0.3f, 0);
			}
		}
	}

	private async void PingAtCameraCoords(PingType type)
	{
		if (myPingCount >= MAX_PINGS && !Gtacnr.Utils.CheckTimePassed(lastPingT, PING_DURATION))
		{
			Utils.PlayErrorSound();
			return;
		}
		Vector3 val = RaycastGameplayCamera(1000f);
		if (!(val == default(Vector3)))
		{
			myPingCount++;
			lastPingT = DateTime.Now;
			PingInfo pingInfo = new PingInfo
			{
				Position = val,
				Type = type,
				Label = ((type == PingType.Pointer) ? "Ping" : "Caution"),
				Author = "You"
			};
			CreatePing(pingInfo);
			BaseScript.TriggerServerEvent("gtacnr:pingLocation", new object[1] { pingInfo.Json() });
			await BaseScript.Delay(PING_DURATION);
			myPingCount--;
		}
	}

	private async void CreatePing(PingInfo pingInfo)
	{
		if (pingInfo.AuthorUid == null || !BlockScript.IsBlocked(pingInfo.AuthorUid))
		{
			pings.Add(pingInfo);
			Blip blip = World.CreateBlip(pingInfo.Position);
			blip.Sprite = (BlipSprite)11;
			blip.Color = PingTypeToBlipColor(pingInfo.Type);
			Utils.SetBlipName(blip, pingInfo.Label ?? "Ping", "ping");
			blip.IsShortRange = true;
			API.SetBlipDisplay(((PoolObject)blip).Handle, 5);
			if (pingInfo.Type == PingType.Pointer || pingInfo.Type == PingType.Revive)
			{
				Game.PlaySound("5_SEC_WARNING", "HUD_MINI_GAME_SOUNDSET");
			}
			else if (pingInfo.Type == PingType.Caution)
			{
				Game.PlaySound("Beep_Red", "DLC_HEIST_HACKING_SNAKE_SOUNDS");
			}
			await BaseScript.Delay(PING_DURATION);
			pings.Remove(pingInfo);
			((PoolObject)blip).Delete();
		}
	}

	[EventHandler("gtacnr:createPing")]
	private void OnCreatePing(string jPingInfo)
	{
		CreatePing(jPingInfo.Unjson<PingInfo>());
	}

	private Vector3 RotationToDirection(Vector3 rotation)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector((float)(Math.PI / 180.0 * (double)rotation.X), (float)(Math.PI / 180.0 * (double)rotation.Y), (float)(Math.PI / 180.0 * (double)rotation.Z));
		return new Vector3((float)((0.0 - Math.Sin(val.Z)) * Math.Abs(Math.Cos(val.X))), (float)(Math.Cos(val.Z) * Math.Abs(Math.Cos(val.X))), (float)Math.Sin(val.X));
	}

	private Vector3 RaycastGameplayCamera(float distance)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		Vector3 gameplayCamRot = API.GetGameplayCamRot(0);
		Vector3 gameplayCamCoord = API.GetGameplayCamCoord();
		Vector3 val = RotationToDirection(gameplayCamRot);
		Vector3 val2 = default(Vector3);
		((Vector3)(ref val2))._002Ector(gameplayCamCoord.X + val.X * distance, gameplayCamCoord.Y + val.Y * distance, gameplayCamCoord.Z + val.Z * distance);
		bool flag = false;
		Vector3 zero = Vector3.Zero;
		Vector3 zero2 = Vector3.Zero;
		int num = 0;
		API.GetShapeTestResult(API.StartShapeTestRay(gameplayCamCoord.X, gameplayCamCoord.Y, gameplayCamCoord.Z, val2.X, val2.Y, val2.Z, -1, ((PoolObject)Game.PlayerPed).Handle, 1), ref flag, ref zero, ref zero2, ref num);
		if (!flag)
		{
			return default(Vector3);
		}
		return zero;
	}

	[Command("ping")]
	private void PingCommand()
	{
		PingAtCameraCoords(PingType.Pointer);
	}

	[Command("ping_caution")]
	private void PingCautionCommand()
	{
		PingAtCameraCoords(PingType.Caution);
	}
}
