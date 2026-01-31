using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model.SpecialEvents.Halloween;
using Gtacnr.ResponseCodes;

namespace Gtacnr.Client.Events.Holidays.Halloween;

public class PumpkinScript : Script
{
	private readonly int pumpkinModel = API.GetHashKey("prop_veg_crop_03_pump");

	private readonly Dictionary<string, Pumpkin> pumpkins = Gtacnr.Utils.LoadJson<List<Pumpkin>>("data/specialEvents/halloween/pumpkins.json").ToDictionary((Pumpkin x) => x.Id, (Pumpkin x) => x);

	private readonly List<Monster> monsters = Gtacnr.Utils.LoadJson<List<Monster>>("data/specialEvents/halloween/monsters.json");

	private HashSet<Pumpkin> foundPumpkins = new HashSet<Pumpkin>();

	private HashSet<Pumpkin> streamedInPumpkins = new HashSet<Pumpkin>();

	private Pumpkin currentPumpkin;

	private bool isCollectingPumpkin;

	private bool instructionsShown;

	[EventHandler("gtacnr:halloween:initialize")]
	private void OnHalloweenInitialize()
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		base.Update += PumpkinDistanceTask;
		base.Update += ControlsTask;
		Chat.AddSuggestion("pumpkins", "Tells you how many pumpkins you've found.");
		API.RegisterCommand("pumpkins", InputArgument.op_Implicit((Delegate)(Action)delegate
		{
			ShowPumpkinsCount();
		}), false);
		foreach (Monster monster in monsters)
		{
			monster.ClosestPumpkin = null;
			float num = 2500f;
			foreach (Pumpkin value in pumpkins.Values)
			{
				Vector3 position = monster.Position;
				if (((Vector3)(ref position)).DistanceToSquared(value.Position) < num)
				{
					monster.ClosestPumpkin = value;
				}
			}
			if (monster.ClosestPumpkin == null)
			{
				Print("Warning: monster " + monster.Model + " has no closest pumpkin.");
			}
		}
		Chat.AddSuggestion("/pumpkins", "Shows how many pumpkins you have collected and how many are left.");
	}

	private void ShowPumpkinsCount(bool showNearby = false)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Vector3 pPos = ((Entity)Game.PlayerPed).Position;
		int num = streamedInPumpkins.Count(delegate(Pumpkin p)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = p.Position;
			return ((Vector3)(ref position)).DistanceToSquared(pPos) < 10000f;
		});
		string arg = $" There are {num} ~o~pumpkins ~s~nearby.";
		if (!showNearby)
		{
			arg = "";
		}
		Utils.SendNotification($"You've found {foundPumpkins.Count} ~o~pumpkins ~s~out of {pumpkins.Count}.{arg}");
	}

	[EventHandler("gtacnr:halloween:pumpkinHint")]
	private void OnPumpkinHint()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		List<Pumpkin> list = pumpkins.Values.Except<Pumpkin>(foundPumpkins).ToList();
		if (list.Count > 0)
		{
			int randomInt = Gtacnr.Utils.GetRandomInt(0, list.Count);
			Pumpkin pumpkin = list[randomInt];
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector(pumpkin.Position.X + (float)Gtacnr.Utils.GetRandomDouble(-10.0, 10.0), pumpkin.Position.Y + (float)Gtacnr.Utils.GetRandomDouble(-10.0, 10.0), pumpkin.Position.Z);
			API.SetBlipRouteColour(((PoolObject)GPSScript.SetDestination("Pumpkin Hint", val, 50f, shortRange: false, null, (BlipColor)17, 128, autoDelete: true)).Handle, 17);
			string locationName = Utils.GetLocationName(val);
			string text = "";
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null)
			{
				text = " Get in a ~b~vehicle ~s~to see the GPS route.";
			}
			Utils.DisplaySubtitle("There's a ~o~pumpkin somewhere in ~y~" + locationName + "~s~." + text);
		}
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		if (HalloweenScript.IsHalloween)
		{
			foundPumpkins = new HashSet<Pumpkin>(from s in (await TriggerServerEventAsync<string>("gtacnr:halloween:getFoundPumpkins", new object[0])).Unjson<List<string>>()
				select pumpkins[s]);
			await BaseScript.Delay(3000);
			Utils.DisplayHelpText("Find all the ~o~Pumpkins ~s~before the end of the ~o~event ~s~to win a ~g~big prize~s~.");
			ShowPumpkinsCount(showNearby: true);
			base.Update += StreamPumpkinsTask;
		}
	}

	private async Coroutine StreamPumpkinsTask()
	{
		Vector3 plPos = ((Entity)Game.PlayerPed).Position;
		foreach (Pumpkin item in pumpkins.Values.Except<Pumpkin>(foundPumpkins))
		{
			Vector3 position = item.Position;
			if (plPos.X < position.X + 200f && plPos.X > position.X - 200f && plPos.Y < position.Y + 200f && plPos.Y > position.Y - 200f && plPos.Z < position.Z + 200f && plPos.Z > position.Z - 200f)
			{
				await CreatePumpkin(item);
			}
			else
			{
				DestroyPumpkin(item);
			}
		}
		await BaseScript.Delay(5000);
	}

	private async Coroutine PumpkinDistanceTask()
	{
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		currentPumpkin = null;
		foreach (Pumpkin streamedInPumpkin in streamedInPumpkins)
		{
			Vector3 position2 = streamedInPumpkin.Position;
			if (((Vector3)(ref position2)).DistanceToSquared(position) < 2.25f)
			{
				currentPumpkin = streamedInPumpkin;
				break;
			}
		}
		if (currentPumpkin == null || isCollectingPumpkin)
		{
			DisableInstructionalButtons();
		}
		else
		{
			EnableInstructionalButtons();
		}
		await Script.Wait(100);
		void DisableInstructionalButtons()
		{
			if (instructionsShown)
			{
				instructionsShown = false;
				Utils.RemoveInstructionalButton("collectPumpkin");
			}
		}
		void EnableInstructionalButtons()
		{
			if (!instructionsShown)
			{
				instructionsShown = true;
				Utils.AddInstructionalButton("collectPumpkin", new InstructionalButton("Collect", 2, (Control)51));
			}
		}
	}

	private async void CollectPumpkin(Pumpkin pumpkin)
	{
		if (isCollectingPumpkin)
		{
			return;
		}
		isCollectingPumpkin = true;
		try
		{
			CollectCollectibleResponse collectCollectibleResponse = (CollectCollectibleResponse)(await TriggerServerEventAsync<int>("gtacnr:halloween:collectPumpkin", new object[1] { pumpkin.Id }));
			switch (collectCollectibleResponse)
			{
			case CollectCollectibleResponse.Success:
				Game.PlayerPed.Task.PlayAnimation("random@domestic", "pickup_low", 4f, 2000, (AnimationFlags)51);
				ClearAnim();
				Game.PlaySound("ROBBERY_MONEY_TOTAL", "HUD_FRONTEND_CUSTOM_SOUNDSET");
				DestroyPumpkin(pumpkin);
				foundPumpkins.Add(pumpkin);
				ShowPumpkinsCount(showNearby: true);
				foreach (Monster monster in monsters)
				{
					if (monster.ClosestPumpkin == pumpkin)
					{
						SpawnMonster(monster);
					}
				}
				pumpkin = null;
				break;
			case CollectCollectibleResponse.AlreadyCollected:
				Utils.DisplayHelpText("~r~You've already collected this pumpkin (" + pumpkin.Id + ").");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x89-{(int)collectCollectibleResponse}, {pumpkin.Id}"));
				break;
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		isCollectingPumpkin = false;
		static async void ClearAnim()
		{
			Game.PlayerPed.Task.ClearAnimation("random@domestic", "pickup_low");
		}
	}

	private async void SpawnMonster(Monster monster)
	{
		AudioScript.PlayAudio("impact.wav");
		API.SetArtificialLightsState(true);
		API.StartAudioScene("AGENCY_H_2_USE_ELEVATOR");
		await BaseScript.Delay(2000);
		AudioScript.PlayAudio("scary_laugh.wav");
		await BaseScript.Delay(2000);
		Ped ped = await Utils.CreateLocalPed(Model.op_Implicit(monster.Model), monster.Position, monster.Heading);
		if (!((Entity)(object)ped != (Entity)null))
		{
			return;
		}
		((Entity)ped).Health = 3000;
		API.GiveWeaponToPed(((PoolObject)ped).Handle, (uint)API.GetHashKey("weapon_dagger"), 1, false, true);
		API.TaskCombatPed(((PoolObject)ped).Handle, ((PoolObject)Game.PlayerPed).Handle, 0, 16);
		API.StopPedSpeaking(((PoolObject)ped).Handle, true);
		API.DisablePedPainAudio(((PoolObject)ped).Handle, true);
		API.SetAmbientVoiceName(((PoolObject)ped).Handle, "kerry");
		Func<Coroutine> task = null;
		task = async delegate
		{
			await Script.Wait(1000);
			if ((Entity)(object)ped == (Entity)null || ((Entity)ped).IsDead)
			{
				AudioScript.PlayAudio("impact.wav");
				Delete();
			}
			else
			{
				Vector3 position = ((Entity)ped).Position;
				if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 10000f)
				{
					Delete();
				}
			}
		};
		base.Update += task;
		void Delete()
		{
			if ((Entity)(object)ped != (Entity)null)
			{
				((PoolObject)ped).Delete();
			}
			base.Update -= task;
			API.SetArtificialLightsState(false);
			API.StopAudioScenes();
		}
	}

	private async Coroutine ControlsTask()
	{
		if (currentPumpkin != null && Game.IsControlJustPressed(2, (Control)51) && !isCollectingPumpkin)
		{
			CollectPumpkin(currentPumpkin);
		}
	}

	private async Coroutine CreatePumpkin(Pumpkin pumpkin)
	{
		if (pumpkin != null && !((Entity)(object)pumpkin.Prop != (Entity)null) && !streamedInPumpkins.Contains(pumpkin))
		{
			using DisposableModel propModel = new DisposableModel(Model.op_Implicit(pumpkinModel));
			await propModel.Load();
			pumpkin.Prop = new Prop(API.CreateObject(Model.op_Implicit(propModel.Model), pumpkin.Position.X, pumpkin.Position.Y, pumpkin.Position.Z, false, false, false));
			((Entity)pumpkin.Prop).IsInvincible = true;
			((Entity)pumpkin.Prop).IsCollisionEnabled = false;
			API.PlaceObjectOnGroundProperly(((PoolObject)pumpkin.Prop).Handle);
			streamedInPumpkins.Add(pumpkin);
		}
	}

	private void DestroyPumpkin(Pumpkin pumpkin)
	{
		if (pumpkin != null && !((Entity)(object)pumpkin.Prop == (Entity)null) && streamedInPumpkins.Contains(pumpkin))
		{
			((PoolObject)pumpkin.Prop).Delete();
			pumpkin.Prop = null;
			streamedInPumpkins.Remove(pumpkin);
		}
	}

	[Command("savepumpkin")]
	private async void SavePumpkinCommand(string[] args)
	{
		if (args.Length < 1)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /savepumpkin [id]");
			return;
		}
		string text = args[0];
		if (pumpkins.ContainsKey(text))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "There is already a pumpkin with that id!");
			return;
		}
		Pumpkin obj = new Pumpkin
		{
			Id = text
		};
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		obj.Position_ = ((Vector3)(ref position)).ToArray();
		Pumpkin pumpkin = obj;
		pumpkins.Add(text, pumpkin);
		await CreatePumpkin(pumpkin);
		BaseScript.TriggerServerEvent("gtacnr:halloween:savePumpkin", new object[1] { pumpkin.Json() });
	}
}
