namespace Gtacnr.ResponseCodes;

public enum ExtendRentalResponse
{
	Timeout,
	Success,
	GenericError,
	Unauthorized,
	TransactionError,
	InvalidCharacter,
	InvalidVehicle,
	InvalidVehicleModel,
	Cooldown,
	NoMoney,
	RentNotOverdue,
	NotARentalVehicle,
	AttachedToTowtruck,
	Unavailable,
	UnableToDespawn,
	UnableToTransfer
}
