namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class PlayerJoinedEventArgs
{
	public int PlayerId { get; set; }

	public PlayerJoinedEventArgs(int playerId)
	{
		PlayerId = playerId;
	}
}
