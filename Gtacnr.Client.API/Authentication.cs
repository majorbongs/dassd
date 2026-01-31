using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.API;

public static class Authentication
{
	private class InnerScript : Script
	{
		private static Dictionary<int, string> accountIds = new Dictionary<int, string>();

		private static Dictionary<int, string> accountNames = new Dictionary<int, string>();

		public static InnerScript Instance { get; private set; }

		public InnerScript()
		{
			Instance = this;
		}

		[EventHandler("gtacnr:auth:accountIdChanged")]
		private void OnAccountIdChanged(int playerid, string oldId, string newId)
		{
			accountIds[playerid] = newId;
		}

		[EventHandler("gtacnr:auth:accountNameChanged")]
		private void OnAccountNameChanged(int playerid, string oldName, string newName)
		{
			accountNames[playerid] = newName;
		}

		public async Task<string> GetAccountId(int playerid)
		{
			if (!accountIds.ContainsKey(playerid))
			{
				Dictionary<int, string> dictionary = accountIds;
				dictionary[playerid] = await TriggerServerEventAsync<string>("gtacnr:auth:getAccountId", new object[1] { playerid });
			}
			return accountIds[playerid];
		}

		public async Task<string> GetAccountName(int playerid)
		{
			if (!accountNames.ContainsKey(playerid))
			{
				Dictionary<int, string> dictionary = accountNames;
				dictionary[playerid] = await TriggerServerEventAsync<string>("gtacnr:auth:getAccountName", new object[1] { playerid });
			}
			return accountNames[playerid];
		}
	}

	public static async Task<string> GetAccountName()
	{
		return await InnerScript.Instance.GetAccountName(API.GetPlayerServerId(API.PlayerId()));
	}

	public static async Task<string> GetAccountName(int playerid)
	{
		return await InnerScript.Instance.GetAccountName(playerid);
	}

	public static async Task<string> GetAccountName(Player player)
	{
		return await InnerScript.Instance.GetAccountName(player.ServerId);
	}

	public static async Task<string> GetAccountId()
	{
		return await InnerScript.Instance.GetAccountId(API.GetPlayerServerId(API.PlayerId()));
	}

	public static async Task<string> GetAccountId(int playerid)
	{
		return await InnerScript.Instance.GetAccountId(playerid);
	}

	public static async Task<string> GetAccountId(Player player)
	{
		return await InnerScript.Instance.GetAccountId(player.ServerId);
	}
}
