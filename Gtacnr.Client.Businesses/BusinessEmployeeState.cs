using System;
using CitizenFX.Core;
using Gtacnr.Model;

namespace Gtacnr.Client.Businesses;

public class BusinessEmployeeState
{
	public Business Business { get; set; }

	public string BusinessId { get; set; }

	public Ped Ped { get; set; }

	public bool IsActive { get; set; }

	public bool IsBeingRobbed { get; set; }

	public bool IsAttacking { get; set; }

	public bool IsScared { get; set; }

	public bool DeathEventTriggered { get; set; }

	public bool PreventRespawn { get; set; }

	public DateTime DeadT { get; set; }

	public DateTime LastScaredT { get; set; }

	public void ResetState()
	{
		IsActive = false;
		IsBeingRobbed = false;
		IsAttacking = false;
		IsScared = false;
		DeathEventTriggered = false;
		PreventRespawn = false;
		DeadT = default(DateTime);
		LastScaredT = default(DateTime);
	}
}
