namespace Gtacnr.ResponseCodes;

public class PickpocketResponse
{
	public PickpocketResponseCode Code { get; set; } = PickpocketResponseCode.GenericError;

	public bool Alerted { get; set; }

	public bool Reported { get; set; }

	public bool Aggressive { get; set; }

	public int AmountStolen { get; set; }

	public bool FakeWallet { get; set; }
}
