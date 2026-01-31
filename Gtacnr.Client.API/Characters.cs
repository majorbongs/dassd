using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Model;

namespace Gtacnr.Client.API;

public static class Characters
{
	private class InnerScript : Script
	{
		public static InnerScript Instance { get; private set; }

		public InnerScript()
		{
			Instance = this;
		}

		public async Task<bool> Create(int slot, Character character)
		{
			return await TriggerServerEventAsync<bool>("gtacnr:characters:create", new object[3]
			{
				"",
				slot,
				character.Json()
			});
		}

		public async Task<Character> Get(int slot)
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:characters:get", new object[2] { "", slot });
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Unjson<Character>();
			}
			return null;
		}

		public async Task<List<Character>> GetAll()
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:characters:get", new object[2] { "", -1 });
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Unjson<List<Character>>();
			}
			return null;
		}

		public async Task<int> Count()
		{
			return await TriggerServerEventAsync<int>("gtacnr:characters:count", new object[0]);
		}

		public new async Task<bool> Update(int slot, Character character)
		{
			return await TriggerServerEventAsync<bool>("gtacnr:characters:update", new object[3]
			{
				"",
				slot,
				character.Json()
			});
		}

		public async Task<Character> GetActiveCharacter()
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:characters:getActive", new object[0]);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Unjson<Character>();
			}
			return null;
		}

		public async Task<string> GetActiveCharacterId()
		{
			return await TriggerServerEventAsync<string>("gtacnr:characters:getActiveId", new object[0]);
		}

		public void SetActiveCharacter(int slot)
		{
			BaseScript.TriggerServerEvent("gtacnr:characters:setActive", new object[1] { slot });
		}
	}

	public static readonly int MaxSlots = 100;

	public static async Task<bool> Create(int slot, Character character)
	{
		return await InnerScript.Instance.Create(slot, character);
	}

	public static async Task<Character> Get(int slot)
	{
		return await InnerScript.Instance.Get(slot);
	}

	public static async Task<List<Character>> GetAll()
	{
		return await InnerScript.Instance.GetAll();
	}

	public static async Task<int> Count()
	{
		return await InnerScript.Instance.Count();
	}

	public static async Task<bool> Update(int slot, Character character)
	{
		return await InnerScript.Instance.Update(slot, character);
	}

	public static async Task<Character> GetActiveCharacter()
	{
		return await InnerScript.Instance.GetActiveCharacter();
	}

	public static async Task<string> GetActiveCharacterId()
	{
		return await InnerScript.Instance.GetActiveCharacterId();
	}

	public static void SetActiveCharacter(int slot)
	{
		InnerScript.Instance.SetActiveCharacter(slot);
	}
}
