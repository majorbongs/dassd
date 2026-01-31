namespace Gtacnr.Client.Events.Holidays.AprilsFools;

public class AprilsFoolsScript : Script
{
	public static bool IsAprilsFools { get; private set; }

	protected override async void OnStarted()
	{
		IsAprilsFools = await TriggerServerEventAsync<bool>("gtacnr:christmas:isAprilsFools", new object[0]);
	}
}
