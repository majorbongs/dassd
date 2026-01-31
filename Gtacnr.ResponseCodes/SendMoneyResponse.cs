namespace Gtacnr.ResponseCodes;

public enum SendMoneyResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	TransactionError,
	InvalidUser,
	InvalidCharacter,
	InvalidPed,
	InvalidAmount,
	InvalidRecipient,
	InsufficientLevel,
	InsufficientFunds,
	TooFar,
	PlayerLimit,
	GlobalLimit,
	PlayerCooldown,
	GlobalCooldown,
	TargetPlayerLimit,
	TargetGlobalLimit,
	TargetPlayerCooldown,
	TargetGlobalCooldown
}
