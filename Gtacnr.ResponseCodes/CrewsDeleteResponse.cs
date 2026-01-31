namespace Gtacnr.ResponseCodes;

public enum CrewsDeleteResponse
{
	Success,
	CrewNotFound,
	NotCrewOwner,
	CrewHasMultipleMembers,
	UnknownError
}
