namespace Gtacnr.ResponseCodes;

public enum VerifyEmailResponse
{
	Timeout,
	Success,
	GenericError,
	FakeUsername,
	EmptyUsername,
	IncorrectCode,
	ExpiredCode,
	AlreadyRegistered
}
