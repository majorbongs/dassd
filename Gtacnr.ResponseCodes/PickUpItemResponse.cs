namespace Gtacnr.ResponseCodes;

public enum PickUpItemResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	InvalidUser,
	InvalidDrop,
	InvalidProp,
	PropDoesntMatch,
	InventoryError,
	CannotDrop,
	NoSpaceLeft,
	ItemLimitReached,
	TooManyDrops,
	JobNotPermitted,
	TooFar,
	ConcurrentPickUp,
	JobNotAllowed
}
