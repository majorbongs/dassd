namespace Gtacnr.ResponseCodes;

public enum ShopliftResponse
{
	Timeout,
	Success,
	GenericError,
	Spotted,
	Cooldown,
	Busy,
	InvalidUser,
	InvalidCharacter,
	InvalidPed,
	InvalidSupply,
	InvalidSupplyData,
	InvalidBusiness,
	TooFar,
	OutOfStock,
	InvalidItem,
	TransactionError,
	InventoryError,
	NoSpaceLeft,
	ItemLimitReached,
	ScriptCanceled,
	JobNotPermitted
}
