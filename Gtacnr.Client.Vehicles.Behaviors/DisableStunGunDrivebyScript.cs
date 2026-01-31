using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Inventory;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class DisableStunGunDrivebyScript : Script
{
	private bool isDisabled;

	protected override void OnStarted()
	{
		EnableStunGun();
		ArmoryScript.LoadoutChanged += OnLoadoutChanged;
		ArmoryScript.WeaponEquipped += OnWeaponEquipped;
	}

	private void OnWeaponEquipped(object sender, ArmoryWeaponEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)e.WeaponHash == 911657153)
		{
			isDisabled = false;
		}
	}

	private void OnLoadoutChanged(object sender, EventArgs e)
	{
		if (isDisabled)
		{
			isDisabled = false;
		}
	}

	private void EnableStunGun()
	{
		API.SetCanPedEquipWeapon(API.PlayerPedId(), 911657153u, true);
	}

	private void DisableStunGun()
	{
		uint num = 0u;
		API.GetCurrentPedWeapon(API.PlayerPedId(), ref num, true);
		if (num == 911657153)
		{
			API.SetCurrentPedWeapon(API.PlayerPedId(), 2725352035u, true);
		}
		API.SetCanPedEquipWeapon(API.PlayerPedId(), 911657153u, false);
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(500);
		bool flag = isDisabled;
		isDisabled = (Entity)(object)Game.PlayerPed != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null;
		if (flag && !isDisabled)
		{
			EnableStunGun();
		}
		else if (!flag && isDisabled)
		{
			DisableStunGun();
		}
	}
}
