using System.Collections.Generic;

namespace Gtacnr.Model;

public class Loadout
{
	public List<LoadoutWeapon> WeaponData { get; set; } = new List<LoadoutWeapon>();

	public List<LoadoutAmmo> AmmoData { get; set; } = new List<LoadoutAmmo>();

	public List<LoadoutAttachment> AttachmentData { get; set; } = new List<LoadoutAttachment>();
}
