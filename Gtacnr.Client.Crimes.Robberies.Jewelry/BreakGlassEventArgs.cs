namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class BreakGlassEventArgs
{
	public int GlassIndex { get; set; }

	public BreakGlassEventArgs(int glassIndex)
	{
		GlassIndex = glassIndex;
	}
}
