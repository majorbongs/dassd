namespace Gtacnr.Model.Enums;

public enum DropReason
{
	Unknown,
	Timeout,
	Quit,
	Crash,
	Kicked,
	KickedByPingCheck,
	KickedByAFKCheck,
	KickedBecauseOfServerError,
	Banned,
	BannedByAnticheat,
	ServerRestart
}
