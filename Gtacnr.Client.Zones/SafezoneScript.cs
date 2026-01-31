using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Zones;

public class SafezoneScript : Script
{
	private static List<Safezone> safezones = Gtacnr.Utils.LoadJson<List<Safezone>>("data/safezones.json");

	public static Safezone Current { get; private set; }

	public static Safezone GetSafezoneAtCoords(Vector3 pos, bool onlyEnabled = false)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Safezone result = null;
		foreach (Safezone safezone in safezones)
		{
			if ((!onlyEnabled || safezone.Enabled) && ((Vector3)(ref pos)).DistanceToSquared2D(safezone.Position_) < safezone.Radius * safezone.Radius)
			{
				result = safezone;
				break;
			}
		}
		return result;
	}

	protected override void OnStarted()
	{
		RefreshSafezoneBlips();
		WeaponEventsScript.WeaponChanged += OnWeaponChanged;
	}

	private void RefreshSafezoneBlips()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		foreach (Safezone safezone in safezones)
		{
			if (safezone.Blip != (Blip)null)
			{
				((PoolObject)safezone.Blip).Delete();
				safezone.Blip = null;
			}
			if (safezone.Enabled)
			{
				safezone.Blip = World.CreateBlip(safezone.Position_, safezone.Radius);
				safezone.Blip.IsShortRange = false;
				safezone.Blip.Sprite = (BlipSprite)(-1);
				safezone.Blip.Color = (BlipColor)0;
				safezone.Blip.Alpha = 64;
				Utils.SetBlipName(safezone.Blip, "Safezone", "safe_zone");
				API.SetBlipDisplay(((PoolObject)safezone.Blip).Handle, 8);
			}
		}
	}

	[Update]
	private async Coroutine UpdateTask()
	{
		await Script.Wait(250);
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			Current = null;
			return;
		}
		Safezone safezoneAtCoords = GetSafezoneAtCoords(((Entity)Game.PlayerPed).Position, onlyEnabled: true);
		if (safezoneAtCoords == Current)
		{
			return;
		}
		Current = safezoneAtCoords;
		if (safezoneAtCoords != null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.GUN_FREE_ZONE_ENTERED));
			Weapon current = Game.PlayerPed.Weapons.Current;
			WeaponDefinition weaponDefinitionByHash = Gtacnr.Data.Items.GetWeaponDefinitionByHash((uint)(int)current.Hash);
			int weaponDamageType = API.GetWeaponDamageType((uint)(int)current.Hash);
			bool flag = weaponDefinitionByHash?.CanEnterSafeZones ?? false;
			if ((weaponDamageType == 3 || weaponDamageType == 5 || weaponDamageType == 6) && !flag)
			{
				Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
				Utils.PlayErrorSound();
			}
			RemoveWeaponsInExtraSeats();
		}
		else
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.GUN_FREE_ZONE_LEFT));
		}
		static async void RemoveWeaponsInExtraSeats()
		{
			while ((int)Game.PlayerPed.SeatIndex > 2 && GetSafezoneAtCoords(((Entity)Game.PlayerPed).Position, onlyEnabled: true) != null)
			{
				await BaseScript.Delay(10);
				Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			}
		}
	}

	private async void OnWeaponChanged(object sender, WeaponEventArgs e)
	{
		if (Current == null)
		{
			return;
		}
		WeaponDefinition weaponDefinitionByHash = Gtacnr.Data.Items.GetWeaponDefinitionByHash((uint)(int)e.NewWeaponHash);
		int weaponDamageType = API.GetWeaponDamageType((uint)(int)e.NewWeaponHash);
		bool flag = weaponDefinitionByHash?.CanEnterSafeZones ?? false;
		if ((weaponDamageType == 3 || weaponDamageType == 5 || weaponDamageType == 6) && !flag)
		{
			WeaponBehaviorScript.BlockWeaponSwitchingById("safeZone");
			if (!Game.PlayerPed.IsInVehicle())
			{
				Utils.PlayErrorSound();
			}
			await BaseScript.Delay(100);
			WeaponBehaviorScript.UnblockWeaponSwitchingById("safeZone");
		}
	}

	[EventHandler("gtacnr:redzones:activeRedzoneChanged")]
	private void OnRedzoneChanged(string jRedzone)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		safezones.ForEach(delegate(Safezone s)
		{
			s.Enabled = true;
		});
		RefreshSafezoneBlips();
		Redzone redzone = jRedzone.Unjson<Redzone>();
		if (redzone != null)
		{
			Safezone safezoneAtCoords = GetSafezoneAtCoords(redzone.Position);
			if (safezoneAtCoords != null)
			{
				safezoneAtCoords.Enabled = false;
				RefreshSafezoneBlips();
			}
		}
	}
}
