using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum ModerationActionReason
{
	[Description("RDM")]
	RDM,
	[Description("Cross Teaming")]
	CrossTeaming,
	[Description("Cheating")]
	Cheating,
	[Description("Exploiting")]
	Exploiting,
	[Description("Combat Logging")]
	CombatLogging,
	[Description("Evading a Ban")]
	BanEvading,
	[Description("Lying to Staff")]
	LyingToStaff,
	[Description("Staff Impersonation")]
	StaffImpersonation,
	[Description("Advertising")]
	Advertising,
	[Description("Bad Conduct")]
	BadConduct
}
