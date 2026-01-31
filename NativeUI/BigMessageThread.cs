using CitizenFX.Core;
using Gtacnr.Client;

namespace NativeUI;

public class BigMessageThread : Script
{
	public static BigMessageHandler MessageInstance { get; set; }

	public BigMessageThread()
	{
		MessageInstance = new BigMessageHandler();
		base.Update += BigMessageThread_Tick;
	}

	private async Coroutine BigMessageThread_Tick()
	{
		MessageInstance.Update();
	}
}
