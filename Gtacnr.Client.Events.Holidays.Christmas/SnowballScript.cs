using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Vehicles.Behaviors;

namespace Gtacnr.Client.Events.Holidays.Christmas;

public class SnowballScript : Script
{
	private static readonly uint snowballHash = (uint)API.GetHashKey("weapon_snowball");

	[EventHandler("gtacnr:christmas:initialize")]
	private void OnChristmasInitialize()
	{
		base.Update += SnowballTask;
	}

	private async Coroutine SnowballTask()
	{
		if (Game.IsControlJustReleased(2, (Control)47))
		{
			await PickupSnowball();
		}
	}

	private bool CanPickupSnowball()
	{
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Invalid comparison between Unknown and I4
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Invalid comparison between Unknown and I4
		if (!Game.PlayerPed.IsInVehicle() && !((Entity)Game.PlayerPed).IsDead && !API.IsPlayerSwitchInProgress() && !Game.IsPaused && API.IsScreenFadedIn() && !Game.PlayerPed.IsInParachuteFreeFall && !Game.PlayerPed.IsFalling && !Game.PlayerPed.IsBeingStunned && !Game.PlayerPed.IsWalking && !Game.PlayerPed.IsRunning && !Game.PlayerPed.IsSprinting && !Game.PlayerPed.IsSwimming && !Game.PlayerPed.IsSwimmingUnderWater && !Game.PlayerPed.IsDiving && !CuffedScript.IsInCustody && !CuffedScript.IsBeingCuffedOrUncuffed && (Entity)(object)EnterVehicleScript.TargetVehicle == (Entity)null)
		{
			if ((int)Game.PlayerPed.Weapons.Current.Hash != 126349499)
			{
				return (int)Game.PlayerPed.Weapons.Current.Hash == -1569615261;
			}
			return true;
		}
		return false;
	}

	private async Coroutine PickupSnowball()
	{
		if (!CanPickupSnowball())
		{
			return;
		}
		Game.PlayerPed.Task.ClearAll();
		string snowball_anim_dict = "anim@mp_snowball";
		string snowball_anim_name = "pickup_snowball";
		int maxSnowballs = 10;
		API.GetMaxAmmo(((PoolObject)Game.PlayerPed).Handle, snowballHash, ref maxSnowballs);
		if (API.GetAmmoInPedWeapon(((PoolObject)Game.PlayerPed).Handle, snowballHash).Clamp(0, maxSnowballs) < maxSnowballs)
		{
			API.SetPedCurrentWeaponVisible(((PoolObject)Game.PlayerPed).Handle, false, true, false, false);
			if (!API.HasAnimDictLoaded(snowball_anim_dict))
			{
				API.RequestAnimDict(snowball_anim_dict);
				while (!API.HasAnimDictLoaded(snowball_anim_dict))
				{
					await Script.Yield();
				}
			}
			API.TaskPlayAnim(((PoolObject)Game.PlayerPed).Handle, snowball_anim_dict, snowball_anim_name, 8f, 1f, 1500, 0, 0f, false, false, false);
			bool fired = false;
			float dur = API.GetAnimDuration(snowball_anim_dict, snowball_anim_name);
			int timer = API.GetGameTimer();
			while (API.GetEntityAnimCurrentTime(((PoolObject)Game.PlayerPed).Handle, snowball_anim_dict, snowball_anim_name) < 0.97f)
			{
				await Script.Yield();
				if (!fired)
				{
					if (API.HasAnimEventFired(((PoolObject)Game.PlayerPed).Handle, (uint)API.GetHashKey("CreateObject")))
					{
						API.AddAmmoToPed(((PoolObject)Game.PlayerPed).Handle, snowballHash, 2);
						API.GiveWeaponToPed(((PoolObject)Game.PlayerPed).Handle, snowballHash, 0, true, true);
						if (API.GetAmmoInPedWeapon(((PoolObject)Game.PlayerPed).Handle, snowballHash) > maxSnowballs)
						{
							API.SetPedAmmo(((PoolObject)Game.PlayerPed).Handle, snowballHash, maxSnowballs);
						}
						fired = true;
					}
					else if (API.HasAnimEventFired(((PoolObject)Game.PlayerPed).Handle, (uint)API.GetHashKey("Interrupt")))
					{
						break;
					}
				}
				else if (API.HasAnimEventFired(((PoolObject)Game.PlayerPed).Handle, (uint)API.GetHashKey("Interrupt")))
				{
					break;
				}
				if ((float)(API.GetGameTimer() - timer) > dur * 1000f)
				{
					break;
				}
			}
		}
		else
		{
			Utils.DisplayHelpText($"You can't carry more than ~b~{maxSnowballs} snowballs~s~!");
		}
	}

	public static void ResetSnowballs()
	{
		API.SetPedAmmo(((PoolObject)Game.PlayerPed).Handle, snowballHash, 0);
	}
}
