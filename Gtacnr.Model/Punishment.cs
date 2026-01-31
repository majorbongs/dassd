using System;

namespace Gtacnr.Model;

public class Punishment
{
	public string Id { get; set; }

	public string UserId { get; set; }

	public string IssuerId { get; set; }

	public DateTime DateTime { get; set; }

	public PunishmentType Type { get; set; }

	public bool Canceled { get; set; }

	public PunishmentData Data { get; set; }
}
