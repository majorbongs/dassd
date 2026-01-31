using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum ItemCategory
{
	[Description("@{cat_food}")]
	Foods,
	[Description("@{cat_drinks}")]
	Drinks,
	[Description("@{cat_drugs}")]
	Drugs,
	[Description("@{cat_smoking}")]
	Smoking,
	[Description("@{cat_tools}")]
	Tools,
	[Description("@{cat_devices}")]
	Devices,
	[Description("@{cat_gear}")]
	Gear,
	[Description("@{cat_gift_cards}")]
	Cards,
	[Description("@{cat_paperwork}")]
	Paperwork,
	[Description("@{cat_gambling}")]
	Gambling,
	[Description("@{cat_stolen}")]
	Stolen,
	[Description("@{cat_special}")]
	Special,
	[Description("@{cat_other}")]
	Other,
	[Description("@{cat_weapons}")]
	Weapons,
	[Description("@{cat_ammo}")]
	Ammo,
	[Description("@{cat_attachments}")]
	Attachments,
	[Description("@{cat_clothing}")]
	Clothing
}
