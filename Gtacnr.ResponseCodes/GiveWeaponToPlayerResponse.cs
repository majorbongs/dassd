namespace Gtacnr.ResponseCodes;

public enum GiveWeaponToPlayerResponse
{
	Timeout,
	Success,
	GenericError,
	InvalidPlayer,
	InvalidWeapon,
	AlreadyHave,
	InsufficientLevel,
	PremiumRequired,
	NotAllowedToGiveThisWeapon
}
