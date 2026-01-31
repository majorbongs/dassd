namespace Gtacnr.ResponseCodes;

public enum SellVehicleResponse
{
	Timeout,
	Success,
	GenericError,
	Unauthorized,
	TransactionError,
	InvalidCharacter,
	InvalidJob,
	InvalidVehicle,
	InvalidVehicleModel,
	InvalidVehicleData,
	InvalidRoutingBucket,
	UnableToDespawn,
	UnableToTransfer,
	JobMismatch,
	Cooldown,
	MembershipTier,
	Unavailable,
	AttachedToTowtruck
}
