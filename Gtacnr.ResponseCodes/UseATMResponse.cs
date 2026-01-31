namespace Gtacnr.ResponseCodes;

public enum UseATMResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	InvalidAmount,
	InsufficientFunds,
	CantCoverFees,
	InvalidATM,
	TransactionError,
	TooFar
}
