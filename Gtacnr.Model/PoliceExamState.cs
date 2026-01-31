using System.Collections.Generic;

namespace Gtacnr.Model;

public class PoliceExamState
{
	public int TerminationReason { get; set; }

	public Dictionary<int, List<int>> Answers { get; set; } = new Dictionary<int, List<int>>();
}
