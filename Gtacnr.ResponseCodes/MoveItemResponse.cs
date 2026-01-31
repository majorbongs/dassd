namespace Gtacnr.ResponseCodes;

public enum MoveItemResponse
{
	Timeout,
	Success,
	GenericError,
	InvalidCharacter,
	InvalidJob,
	InvalidItem,
	InventoryError,
	InsufficientAmount,
	LimitReached,
	NoSpaceLeft
}
