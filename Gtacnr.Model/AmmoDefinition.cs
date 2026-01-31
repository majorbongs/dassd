using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class AmmoDefinition : InventoryItemBase, IItemOrService
{
	[JsonIgnore]
	public int Hash { get; set; }

	public AmmoDefinition()
	{
		base.Category = ItemCategory.Weapons;
	}
}
