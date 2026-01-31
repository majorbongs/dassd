namespace Gtacnr.ResponseCodes;

public enum EnterExitPropertyResponse
{
	Timeout,
	Success,
	GenericError,
	InvalidPed,
	InvalidCharacter,
	InvalidProperty,
	InvalidVehicle,
	InvalidJob,
	UnableToRegisterVehicle,
	UnableToStoreVehicle,
	TooFar,
	MissingPermission,
	VehicleNotOwned,
	AlreadyInOrOut,
	OwnerOffline,
	NotInVehicle,
	NoParkingSpace,
	InMaintenance,
	NoAvailableRoutingBucket
}
