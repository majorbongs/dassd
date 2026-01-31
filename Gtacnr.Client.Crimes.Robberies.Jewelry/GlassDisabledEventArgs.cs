namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class GlassDisabledEventArgs
{
	public int GlassIndex { get; set; }

	public int PlayerId { get; set; }

	public bool Broken { get; set; }

	public GlassDisabledEventArgs(int glassIndex, int playerId, bool broken)
	{
		GlassIndex = glassIndex;
		PlayerId = playerId;
		Broken = broken;
	}
}
