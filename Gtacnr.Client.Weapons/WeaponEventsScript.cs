using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Weapons;

public class WeaponEventsScript : Script
{
	private WeaponHash previousWeapon = (WeaponHash)(-1569615261);

	public static event EventHandler<WeaponEventArgs> WeaponChanged;

	[Update]
	private async Coroutine WeaponEventsTask()
	{
		await Script.Wait(10);
		if (!((Entity)(object)Game.PlayerPed == (Entity)null) && Utils.IsFreemodePed(Game.PlayerPed))
		{
			uint num = 0u;
			API.GetCurrentPedWeapon(API.PlayerPedId(), ref num, true);
			WeaponHash val = (WeaponHash)((num == 0) ? (-1569615261) : ((int)num));
			if (val != previousWeapon)
			{
				WeaponEventArgs e = new WeaponEventArgs(previousWeapon, val);
				previousWeapon = val;
				WeaponEventsScript.WeaponChanged?.Invoke(this, e);
			}
		}
	}
}
