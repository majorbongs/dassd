using System;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Items;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Characters;

public class StaminaScript : Script
{
	private DateTime lastRestoreT;

	[Update]
	private async Coroutine StaminaTask()
	{
		await Script.Wait(500);
		foreach (string item in DrugScript.CurrentDrugs.Values.Select((DrugScript.DrugState v) => v.ItemId))
		{
			string extraDataString = Gtacnr.Data.Items.GetItemDefinition(item).GetExtraDataString("EffectType");
			if (extraDataString == "Cocaine" || extraDataString == "Caffeine")
			{
				return;
			}
		}
		WeaponDefinition weaponDefinitionByHash = Gtacnr.Data.Items.GetWeaponDefinitionByHash((uint)(int)Game.PlayerPed.Weapons.Current.Hash);
		WeaponWeight weaponWeight = WeaponWeight.Light;
		if (weaponDefinitionByHash != null)
		{
			weaponWeight = weaponDefinitionByHash.WeaponWeight;
		}
		float num = 1f;
		float runSpeedMultThisFrame = 1f;
		switch (weaponWeight)
		{
		case WeaponWeight.Light:
			num = 1f;
			runSpeedMultThisFrame = 1.15f;
			break;
		case WeaponWeight.MediumLight:
			num = 0.8f;
			runSpeedMultThisFrame = 1.1f;
			break;
		case WeaponWeight.Medium:
			num = 0.6f;
			runSpeedMultThisFrame = 1.05f;
			break;
		case WeaponWeight.MediumHeavy:
			num = 0.4f;
			runSpeedMultThisFrame = 1f;
			break;
		case WeaponWeight.Heavy:
			num = 0f;
			runSpeedMultThisFrame = 1f;
			API.SetPlayerStamina(Game.Player.Handle, 0f);
			break;
		}
		Game.Player.SetRunSpeedMultThisFrame(runSpeedMultThisFrame);
		if (Gtacnr.Utils.CheckTimePassed(lastRestoreT, 13000.0) && weaponWeight != WeaponWeight.Heavy)
		{
			lastRestoreT = DateTime.Now;
			API.RestorePlayerStamina(Game.Player.Handle, num);
		}
	}
}
