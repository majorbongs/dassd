namespace Gtacnr.ResponseCodes;

public enum RacingSystemResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	AlreadyInRace,
	NotInRace,
	InvalidTarget,
	InvalidCharacter,
	InvalidCharacterId,
	NotEnoughMoney,
	InvalidJob,
	TransactionError,
	InvalidHost,
	RaceAlreadyStarted,
	NotInvited,
	InvalidBet,
	NoTrackSelected,
	TooFarFromFirstCheckpoint,
	TooCloseToFirstCheckpoint,
	TrackHasTooFewCheckpoints,
	LapStartFinishDistanceTooFar,
	DidntAcceptInvitation,
	AlreadyInvited
}
