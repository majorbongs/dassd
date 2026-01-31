using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum ClothingItemType
{
	[Description("@{cat_tops}")]
	Tops,
	[Description("@{cat_bottoms}")]
	Pants,
	[Description("@{cat_skirts}")]
	Skirts,
	[Description("@{cat_shoes}")]
	Shoes,
	[Description("@{cat_hats}")]
	Hats,
	[Description("@{cat_masks}")]
	Masks,
	[Description("@{cat_bags}")]
	Bags,
	[Description("@{cat_glasses}")]
	Glasses,
	[Description("@{cat_watches}")]
	Watches,
	[Description("@{cat_bracelets}")]
	Bracelets,
	[Description("@{cat_chains}")]
	Chains,
	[Description("@{cat_rings}")]
	Rings,
	[Description("@{cat_earrings}")]
	Earrings,
	[Description("@{cat_hairstyles}")]
	Hairstyles,
	[Description("@{cat_tattoos}")]
	Tattoos,
	[Description("@{cat_outfits}")]
	Outfits,
	[Description("@{cat_uniforms}")]
	Uniforms,
	[Description("@{cat_armor}")]
	Armor,
	[Description("@{cat_other}")]
	Other,
	[Description("@{cat_staff}")]
	Staff
}
