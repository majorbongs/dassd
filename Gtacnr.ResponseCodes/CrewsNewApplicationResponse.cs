namespace Gtacnr.ResponseCodes;

public enum CrewsNewApplicationResponse
{
	Timeout,
	Success,
	AlreadyInCrew,
	AlreadyApplied,
	CrewNotFound,
	TooManyRequests,
	CrewNotAcceptingApplications,
	UnknownError
}
