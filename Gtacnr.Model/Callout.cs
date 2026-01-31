using System;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class Callout
{
	public char Letter { get; set; }

	public bool UseLetter { get; set; }

	public PlayerState Target { get; set; }

	public PlayerState Caller { get; set; }

	public DateTime DateTime { get; set; }

	public Vector3 Position { get; set; }

	public float Range { get; set; }

	public string ReasonId { get; set; }

	public string Type { get; set; }

	public string Details { get; set; }

	public bool Responded { get; set; }
}
