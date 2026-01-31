using System.ComponentModel;

namespace Gtacnr.Model.Enums;

public enum StaffLevel : ushort
{
	[Description("None")]
	None = 0,
	[Description("Other Staff")]
	Helper = 10,
	[Description("Trainee Tester")]
	TrialTester = 15,
	[Description("Tester")]
	Tester = 20,
	[Description("Lead Tester")]
	LeadTester = 25,
	[Description("Designer")]
	Designer = 30,
	[Description("Developer")]
	Developer = 40,
	[Description("Trainee Moderator")]
	TrialModerator = 100,
	[Description("Moderator")]
	Moderator = 110,
	[Description("Lead Moderator")]
	LeadModerator = 115,
	[Description("Admin")]
	Admin = 120,
	[Description("Manager")]
	Manager = 130,
	[Description("Community Manager")]
	CommunityManager = 140,
	[Description("Staff Manager")]
	StaffManager = 150,
	[Description("Co-Founder")]
	Coowner = 999,
	[Description("Founder")]
	Owner = 1000
}
