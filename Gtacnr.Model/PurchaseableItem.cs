using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class PurchaseableItem
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public string Icon { get; set; }

	public PurchaseableItemType Type { get; set; }

	public MembershipTier Tier { get; set; }

	public int DurationDays { get; set; }

	public bool IsTransferable { get; set; }

	public bool IsSubscription { get; set; }

	public string Extra { get; set; }
}
