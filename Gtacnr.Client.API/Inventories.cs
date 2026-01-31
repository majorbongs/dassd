using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;

namespace Gtacnr.Client.API;

public static class Inventories
{
	private class InnerScript : Script
	{
		public static InnerScript Instance { get; private set; }

		public InnerScript()
		{
			Instance = this;
		}

		public async Task<List<InventoryEntry>> GetPrimaryInventory()
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:inventories:getPrimary", new object[0]);
			if (string.IsNullOrEmpty(text))
			{
				return new List<InventoryEntry>();
			}
			return text.Unjson<List<InventoryEntry>>();
		}

		public async Task<List<InventoryEntry>> GetJobInventory(string job)
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:inventories:getSecondaryInventory", new object[2] { 3, job });
			if (string.IsNullOrEmpty(text))
			{
				return new List<InventoryEntry>();
			}
			return text.Unjson<List<InventoryEntry>>();
		}

		public async Task<List<InventoryEntry>> GetStorageInventory(string storageId)
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:inventories:getSecondaryInventory", new object[2] { 4, storageId });
			if (string.IsNullOrEmpty(text))
			{
				return new List<InventoryEntry>();
			}
			return text.Unjson<List<InventoryEntry>>();
		}

		public async Task<InventoryEntry> GetEntry(string itemId)
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:inventories:getEntry", new object[1] { itemId });
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			return text.Unjson<InventoryEntry>();
		}

		public async Task<UseItemResponse> UseItem(string item, float amount, bool triggerClientEvents = true)
		{
			if (triggerClientEvents)
			{
				BaseScript.TriggerEvent("gtacnr:inventories:usingItem", new object[2]
				{
					item,
					Math.Abs(amount)
				});
				if (API.WasEventCanceled())
				{
					return UseItemResponse.ClientScriptCanceled;
				}
			}
			UseItemResponse useItemResponse = (UseItemResponse)(await TriggerServerEventAsync<string>("gtacnr:inventories:useItem", new object[2] { item, amount })).Unjson<int>();
			if (useItemResponse == UseItemResponse.Success)
			{
				BaseScript.TriggerEvent("gtacnr:inventories:usedItem", new object[2]
				{
					item,
					Math.Abs(amount)
				});
			}
			else
			{
				BaseScript.TriggerEvent("gtacnr:inventories:itemUseFailed", new object[3]
				{
					item,
					Math.Abs(amount),
					(int)useItemResponse
				});
			}
			return useItemResponse;
		}

		public async Task<GiveItemResponse> GiveItem(int playerId, string item, float amount)
		{
			BaseScript.TriggerEvent("gtacnr:inventories:givingItem", new object[3]
			{
				playerId,
				item,
				Math.Abs(amount)
			});
			if (API.WasEventCanceled())
			{
				return GiveItemResponse.ClientScriptCanceled;
			}
			GiveItemResponse giveItemResponse = (GiveItemResponse)(await TriggerServerEventAsync<string>("gtacnr:inventories:giveItem", new object[3] { playerId, item, amount })).Unjson<int>();
			if (giveItemResponse == GiveItemResponse.Success)
			{
				BaseScript.TriggerEvent("gtacnr:inventories:gaveItem", new object[3]
				{
					playerId,
					item,
					Math.Abs(amount)
				});
			}
			return giveItemResponse;
		}
	}

	public static async Task<List<InventoryEntry>> GetPrimaryInventory()
	{
		return await InnerScript.Instance.GetPrimaryInventory();
	}

	public static async Task<List<InventoryEntry>> GetJobInventory(string job)
	{
		return await InnerScript.Instance.GetJobInventory(job);
	}

	public static async Task<List<InventoryEntry>> GetStorageInventory(string storageId)
	{
		return await InnerScript.Instance.GetStorageInventory(storageId);
	}

	public static async Task<InventoryEntry> GetEntry(string itemId)
	{
		return await InnerScript.Instance.GetEntry(itemId);
	}

	public static async Task<UseItemResponse> UseItem(string item, float amount = 1f, bool triggerClientEvents = true)
	{
		return await InnerScript.Instance.UseItem(item, amount, triggerClientEvents);
	}

	public static async Task<GiveItemResponse> GiveItem(Player player, string item, float amount = 1f)
	{
		return await InnerScript.Instance.GiveItem(player.ServerId, item, amount);
	}
}
