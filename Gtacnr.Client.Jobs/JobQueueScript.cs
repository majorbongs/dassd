using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Jobs;

public class JobQueueScript : Script
{
	private static Vector3 queueStartPos;

	private static Blip? queueAreaBlip;

	private static Blip? queueBlip;

	private static JobQueueScript instance;

	public JobQueueScript()
	{
		instance = this;
		DeathScript.Respawning += OnRspawning;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnRspawning(object sender, EventArgs e)
	{
		QueueCleanup();
	}

	[EventHandler("gtacnr:jobQueue:receivePosition")]
	private void OnPositionReceived(int position, int count, string job)
	{
		Job jobData = Gtacnr.Data.Jobs.GetJobData(job);
		if (jobData != null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_QUEUE_POSITION, position, count, jobData.GetColoredName()));
		}
	}

	[EventHandler("gtacnr:jobQueue:switch")]
	private void OnJobSwitched(string job)
	{
		Job jobData = Gtacnr.Data.Jobs.GetJobData(job);
		if (jobData != null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_QUEUE_FINISHED, jobData.GetColoredName()));
		}
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		QueueCleanup();
	}

	private void CancelQueue()
	{
		BaseScript.TriggerServerEvent("gtacnr:jobQueue:cancel", new object[0]);
		QueueCleanup();
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_QUEUE_LEFT));
	}

	public static void StartQueue()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (!((Entity)(object)Game.PlayerPed == (Entity)null))
		{
			queueStartPos = ((Entity)Game.PlayerPed).Position;
			if (queueAreaBlip != (Blip)null)
			{
				instance.QueueCleanup();
			}
			queueAreaBlip = World.CreateBlip(queueStartPos, 20f);
			queueAreaBlip.IsShortRange = false;
			queueAreaBlip.Sprite = (BlipSprite)(-1);
			queueAreaBlip.Color = (BlipColor)7;
			queueAreaBlip.Alpha = 64;
			Utils.SetBlipName(queueAreaBlip, "Job Queue Area", "jobQueueArea");
			API.SetBlipDisplay(((PoolObject)queueAreaBlip).Handle, 8);
			queueBlip = World.CreateBlip(queueStartPos);
			queueBlip.IsShortRange = false;
			queueBlip.Sprite = (BlipSprite)464;
			queueBlip.Color = (BlipColor)7;
			queueBlip.Alpha = 255;
			Utils.SetBlipName(queueBlip, "Job Queue", "jobQueue");
			API.SetBlipDisplay(((PoolObject)queueBlip).Handle, 3);
			instance.AttachTask();
		}
	}

	private void QueueCleanup()
	{
		Blip? obj = queueAreaBlip;
		if (obj != null)
		{
			((PoolObject)obj).Delete();
		}
		Blip? obj2 = queueBlip;
		if (obj2 != null)
		{
			((PoolObject)obj2).Delete();
		}
		base.Update -= CheckDistanceTask;
	}

	public void AttachTask()
	{
		base.Update += CheckDistanceTask;
	}

	private async Coroutine CheckDistanceTask()
	{
		await Script.Wait(1000);
		if (!((Entity)(object)Game.PlayerPed == (Entity)null) && ((Vector3)(ref queueStartPos)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > (float)20.Square())
		{
			CancelQueue();
		}
	}
}
