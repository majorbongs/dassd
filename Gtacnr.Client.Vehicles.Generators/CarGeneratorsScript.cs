using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;

namespace Gtacnr.Client.Vehicles.Generators;

public class CarGeneratorsScript : Script
{
	private readonly int RESPAWN_COOLDOWN = 30000;

	private List<CarGeneratorGroup> generatorGroups = new List<CarGeneratorGroup>();

	private CarGeneratorGroup closestGeneratorGroup;

	private Random random = new Random();

	protected override void OnStarted()
	{
		List<string> list = Gtacnr.Utils.LoadCurrentResourceFile("data/cargens/files.json").Unjson<List<string>>();
		generatorGroups.Clear();
		foreach (string item in list)
		{
			try
			{
				CarGeneratorGroup carGeneratorGroup = Gtacnr.Utils.LoadCurrentResourceFile("data/cargens/" + item).Unjson<CarGeneratorGroup>();
				Print($"Loaded vehicle generator group: {carGeneratorGroup.Name} ({carGeneratorGroup.Generators.Count} vehicles)");
				generatorGroups.Add(carGeneratorGroup);
			}
			catch
			{
				Print("Unable to load " + item);
			}
		}
	}

	[Update]
	private async Coroutine FindClosestGeneratorGroupTask()
	{
		if ((Entity)(object)Game.PlayerPed == (Entity)null || !SpawnScript.HasSpawned)
		{
			return;
		}
		await Script.Wait(10000);
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 100000000f;
		closestGeneratorGroup = null;
		foreach (CarGeneratorGroup generatorGroup in generatorGroups)
		{
			float num2 = ((Vector3)(ref position)).DistanceToSquared(generatorGroup.Position);
			if (num2 < num && num2 < generatorGroup.Range * generatorGroup.Range)
			{
				num = num2;
				closestGeneratorGroup = generatorGroup;
			}
		}
	}

	[Update]
	private async Coroutine RemoveDefaultGeneratorsTick()
	{
		CarGeneratorGroup carGeneratorGroup = closestGeneratorGroup;
		if (!((Entity)(object)Game.PlayerPed == (Entity)null) && SpawnScript.HasSpawned && carGeneratorGroup != null && carGeneratorGroup.Generators.Count != 0)
		{
			Tuple<Vector2, Vector2> boundaries = carGeneratorGroup.GetBoundaries();
			API.RemoveVehiclesFromGeneratorsInArea(boundaries.Item1.X, boundaries.Item1.Y, carGeneratorGroup.Position.Z - 5f, boundaries.Item2.X, boundaries.Item2.Y, carGeneratorGroup.Position.Z + 5f, 0);
			await Script.Wait(100);
		}
	}

	[Update]
	private async Coroutine GenerateCarsTask()
	{
		if ((Entity)(object)Game.PlayerPed == (Entity)null || !SpawnScript.HasSpawned)
		{
			return;
		}
		await Script.Wait(Gtacnr.Utils.GetRandomInt(2500, 10000));
		if (closestGeneratorGroup == null)
		{
			return;
		}
		CarGeneratorGroup generatorGroup = closestGeneratorGroup;
		List<Player> list = new List<Player>();
		foreach (Player player in ((BaseScript)this).Players)
		{
			Vector3 position = ((Entity)player.Character).Position;
			if (((Vector3)(ref position)).DistanceToSquared(generatorGroup.Position) < generatorGroup.Range * generatorGroup.Range)
			{
				list.Add(player);
			}
		}
		list = list.OrderBy((Player p) => p.ServerId).ToList();
		if (list.Count == 0 || list.First().ServerId != Game.Player.ServerId)
		{
			return;
		}
		int i = 0;
		List<Vehicle> createdVehicles = new List<Vehicle>();
		foreach (CarGenerator cargen in generatorGroup.Generators.ToList())
		{
			i++;
			await Script.Yield();
			try
			{
				if (!Gtacnr.Utils.CheckTimePassed(cargen.LastSpawnTimestamp, RESPAWN_COOLDOWN))
				{
					continue;
				}
				int r = random.Next(cargen.ModelHashes.Count);
				uint modelHash = cargen.ModelHashes[r];
				try
				{
					using DisposableModel carModel = new DisposableModel(modelHash);
					await carModel.Load();
					Vector3 val = default(Vector3);
					Vector3 val2 = default(Vector3);
					API.GetModelDimensions(modelHash, ref val, ref val2);
					float num = (val2 - val).Y / 3f;
					if (API.IsPositionOccupied(cargen.Position.X, cargen.Position.Y, cargen.Position.Z, num, false, true, true, false, false, 0, false))
					{
						goto end_IL_032b;
					}
					if (generatorGroup.Generators.Count < 5 || random.NextDouble() >= 0.20000000298023224)
					{
						Vehicle val3 = new Vehicle(API.CreateVehicle(modelHash, cargen.Position.X, cargen.Position.Y, cargen.Position.Z, cargen.Position.W, true, true));
						createdVehicles.Add(val3);
						API.SetVehicleOnGroundProperly(((PoolObject)val3).Handle);
						if (cargen.ModelLiveries != null && cargen.ModelLiveries.Count > 0)
						{
							List<int> list2 = cargen.ModelLiveries[r];
							if (list2.Count > 0)
							{
								int num2 = list2[random.Next(list2.Count)];
								API.SetVehicleLivery(((PoolObject)val3).Handle, num2);
							}
						}
						API.NetworkFadeInEntity(((PoolObject)val3).Handle, true);
						int handle = ((PoolObject)val3).Handle;
						API.SetEntityAsNoLongerNeeded(ref handle);
					}
					cargen.LastSpawnTimestamp = DateTime.UtcNow;
					await Script.Wait(10);
					goto end_IL_0250;
					end_IL_032b:;
				}
				catch (ArgumentException)
				{
					generatorGroup.Generators.Remove(cargen);
				}
				end_IL_0250:;
			}
			catch (Exception exception)
			{
				Print($"Warning: an exception has occurred while generating car in group {generatorGroup.Name}, cargen #{i}.");
				Print(exception);
			}
		}
		if (createdVehicles.Count > 0)
		{
			await AntiEntitySpawnScript.RegisterEntities(createdVehicles.Cast<Entity>().ToList());
		}
	}
}
