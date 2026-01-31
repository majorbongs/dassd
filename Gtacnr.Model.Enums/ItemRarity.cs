using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum ItemRarity
{
	[Description("Common")]
	Common,
	[Description("Uncommon")]
	Uncommon,
	[Description("Rare")]
	Rare,
	[Description("Very Rare")]
	VeryRare,
	[Description("Legendary")]
	Legendary,
	[Description("Unique")]
	Unique
}
