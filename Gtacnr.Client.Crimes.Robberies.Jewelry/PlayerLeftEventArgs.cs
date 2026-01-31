namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class PlayerLeftEventArgs
{
	public int PlayerId { get; set; }

	public int ReasonCode { get; set; }

	public PlayerLeftEventArgs(int playerId, int reasonCode)
	{
		PlayerId = playerId;
		ReasonCode = reasonCode;
	}
}
