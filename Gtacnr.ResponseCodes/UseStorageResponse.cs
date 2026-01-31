namespace Gtacnr.ResponseCodes;

public enum UseStorageResponse
{
	GenericError,
	Success,
	Busy,
	InvalidCharacter,
	InvalidItem,
	InvalidAmount,
	InvalidProperty,
	InvalidDestination,
	InventoryError,
	NoSpaceLeft,
	LimitReached,
	InsufficientAmount,
	MissingPermissions,
	JobNotAllowed
}
