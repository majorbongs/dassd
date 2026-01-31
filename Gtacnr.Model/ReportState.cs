using System.ComponentModel;

namespace Gtacnr.Model;

public enum ReportState
{
	[Description("Pending")]
	Pending,
	[Description("Assigned")]
	Assigned,
	[Description("Solved")]
	Solved
}
