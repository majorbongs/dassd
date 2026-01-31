namespace Gtacnr.ResponseCodes;

public enum CreateHitmanContractResponse : uint
{
	InvalidTarget,
	InvalidReward,
	TooManyActiveContracts,
	GenericError,
	NotEnoughMoney,
	TransactionError,
	Duplicate,
	TooFar,
	Success
}
