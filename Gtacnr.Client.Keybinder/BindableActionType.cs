using System.ComponentModel;

namespace Gtacnr.Client.Keybinder;

public enum BindableActionType
{
	[Description("Equip")]
	EquipWeapon,
	[Description("Use")]
	UseItem,
	[Description("Run")]
	Custom
}
