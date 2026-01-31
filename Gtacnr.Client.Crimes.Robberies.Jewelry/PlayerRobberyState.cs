using System.Collections.Generic;
using Gtacnr.Model;
using Gtacnr.Model.Robberies.Jewelry;

namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class PlayerRobberyState
{
	public bool IsParticipating { get; private set; }

	public RobberyPhase Phase { get; set; }

	public List<InventoryEntry> TakenItems { get; set; } = new List<InventoryEntry>();

	public int TakenItemsCount { get; set; }

	public bool WereCopsCalled { get; set; }

	public int TargetGlassIndex { get; set; }

	public bool IsBreakingGlass { get; set; }

	public void StartParticipating()
	{
		IsParticipating = true;
		Phase = RobberyPhase.Stealing;
		TakenItems = new List<InventoryEntry>();
		TakenItemsCount = 0;
		WereCopsCalled = false;
		TargetGlassIndex = -1;
	}

	public void StopParticipating()
	{
		IsParticipating = false;
		Phase = RobberyPhase.Finished;
		TakenItems = new List<InventoryEntry>();
		TakenItemsCount = 0;
		WereCopsCalled = false;
		TargetGlassIndex = -1;
	}
}
