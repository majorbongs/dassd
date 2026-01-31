namespace Gtacnr.ResponseCodes;

public enum SwitchJobResponse
{
	Timeout,
	Success,
	GenericError,
	InvalidJob,
	TooFar,
	WantedLevel,
	Level,
	Playtime,
	SameJob,
	Blacklisted,
	Cooldown,
	PlayerLimit,
	TestRequired,
	InDealership,
	VehicleTowed,
	LicenseRequired,
	AlreadyInTheQueue,
	OnDuty
}
