using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using NativeUI;

namespace Gtacnr.Client.Items;

public class DrugScript : Script
{
	public class DrugState
	{
		public string ItemId { get; set; }

		public float Amount { get; set; }

		public float MaxAmount { get; set; }

		public BarTimerBar Bar { get; set; }

		public List<Func<Coroutine>> EffectTasks { get; set; } = new List<Func<Coroutine>>();

		public DrugState(string itemId, float amount, float maxAmount)
		{
			ItemId = itemId;
			Amount = amount;
			MaxAmount = maxAmount;
		}
	}

	private bool hasReceivedAntidote;

	private static readonly Dictionary<string, DrugState> _currentDrugs = new Dictionary<string, DrugState>();

	private int drugAmountTaskTickCount;

	private IReadOnlyDictionary<string, Func<Task>> animTasks = new Dictionary<string, Func<Task>>
	{
		["OtherScript"] = async delegate
		{
			await BaseScript.Delay(5000);
		},
		["Snort"] = async delegate
		{
			API.RequestAnimDict("gestures@miss@fbi_5");
			while (!API.HasAnimDictLoaded("gestures@miss@fbi_5"))
			{
				await BaseScript.Delay(0);
			}
			Game.PlayerPed.Task.PlayAnimation("gestures@miss@fbi_5", "fbi5_gesture_sniff", 4f, 4000, (AnimationFlags)51);
			Prop prop = await World.CreateProp(new Model((Gtacnr.Utils.GetRandomDouble() > 0.5) ? "h4_prop_battle_sniffing_pipe" : "tr_prop_tr_note_rolled_01a"), ((Entity)Game.PlayerPed).Position, false, false);
			API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422), 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, true, 1, true);
			await BaseScript.Delay(4000);
			Game.PlayerPed.Task.ClearAnimation("gestures@miss@fbi_5", "fbi5_gesture_sniff");
			((PoolObject)prop).Delete();
		},
		["Inject"] = async delegate
		{
			API.RequestAnimDict("rcmpaparazzo1ig_4");
			while (!API.HasAnimDictLoaded("rcmpaparazzo1ig_4"))
			{
				await BaseScript.Delay(0);
			}
			await Game.PlayerPed.Task.PlayAnimation("rcmpaparazzo1ig_4", "miranda_shooting_up", 4f, 4f, -1, (AnimationFlags)48, 1f);
			Prop prop = await World.CreateProp(new Model("prop_syringe_01"), ((Entity)Game.PlayerPed).Position, false, false);
			API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 57005), 0.175f, 0.01f, -0.085f, 206.4f, -227.5f, 37.8f, true, true, false, true, 1, true);
			float animT = 0.895f;
			while (animT < 0.99f)
			{
				await BaseScript.Delay(0);
				animT += 0.000275f;
				API.SetEntityAnimCurrentTime(((PoolObject)Game.PlayerPed).Handle, "rcmpaparazzo1ig_4", "miranda_shooting_up", animT);
				if (animT > 0.94f)
				{
					API.SetFacialIdleAnimOverride(API.PlayerPedId(), "mood_drunk_1", (string)null);
				}
			}
			Game.PlayerPed.Task.ClearAnimation("rcmpaparazzo1ig_4", "miranda_shooting_up");
			((PoolObject)prop).Delete();
		},
		["SmokePipe"] = async delegate
		{
			API.RequestAnimDict("amb@world_human_aa_smoke@male@idle_a");
			while (!API.HasAnimDictLoaded("amb@world_human_aa_smoke@male@idle_a"))
			{
				await BaseScript.Delay(0);
			}
			Game.PlayerPed.Task.PlayAnimation("amb@world_human_aa_smoke@male@idle_a", "idle_b", 4f, 7000, (AnimationFlags)51);
			Prop prop = await World.CreateProp(Model.op_Implicit("prop_cs_meth_pipe"), ((Entity)Game.PlayerPed).Position, false, false);
			API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422), 0f, 0f, 0f, 0f, 0f, 270f, true, true, false, true, 1, true);
			await BaseScript.Delay(7000);
			Game.PlayerPed.Task.ClearAnimation("amb@world_human_aa_smoke@male@idle_a", "idle_b");
			((PoolObject)prop).Delete();
		},
		["Drink"] = async delegate
		{
			API.RequestAnimDict("mp_player_intdrink");
			while (!API.HasAnimDictLoaded("mp_player_intdrink"))
			{
				await BaseScript.Delay(0);
			}
			Game.PlayerPed.Task.PlayAnimation("mp_player_intdrink", "loop_bottle", 4f, 4500, (AnimationFlags)49);
			Prop prop = await World.CreateProp(Model.op_Implicit("prop_cs_script_bottle_01"), ((Entity)Game.PlayerPed).Position, false, false);
			API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 18905), 0.12f, 0.008f, 0.03f, 240f, -60f, 0f, true, true, false, true, 1, true);
			await BaseScript.Delay(4500);
			Game.PlayerPed.Task.ClearAnimation("mp_player_intdrink", "loop_bottle");
			((PoolObject)prop).Delete();
		},
		["Eat"] = async delegate
		{
			API.RequestAnimDict("mp_player_inteat@burger");
			while (!API.HasAnimDictLoaded("mp_player_inteat@burger"))
			{
				await BaseScript.Delay(0);
			}
			Game.PlayerPed.Task.PlayAnimation("mp_player_inteat@burger", "mp_player_int_eat_burger", 4f, 1200, (AnimationFlags)49);
			Prop prop = await World.CreateProp(Model.op_Implicit("v_club_vu_pills"), ((Entity)Game.PlayerPed).Position, false, false);
			API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 18905), 0.12f, 0.008f, 0.03f, 240f, -60f, 0f, true, true, false, true, 1, true);
			await BaseScript.Delay(1200);
			Game.PlayerPed.Task.ClearAnimation("mp_player_inteat@burger", "mp_player_int_eat_burger");
			((PoolObject)prop).Delete();
		}
	};

	private static DateTime opiateRagdollT;

	private IReadOnlyDictionary<string, List<Func<DrugState, Task>>> effectTasks = new Dictionary<string, List<Func<DrugState, Task>>>
	{
		["Cocaine"] = new List<Func<DrugState, Task>>
		{
			async delegate(DrugState drugState)
			{
				float val = 1.15f + 0.04f * drugState.Amount;
				val = (AntiSpeedModifiers.RunningSpeedModifier = val.Clamp(1.15f, 1.3f));
				Game.Player.SetRunSpeedMultThisFrame(val);
				await BaseScript.Delay(1000);
			},
			async delegate(DrugState drugState)
			{
				float val = 0.35f * drugState.Amount;
				val = val.Clamp(0.3f, 1f);
				API.RestorePlayerStamina(Game.Player.Handle, val);
				await BaseScript.Delay(8000);
			},
			async delegate
			{
				API.RequestAnimSet("move_m@brave");
				while (!API.HasAnimSetLoaded("move_m@brave"))
				{
					await BaseScript.Delay(1);
				}
				API.SetPedMovementClipset(API.PlayerPedId(), "move_m@brave", 0.1f);
				API.RemoveAnimSet("move_m@brave");
				API.SetPedMoveRateOverride(API.PlayerPedId(), 25f);
				API.SetFacialIdleAnimOverride(API.PlayerPedId(), "pose_normal_1", (string)null);
				await BaseScript.Delay(1000);
			}
		},
		["Opiate"] = new List<Func<DrugState, Task>>
		{
			async delegate(DrugState drugState)
			{
				if (!IsOverdosing && !((Entity)Game.PlayerPed).IsDead)
				{
					InventoryItem? itemDefinition = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId);
					float extraDataFloat = itemDefinition.GetExtraDataFloat("HealthRestoreAmount");
					float extraDataFloat2 = itemDefinition.GetExtraDataFloat("OverdoseAmount");
					int num = (extraDataFloat * (drugState.Amount / extraDataFloat2)).Clamp(1f, 6f).ToIntCeil();
					AntiHealthLockScript.JustHealed();
					Ped playerPed = Game.PlayerPed;
					((Entity)playerPed).Health = ((Entity)playerPed).Health + num;
					await BaseScript.Delay(500);
				}
			},
			async delegate(DrugState drugState)
			{
				if (!_currentDrugs.Values.Any((DrugState ds) => Gtacnr.Data.Items.GetItemDefinition(ds.ItemId).GetExtraDataString("EffectType").In("Cocaine", "Amphetamine")))
				{
					float extraDataFloat = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId).GetExtraDataFloat("SlowWalkThreshold");
					if (drugState.Amount > extraDataFloat && !CuffedScript.IsInCustody)
					{
						API.RequestAnimSet("move_m@hobo@a");
						while (!API.HasAnimSetLoaded("move_m@hobo@a"))
						{
							await BaseScript.Delay(1);
						}
						API.SetPedMovementClipset(API.PlayerPedId(), "move_m@hobo@a", 0.1f);
						API.RemoveAnimSet("move_m@hobo@a");
						if (Gtacnr.Utils.CheckTimePassed(opiateRagdollT, 40000.0))
						{
							opiateRagdollT = DateTime.UtcNow;
							API.SetPedRagdollOnCollision(((PoolObject)Game.PlayerPed).Handle, true);
						}
						else if (Gtacnr.Utils.CheckTimePassed(opiateRagdollT, 10000.0))
						{
							API.SetPedRagdollOnCollision(((PoolObject)Game.PlayerPed).Handle, false);
						}
					}
					else
					{
						API.ResetPedMovementClipset(((PoolObject)Game.PlayerPed).Handle, 1f);
						API.SetPedRagdollOnCollision(((PoolObject)Game.PlayerPed).Handle, false);
					}
					API.SetFacialIdleAnimOverride(API.PlayerPedId(), "mood_injured_1", (string)null);
					await BaseScript.Delay(1000);
				}
			}
		},
		["Amphetamine"] = new List<Func<DrugState, Task>>
		{
			async delegate(DrugState drugState)
			{
				InventoryItem? itemDefinition = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId);
				float extraDataFloat = itemDefinition.GetExtraDataFloat("OverdoseAmount");
				float extraDataFloat2 = itemDefinition.GetExtraDataFloat("AttackMult");
				float extraDataFloat3 = itemDefinition.GetExtraDataFloat("DefenseMult");
				float meleeWeaponDamageModifier = Math.Max(extraDataFloat2 * (drugState.Amount / extraDataFloat), 1f);
				float num = Math.Min(1f - extraDataFloat3 * (drugState.Amount / extraDataFloat), 1f);
				AntiDamageModifierScript.MeleeWeaponDamageModifier = meleeWeaponDamageModifier;
				AntiDamageModifierScript.MeleeWeaponDefenseModifier = num;
				AntiDamageModifierScript.WeaponDefenseModifier = num;
				await BaseScript.Delay(500);
			},
			async delegate(DrugState drugState)
			{
				float extraDataFloat = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId).GetExtraDataFloat("SoundTripThreshold");
				if (drugState.Amount > extraDataFloat)
				{
					API.SetAudioSpecialEffectMode(2);
				}
			},
			async delegate
			{
				if (!_currentDrugs.Values.Any((DrugState ds) => Gtacnr.Data.Items.GetItemDefinition(ds.ItemId).GetExtraDataString("EffectType") == "Cocaine"))
				{
					API.RequestAnimSet("move_m@quick");
					while (!API.HasAnimSetLoaded("move_m@quick"))
					{
						await BaseScript.Delay(1);
					}
					API.SetPedMovementClipset(API.PlayerPedId(), "move_m@quick", 0.1f);
					API.RemoveAnimSet("move_m@quick");
					API.SetPedMoveRateOverride(API.PlayerPedId(), 25f);
					API.SetFacialIdleAnimOverride(API.PlayerPedId(), "mood_stressed_1", (string)null);
					await BaseScript.Delay(1000);
				}
			}
		},
		["Caffeine"] = new List<Func<DrugState, Task>>
		{
			async delegate(DrugState drugState)
			{
				float val = 0.35f * drugState.Amount;
				val = val.Clamp(0.3f, 1f);
				API.RestorePlayerStamina(Game.Player.Handle, val);
				await BaseScript.Delay(8000);
			}
		},
		["THC"] = new List<Func<DrugState, Task>>
		{
			async delegate(DrugState drugState)
			{
				InventoryItem? itemDefinition = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId);
				string extraDataString = itemDefinition.GetExtraDataString("PostFx");
				if (itemDefinition.GetExtraDataBool("PostFxLooped"))
				{
					API.AnimpostfxPlay(extraDataString, 0, true);
				}
				else
				{
					if (!API.AnimpostfxIsRunning(extraDataString))
					{
						API.AnimpostfxPlay(extraDataString, 0, false);
					}
					await BaseScript.Delay(1000);
				}
			},
			async delegate(DrugState drugState)
			{
				float extraDataFloat = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId).GetExtraDataFloat("SoundTripThreshold");
				if (drugState.Amount > extraDataFloat)
				{
					API.SetAudioSpecialEffectMode(2);
				}
			},
			async delegate(DrugState drugState)
			{
				if (!_currentDrugs.Values.Any((DrugState ds) => Gtacnr.Data.Items.GetItemDefinition(ds.ItemId).GetExtraDataString("EffectType").In("Cocaine", "Amphetamine")))
				{
					float extraDataFloat = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId).GetExtraDataFloat("SlowWalkThreshold");
					if (drugState.Amount > extraDataFloat && !CuffedScript.IsInCustody)
					{
						API.RequestAnimSet("move_m@drunk@a");
						while (!API.HasAnimSetLoaded("move_m@drunk@a"))
						{
							await BaseScript.Delay(1);
						}
						API.SetPedMovementClipset(API.PlayerPedId(), "move_m@drunk@a", 0.1f);
						API.RemoveAnimSet("move_m@drunk@a");
					}
					else
					{
						API.ResetPedMovementClipset(((PoolObject)Game.PlayerPed).Handle, 1f);
					}
					API.SetFacialIdleAnimOverride(API.PlayerPedId(), "mood_drunk_1", (string)null);
					await BaseScript.Delay(1000);
				}
			}
		}
	};

	public static bool IsTakingDrugs { get; private set; }

	public static bool IsOverdosing { get; private set; }

	public static IReadOnlyDictionary<string, DrugState> CurrentDrugs => _currentDrugs;

	public static bool IsPlayerOverdosing(Player player)
	{
		return (dynamic)(player.State.Get("gtacnr:drugs:isOverdosing") ?? ((object)false));
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition.GetExtraDataBool("UseDrugsScript"))
		{
			if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() && (itemDefinition.IsIllegal || itemDefinition.IsIntoxicant))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_ON_DUTY, itemDefinition.Name));
				API.CancelEvent();
			}
			else if (Game.PlayerPed.IsBeingStunned)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_BEING_STUNNED, itemDefinition.Name));
				API.CancelEvent();
			}
			else if (CuffedScript.IsBeingCuffedOrUncuffed)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_BEING_CUFFED, itemDefinition.Name));
				API.CancelEvent();
			}
			else if (CuffedScript.IsCuffed)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_CUFFED, itemDefinition.Name));
				API.CancelEvent();
			}
			else if (IsTakingDrugs)
			{
				Utils.PlayErrorSound();
				API.CancelEvent();
			}
		}
	}

	[EventHandler("gtacnr:inventories:usedItem")]
	private void OnUsedItem(string itemId, float amount)
	{
		if (Gtacnr.Data.Items.GetItemDefinition(itemId).GetExtraDataBool("UseDrugsScript"))
		{
			UseDrug(itemId, amount);
		}
	}

	[EventHandler("gtacnr:respawned")]
	private void OnRespawned()
	{
		ClearAllDrugSpecialEffects();
	}

	private async void UseDrug(string itemId, float amount)
	{
		InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (!itemInfo.GetExtraDataBool("UseDrugsScript"))
		{
			return;
		}
		string extraDataString = itemInfo.GetExtraDataString("AnimType");
		if (extraDataString != null && animTasks.ContainsKey(extraDataString))
		{
			try
			{
				IsTakingDrugs = true;
				WeaponBehaviorScript.BlockWeaponSwitchingById("drugs");
				API.ClearPedSecondaryTask(((PoolObject)Game.PlayerPed).Handle);
				if (!Game.PlayerPed.IsInVehicle())
				{
					Game.PlayerPed.Task.ClearAll();
				}
				await animTasks[extraDataString]();
			}
			finally
			{
				WeaponBehaviorScript.UnblockWeaponSwitchingById("drugs");
				IsTakingDrugs = false;
			}
		}
		string key = itemInfo.GetExtraDataString("StateBagVar") ?? itemId;
		if (!_currentDrugs.ContainsKey(key))
		{
			_currentDrugs[key] = new DrugState(itemId, amount, amount);
			AttachDrugTasks(_currentDrugs[key]);
			return;
		}
		_currentDrugs[key].Amount += amount;
		if (_currentDrugs[key].Amount > _currentDrugs[key].MaxAmount)
		{
			_currentDrugs[key].MaxAmount = _currentDrugs[key].Amount;
		}
	}

	private void ClearAllDrugSpecialEffects()
	{
		API.ResetPedMovementClipset(((PoolObject)Game.PlayerPed).Handle, 1f);
		API.ClearFacialIdleAnimOverride(((PoolObject)Game.PlayerPed).Handle);
		API.SetPedMoveRateOverride(API.PlayerPedId(), 1f);
		API.SetPedRagdollOnCollision(((PoolObject)Game.PlayerPed).Handle, false);
		API.ClearTimecycleModifier();
		API.AnimpostfxStopAll();
		AntiDamageModifierScript.MeleeWeaponDamageModifier = 1f;
		AntiDamageModifierScript.MeleeWeaponDefenseModifier = 1f;
		AntiDamageModifierScript.WeaponDefenseModifier = 1f;
		AntiSpeedModifiers.RunningSpeedModifier = 1f;
	}

	[Update]
	private async Coroutine DrugAmountTask()
	{
		await Script.Wait(100);
		drugAmountTaskTickCount++;
		if (_currentDrugs.Count == 0)
		{
			return;
		}
		foreach (DrugState item in _currentDrugs.Values.ToList())
		{
			DrugState drugState = item;
			InventoryItem itemInfo;
			try
			{
				itemInfo = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId);
				float extraDataFloat = itemInfo.GetExtraDataFloat("WearOffSpeed");
				float extraDataFloat2 = itemInfo.GetExtraDataFloat("OverdoseAmount");
				string key = itemInfo.GetExtraDataString("StateBagVar") ?? drugState.ItemId;
				drugState.Amount -= extraDataFloat;
				if (drugState.Amount <= 0f)
				{
					drugState.Amount = 0f;
				}
				if (drugState.Amount <= 0f)
				{
					DetachDrugTasks(drugState);
					_currentDrugs.Remove(key);
					if (_currentDrugs.Count == 0)
					{
						ClearAllDrugSpecialEffects();
					}
				}
				else if (drugState.Amount > extraDataFloat2 && !IsOverdosing)
				{
					Overdose();
				}
				if ((((Entity)Game.PlayerPed).IsDead || (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() && (itemInfo.IsIllegal || itemInfo.IsIntoxicant))) && !IsOverdosing)
				{
					drugState.Amount = 0f;
				}
				if (drugState.Bar == null && drugState.Amount > 0f)
				{
					string text = itemInfo.GetExtraDataString("BarText") ?? itemInfo.Name;
					drugState.Bar = new BarTimerBar(text.ToUpperInvariant());
					TimerBarScript.AddTimerBar(drugState.Bar);
				}
				else if (drugState.Amount <= 0f)
				{
					TimerBarScript.RemoveTimerBar(drugState.Bar);
					drugState.Bar = null;
				}
				if (drugState.Bar != null)
				{
					drugState.Bar.Percentage = drugState.Amount / drugState.MaxAmount;
					drugState.Bar.Color = ((drugState.Bar.Percentage < 0.1f) ? BarColors.Red : ((drugState.Bar.Percentage < 0.3f) ? BarColors.Orange : BarColors.Yellow));
					drugState.Bar.TextColor = ((drugState.Bar.Percentage < 0.1f) ? TextColors.Red : ((drugState.Bar.Percentage < 0.3f) ? TextColors.Orange : TextColors.Yellow));
				}
				if (drugAmountTaskTickCount % 20 == 0 || _currentDrugs.Count == 0)
				{
					BaseScript.TriggerServerEvent("gtacnr:drugs:stateUpdated", new object[1] { _currentDrugs.ToDictionary<KeyValuePair<string, DrugState>, string, float>((KeyValuePair<string, DrugState> kvp) => kvp.Key, (KeyValuePair<string, DrugState> kvp) => kvp.Value.Amount).Json() });
				}
			}
			catch (Exception exception)
			{
				Print(exception);
			}
			async void Overdose()
			{
				_ = 1;
				try
				{
					IsOverdosing = true;
					Game.Player.State.Set("gtacnr:drugs:isOverdosing", (object)true, true);
					Utils.DisplayHelpText("You ~r~overdosed ~s~" + itemInfo.Name + " and you need ~p~medical attention~s~.");
					float tcIntensity = 0.01f;
					API.SetTimecycleModifier("BarryFadeOut");
					API.SetTimecycleModifierStrength(tcIntensity);
					await BaseScript.Delay(2000);
					if (DeathScript.HasSpawnProtection)
					{
						DeathScript.HasSpawnProtection = false;
						((Entity)Game.PlayerPed).IsInvincible = false;
					}
					bool showHelpText = true;
					for (int i = 0; i < 500; i++)
					{
						if (hasReceivedAntidote)
						{
							hasReceivedAntidote = false;
							showHelpText = false;
							drugState.Amount = 0f;
							break;
						}
						Ped playerPed = Game.PlayerPed;
						((Entity)playerPed).Health = ((Entity)playerPed).Health - 1;
						if (((Entity)Game.PlayerPed).Health <= 0 || ((Entity)Game.PlayerPed).IsDead)
						{
							drugState.Amount = 0f;
							DeathScript.ForceDeathCause = -999;
							Game.PlayerPed.Kill();
							return;
						}
						if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
						{
							Game.PlayerPed.Task.LeaveVehicle((LeaveVehicleFlags)4096);
						}
						tcIntensity += 0.0025f;
						tcIntensity = tcIntensity.Clamp(0f, 0.55f);
						API.SetTimecycleModifierStrength(tcIntensity);
						Game.PlayerPed.Ragdoll(1000, (RagdollType)0);
						await BaseScript.Delay(100);
					}
					API.ClearTimecycleModifier();
					Game.PlayerPed.CancelRagdoll();
					if (showHelpText)
					{
						Utils.DisplayHelpText("You ~p~recovered ~s~from the " + itemInfo.Name + " overdose.");
					}
				}
				finally
				{
					IsOverdosing = false;
					Game.Player.State.Set("gtacnr:drugs:isOverdosing", (object)false, true);
				}
			}
		}
	}

	[Update]
	private async Coroutine DisableControlsTask()
	{
		if (IsTakingDrugs)
		{
			Game.DisableControlThisFrame(2, (Control)25);
			Game.DisableControlThisFrame(2, (Control)24);
		}
		if (IsOverdosing)
		{
			API.SetAudioSpecialEffectMode(2);
		}
	}

	private bool AttachDrugTasks(DrugState drugState)
	{
		if (drugState.EffectTasks.Count > 0)
		{
			return false;
		}
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId);
		string extraDataString = itemDefinition.GetExtraDataString("EffectType");
		if (!effectTasks.ContainsKey(extraDataString))
		{
			return false;
		}
		foreach (Func<DrugState, Task> effectTask in effectTasks[extraDataString])
		{
			Func<Coroutine> func = async delegate
			{
				await effectTask(drugState);
			};
			base.Update += func;
			drugState.EffectTasks.Add(func);
		}
		if (itemDefinition.HasExtraData("StateBagVar"))
		{
			string extraDataString2 = itemDefinition.GetExtraDataString("StateBagVar");
			Game.Player.State.Set("gtacnr:" + extraDataString2, (object)true, true);
		}
		return true;
	}

	private bool DetachDrugTasks(DrugState drugState)
	{
		if (drugState.EffectTasks.Count == 0)
		{
			return false;
		}
		foreach (Func<Coroutine> effectTask in drugState.EffectTasks)
		{
			base.Update -= effectTask;
		}
		drugState.EffectTasks.Clear();
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(drugState.ItemId);
		if (itemDefinition.HasExtraData("StateBagVar"))
		{
			string extraDataString = itemDefinition.GetExtraDataString("StateBagVar");
			Game.Player.State.Set("gtacnr:" + extraDataString, (object)false, true);
		}
		return true;
	}

	[EventHandler("gtacnr:drugs:getAntidote")]
	private void GetAntidote(int giverId)
	{
		hasReceivedAntidote = true;
		PlayerState playerState = LatentPlayers.Get(giverId);
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition("naloxone");
		Utils.DisplayHelpText("You received ~p~" + itemDefinition.Name + " ~s~from " + playerState.ColorNameAndId + " and you recovered from the ~r~drug overdose~s~.");
	}
}
