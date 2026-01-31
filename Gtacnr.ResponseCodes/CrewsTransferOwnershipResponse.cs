namespace Gtacnr.ResponseCodes;

public enum CrewsTransferOwnershipResponse
{
	Success,
	CrewNotFound,
	NotCrewOwner,
	NewOwnerNotFound,
	NewOwnerNotInCrew,
	InvalidTarget,
	UnknownError
}
