namespace Gtacnr.ResponseCodes;

public enum DropItemResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	InvalidUser,
	InvalidItem,
	InvalidAmount,
	InvalidProp,
	InventoryError,
	CannotDrop,
	UnableToCreateDrop,
	InsufficientAmount,
	TooManyDrops
}
