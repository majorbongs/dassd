using System.Collections.Generic;

namespace Gtacnr.Model;

public class PoliceExamQuestion
{
	public string Question { get; set; }

	public List<PoliceExamOption> Options { get; set; } = new List<PoliceExamOption>();

	public int? OriginalIndex { get; set; }

	public void InitOptionsIndex()
	{
		for (int i = 0; i < Options.Count; i++)
		{
			Options[i].OriginalIndex = i;
		}
	}
}
