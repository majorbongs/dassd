using System.ComponentModel;

namespace Gtacnr.Model;

public enum ReportReason
{
	[Description("Other")]
	Other,
	[Description("RDM")]
	RDM,
	[Description("Cross Teaming")]
	CrossTeaming,
	[Description("Cheating")]
	Cheating,
	[Description("Quitting")]
	Quitting,
	[Description("Spamming")]
	Spamming,
	[Description("Hate Speech")]
	HateSpeech,
	[Description("Harassment")]
	Harassment
}
