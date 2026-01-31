namespace Gtacnr.ResponseCodes;

public enum SendVehicleToMaintenanceResponse
{
	Timeout,
	Success,
	Busy,
	GenericError,
	Unauthorized,
	TransactionError,
	InvalidCharacter,
	InvalidVehicle,
	InvalidVehicleModel,
	AlreadyInMaintenance,
	AttachedToTowtruck,
	UnableToSaveHealthData,
	UnableToDespawn,
	NoMoney,
	RentOverdue
}
