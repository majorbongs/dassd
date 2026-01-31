using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class WeaponComponentDefinition : InventoryItemBase, IItemOrService
{
	[JsonIgnore]
	public int Hash { get; set; }

	public WeaponComponentType Type { get; set; }

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

	public WeaponComponentDefinition()
	{
		base.Category = ItemCategory.Attachments;
	}
}
