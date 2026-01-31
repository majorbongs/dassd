namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class RobberyStartedEventArgs
{
	public int PlayerId { get; set; }

	public RobberyStartedEventArgs(int playerId)
	{
		PlayerId = playerId;
	}
}
