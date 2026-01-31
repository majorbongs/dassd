using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum PurchaseableItemType
{
	[Description("Undefined")]
	Undefined,
	[Description("Premium Membership Subscription")]
	MembershipSubscription,
	[Description("Premium Membership Gift")]
	MembershipGift,
	[Description("Premium Membership Pre-Order")]
	MembershipPreOrder,
	[Description("Token")]
	Token,
	[Description("Other")]
	Other
}
