using System.Collections.Generic;

namespace Gtacnr.Model;

public class DailyChallengeResponse
{
	public List<DailyChallengeEntry> DailyChallengeEntries { get; set; } = new List<DailyChallengeEntry>();

	public Dictionary<string, uint> PlayerProgress { get; set; } = new Dictionary<string, uint>();
}
