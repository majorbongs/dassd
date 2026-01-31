using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace Gtacnr.Client.API;

public static class Money
{
	private class InnerScript : Script
	{
		public Dictionary<string, long> cachedBalance = new Dictionary<string, long>();

		public static InnerScript Instance { get; private set; }

		public InnerScript()
		{
			Instance = this;
		}

		public async Task<long> GetBalance(string account)
		{
			long num = await TriggerServerEventAsync<long>("gtacnr:money:getBalance", new object[1] { account });
			if (num < 0)
			{
				return 0L;
			}
			cachedBalance[account] = num;
			return cachedBalance[account];
		}

		[EventHandler("gtacnr:money:moneyUpdated")]
		private void OnMoneyUpdated(string account, long oldBalance, long newBalance)
		{
			cachedBalance[account] = newBalance;
		}
	}

	public static async Task<long> GetBalance(string account)
	{
		return await InnerScript.Instance.GetBalance(account);
	}

	public static long GetCachedBalance(string account)
	{
		if (!InnerScript.Instance.cachedBalance.ContainsKey(account))
		{
			return -1L;
		}
		return InnerScript.Instance.cachedBalance[account];
	}

	public static async Task<long> GetCachedBalanceOrFetch(string account)
	{
		return (!InnerScript.Instance.cachedBalance.ContainsKey(account)) ? (await GetBalance(account)) : InnerScript.Instance.cachedBalance[account];
	}
}
