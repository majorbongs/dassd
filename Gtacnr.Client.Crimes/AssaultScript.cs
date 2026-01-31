using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Communication;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Client.Jobs.Trucker;

namespace Gtacnr.Client.Crimes;

public class AssaultScript : Script
{
	private Dictionary<int, DateTime> threatenedPedsList = new Dictionary<int, DateTime>();

	private Dictionary<int, DateTime> damagedPedsList = new Dictionary<int, DateTime>();

	private Dictionary<int, DateTime> damagedPlayersList = new Dictionary<int, DateTime>();

	private DateTime lastDmgEventT;

	private List<WeaponHash> disallowedWeapons = new List<WeaponHash>
	{
		(WeaponHash)(-1569615261),
		(WeaponHash)(-1951375401),
		(WeaponHash)1233104067,
		(WeaponHash)126349499,
		(WeaponHash)600439132,
		(WeaponHash)(-37975472),
		(WeaponHash)(-1600701090),
		(WeaponHash)101631238,
		(WeaponHash)(-72657034)
	};

	public static event EventHandler<PlayerDamagedEvent> PlayerDamaged;

	[EventHandler("gameEventTriggered")]
	private void OnGameEventTriggered(string name, List<object> args)
	{
		try
		{
			if (name == "CEventNetworkEntityDamage")
			{
				int num = Convert.ToInt32(args[0]);
				int num2 = Convert.ToInt32(args[1]);
				bool isDead = Convert.ToBoolean(args[5]);
				int num3 = Convert.ToInt32(args[6]);
				if (num3 == 0)
				{
					num3 = Convert.ToInt32(args[8]);
				}
				Entity val = Entity.FromHandle(num);
				Entity val2 = Entity.FromHandle(num2);
				if (val != (Entity)null && val2 != (Entity)null)
				{
					OnDamage(val, val2, num3, isDead);
				}
			}
		}
		catch
		{
		}
	}

	private void OnDamage(Entity victim, Entity aggressor, int weapon, bool isDead)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Expected I4, but got Unknown
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		if (API.IsPedAPlayer(((PoolObject)victim).Handle) && API.IsPedAPlayer(((PoolObject)aggressor).Handle))
		{
			int num = API.NetworkGetPlayerIndexFromPed(((PoolObject)victim).Handle);
			int playerServerId = API.GetPlayerServerId(num);
			int num2 = API.NetworkGetPlayerIndexFromPed(((PoolObject)aggressor).Handle);
			int playerServerId2 = API.GetPlayerServerId(num2);
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			string locationName = Utils.GetLocationName(position);
			if (num == num2 || !Gtacnr.Utils.CheckTimePassed(lastDmgEventT, 50.0))
			{
				return;
			}
			lastDmgEventT = DateTime.Now;
			if (num == API.PlayerId())
			{
				BaseScript.TriggerEvent("gtacnr:tookPlayerDamage", new object[4] { playerServerId2, weapon, isDead, locationName });
				BaseScript.TriggerServerEvent("gtacnr:tookPlayerDamage", new object[4] { playerServerId2, weapon, isDead, locationName });
			}
			else if (num2 == API.PlayerId())
			{
				if ((weapon == -1553120962 || weapon == 133987706) && ((Vector3)(ref position)).DistanceToSquared(victim.Position) > 100f)
				{
					return;
				}
				AssaultScript.PlayerDamaged?.Invoke(this, new PlayerDamagedEvent(playerServerId, ((PoolObject)victim).Handle, weapon));
				BaseScript.TriggerEvent("gtacnr:gavePlayerDamage", new object[4] { playerServerId, weapon, isDead, locationName });
				BaseScript.TriggerServerEvent("gtacnr:gavePlayerDamage", new object[4] { playerServerId, weapon, isDead, locationName });
			}
		}
		if (((PoolObject)aggressor).Handle == API.PlayerPedId() && isDead)
		{
			Vehicle val = (Vehicle)(object)((victim is Vehicle) ? victim : null);
			if (val != null && !string.IsNullOrEmpty(LatentVehicleStateScript.Get(((Entity)val).NetworkId)?.DeliveryId))
			{
				BaseScript.TriggerServerEvent("gtacnr:trucker:playerGoodsDestroyed", new object[2]
				{
					victim.NetworkId,
					(int)Game.PlayerPed.Weapons.Current.Hash
				});
			}
		}
		if (!isDead)
		{
			OnAssault(victim, aggressor, weapon);
		}
		else
		{
			OnKill(victim, aggressor, weapon);
		}
	}

	private async void OnAssault(Entity victim, Entity aggressor, int weapon)
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() || victim == (Entity)null || aggressor == (Entity)null || ((PoolObject)aggressor).Handle != API.PlayerPedId() || ((PoolObject)victim).Handle == API.PlayerPedId())
		{
			return;
		}
		bool flag = weapon == -1553120962 || weapon == 133987706;
		if (flag && !((Ped)((aggressor is Ped) ? aggressor : null)).IsInVehicle())
		{
			return;
		}
		if (((object)victim).GetType() == typeof(Ped))
		{
			if (API.GetPedType(((PoolObject)victim).Handle) == 28)
			{
				return;
			}
		}
		else if (((object)victim).GetType() == typeof(Vehicle))
		{
			Vehicle val = (Vehicle)(object)((victim is Vehicle) ? victim : null);
			if ((Entity)(object)val.Driver != (Entity)null && val.Driver.Exists())
			{
				victim = (Entity)(object)val.Driver;
			}
			else
			{
				if (val.PassengerCount <= 0)
				{
					VehicleDamageScript.OnDamageVehicle((Vehicle)(object)((victim is Vehicle) ? victim : null), weapon);
					TruckerJobScript.OnDamageVehicle((Vehicle)(object)((victim is Vehicle) ? victim : null), weapon);
					return;
				}
				victim = (Entity)(object)val.Passengers.First();
			}
		}
		if ((victim is Ped && ((Ped)((victim is Ped) ? victim : null)).IsInVehicle() && flag) || victim is Prop || API.IsEntityDead(((PoolObject)victim).Handle))
		{
			return;
		}
		if (API.IsPedAPlayer(((PoolObject)victim).Handle))
		{
			int playerServerId = API.GetPlayerServerId(API.NetworkGetPlayerIndexFromPed(((PoolObject)victim).Handle));
			if (!damagedPlayersList.ContainsKey(playerServerId) && !PartyScript.PartyMembers.Contains(playerServerId))
			{
				damagedPlayersList[playerServerId] = DateTime.UtcNow;
				BaseScript.TriggerServerEvent("gtacnr:crimes:assaultPlayer", new object[2] { playerServerId, weapon });
			}
		}
		else
		{
			if (!API.NetworkGetEntityIsNetworked(((PoolObject)victim).Handle))
			{
				return;
			}
			await BaseScript.Delay(5000);
			if (!API.IsEntityDead(((PoolObject)victim).Handle) && API.NetworkGetEntityIsNetworked(((PoolObject)victim).Handle))
			{
				int num = API.NetworkGetNetworkIdFromEntity(((PoolObject)victim).Handle);
				if (!damagedPedsList.ContainsKey(num))
				{
					damagedPedsList[num] = DateTime.UtcNow;
					BaseScript.TriggerServerEvent("gtacnr:crimes:assault", new object[2] { num, weapon });
				}
			}
		}
	}

	private async void OnKill(Entity victim, Entity aggressor, int weapon)
	{
		if (victim == (Entity)null || aggressor == (Entity)null || ((object)victim).GetType() != typeof(Ped) || ((PoolObject)aggressor).Handle != API.PlayerPedId() || ((PoolObject)victim).Handle == API.PlayerPedId())
		{
			return;
		}
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			ServeProtectScript.OnKill(victim, aggressor, weapon);
			return;
		}
		if (!API.IsPedAPlayer(((PoolObject)victim).Handle))
		{
			if (!API.NetworkGetEntityIsNetworked(((PoolObject)victim).Handle))
			{
				return;
			}
			if (API.GetPedType(((PoolObject)victim).Handle) == 28)
			{
				if (weapon != -1553120962 && weapon != 133987706)
				{
					BaseScript.TriggerServerEvent("gtacnr:crimes:killAnimal", new object[2]
					{
						API.NetworkGetNetworkIdFromEntity(((PoolObject)victim).Handle),
						weapon
					});
				}
			}
			else
			{
				BaseScript.TriggerServerEvent("gtacnr:crimes:murder", new object[2]
				{
					API.NetworkGetNetworkIdFromEntity(((PoolObject)victim).Handle),
					weapon
				});
			}
			return;
		}
		int num = API.NetworkGetPlayerIndexFromPed(((PoolObject)victim).Handle);
		int playerId = API.GetPlayerServerId(num);
		await BaseScript.Delay(3000);
		if (!damagedPlayersList.ContainsKey(playerId) && !PartyScript.PartyMembers.Contains(playerId))
		{
			damagedPlayersList[playerId] = DateTime.UtcNow;
			if (await Crime.GetWantedLevel() < 3)
			{
				BaseScript.TriggerServerEvent("gtacnr:crimes:assaultPlayer", new object[2] { playerId, weapon });
			}
		}
	}

	[Update]
	private async Coroutine DetectWeaponThreatTick()
	{
		await Script.Wait(1000);
		if ((Entity)(object)Game.PlayerPed == (Entity)null || Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			return;
		}
		uint num = 0u;
		API.GetCurrentPedWeapon(((PoolObject)Game.PlayerPed).Handle, ref num, true);
		if (disallowedWeapons.Contains((WeaponHash)num))
		{
			return;
		}
		int target = -1;
		API.GetEntityPlayerIsFreeAimingAt(Game.Player.Handle, ref target);
		if (target == -1 || !API.NetworkGetEntityIsNetworked(target))
		{
			return;
		}
		int targetNetId = API.NetworkGetNetworkIdFromEntity(target);
		if (threatenedPedsList.ContainsKey(targetNetId) || (API.IsEntityAMissionEntity(target) && !API.IsPedAPlayer(target)) || !API.IsEntityAPed(target) || API.IsEntityDead(target) || API.GetPedType(target) == 28)
		{
			return;
		}
		DateTime t = DateTime.UtcNow;
		while (API.IsPlayerFreeAimingAtEntity(Game.Player.Handle, target))
		{
			await Script.Yield();
			if (!Gtacnr.Utils.CheckTimePassed(t, 250.0))
			{
				continue;
			}
			if (API.IsEntityDead(target))
			{
				break;
			}
			await Script.Wait(3500);
			if (!API.IsEntityDead(target))
			{
				int playerServerId = API.GetPlayerServerId(API.NetworkGetPlayerIndexFromPed(target));
				if (API.IsPedAPlayer(target) && !damagedPlayersList.ContainsKey(playerServerId) && !PartyScript.PartyMembers.Contains(playerServerId))
				{
					threatenedPedsList.Add(targetNetId, DateTime.UtcNow);
					BaseScript.TriggerServerEvent("gtacnr:crimes:threatenPlayer", new object[1] { playerServerId });
				}
				else if (!API.IsPedAPlayer(target) && !damagedPedsList.ContainsKey(targetNetId))
				{
					threatenedPedsList.Add(targetNetId, DateTime.UtcNow);
					BaseScript.TriggerServerEvent("gtacnr:crimes:threaten", new object[1] { targetNetId });
				}
			}
			break;
		}
	}

	[Update]
	private async Coroutine CleanDamagedPedsListTick()
	{
		await Script.Wait(10000);
		foreach (int item in damagedPedsList.Keys.ToList())
		{
			if (Gtacnr.Utils.CheckTimePassed(damagedPedsList[item], 120000.0))
			{
				damagedPedsList.Remove(item);
			}
		}
		foreach (int item2 in damagedPlayersList.Keys.ToList())
		{
			if (Gtacnr.Utils.CheckTimePassed(damagedPlayersList[item2], 60000.0))
			{
				damagedPlayersList.Remove(item2);
			}
		}
		foreach (int item3 in threatenedPedsList.Keys.ToList())
		{
			if (Gtacnr.Utils.CheckTimePassed(threatenedPedsList[item3], 120000.0))
			{
				threatenedPedsList.Remove(item3);
			}
		}
	}

	[EventHandler("gtacnr:died")]
	private void OnDead(int killerId, int cause)
	{
		damagedPedsList.Clear();
		damagedPlayersList.Clear();
		threatenedPedsList.Clear();
	}
}
