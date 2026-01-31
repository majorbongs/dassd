using Gtacnr.Model.Robberies.Jewelry;

namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class PhaseChangedEventArgs
{
	public RobberyPhase OldPhase { get; set; }

	public RobberyPhase NewPhase { get; set; }

	public PhaseChangedEventArgs(RobberyPhase oldPhase, RobberyPhase newPhase)
	{
		OldPhase = oldPhase;
		NewPhase = newPhase;
	}
}
