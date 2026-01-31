using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Model;

namespace Gtacnr.Client.Anticheat;

public class AntiEntitySpawnScript : Script
{
	private static readonly int ATTEMPTS = 3;

	private static AntiEntitySpawnScript instance;

	public AntiEntitySpawnScript()
	{
		instance = this;
	}

	public static async Task<bool> RegisterEntity(Entity entity)
	{
		int attempts = ATTEMPTS;
		while (attempts > 0)
		{
			attempts--;
			await BaseScript.Delay(500);
			if (API.NetworkGetEntityIsNetworked(((PoolObject)entity).Handle) && API.NetworkDoesNetworkIdExist(entity.NetworkId))
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:registerEntity", new object[1] { entity.NetworkId });
				instance.Print($"Registered (Net ID: {entity.NetworkId}).");
				return true;
			}
		}
		return false;
	}

	public static async Task<bool> RegisterEntities(List<Entity> entities)
	{
		int attempts = ATTEMPTS;
		while (attempts > 0)
		{
			attempts--;
			await BaseScript.Delay(500);
			if (entities.All((Entity entity) => API.NetworkGetEntityIsNetworked(((PoolObject)entity).Handle) && API.NetworkDoesNetworkIdExist(entity.NetworkId)))
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:registerEntities", new object[1] { entities.Select((Entity e) => e.NetworkId).ToList().Json() });
				instance.Print($"Registered {entities.Count} entities.");
				return true;
			}
		}
		List<int> list = (from e in entities
			where API.NetworkGetEntityIsNetworked(((PoolObject)e).Handle)
			select e.NetworkId).ToList();
		if (list.Count > 0)
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:registerEntities", new object[1] { list.Json() });
			instance.Print($"Registered {list.Count} entities.");
		}
		return false;
	}

	public static async Task<bool> RegisterEntities(params Entity[] entities)
	{
		return await RegisterEntities(new List<Entity>(entities));
	}

	[EventHandler("gtacnr:getLocalEntityInfo")]
	private void OnGetLocalEntityInfo(int token, int localHandle)
	{
		Respond(EntityInfo.FromEntity(Entity.FromHandle(localHandle)));
		void Respond(EntityInfo entityInfo)
		{
			BaseScript.TriggerServerEvent("gtacnr:getLocalEntityInfo:response", new object[2]
			{
				token,
				entityInfo.Json()
			});
		}
	}
}
