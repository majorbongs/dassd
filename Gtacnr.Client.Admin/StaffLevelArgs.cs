using System;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Admin;

public class StaffLevelArgs : EventArgs
{
	public StaffLevel PreviousStaffLevel { get; set; }

	public StaffLevel NewStaffLevel { get; set; }

	public StaffLevelArgs(StaffLevel previousStaffLevel, StaffLevel newStaffLevel)
	{
		PreviousStaffLevel = previousStaffLevel;
		NewStaffLevel = newStaffLevel;
	}
}
