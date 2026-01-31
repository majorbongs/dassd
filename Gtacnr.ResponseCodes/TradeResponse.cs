namespace Gtacnr.ResponseCodes;

public enum TradeResponse : byte
{
	Success,
	InsufficientAmount,
	InvalidItem,
	ItemLimitReached,
	ItemNotTransferable,
	JobNotAllowed,
	NoSpaceLeft,
	Transaction,
	TransferLimit,
	GenericError
}
