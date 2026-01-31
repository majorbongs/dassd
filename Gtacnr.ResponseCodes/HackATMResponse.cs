namespace Gtacnr.ResponseCodes;

public enum HackATMResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	InvalidCharacter,
	InvalidPed,
	InvalidJob,
	MissingToolkit,
	Cooldown,
	InventoryError,
	TransactionError,
	InvalidATM,
	TooFar
}
