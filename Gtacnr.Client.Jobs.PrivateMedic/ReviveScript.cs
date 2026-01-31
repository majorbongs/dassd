using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.PrivateMedic;

public class ReviveScript : BaseReviveScript
{
	protected override string StartReviveEvent => "gtacnr:privateMedic:startRevive";

	protected override string EndReviveEvent => "gtacnr:privateMedic:endRevive";

	protected override string CancelReviveEvent => "gtacnr:privateMedic:cancelRevive";

	protected override int ReviveSeconds => 5;

	protected override bool AbortCondition(Player target, Vector3 reviveStartedPos, int startingHealth)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		if (!base.AbortCondition(target, reviveStartedPos, startingHealth) && !CuffedScript.IsBeingCuffedOrUncuffed)
		{
			return API.IsPedBeingStunned(API.PlayerPedId(), 0);
		}
		return true;
	}

	protected override bool IsJobAllowed(JobsEnum jobId)
	{
		return jobId == JobsEnum.PrivateMedic;
	}
}
