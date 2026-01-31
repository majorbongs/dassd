namespace Gtacnr.ResponseCodes;

public enum ChangePasswordResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	EmptyUsername,
	FakeUsername,
	APIError
}
