using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Crimes;

namespace Gtacnr.Client.Anticheat;

public class AntiMagicBulletScript : Script
{
	private readonly DetectionThresholdManager detectionManager = new DetectionThresholdManager(10, TimeSpan.FromSeconds(60.0));

	private readonly HashSet<int> ignoredGroups = new HashSet<int>
	{
		API.GetHashKey("GROUP_SHOTGUN"),
		API.GetHashKey("GROUP_SNIPER")
	};

	public AntiMagicBulletScript()
	{
		AssaultScript.PlayerDamaged += OnPlayerDamaged;
	}

	private void OnPlayerDamaged(object sender, PlayerDamagedEvent e)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		if (API.GetWeaponDamageType((uint)e.weapon) != 3)
		{
			return;
		}
		int weapontypeGroup = API.GetWeapontypeGroup((uint)e.weapon);
		if (ignoredGroups.Contains(weapontypeGroup) || API.IsPedRagdoll(e.playerPed) || API.IsPedDeadOrDying(e.playerPed, false) || !API.IsEntityOccluded(e.playerPed))
		{
			return;
		}
		int num = 0;
		if (API.GetEntityPlayerIsFreeAimingAt(Game.Player.Handle, ref num) && num == e.playerPed)
		{
			return;
		}
		RaycastResult val = World.Raycast(GameplayCamera.Position, API.GetEntityCoords(e.playerPed, false), (IntersectOptions)13, (Entity)null);
		Entity hitEntity = ((RaycastResult)(ref val)).HitEntity;
		if (((hitEntity != null) ? new int?(((PoolObject)hitEntity).Handle) : ((int?)null)) != e.playerPed && !API.HasEntityClearLosToEntity(((PoolObject)Game.PlayerPed).Handle, e.playerPed, 1))
		{
			detectionManager.AddDetection();
			if (detectionManager.IsThresholdExceeded)
			{
				object[] obj = new object[4] { 30, 2, "magic bullet", null };
				string[] obj2 = new string[6]
				{
					"Target: ",
					API.GetEntityCoords(e.playerPed, false).Json(),
					", my position: ",
					((Entity)Game.PlayerPed).Position.Json(),
					", weapon: ",
					null
				};
				uint weapon = (uint)e.weapon;
				obj2[5] = weapon.ToString("X");
				obj[3] = string.Concat(obj2);
				BaseScript.TriggerServerEvent("gtacnr:ac:logMe", obj);
			}
		}
	}
}
