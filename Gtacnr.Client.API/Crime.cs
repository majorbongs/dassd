using System.Threading.Tasks;
using CitizenFX.Core;

namespace Gtacnr.Client.API;

public static class Crime
{
	private class InnerScript : Script
	{
		public int? currentWantedLevel;

		public int? currentBounty;

		public int? currentFine;

		public static InnerScript Instance { get; private set; }

		public InnerScript()
		{
			Instance = this;
		}

		[EventHandler("gtacnr:crimes:wantedLevelChanged")]
		private void OnWantedLevelChanged(int oldLevel, int newLevel)
		{
			currentWantedLevel = newLevel;
		}

		[EventHandler("gtacnr:crimes:bountyChanged")]
		private void OnBountyChanged(int oldAmount, int newAmount)
		{
			currentBounty = newAmount;
		}

		[EventHandler("gtacnr:crimes:fineChanged")]
		private void OnFineChanged(int oldAmount, int newAmount)
		{
			currentFine = newAmount;
		}

		[EventHandler("gtacnr:spawned")]
		private async void OnSpawned()
		{
			await GetWantedLevel(force: true);
			await GetBounty(force: true);
		}

		public async Task<int> GetWantedLevel(bool force)
		{
			if (force || !currentWantedLevel.HasValue)
			{
				currentWantedLevel = await TriggerServerEventAsync<int>("gtacnr:crimes:getWantedLevel", new object[0]);
			}
			return currentWantedLevel.Value;
		}

		public async Task<int> GetBounty(bool force)
		{
			if (force || !currentBounty.HasValue)
			{
				currentBounty = await TriggerServerEventAsync<int>("gtacnr:crimes:getBounty", new object[0]);
			}
			return currentBounty.Value;
		}

		public async Task<int> GetFine(bool force)
		{
			if (force || !currentFine.HasValue)
			{
				currentFine = await TriggerServerEventAsync<int>("gtacnr:crimes:getFine", new object[0]);
			}
			return currentFine.Value;
		}
	}

	public static int? CachedWantedLevel => InnerScript.Instance.currentWantedLevel;

	public static int? CachedBounty => InnerScript.Instance.currentBounty;

	public static int? CachedFine => InnerScript.Instance.currentFine;

	public static async Task<int> GetWantedLevel(bool force = false)
	{
		return await InnerScript.Instance.GetWantedLevel(force);
	}

	public static async Task<int> GetBounty(bool force = false)
	{
		return await InnerScript.Instance.GetBounty(force);
	}

	public static async Task<int> GetFine(bool force = false)
	{
		return await InnerScript.Instance.GetFine(force);
	}
}
