using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs;

public class JobScript : Script
{
	private DateTime lastPressTime = DateTime.MinValue;

	private bool isHudShown;

	private string jobName = "";

	private Color jobColor = uint.MaxValue;

	public JobScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		try
		{
			Job jobData = Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId);
			jobName = ((e.CurrentJobId == "none") ? "" : jobData.Name);
			jobColor = Gtacnr.Utils.GetColorRGB(e.CurrentJobEnum, 0);
			Set();
			Display();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned()
	{
		LoadJob();
		Display();
	}

	private void LoadJob()
	{
		string cachedJob = Gtacnr.Client.API.Jobs.CachedJob;
		JobsEnum cachedJobEnum = Gtacnr.Client.API.Jobs.CachedJobEnum;
		Job jobData = Gtacnr.Data.Jobs.GetJobData(cachedJob);
		jobName = ((cachedJob == "none") ? "" : jobData.Name);
		jobColor = Gtacnr.Utils.GetColorRGB(cachedJobEnum, 0);
		Set();
		Display();
	}

	private void Set()
	{
		BaseScript.TriggerEvent("gtacnr:hud:setJob", new object[1] { jobName.ToLowerInvariant() });
		BaseScript.TriggerEvent("gtacnr:hud:setJobColor", new object[3] { jobColor.R, jobColor.G, jobColor.B });
	}

	private void Display()
	{
		if (!(jobName == ""))
		{
			lastPressTime = DateTime.UtcNow;
		}
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		if (API.IsControlJustPressed(2, 20))
		{
			Display();
		}
		if (!Gtacnr.Utils.CheckTimePassed(lastPressTime, 4250.0))
		{
			if (!isHudShown)
			{
				isHudShown = true;
				BaseScript.TriggerEvent("gtacnr:hud:toggleJob", new object[1] { true });
			}
		}
		else if (isHudShown)
		{
			isHudShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleJob", new object[1] { false });
		}
	}
}
