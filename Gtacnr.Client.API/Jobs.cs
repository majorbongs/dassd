using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.Jobs;
using Gtacnr.Client.Jobs.Trucker;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;

namespace Gtacnr.Client.API;

public static class Jobs
{
	private class InnerScript : Script
	{
		public static string CurrentJob = null;

		public static JobsEnum CurrentJobEnum = JobsEnum.Invalid;

		public static InnerScript Instance { get; private set; }

		public static Dictionary<string, Job> Jobs { get; private set; } = new Dictionary<string, Job>();

		public InnerScript()
		{
			Instance = this;
		}

		[EventHandler("gtacnr:jobs:jobChanged")]
		private void OnJobChanged(string previousJob, string currentJob)
		{
			JobArgs jobArgs = new JobArgs(previousJob, currentJob);
			CurrentJob = jobArgs.CurrentJobId;
			CurrentJobEnum = jobArgs.CurrentJobEnum;
			try
			{
				Gtacnr.Client.API.Jobs.JobChangedEvent?.Invoke(this, jobArgs);
			}
			catch (Exception exception)
			{
				Print("An exception has occurred in JobChangedEvent(" + previousJob + ", " + currentJob + ")");
				Print(exception);
			}
		}

		public async Task<string> GetCurrentJobId(bool force)
		{
			if (force || CurrentJob == null)
			{
				CurrentJob = await TriggerServerEventAsync<string>("gtacnr:jobs:get", new object[0]);
				CurrentJobEnum = Gtacnr.Utils.JobMapper.JobToEnum(CurrentJob);
			}
			return CurrentJob;
		}

		public async Task<SwitchJobResponse> Switch(string job, string department = "default")
		{
			return (SwitchJobResponse)(await TriggerServerEventAsync<int>("gtacnr:jobs:switch", new object[3] { 0, job, department }));
		}
	}

	public static string CachedJob => InnerScript.CurrentJob;

	public static JobsEnum CachedJobEnum => InnerScript.CurrentJobEnum;

	public static event EventHandler<JobArgs> JobChangedEvent;

	public static async Task<string> GetCurrentJobId(bool force = false)
	{
		return await InnerScript.Instance.GetCurrentJobId(force);
	}

	public static async Task<SwitchJobResponse> Switch(string job, string department = "default")
	{
		return await InnerScript.Instance.Switch(job, department);
	}

	public static async Task<bool> TrySwitch(string job, string department = "default", string successMessage = null, Func<Task> taskBeforeSwitching = null, Func<Task> taskAfterSwitching = null)
	{
		Job jobInfo = Gtacnr.Data.Jobs.GetJobData(job);
		if (jobInfo == null)
		{
			Debug.WriteLine("^1Attempted to switch into invalid job: " + job);
			return false;
		}
		if (Game.PlayerPed.IsInVehicle())
		{
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.JOBS_CANT_CHANGE_INSIDE_VEHICLE));
			return false;
		}
		int currentWantedLevel = await Crime.GetWantedLevel();
		if (jobInfo.HasToBeInnocent && currentWantedLevel > 0)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_WANTED_LEVEL, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		}
		if (TruckerJobScript.CurrentDelivery != null)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.JOBS_CANT_CHANGE_WHILE_ON_MISSION));
			return false;
		}
		await (taskBeforeSwitching?.Invoke());
		SwitchJobResponse result = await InnerScript.Instance.Switch(job, department);
		await (taskAfterSwitching?.Invoke());
		switch (result)
		{
		case SwitchJobResponse.Success:
			if (!string.IsNullOrEmpty(successMessage))
			{
				Utils.SendNotification(successMessage);
			}
			return true;
		case SwitchJobResponse.Level:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_MIN_LEVEL, jobInfo.MinLevel, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		case SwitchJobResponse.Playtime:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_MIN_PLAYTIME, jobInfo.MinPlaytime, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		case SwitchJobResponse.Cooldown:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_SWITCH_COOLDOWN, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		case SwitchJobResponse.InvalidJob:
			Debug.WriteLine("Attempted to switch into invalid job: " + job);
			return false;
		case SwitchJobResponse.Blacklisted:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_BANNED, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		case SwitchJobResponse.WantedLevel:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_WANTED_LEVEL, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		case SwitchJobResponse.TestRequired:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_TEST_REQUIRED, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		case SwitchJobResponse.LicenseRequired:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_LICENSE_REQUIRED, Gtacnr.Data.Items.GetItemDefinition(jobInfo.RequiredItem).Name, jobInfo.GetColoredName(currentWantedLevel)));
			return false;
		case SwitchJobResponse.PlayerLimit:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_PLAYER_LIMIT, jobInfo.GetColoredName(currentWantedLevel), $"{jobInfo.MaxPlayersPercent * 100f:0}") + " " + LocalizationController.S(Entries.Jobs.JOBS_QUEUE_ADDED));
			JobQueueScript.StartQueue();
			return false;
		case SwitchJobResponse.VehicleTowed:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.VEHICLE_ATTACHED_TO_TOW_TRUCK));
			return false;
		case SwitchJobResponse.AlreadyInTheQueue:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.JOBS_QUEUE_ALREADY_IN));
			return false;
		case SwitchJobResponse.OnDuty:
			Utils.DisplayHelpText("~r~You can't change into that job when on moderation duty!");
			return false;
		default:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC2-{(int)result}"));
			return false;
		}
	}
}
