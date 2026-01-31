namespace Gtacnr.ResponseCodes;

public enum UseItemResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	CannotUse,
	InvalidItem,
	InvalidAmount,
	InsufficientAmount,
	ScriptCanceled,
	ClientScriptCanceled,
	RateLimited
}
