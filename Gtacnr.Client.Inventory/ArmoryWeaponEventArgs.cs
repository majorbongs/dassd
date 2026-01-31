using System;
using CitizenFX.Core;

namespace Gtacnr.Client.Inventory;

public class ArmoryWeaponEventArgs : EventArgs
{
	public WeaponHash WeaponHash { get; private set; }

	public ArmoryWeaponEventArgs(WeaponHash weaponHash)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		WeaponHash = weaponHash;
	}
}
