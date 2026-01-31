using System;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.IMenu;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police;

public class SpikeStripScript : Script
{
	private static bool isUsingSpikeStrip;

	private readonly TimeSpan spikeStripCooldown = TimeSpan.FromMinutes(1.0);

	private DateTime spikeStripT;

	private Prop spikeStripBagProp;

	private bool keysAttached;

	private Prop? previewProp;

	private bool popTiresTaskAttached;

	private Prop spikeProp;

	public static bool IsUsingSpikeStrip => isUsingSpikeStrip;

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		if (itemId == "spike_strip")
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
			Job jobData = Gtacnr.Data.Jobs.GetJobData("police");
			if (isUsingSpikeStrip)
			{
				Utils.SendNotification("You are already using a ~y~" + itemDefinition.Name + "~s~.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
			if (amount != 1f)
			{
				Utils.SendNotification("You can only use one ~y~" + itemDefinition.Name + " ~s~at a time.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
			if (Gtacnr.Client.API.Jobs.CachedJob != "police")
			{
				Utils.SendNotification("You must be a ~b~" + jobData.Name + " ~s~to place a ~y~" + itemDefinition.Name + "~s~.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
			if (Game.PlayerPed.IsInVehicle())
			{
				Utils.SendNotification("You cannot use a ~y~" + itemDefinition.Name + " ~s~when you're in a vehicle.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
			if (InventoryMenuScript.Cache != null)
			{
				InventoryEntry inventoryEntry = InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemId);
				if (inventoryEntry == null || inventoryEntry.Amount < 1f)
				{
					Utils.SendNotification("You don't have a ~r~" + itemDefinition.Name + "~s~.");
					Utils.PlayErrorSound();
					API.CancelEvent();
					return;
				}
			}
			if (!Gtacnr.Utils.CheckTimePassed(spikeStripT, spikeStripCooldown))
			{
				Utils.SendNotification($"You must wait ~r~{Gtacnr.Utils.GetCooldownTimeLeft(spikeStripT, spikeStripCooldown).TotalSeconds.ToIntCeil()} seconds ~s~before placing another ~y~{itemDefinition.Name}~s~.");
				Utils.PlayErrorSound();
				API.CancelEvent();
			}
			else
			{
				EquipSpikeStrip();
				API.CancelEvent();
			}
		}
		else if (!(itemId == "road_cone"))
		{
			_ = itemId == "road_barrier";
		}
	}

	private async void EquipSpikeStrip()
	{
		if (!isUsingSpikeStrip)
		{
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			isUsingSpikeStrip = true;
			base.Update += SpikeStripTask;
			AttachSpikeStripKeys();
			spikeStripBagProp = await World.CreateProp(Model.op_Implicit(API.GetHashKey(Gtacnr.Data.Items.GetItemDefinition("spike_strip").Model)), ((Entity)Game.PlayerPed).Position, true, false);
			Prop obj = spikeStripBagProp;
			int pedBoneIndex = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422);
			API.AttachEntityToEntity(((PoolObject)obj).Handle, ((PoolObject)Game.PlayerPed).Handle, pedBoneIndex, 0.28f, -0.005f, 0.015f, 6.3f, -96.6f, -121.8f, true, true, false, true, 1, true);
			bool validPosition;
			Vector3 previewPos = CalculateSpikeStripPosition(out validPosition);
			using (DisposableModel propModel = new DisposableModel(Model.op_Implicit(API.GetHashKey("p_stinger_04"))))
			{
				await propModel.Load();
				previewProp = new Prop(API.CreateObject(Model.op_Implicit(propModel.Model), previewPos.X, previewPos.Y, previewPos.Z - 25f, false, false, false));
			}
			((Entity)previewProp).Opacity = 100;
			((Entity)previewProp).Rotation = new Vector3(0f, 0f, ((Entity)Game.PlayerPed).Heading + 180f);
			((Entity)previewProp).IsCollisionEnabled = false;
		}
	}

	private async void UnequipSpikeStrip()
	{
		if (!isUsingSpikeStrip)
		{
			return;
		}
		isUsingSpikeStrip = false;
		base.Update -= SpikeStripTask;
		DetachSpikeStripKeys();
		if (!((Entity)(object)spikeStripBagProp == (Entity)null))
		{
			((Entity)spikeStripBagProp).Detach();
			((PoolObject)spikeStripBagProp).Delete();
			Prop? obj = previewProp;
			if (obj != null)
			{
				((PoolObject)obj).Delete();
			}
			previewProp = null;
		}
	}

	private async Coroutine SpikeStripTask()
	{
		if (isUsingSpikeStrip)
		{
			if (Game.PlayerPed.IsGettingIntoAVehicle || Game.PlayerPed.IsInVehicle() || (int)Weapon.op_Implicit(Game.PlayerPed.Weapons.Current) != -1569615261)
			{
				UnequipSpikeStrip();
			}
			else if ((Entity)(object)previewProp != (Entity)null)
			{
				bool validPosition;
				Vector3 val = CalculateSpikeStripPosition(out validPosition);
				API.SetEntityCoordsNoOffset(((PoolObject)previewProp).Handle, val.X, val.Y, val.Z, false, false, false);
				((Entity)previewProp).Rotation = new Vector3(0f, 0f, ((Entity)Game.PlayerPed).Heading + 180f);
			}
			await BaseScript.Delay(50);
		}
	}

	private void AttachSpikeStripKeys()
	{
		if (!keysAttached)
		{
			keysAttached = true;
			Utils.AddInstructionalButton("useSpikeStrip", new InstructionalButton("Spike Strip", 2, (Control)24));
			KeysScript.AttachListener((Control)24, OnSpikeStripKeyEvent, 50);
		}
	}

	private void DetachSpikeStripKeys()
	{
		if (keysAttached)
		{
			keysAttached = false;
			Utils.RemoveInstructionalButton("useSpikeStrip");
			KeysScript.DetachListener((Control)24, OnSpikeStripKeyEvent);
		}
	}

	private bool OnSpikeStripKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (!keysAttached)
		{
			return false;
		}
		if (eventType == KeyEventType.JustPressed)
		{
			KeysScript.DetachListener((Control)24, OnSpikeStripKeyEvent);
			PlaceSpikeStrip();
			return true;
		}
		return false;
	}

	private static Vector3 CalculateSpikeStripPosition(out bool validPosition)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = (((Entity)Game.PlayerPed).Heading + 90f).ToRadians();
		position.X += (float)(1.5 * Math.Cos(num));
		position.Y += (float)(1.5 * Math.Sin(num));
		float num2 = 0f;
		API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref num2, false);
		validPosition = true;
		if (num2 > position.Z + 2f || num2 < position.Z - 5f)
		{
			validPosition = false;
		}
		position.Z = num2 + 0.5f;
		return position;
	}

	private async void PlaceSpikeStrip()
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition("spike_strip");
		UnequipSpikeStrip();
		spikeStripT = DateTime.UtcNow;
		bool validPosition;
		Vector3 position = CalculateSpikeStripPosition(out validPosition);
		if (!validPosition)
		{
			Utils.SendNotification("You can't place a ~y~" + itemDefinition.Name + " ~s~here.");
			Utils.PlayErrorSound();
			return;
		}
		Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
		await Utils.LoadAnimDictionary("weapons@projectile@");
		await Game.PlayerPed.Task.PlayAnimation("weapons@projectile@", "throw_l_fb_stand", 4f, -4f, 800, (AnimationFlags)51, 1f);
		await BaseScript.Delay(350);
		Prop prop = await World.CreateProp(Model.op_Implicit(API.GetHashKey("p_stinger_04")), position, false, false);
		((Entity)prop).Rotation = new Vector3(0f, 0f, ((Entity)Game.PlayerPed).Heading + 180f);
		API.SetEntityCollision(((PoolObject)prop).Handle, true, true);
		API.SetEntityAsMissionEntity(((PoolObject)prop).Handle, true, true);
		API.ActivatePhysics(((PoolObject)prop).Handle);
		await AntiEntitySpawnScript.RegisterEntity((Entity)(object)prop);
		ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:police:spikeStripPlaced", ((Entity)prop).NetworkId);
		if (responseCode == ResponseCode.Success)
		{
			Utils.DisplayHelpText("~b~Spike strip ~s~placed. It will only affect ~o~suspects ~s~with two stars or more.");
			return;
		}
		((PoolObject)prop).Delete();
		Utils.DisplayError(responseCode, "The ~y~spike strip ~s~has been removed.", "PlaceSpikeStrip");
	}

	[Update]
	private async Coroutine FindSpikeStripsTask()
	{
		await BaseScript.Delay(500);
		spikeProp = null;
		if (Gtacnr.Client.API.Crime.CachedWantedLevel >= 2 && (Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			int closestObjectOfType = API.GetClosestObjectOfType(((Entity)currentVehicle).Position.X, ((Entity)currentVehicle).Position.Y, ((Entity)currentVehicle).Position.Z, 60f, (uint)API.GetHashKey("p_stinger_04"), true, true, true);
			if (API.DoesEntityExist(closestObjectOfType))
			{
				spikeProp = new Prop(closestObjectOfType);
				if (!popTiresTaskAttached)
				{
					popTiresTaskAttached = true;
					base.Update += PopTiresTask;
				}
				return;
			}
		}
		if (popTiresTaskAttached)
		{
			popTiresTaskAttached = false;
			base.Update -= PopTiresTask;
		}
	}

	private async Coroutine PopTiresTask()
	{
		await BaseScript.Delay(20);
		if ((Entity)(object)spikeProp == (Entity)null || (Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null)
		{
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if (currentVehicle.Wheels == null || !API.IsEntityTouchingEntity(((PoolObject)currentVehicle).Handle, ((PoolObject)spikeProp).Handle))
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < 16; i++)
		{
			if (!API.IsVehicleTyreBurst(((PoolObject)currentVehicle).Handle, i, false))
			{
				API.SetVehicleTyreBurst(((PoolObject)currentVehicle).Handle, i, true, 500f);
				flag = true;
			}
		}
		if (flag)
		{
			BaseScript.TriggerServerEvent("gtacnr:police:spikeStripHit", new object[1] { ((Entity)spikeProp).NetworkId });
		}
	}
}
