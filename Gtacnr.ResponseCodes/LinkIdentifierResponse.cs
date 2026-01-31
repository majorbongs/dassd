namespace Gtacnr.ResponseCodes;

public enum LinkIdentifierResponse
{
	Timeout,
	Success,
	GenericError,
	Busy,
	InvalidIdentifierType,
	EmptyIdentifier,
	EmptyUsername,
	AlreadyLinked,
	UnableToUnlink,
	UnableToLink,
	FakeUsername
}
