using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Jobs.Police.Arrest;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class EnterVehicleScript : Script
{
	private bool instructionsShown;

	private Vehicle targetVehicle;

	private VehicleSeat targetSeat;

	public static EnterVehicleScript script;

	private string[] seatBones = new string[17]
	{
		"seat_pside_f", "seat_dside_r", "seat_pside_r", "seat_dside_r1", "seat_pside_r1", "seat_dside_r2", "seat_pside_r2", "seat_dside_r3", "seat_pside_r3", "seat_dside_r4",
		"seat_pside_r4", "seat_dside_r5", "seat_pside_r5", "seat_dside_r6", "seat_pside_r6", "seat_dside_r7", "seat_pside_r7"
	};

	private string[] seatBonesMotorcycles = new string[1] { "seat_r" };

	private Vehicle previousVehicle;

	public static Vehicle TargetVehicle => script.targetVehicle;

	public EnterVehicleScript()
	{
		script = this;
	}

	protected override void OnStarted()
	{
		KeysScript.AttachListener((Control)47, OnKeyEvent);
		API.ResetFlyThroughWindscreenParams();
		API.SetFlyThroughWindscreenParams(35f, 35f, 15f, 500f);
	}

	public bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 47 && eventType == KeyEventType.JustPressed)
		{
			TryEnterVehicleAsPassenger();
			return true;
		}
		return false;
	}

	private bool CanTryToEnterVehicle()
	{
		if (!CuffedScript.IsInCustody && !CuffedScript.IsBeingCuffedOrUncuffed && ((Entity)Game.PlayerPed).IsAlive && !Game.PlayerPed.IsBeingStunned && !Game.PlayerPed.IsInVehicle())
		{
			return !Game.PlayerPed.IsGettingIntoAVehicle;
		}
		return false;
	}

	private async void TryEnterVehicleAsPassenger()
	{
		if (CanTryToEnterVehicle())
		{
			FindTargetVehicleAndSeat();
			if (!((Entity)(object)targetVehicle == (Entity)null) && (int)targetSeat >= 0)
			{
				await TaskTryEnterVehicle();
			}
		}
	}

	private async Task TaskTryEnterVehicle()
	{
		Game.PlayerPed.Task.ClearAll();
		Game.PlayerPed.Task.EnterVehicle(targetVehicle, targetSeat, -1, 0f, 0);
		DateTime t = DateTime.UtcNow;
		await BaseScript.Delay(500);
		while (!Gtacnr.Utils.CheckTimePassed(t, 10000.0))
		{
			await BaseScript.Delay(0);
			if (Game.IsControlPressed(2, (Control)268) || Game.IsControlPressed(2, (Control)32) || Game.IsControlPressed(2, (Control)269) || Game.IsControlPressed(2, (Control)33) || Game.IsControlPressed(2, (Control)266) || Game.IsControlPressed(2, (Control)34) || Game.IsControlPressed(2, (Control)267) || Game.IsControlPressed(2, (Control)35))
			{
				Game.PlayerPed.Task.ClearAll();
				break;
			}
		}
	}

	private void FindTargetVehicleAndSeat()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Invalid comparison between Unknown and I4
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		targetVehicle = null;
		targetSeat = (VehicleSeat)(-3);
		if (!CanTryToEnterVehicle())
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 16f;
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player == Game.Player)
			{
				continue;
			}
			Vector3 position2 = ((Entity)player.Character).Position;
			if (((Vector3)(ref position2)).DistanceToSquared(position) > 50f.Square())
			{
				continue;
			}
			Vehicle val = player.Character.CurrentVehicle ?? player.Character.VehicleTryingToEnter;
			if ((Entity)(object)val == (Entity)null)
			{
				continue;
			}
			int num2 = Math.Min(14, API.GetVehicleModelNumberOfSeats((uint)((Entity)val).Model.Hash)) - 1;
			int num3 = -1;
			string[] array = seatBones;
			if ((int)val.ClassType == 8)
			{
				array = seatBonesMotorcycles;
			}
			for (int i = 0; i < num2; i++)
			{
				if (i < array.Length)
				{
					int entityBoneIndexByName = API.GetEntityBoneIndexByName(((PoolObject)val).Handle, array[i]);
					Vector3 worldPositionOfEntityBone = API.GetWorldPositionOfEntityBone(((PoolObject)val).Handle, entityBoneIndexByName);
					float num4 = ((Vector3)(ref position)).DistanceToSquared(worldPositionOfEntityBone);
					if (num4 < num && API.GetPedInVehicleSeat(((PoolObject)val).Handle, i) <= 0)
					{
						num = num4;
						num3 = i;
					}
				}
				else if (num3 == -1 && API.GetPedInVehicleSeat(((PoolObject)val).Handle, i) <= 0)
				{
					num3 = i;
					break;
				}
			}
			if (num3 != -1)
			{
				targetVehicle = val;
				targetSeat = (VehicleSeat)num3;
			}
		}
	}

	[Update]
	private async Coroutine CheckVehicleTask()
	{
		await Script.Wait(500);
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle != (Entity)(object)previousVehicle)
		{
			if ((Entity)(object)currentVehicle != (Entity)null && (Entity)(object)previousVehicle == (Entity)null)
			{
				BaseScript.TriggerEvent("gtacnr:enterVehicle", new object[1] { ((PoolObject)currentVehicle).Handle });
			}
			else if ((Entity)(object)currentVehicle == (Entity)null && (Entity)(object)previousVehicle != (Entity)null)
			{
				BaseScript.TriggerEvent("gtacnr:exitVehicle", new object[1] { ((PoolObject)previousVehicle).Handle });
			}
		}
		previousVehicle = currentVehicle;
		FindTargetVehicleAndSeat();
		if ((Entity)(object)targetVehicle != (Entity)null)
		{
			EnableInstructionalButtons();
		}
		else
		{
			DisableInstructionalButtons();
		}
	}

	private void EnableInstructionalButtons()
	{
		if (!instructionsShown)
		{
			instructionsShown = true;
			Utils.AddInstructionalButton("enter", new InstructionalButton("Enter", 2, (Control)47));
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionsShown)
		{
			instructionsShown = false;
			Utils.RemoveInstructionalButton("enter");
		}
	}
}
