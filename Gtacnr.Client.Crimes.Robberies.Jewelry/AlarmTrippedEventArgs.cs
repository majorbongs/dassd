namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class AlarmTrippedEventArgs
{
	public int ReasonCode { get; set; }

	public AlarmTrippedEventArgs(int reasonCode)
	{
		ReasonCode = reasonCode;
	}
}
