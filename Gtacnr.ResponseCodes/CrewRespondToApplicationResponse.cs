namespace Gtacnr.ResponseCodes;

public enum CrewRespondToApplicationResponse
{
	Timeout,
	Success,
	ApplicationNotFound,
	AlreadyProcessed,
	PlayerAlreadyInCrew,
	NoPermission,
	UnknownError
}
