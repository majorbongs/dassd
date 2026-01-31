using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum MembershipTier : byte
{
	[Description("None")]
	None,
	[Description("Silver Membership")]
	Silver,
	[Description("Gold Membership")]
	Gold,
	[Description("Diamond Membership")]
	Diamond
}
