using System;

namespace Gtacnr.Model;

public class KnockInfo
{
	public int PlayerId { get; set; }

	public string PropertyId { get; set; }

	public DateTime Timestamp { get; }

	public KnockInfo()
	{
		Timestamp = DateTime.UtcNow;
	}
}
