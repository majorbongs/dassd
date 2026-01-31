using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Communication;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Inventory;
using Gtacnr.Data;
using Gtacnr.Model;

namespace Gtacnr.Client.Characters.Lifecycle;

public class SpawnScript : Script
{
	private readonly Random random = new Random();

	private static List<SpawnLocation> spawnLocations = (from l in Gtacnr.Utils.LoadJson<List<SpawnLocation>>("data/spawns.json")
		where l.HasRequiredResource()
		select l).ToList();

	public static bool HasSpawned { get; set; } = false;

	public static IEnumerable<SpawnLocation> SpawnLocations => spawnLocations;

	public static SpawnLocation GetSpawnLocationAtCoords(Vector3 pos)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		SpawnLocation result = null;
		foreach (SpawnLocation spawnLocation in spawnLocations)
		{
			if (((Vector3)(ref pos)).DistanceToSquared(spawnLocation.Position) < 9f)
			{
				result = spawnLocation;
				break;
			}
		}
		return result;
	}

	protected override void OnStarted()
	{
		Print($"Loaded {spawnLocations.Count()} spawn locations.");
		BaseScript.TriggerServerEvent("gtacnr:joined", new object[0]);
	}

	[EventHandler("gtacnr:spawn")]
	private async void OnRequestSpawn(Vector4 coordinates, bool reloadLoadout = false)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		LoadingPrompt.Show("Spawning", (LoadingSpinnerType)5);
		if (!(await TriggerServerEventAsync<bool>("gtacnr:spawnRequested", new object[1] { coordinates })))
		{
			Print("The server has canceled spawning.");
			LoadingPrompt.Hide();
		}
		else if (!HasSpawned)
		{
			BaseScript.TriggerEvent("gtacnr:spawning", new object[1] { coordinates });
			BaseScript.TriggerServerEvent("gtacnr:spawning", new object[1] { coordinates });
			await Spawn(coordinates);
			HasSpawned = true;
			DeathScript.IsAlive = true;
			BaseScript.TriggerEvent("gtacnr:spawned", new object[1] { coordinates });
			BaseScript.TriggerServerEvent("gtacnr:spawned", new object[1] { coordinates });
			if (reloadLoadout)
			{
				await ArmoryScript.ReloadLoadout();
			}
		}
	}

	private async Task Spawn(Vector4 coords)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		Utils.Unfreeze();
		Chat.Clear();
		if (!API.IsScreenFadedOut())
		{
			await Utils.FadeOut(2000);
		}
		if (coords == default(Vector4))
		{
			int attempts = 1;
			string cachedJob;
			while (true)
			{
				if (attempts > 10)
				{
					BaseScript.TriggerServerEvent("gtacnr:unableToSpawn", new object[0]);
					return;
				}
				await BaseScript.Delay(2000);
				cachedJob = Gtacnr.Client.API.Jobs.CachedJob;
				if (Gtacnr.Data.Jobs.GetJobData(cachedJob) != null)
				{
					break;
				}
				Print($"Loading job data... (attempt {attempts}/10)...");
				attempts++;
			}
			Job jobData = Gtacnr.Data.Jobs.GetJobData(cachedJob);
			Print("Current Job: " + jobData.Name);
			string spawnJob = jobData.Id;
			if (!jobData.SeparateSpawnLocations)
			{
				spawnJob = "none";
			}
			List<SpawnLocation> list = spawnLocations.Where((SpawnLocation spw) => spw.Job == spawnJob).ToList();
			SpawnLocation spawnLocation = list[random.Next(list.Count)];
			coords = new Vector4(spawnLocation.Position.X, spawnLocation.Position.Y, spawnLocation.Position.Z, spawnLocation.Heading);
			try
			{
				if (spawnLocation.Radio > 0f)
				{
					RadioScript.SetChannel(spawnLocation.Radio);
				}
			}
			catch (Exception exception)
			{
				Print($"Unable to set the radio to {spawnLocation.Radio}.");
				Print(exception);
			}
		}
		string locationName = Utils.GetLocationName(coords.XYZ());
		Print($"Spawning at: {locationName} ({coords.X}, {coords.Y}, {coords.Z})");
		((Entity)Game.PlayerPed).Heading = coords.W;
		API.SetGameplayCamRelativePitch(0f, 1f);
		API.SetGameplayCamRelativeHeading(0f);
		API.SetFocusPosAndVel(coords.X, coords.Y, coords.Z, 0f, 0f, 0f);
		API.StopAllAlarms(true);
		await Utils.TeleportToCoords(coords.XYZ(), coords.W, Utils.TeleportFlags.PlaceOnGround);
		Utils.Freeze();
		API.DisplayHud(false);
		API.DisplayRadar(false);
		HideHUDScript.ToggleChat(toggle: false, showMessage: false);
		LoadingPrompt.Hide();
		API.ShutdownLoadingScreen();
		API.ShutdownLoadingScreenNui();
		if (API.IsScreenFadedOut())
		{
			await Utils.FadeIn(2000);
		}
		API.SetEntityInvincible(((PoolObject)Game.PlayerPed).Handle, false);
		API.SetEntityCollision(((PoolObject)Game.PlayerPed).Handle, true, true);
		API.SetEntityVisible(((PoolObject)Game.PlayerPed).Handle, true, false);
		API.SetLocalPlayerVisibleLocally(true);
		API.ResetEntityAlpha(((PoolObject)Game.PlayerPed).Handle);
		API.NetworkResurrectLocalPlayer(coords.X, coords.Y, coords.Z, coords.W, true, true);
		API.ResurrectPed(((PoolObject)Game.PlayerPed).Handle);
		if (Utils.IsSwitchInProgress())
		{
			Task task = Utils.SwitchIn();
			await BaseScript.Delay(1000);
			SetPos();
			await task;
		}
		API.DisplayHud(true);
		API.DisplayRadar(true);
		HideHUDScript.ToggleChat(toggle: true, showMessage: false);
		API.SetFocusEntity(((PoolObject)Game.PlayerPed).Handle);
		Utils.Unfreeze();
		if (API.IsPedFalling(((PoolObject)Game.PlayerPed).Handle) || API.IsPedInParachuteFreeFall(((PoolObject)Game.PlayerPed).Handle))
		{
			SetPos();
		}
		void SetPos()
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(coords.X, coords.Y, coords.Z + 0.5f, ref z, false))
			{
				coords.Z = z;
				AntiTeleportScript.JustTeleported();
				((Entity)Game.PlayerPed).Position = coords.XYZ();
				((Entity)Game.PlayerPed).Heading = coords.W;
			}
		}
	}

	protected override async void OnStopping()
	{
		await Utils.FadeOut(100);
	}
}
