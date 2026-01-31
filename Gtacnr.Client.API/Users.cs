using System;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace Gtacnr.Client.API;

public static class Users
{
	public class XPEventArgs
	{
		public int OldXP { get; set; }

		public int NewXP { get; set; }

		public int Amount { get; set; }

		public int Bonus { get; set; }

		public XPEventArgs(int oldXP, int newXP, int amount, int bonus)
		{
			OldXP = oldXP;
			NewXP = newXP;
			Amount = amount;
			Bonus = bonus;
		}
	}

	private class InnerScript : Script
	{
		public int? xpCache;

		public static InnerScript Instance { get; private set; }

		public InnerScript()
		{
			Instance = this;
		}

		protected override async void OnStarted()
		{
			await GetXP(force: true);
		}

		public async Task<int> GetXP(bool force = false)
		{
			if (!xpCache.HasValue || force)
			{
				xpCache = await TriggerServerEventAsync<int>("gtacnr:users:getXP", new object[0]);
			}
			return xpCache.Value;
		}

		[EventHandler("gtacnr:users:XPChanged")]
		private void OnXPChanged(int oldXP, int newXP, int amount, int bonus)
		{
			xpCache = newXP;
			XPEventArgs e = new XPEventArgs(oldXP, newXP, amount, bonus);
			Users.XPChanged?.Invoke(this, e);
		}
	}

	public static int CachedXP => InnerScript.Instance.xpCache ?? (-1);

	public static event EventHandler<XPEventArgs>? XPChanged;

	public static async Task<int> GetXP(bool force = false)
	{
		return await InnerScript.Instance.GetXP(force);
	}
}
