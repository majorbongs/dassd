using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Paramedic;

public class ReviveScript : BaseReviveScript
{
	protected override string StartReviveEvent => "gtacnr:ems:startRevive";

	protected override string EndReviveEvent => "gtacnr:ems:endRevive";

	protected override string CancelReviveEvent => "gtacnr:ems:cancelRevive";

	protected override int ReviveSeconds => 3;

	protected override bool IsJobAllowed(JobsEnum jobId)
	{
		return jobId.IsEMSOrFD();
	}
}
