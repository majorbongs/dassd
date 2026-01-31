using System;

namespace Gtacnr.Model;

public sealed class CrewCreationRequestReview
{
	public Guid RequestId { get; set; }

	public string ReviewerName { get; set; }

	public bool Approved { get; set; }
}
