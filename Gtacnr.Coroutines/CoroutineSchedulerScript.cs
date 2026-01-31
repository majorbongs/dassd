using System;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace Gtacnr.Coroutines;

public class CoroutineSchedulerScript : BaseScript
{
	private DateTime startTime;

	public CoroutineSchedulerScript()
	{
		startTime = DateTime.UtcNow;
		Scheduler.Initialize();
		((BaseScript)this).Tick += Update;
	}

	private async Task Update()
	{
		Scheduler.CurrentTime = (TimePoint)(ulong)(DateTime.UtcNow - startTime).TotalMilliseconds;
		Scheduler.Update();
	}
}
