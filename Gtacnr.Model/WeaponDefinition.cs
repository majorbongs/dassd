using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class WeaponDefinition : InventoryItemBase, IItemOrService
{
	[JsonIgnore]
	public int Hash { get; set; }

	public new WeaponCategory Category { get; set; }

	public WeaponWeight WeaponWeight { get; set; }

	public float Sway { get; set; }

	public bool CanEnterSafeZones { get; set; }

	public float GlassBreakChance { get; set; }

	public Vector3 PreviewRotation
	{
		get
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			if (PreviewRotation_ != null)
			{
				return new Vector3(PreviewRotation_[0], PreviewRotation_[1], PreviewRotation_[2]);
			}
			return default(Vector3);
		}
	}

	public float[] PreviewRotation_ { get; set; }

	public WeaponTextureInfo TextureInfo { get; set; }

	public override string ToString()
	{
		return base.Name;
	}

	public WeaponDefinition()
	{
		base.Category = ItemCategory.Weapons;
	}
}
