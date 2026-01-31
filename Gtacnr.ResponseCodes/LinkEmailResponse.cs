namespace Gtacnr.ResponseCodes;

public enum LinkEmailResponse
{
	Timeout,
	Success,
	GenericError,
	InvalidEmail,
	Busy,
	Cooldown,
	EmptyUsername,
	FakeUsername,
	APIError
}
