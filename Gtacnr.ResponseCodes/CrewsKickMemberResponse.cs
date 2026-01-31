namespace Gtacnr.ResponseCodes;

public enum CrewsKickMemberResponse
{
	Timeout,
	Success,
	CannotModifySelf,
	NoCrew,
	NoPermission,
	MemberNotFound,
	InvalidReason,
	UnknownError
}
