using System;
using System.Collections.Generic;

namespace Gtacnr.Model;

public class RaceState
{
	public int HostId { get; set; }

	public Dictionary<int, RaceParticipantInfo> Participants { get; set; } = new Dictionary<int, RaceParticipantInfo>();

	public Dictionary<int, long> LeftParticipantsBets { get; set; } = new Dictionary<int, long>();

	public Dictionary<int, long> RefundedBets { get; set; } = new Dictionary<int, long>();

	public RaceTrack CurrentTrack { get; set; }

	public DateTime LastCheckpointPass { get; set; }

	public bool TimeWarningIssued { get; set; }

	public bool RaceStarted { get; set; }
}
