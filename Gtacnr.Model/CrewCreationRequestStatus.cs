namespace Gtacnr.Model;

public enum CrewCreationRequestStatus : byte
{
	Pending,
	Approved,
	Rejected,
	Invalidated,
	InvalidatedAndRefunded
}
