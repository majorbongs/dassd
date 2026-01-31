using System;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class PendingAction
{
	public string Id { get; set; }

	public string UserId { get; set; }

	public string IssuerId { get; set; }

	public PendingActionType Type { get; set; }

	public DateTime? DateTime { get; set; }

	public bool Processed { get; set; }

	public DateTime? ProcessDateTime { get; set; }

	public byte[] Data { get; set; }

	public PendingActionData ActionData { get; set; }
}
