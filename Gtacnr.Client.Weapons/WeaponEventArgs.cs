using System;
using CitizenFX.Core;

namespace Gtacnr.Client.Weapons;

public class WeaponEventArgs : EventArgs
{
	public WeaponHash PreviousWeaponHash { get; private set; }

	public WeaponHash NewWeaponHash { get; private set; }

	public WeaponEventArgs(WeaponHash previousWeaponHash, WeaponHash newWeaponHash)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		PreviousWeaponHash = previousWeaponHash;
		NewWeaponHash = newWeaponHash;
	}
}
