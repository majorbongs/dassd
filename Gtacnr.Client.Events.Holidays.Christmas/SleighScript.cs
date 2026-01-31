using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Vehicles;
using Gtacnr.Model;

namespace Gtacnr.Client.Events.Holidays.Christmas;

public class SleighScript : Script
{
	private class DroppedGift
	{
		public Vector3 Position { get; set; }

		public Blip Blip { get; set; }
	}

	private bool isDropTaskAttached;

	private bool isDrawTaskAttached;

	private bool christmasInitialized;

	private bool staffInit;

	private bool dropInstructionsShown;

	private bool collectInstructionsShown;

	private bool canCollectGift;

	private string closestGiftId;

	private Dictionary<string, DroppedGift> droppedGifts = new Dictionary<string, DroppedGift>();

	private DateTime lastDropTimestamp;

	private int dropCount;

	public SleighScript()
	{
		VehicleEvents.LeftVehicle += OnLeftVehicle;
		StaffLevelScript.StaffLevelInitializedOrChanged += OnStaffLevelInitializedOrChanged;
	}

	[EventHandler("gtacnr:christmas:initialize")]
	private void OnChristmasInitialize()
	{
		christmasInitialized = true;
		AttachDropTaskIfNeeded();
		ObtainAllCurrentlyDroppedGifts();
		base.Update += FindClosestGiftTask;
		KeysScript.AttachListener((Control)51, OnKeyEvent, 20);
	}

	private void OnStaffLevelInitializedOrChanged(object sender, StaffLevelArgs e)
	{
		if ((int)e.PreviousStaffLevel < 10 && (int)e.NewStaffLevel >= 10)
		{
			staffInit = true;
			AttachDropTaskIfNeeded();
			if (christmasInitialized)
			{
				Chat.AddSuggestion("/santa-sleigh", "Spawns a Santa's sleigh.");
			}
		}
		else if ((int)e.PreviousStaffLevel >= 10 && (int)e.NewStaffLevel < 10)
		{
			staffInit = false;
			isDropTaskAttached = false;
			base.Update -= DropTask;
			KeysScript.DetachListener((Control)113, OnKeyEvent);
			if (!Utils.IsFreemodePed(Game.PlayerPed))
			{
				ModeratorMenuScript.RestoreCharacter();
			}
		}
	}

	private void AttachDropTaskIfNeeded()
	{
		if (!isDropTaskAttached && christmasInitialized && staffInit)
		{
			isDropTaskAttached = true;
			base.Update += DropTask;
			KeysScript.AttachListener((Control)113, OnKeyEvent, 100);
		}
	}

	private async void ObtainAllCurrentlyDroppedGifts()
	{
		string text = await TriggerServerEventAsync<string>("gtacnr:christmas:getAllGifts", new object[0]);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		Dictionary<string, string> dictionary = text.Unjson<Dictionary<string, string>>();
		foreach (string key in dictionary.Keys)
		{
			float[] array = dictionary[key].Unjson<float[]>();
			AddGift(key, new Vector3(array[0], array[1], array[2]));
		}
	}

	private void AddGift(string giftId, Vector3 pos)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		Blip val = World.CreateBlip(pos);
		val.Sprite = (BlipSprite)306;
		Utils.SetBlipName(val, "Santa Present", "gift");
		val.Color = (BlipColor)69;
		val.Scale = 0.9f;
		val.IsShortRange = true;
		droppedGifts[giftId] = new DroppedGift
		{
			Position = new Vector3(pos.X, pos.Y, pos.Z),
			Blip = val
		};
	}

	private async void CollectClosestGift()
	{
		if (closestGiftId != null)
		{
			_ = droppedGifts[closestGiftId];
			int num = await TriggerServerEventAsync<int>("gtacnr:christmas:collectGift", new object[1] { closestGiftId });
			switch (num)
			{
			case 2:
				Utils.DisplayHelpText("You collected a ~g~present~s~! You can open your presents in ~b~M ~s~> ~b~Inventory~s~!");
				break;
			case 3:
				Utils.DisplayHelpText("You must ~r~wait ~s~before collecting another ~g~present~s~!");
				break;
			default:
				Utils.DisplayErrorMessage(93, num);
				break;
			}
		}
	}

	private bool IsInSleigh()
	{
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			return ((Entity)Game.PlayerPed.CurrentVehicle).Model == Model.op_Implicit(API.GetHashKey("sled"));
		}
		return false;
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Invalid comparison between Unknown and I4
		if ((int)control == 113 && eventType == KeyEventType.JustPressed)
		{
			if (IsInSleigh())
			{
				DropGift();
				return true;
			}
		}
		else if ((int)control == 51 && eventType == KeyEventType.JustPressed && canCollectGift)
		{
			CollectClosestGift();
			return true;
		}
		return false;
	}

	private async Coroutine DropTask()
	{
		await Script.Wait(500);
		if (IsInSleigh())
		{
			if (!dropInstructionsShown)
			{
				Utils.AddInstructionalButton("dropGift", new InstructionalButton("Drop Present", 2, (Control)113));
				dropInstructionsShown = true;
			}
		}
		else if (dropInstructionsShown)
		{
			Utils.RemoveInstructionalButton("dropGift");
			dropInstructionsShown = false;
		}
	}

	private async Task DropGift()
	{
		if (!Gtacnr.Utils.CheckTimePassed(lastDropTimestamp, 5000.0))
		{
			Utils.SendNotification("You need to ~r~wait ~s~5 seconds between dropping ~g~presents~s~.");
			Utils.PlayErrorSound();
			return;
		}
		if (dropCount > 100)
		{
			if (!Gtacnr.Utils.CheckTimePassed(lastDropTimestamp, 600000.0))
			{
				Utils.SendNotification("You have dropped ~r~too many ~s~presents, please wait.");
				Utils.PlayErrorSound();
				return;
			}
			dropCount = 0;
		}
		float groundZ = World.GetGroundHeight(((Entity)Game.PlayerPed).Position.XY());
		if (((Entity)Game.PlayerPed).Position.Z - 30f < groundZ)
		{
			Utils.SendNotification("You are ~r~too low ~s~to drop ~g~presents~s~.");
			Utils.PlayErrorSound();
			return;
		}
		lastDropTimestamp = DateTime.UtcNow;
		dropCount++;
		Prop prop = await World.CreateProp(Model.op_Implicit("prop_drop_armscrate_01"), ((Entity)Game.PlayerPed).Position + new Vector3(0f, 0f, -3f), true, false);
		AntiEntitySpawnScript.RegisterEntity((Entity)(object)prop);
		API.ActivatePhysics(((PoolObject)prop).Handle);
		Utils.DisplayHelpText("~r~Ho ho ho~s~! You dropped a ~g~present~s~!");
		DropTask();
		async void DropTask()
		{
			DateTime t = DateTime.UtcNow;
			while (!Gtacnr.Utils.CheckTimePassed(t, 90000.0))
			{
				await BaseScript.Delay(100);
				if (!prop.Exists())
				{
					break;
				}
				if (Math.Abs(((Entity)prop).Position.Z - groundZ) < 1.5f)
				{
					Vector3 pos = ((Entity)prop).Position;
					((PoolObject)prop).Delete();
					Prop obj = await World.CreateProp(Model.op_Implicit(new string[2] { "bzzz_prop_gift_orange", "bzzz_prop_gift_purple" }.Random()), pos, true, false);
					AntiEntitySpawnScript.RegisterEntity((Entity)(object)obj);
					API.ActivatePhysics(((PoolObject)obj).Handle);
					BaseScript.TriggerServerEvent("gtacnr:christmas:createGift", new object[3] { pos.X, pos.Y, pos.Z });
					return;
				}
			}
			if (prop.Exists())
			{
				((PoolObject)prop).Delete();
			}
			Utils.DisplayHelpText("The ~g~present ~s~has ~r~despawned~s~.");
		}
	}

	[EventHandler("gtacnr:christmas:onGiftCreated")]
	private void OnGiftCreated(int dropPlayerId, string giftId, float x, float y, float z)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		((Vector3)(ref val))._002Ector(x, y, z);
		PlayerState arg = LatentPlayers.Get(dropPlayerId);
		string locationName = Utils.GetLocationName(val);
		AddGift(giftId, new Vector3(x, y, z));
		Print($"Present dropped by: {arg} in {locationName}");
		Utils.SendNotification("~r~Santa Claus ~s~has dropped a ~g~present ~s~in ~y~" + locationName + "~s~! Collect it before anyone else does.");
		Chat.AddMessage(Gtacnr.Utils.Colors.PlainText, "^1Santa Claus ^0has dropped a ^2present ^0in ^3" + locationName + "^0!");
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(val) < 10000f)
		{
			Utils.DisplayHelpText("The ~g~present dropped by ~r~Santa Claus ~s~is nearby!");
		}
	}

	[EventHandler("gtacnr:christmas:giftCollected")]
	private void OnGiftCollected(int collectPlayerId, string giftId)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (droppedGifts.ContainsKey(giftId))
		{
			DroppedGift droppedGift = droppedGifts[giftId];
			if (droppedGift.Blip != (Blip)null && ((PoolObject)droppedGift.Blip).Exists())
			{
				((PoolObject)droppedGift.Blip).Delete();
			}
			PlayerState? playerState = LatentPlayers.Get(collectPlayerId);
			Utils.SendNotification(string.Concat(str2: Utils.GetLocationName(droppedGift.Position), str0: playerState.ColorNameAndId, str1: " has collected a ~g~present ~s~in ~y~", str3: "~s~."));
			Utils.RemoveInstructionalButton("collectGift");
			droppedGifts.Remove(giftId);
		}
	}

	private async Coroutine FindClosestGiftTask()
	{
		await Script.Wait(1000);
		closestGiftId = null;
		canCollectGift = false;
		if (droppedGifts.Count == 0)
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 10000f;
		foreach (string key in droppedGifts.Keys)
		{
			DroppedGift droppedGift = droppedGifts[key];
			float num2 = ((Vector3)(ref position)).DistanceToSquared(droppedGift.Position);
			if (num2 < num)
			{
				num = num2;
				closestGiftId = key;
				canCollectGift = num2 <= 6.25f;
			}
		}
		if (closestGiftId != null && !isDrawTaskAttached)
		{
			base.Update += DrawMarkerTask;
			isDrawTaskAttached = true;
		}
		else if (closestGiftId == null && isDrawTaskAttached)
		{
			base.Update -= DrawMarkerTask;
			isDrawTaskAttached = false;
		}
		if (canCollectGift && !collectInstructionsShown)
		{
			collectInstructionsShown = true;
			Utils.AddInstructionalButton("collectGift", new InstructionalButton("Collect Present", 2, (Control)51));
		}
		else if (!canCollectGift && collectInstructionsShown)
		{
			collectInstructionsShown = false;
			Utils.RemoveInstructionalButton("collectGift");
		}
	}

	private async Coroutine DrawMarkerTask()
	{
		if (closestGiftId != null && droppedGifts.ContainsKey(closestGiftId))
		{
			Vector3 position = droppedGifts[closestGiftId].Position;
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector(2.5f, 2.5f, 0.4f);
			Color color = Color.FromUint(4125163632u);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref z, false))
			{
				position.Z = z;
			}
			API.DrawMarker(1, position.X, position.Y, position.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}

	private async void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		if ((int)StaffLevelScript.StaffLevel < 100 && ((Entity)e.Vehicle).Model == Model.op_Implicit("sled"))
		{
			await BaseScript.Delay(100);
			bool wasDead = DeathScript.IsAlive != true || ((Entity)Game.PlayerPed).IsDead;
			await ModeratorMenuScript.RestoreCharacter();
			if (wasDead)
			{
				((Entity)Game.PlayerPed).Health = 0;
			}
		}
	}
}
