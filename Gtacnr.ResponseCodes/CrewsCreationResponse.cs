namespace Gtacnr.ResponseCodes;

public enum CrewsCreationResponse
{
	Timeout,
	Success,
	AlreadyInCrew,
	NoCharacter,
	AcronymTaken,
	NameTaken,
	PendingRequest,
	DatabaseError,
	AcronymDisallowed,
	InsufficientFunds,
	InsufficientLevel,
	InvalidAcronym,
	InvalidName,
	UnknownError
}
