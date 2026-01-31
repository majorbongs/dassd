using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;

namespace Gtacnr.Data;

public static class PurchaseableItems
{
	private static readonly Dictionary<string, PurchaseableItem> _purchaseableItems = InitializePurchaseableItems();

	private static Dictionary<string, PurchaseableItem> InitializePurchaseableItems()
	{
		return Utils.LoadJson<List<PurchaseableItem>>("gtacnr_items", "data/store/purchaseableItems.json").ToDictionary((PurchaseableItem p) => p.Id, (PurchaseableItem p) => p);
	}

	public static PurchaseableItem? GetDefinition(string purchItemId)
	{
		if (_purchaseableItems.ContainsKey(purchItemId))
		{
			return _purchaseableItems[purchItemId];
		}
		return null;
	}

	public static IEnumerable<PurchaseableItem> GetAllDefinitions()
	{
		return _purchaseableItems.Values.AsEnumerable();
	}
}
