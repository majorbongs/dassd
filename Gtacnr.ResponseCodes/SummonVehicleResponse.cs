namespace Gtacnr.ResponseCodes;

public enum SummonVehicleResponse
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
	InvalidRoutingBucket,
	InvalidVehicleData,
	InvalidGarage,
	JobMismatch,
	Cooldown,
	NoMoney,
	MembershipTier,
	Unavailable,
	AttachedToTowtruck,
	RentOverdue
}
