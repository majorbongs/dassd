using System;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class MembershipEntry
{
	public string Id { get; set; }

	public string UserId { get; set; }

	public DateTime StartDate { get; set; }

	public DateTime ExpiryDate { get; set; }

	public MembershipTier Tier { get; set; }

	public bool IsTemporary { get; set; }

	public string Remarks { get; set; }

	public MembershipStatus Status { get; set; }
}
