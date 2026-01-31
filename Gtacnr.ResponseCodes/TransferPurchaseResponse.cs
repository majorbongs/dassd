namespace Gtacnr.ResponseCodes;

public enum TransferPurchaseResponse
{
	Timeout,
	Success,
	GenericError,
	DatabaseError,
	Busy,
	InvalidItem,
	InvalidTarget,
	SelfTarget,
	ItemNotTransferrable,
	InsufficientAmount
}
