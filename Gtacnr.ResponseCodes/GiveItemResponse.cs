namespace Gtacnr.ResponseCodes;

public enum GiveItemResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	InvalidUser,
	InvalidTarget,
	InvalidItem,
	InvalidAmount,
	CannotGive,
	TooFar,
	InsufficientAmount,
	NoSpaceLeft,
	ItemLimitReached,
	CantSendToSelf,
	JobNotPermitted,
	ScriptCanceled,
	ClientScriptCanceled,
	RateLimited
}
