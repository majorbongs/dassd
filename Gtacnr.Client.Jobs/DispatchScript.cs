using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Jobs.ArmsDealer;
using Gtacnr.Client.Jobs.DrugDealer;
using Gtacnr.Client.Jobs.Hitman;
using Gtacnr.Client.Jobs.Mechanic;
using Gtacnr.Client.Jobs.Paramedic;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Client.Jobs.PrivateMedic;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs;

public class DispatchScript : Script
{
	public static readonly PoliceDispatch PoliceDispatch = new PoliceDispatch();

	public static readonly ParamedicDispatch ParamedicDispatch = new ParamedicDispatch();

	public static readonly DrugDealerDispatch DrugDealerDispatch = new DrugDealerDispatch();

	public static readonly MechanicDispatch MechanicDispatch = new MechanicDispatch();

	public static readonly HitmanDispatch HitmanDispatch = new HitmanDispatch();

	public static readonly PrivateMedicDispatch PrivateMedicDispatch = new PrivateMedicDispatch();

	public static readonly ArmsDealerDispatch ArmsDealerDispatch = new ArmsDealerDispatch();

	public DispatchScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
		EventHandlerDictionary eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:police:dispatch"] = eventHandlers["gtacnr:police:dispatch"] + (Delegate)new Action<int, string>(PoliceDispatch.OnDispatch);
		eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:ems:dispatch"] = eventHandlers["gtacnr:ems:dispatch"] + (Delegate)new Action<int, string>(ParamedicDispatch.OnDispatch);
		eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:ems:cancelDispatch"] = eventHandlers["gtacnr:ems:cancelDispatch"] + (Delegate)new Action<int>(ParamedicDispatch.OnCancelDispatch);
		eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:drugDealer:receiveCall"] = eventHandlers["gtacnr:drugDealer:receiveCall"] + (Delegate)new Action<int>(DrugDealerDispatch.OnDispatch);
		eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:mechanic:receiveCall"] = eventHandlers["gtacnr:mechanic:receiveCall"] + (Delegate)new Action<int>(MechanicDispatch.OnDispatch);
		eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:hitman:receiveContract"] = eventHandlers["gtacnr:hitman:receiveContract"] + (Delegate)new Action<int, string>(HitmanDispatch.OnDispatch);
		eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:armsDealer:receiveCall"] = eventHandlers["gtacnr:armsDealer:receiveCall"] + (Delegate)new Action<int>(ArmsDealerDispatch.OnDispatch);
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		if (e.PreviousJobId != null)
		{
			switch (e.PreviousJobEnum)
			{
			case JobsEnum.Police:
				PoliceDispatch.ResetMenu();
				break;
			case JobsEnum.Paramedic:
				ParamedicDispatch.ResetMenu();
				break;
			case JobsEnum.DrugDealer:
				DrugDealerDispatch.ResetMenu();
				break;
			case JobsEnum.Mechanic:
				MechanicDispatch.ResetMenu();
				break;
			case JobsEnum.Hitman:
				HitmanDispatch.ResetMenu();
				break;
			case JobsEnum.PrivateMedic:
				PrivateMedicDispatch.ResetMenu();
				break;
			case JobsEnum.ArmsDealer:
				ArmsDealerDispatch.ResetMenu();
				break;
			case JobsEnum.Firefighter:
			case JobsEnum.Security:
			case JobsEnum.TaxiDriver:
			case JobsEnum.DeliveryDriver:
				break;
			}
		}
	}
}
