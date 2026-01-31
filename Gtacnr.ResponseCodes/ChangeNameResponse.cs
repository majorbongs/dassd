namespace Gtacnr.ResponseCodes;

public enum ChangeNameResponse
{
	Timeout,
	Success,
	GenericError,
	Unauthorized,
	TooManyRequests,
	Taken,
	InvalidName,
	TransactionFailed,
	RenameFailed,
	StaffRename,
	WillUseToken,
	Cooldown
}
