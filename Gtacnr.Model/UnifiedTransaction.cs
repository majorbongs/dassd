using System;

namespace Gtacnr.Model;

public class UnifiedTransaction
{
	public string Id { get; set; }

	public string FromCharacterId { get; set; }

	public string FromAccount { get; set; }

	public string ToCharacterId { get; set; }

	public string ToAccount { get; set; }

	public long Amount { get; set; }

	public DateTime DateTime { get; set; }

	public string Description { get; set; }

	public string GetCorrectDescription(string charId)
	{
		if (Description.Contains("\n"))
		{
			int num = Description.IndexOf("\n");
			string result = Description.Substring(0, num);
			string result2 = Description.Substring(num + 1);
			if (!(FromCharacterId == charId))
			{
				return result2;
			}
			return result;
		}
		return Description;
	}
}
