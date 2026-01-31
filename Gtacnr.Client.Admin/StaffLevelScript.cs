using System;
using CitizenFX.Core;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Admin;

public class StaffLevelScript : Script
{
	public static StaffLevel StaffLevel { get; private set; }

	public static bool HasAnyTesterLevel
	{
		get
		{
			if ((int)StaffLevel >= 15)
			{
				return (int)StaffLevel <= 25;
			}
			return false;
		}
	}

	public static event EventHandler<StaffLevelArgs> StaffLevelInitialized;

	public static event EventHandler<StaffLevelArgs> StaffLevelChanged;

	public static event EventHandler<StaffLevelArgs> StaffLevelInitializedOrChanged;

	protected override async void OnStarted()
	{
		await Utils.WaitUntilAccountDataLoaded();
		StaffLevel previousStaffLevel = StaffLevel;
		StaffLevel = (StaffLevel)(await TriggerServerEventAsync<int>("gtacnr:admin:getLevel", new object[0]));
		StaffLevelArgs e = new StaffLevelArgs(previousStaffLevel, StaffLevel);
		StaffLevelScript.StaffLevelInitialized?.Invoke(this, e);
		StaffLevelScript.StaffLevelInitializedOrChanged?.Invoke(this, e);
		Print($"^2Staff level: {StaffLevel}");
	}

	[EventHandler("gtacnr:admin:refreshLevel")]
	private void OnRefreshLevel(int staffLevelInt)
	{
		StaffLevel staffLevel = StaffLevel;
		StaffLevel = (StaffLevel)staffLevelInt;
		StaffLevelArgs e = new StaffLevelArgs(staffLevel, StaffLevel);
		StaffLevelScript.StaffLevelChanged?.Invoke(this, e);
		StaffLevelScript.StaffLevelInitializedOrChanged?.Invoke(this, e);
	}
}
