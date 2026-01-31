using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum WeaponCategory
{
	[Description("@{cat_blades}")]
	Blades,
	[Description("@{cat_blunt}")]
	Blunt,
	[Description("@{cat_handguns}")]
	Handguns,
	[Description("@{cat_smgs}")]
	SMGs,
	[Description("@{cat_lmgs}")]
	LMGs,
	[Description("@{cat_shotguns}")]
	Shotguns,
	[Description("@{cat_assault_rifles}")]
	AssaultRifles,
	[Description("@{cat_precision_rifles}")]
	PrecisionRifles,
	[Description("@{cat_heavy_weapons}")]
	Heavy,
	[Description("@{cat_throwables}")]
	Throwables,
	[Description("@{cat_other}")]
	Other
}
