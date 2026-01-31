using System.Collections.Generic;

namespace Gtacnr.Model.Robberies.Jewelry;

public class RobberyState
{
	public bool IsInProgress { get; set; }

	public List<int> Participants { get; private set; } = new List<int>();

	public int AlarmStartTimeLeft { get; set; } = -1;

	public int AlarmEndTimeLeft { get; set; } = -1;

	public bool WasAlarmTripped => AlarmStartTimeLeft == 0;

	public bool DidAlarmEnd => AlarmEndTimeLeft == 0;

	public List<int> DisabledGlasses { get; private set; } = new List<int>();
}
