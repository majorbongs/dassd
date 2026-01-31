namespace Gtacnr.ResponseCodes;

public enum GarageChangeSlotResponse : byte
{
	Success,
	AlreadyInOrOut,
	InvalidPed,
	InvalidCharacter,
	InvalidJob,
	InvalidVehicle,
	InvalidProperty,
	TooFar,
	UnableToRegisterVehicle,
	UnableToStoreVehicle,
	InMaintenance,
	GenericError,
	VehicleNotFound,
	InvalidParkingSpot,
	Unauthorized,
	OperationFailed
}
