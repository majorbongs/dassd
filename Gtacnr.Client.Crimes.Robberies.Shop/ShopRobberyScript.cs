using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Communication;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Weapons;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using NativeUI;

namespace Gtacnr.Client.Crimes.Robberies.Shop;

public class ShopRobberyScript : Script
{
	public class RobberyState
	{
		public Business Business { get; set; }

		public BusinessEmployee Employee { get; set; }

		public RobberyInfo Info { get; set; }

		public int IntimidationStep { get; set; }

		public int KeepIntimidatedStep { get; set; }

		public bool PoliceCalled { get; set; }

		public bool SafeCrackStarted { get; set; }
	}

	private bool isRobberyTaskAttached;

	private RobberyState lastRobbery;

	private BusinessEmployee robberyEmployee;

	private DateTime robberyEndTimestamp;

	private Random random = new Random();

	private DateTime lastRobberyAttemptTimestamp;

	private DateTime antiSpamTimestamp;

	private bool wasShooting;

	private TextTimerBar counterMoneyBox;

	private TextTimerBar safeMoneyBox;

	private TextTimerBar yourCutBox;

	private BarTimerBar intimidationBox;

	private Dictionary<int, BarTimerBar> playerHealthBoxes = new Dictionary<int, BarTimerBar>();

	private static ShopRobberyScript Instance;

	private readonly HashSet<WeaponHash> disallowedRobberyWeapons = new HashSet<WeaponHash>
	{
		(WeaponHash)(-1569615261),
		(WeaponHash)(-1951375401),
		(WeaponHash)1233104067,
		(WeaponHash)126349499,
		(WeaponHash)600439132,
		(WeaponHash)(-37975472),
		(WeaponHash)(-1600701090),
		(WeaponHash)101631238,
		(WeaponHash)(-72657034)
	};

	public static RobberyState CurrentRobbery { get; private set; }

	public static event EventHandler RobberyInitiated;

	public static event EventHandler RobberyJoined;

	public static event EventHandler RobberyLeft;

	public static event EventHandler RobberyEnded;

	public ShopRobberyScript()
	{
		Instance = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		if (e.CurrentJobEnum.IsPublicService())
		{
			DetachRobberyTask();
		}
		else
		{
			AttachRobberyTask();
		}
	}

	private void AttachRobberyTask()
	{
		if (!isRobberyTaskAttached)
		{
			base.Update += RobberyTask;
			isRobberyTaskAttached = true;
		}
	}

	private void DetachRobberyTask()
	{
		if (isRobberyTaskAttached)
		{
			base.Update -= RobberyTask;
			isRobberyTaskAttached = false;
		}
	}

	private async Coroutine RobberyTask()
	{
		await Script.Wait(250);
		if (BusinessScript.ClosestBusiness == null)
		{
			return;
		}
		Vector3 pPos = ((Entity)Game.PlayerPed).Position;
		Vector3 position;
		BusinessEmployee threatenedEmployee;
		if (CurrentRobbery == null && ((Entity)Game.PlayerPed).IsAlive && !CuffedScript.IsCuffed)
		{
			float num = 900f;
			robberyEmployee = null;
			foreach (BusinessEmployee item in BusinessScript.ClosestBusiness.Employees.Where((BusinessEmployee e) => e?.CanBeRobbed ?? false))
			{
				position = item.Position;
				float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
				if (num2 < num)
				{
					num = num2;
					robberyEmployee = item;
				}
			}
			threatenedEmployee = null;
			if (!BusinessScript.ClosestBusiness.EmployeesAssaulted)
			{
				foreach (BusinessEmployee item2 in BusinessScript.ClosestBusiness.Employees.Where(delegate(BusinessEmployee e)
				{
					if (e != null)
					{
						BusinessEmployeeState state2 = e.State;
						bool? obj2;
						if (state2 == null)
						{
							obj2 = null;
						}
						else
						{
							Ped ped2 = state2.Ped;
							obj2 = ((ped2 != null) ? new bool?(ped2.Exists()) : ((bool?)null));
						}
						bool? flag7 = obj2;
						if (flag7 == true)
						{
							BusinessEmployeeState state3 = e.State;
							bool? obj3;
							if (state3 == null)
							{
								obj3 = null;
							}
							else
							{
								Ped ped3 = state3.Ped;
								obj3 = ((ped3 != null) ? new bool?(((Entity)ped3).IsAlive) : ((bool?)null));
							}
							flag7 = obj3;
							if (flag7 == true)
							{
								return e.State?.IsActive ?? false;
							}
						}
					}
					return false;
				}))
				{
					position = item2.Position;
					float num3 = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
					bool flag = (IsPointingWeaponAtEmployee(item2) || (API.GetMeleeTargetForPed(((PoolObject)Game.PlayerPed).Handle) == ((PoolObject)item2.State.Ped).Handle && Utils.IsAnActualWeapon(Weapon.op_Implicit(Game.PlayerPed.Weapons.Current)))) && num3 < 225f;
					bool flag2 = (((IsShockingEventNearby(29) || IsShockingEventNearby(90) || IsShockingEventNearby(91) || IsShockingEventNearby(104)) && !Game.PlayerPed.IsInVehicle()) || (Game.PlayerPed.IsInVehicle() && (int)Game.PlayerPed.CurrentVehicle.ClassType == 8 && WasPedRecentlyDamagedByPlayer(((PoolObject)item2.State.Ped).Handle))) && Utils.IsAnActualWeapon(Weapon.op_Implicit(Game.PlayerPed.Weapons.Current)) && num3 < 225f;
					if (flag || flag2)
					{
						BusinessScript.ClosestBusiness.AssaultEmployees();
						if (flag)
						{
							threatenedEmployee = item2;
						}
						GuardsAttackPlayer();
						JoinRobbery(scareEmployee: false);
						break;
					}
				}
				foreach (BusinessEmployee item3 in BusinessScript.ClosestBusiness.Employees.Where(delegate(BusinessEmployee e)
				{
					if (e != null)
					{
						BusinessEmployeeState state2 = e.State;
						bool? obj2;
						if (state2 == null)
						{
							obj2 = null;
						}
						else
						{
							Ped ped2 = state2.Ped;
							obj2 = ((ped2 != null) ? new bool?(((Entity)ped2).IsAlive) : ((bool?)null));
						}
						bool? flag7 = obj2;
						return flag7 != true;
					}
					return false;
				}))
				{
					position = item3.Position;
					float num4 = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
					if (WasPedRecentlyDamagedByPlayer(((PoolObject)item3.State.Ped).Handle) && num4 < 225f)
					{
						BusinessScript.ClosestBusiness.AssaultEmployees();
						GuardsAttackPlayer();
						JoinRobbery(scareEmployee: false);
						break;
					}
				}
			}
		}
		if (BusinessScript.ClosestBusiness != null)
		{
			foreach (BusinessEmployee employee in BusinessScript.ClosestBusiness.Employees)
			{
				if (!employee.State.IsActive && !employee.State.DeathEventTriggered)
				{
					employee.State.DeathEventTriggered = true;
					BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:employeeDied", new object[2]
					{
						BusinessScript.ClosestBusiness.Id,
						BusinessScript.ClosestBusiness.Employees.IndexOf(employee)
					});
				}
			}
		}
		if (robberyEmployee == null)
		{
			return;
		}
		BusinessEmployee businessEmployee = robberyEmployee;
		bool? obj;
		if (businessEmployee == null)
		{
			obj = null;
		}
		else
		{
			BusinessEmployeeState state = businessEmployee.State;
			if (state == null)
			{
				obj = null;
			}
			else
			{
				Ped ped = state.Ped;
				obj = ((ped != null) ? new bool?(((Entity)ped).IsAlive) : ((bool?)null));
			}
		}
		bool? flag3 = obj;
		if (flag3 != true)
		{
			return;
		}
		if (CurrentRobbery == null)
		{
			if (IsPointingWeaponAtEmployee(robberyEmployee, 8f) && Gtacnr.Utils.CheckTimePassed(robberyEndTimestamp, 5000.0) && Gtacnr.Utils.CheckTimePassed(antiSpamTimestamp, 10000.0))
			{
				antiSpamTimestamp = DateTime.UtcNow;
				Business closestBusiness = BusinessScript.ClosestBusiness;
				if (lastRobbery != null && lastRobbery.Business.Id == closestBusiness.Id)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.CANNOT_ROB_SAME_STORE_TWICE));
				}
				else if (closestBusiness.IsBeingRobbed)
				{
					JoinRobbery();
				}
				else
				{
					StartRobbery();
				}
			}
			return;
		}
		if (((Entity)robberyEmployee.State.Ped).IsAlive)
		{
			uint num5 = (uint)(int)Game.PlayerPed.Weapons.Current.Hash;
			bool flag4 = API.GetWeaponDamageType(num5) == 3;
			bool flag5 = flag4 && !Gtacnr.Utils.CheckTimePassed(WeaponBehaviorScript.LastGunShotTime, 250.0) && !API.IsPedReloading(((PoolObject)Game.PlayerPed).Handle) && API.GetAmmoInPedWeapon(((PoolObject)Game.PlayerPed).Handle, num5) > 0;
			bool flag6 = API.IsPedFacingPed(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)robberyEmployee.State.Ped).Handle, 60f);
			if (IsPointingWeaponAtEmployee(robberyEmployee))
			{
				IncreaseIntimidation(2);
			}
			if (HasClearLosToEmployee(robberyEmployee))
			{
				if (flag5 && !wasShooting)
				{
					robberyEmployee.State.Ped.PlayAmbientSpeech("GENERIC_FRIGHTENED_HIGH", (SpeechModifier)3);
				}
				if (flag4 && flag6)
				{
					if (flag5)
					{
						IncreaseIntimidation(8);
						if (!API.IsPedCurrentWeaponSilenced(((PoolObject)Game.PlayerPed).Handle) && !CurrentRobbery.PoliceCalled && !CurrentRobbery.Info.PoliceCalled)
						{
							CallPolice();
						}
					}
					else
					{
						KeepIntimidated();
					}
				}
				else if (flag5)
				{
					KeepIntimidated();
				}
			}
			if (CurrentRobbery.IntimidationStep >= 8)
			{
				Intimidate(increaseLevel: true);
				CurrentRobbery.IntimidationStep = 0;
			}
			else if (CurrentRobbery.KeepIntimidatedStep >= 6)
			{
				Intimidate(increaseLevel: false);
				CurrentRobbery.KeepIntimidatedStep = 0;
			}
			wasShooting = flag5;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:leave", new object[2]
			{
				CurrentRobbery.Business.Id,
				1
			});
			ShopRobberyScript.RobberyLeft?.Invoke(this, new EventArgs());
			EndRobbery();
			return;
		}
		if (CuffedScript.IsCuffed)
		{
			BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:leave", new object[2]
			{
				CurrentRobbery.Business.Id,
				2
			});
			ShopRobberyScript.RobberyLeft?.Invoke(this, new EventArgs());
			EndRobbery();
			return;
		}
		if (BusinessScript.ClosestBusiness == CurrentRobbery.Business)
		{
			position = ((Entity)Game.PlayerPed).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared(robberyEmployee.Position) > 2500f))
			{
				return;
			}
		}
		BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:leave", new object[2]
		{
			CurrentRobbery.Business.Id,
			0
		});
		Utils.SendNotification(LocalizationController.S(Entries.Jobs.ABANDONED_ROBBERY));
		ShopRobberyScript.RobberyLeft?.Invoke(this, new EventArgs());
		EndRobbery();
		void GuardsAttackPlayer()
		{
			if (!BusinessScript.ClosestBusiness.CopsCalled && BusinessScript.ClosestBusiness.Employees.Count > 1)
			{
				BaseScript.TriggerServerEvent("gtacnr:businesses:callCops", new object[1] { BusinessScript.ClosestBusiness.Id });
				BusinessScript.ClosestBusiness.CopsCalled = true;
			}
			foreach (BusinessEmployee employee2 in BusinessScript.ClosestBusiness.Employees)
			{
				if (employee2.Role == EmployeeRole.SecurityGuard)
				{
					EmployeeAttackPlayer(employee2);
				}
				else if (employee2 != threatenedEmployee || threatenedEmployee != robberyEmployee)
				{
					EmployeeSetScared(employee2);
				}
			}
		}
		static bool HasClearLosToEmployee(BusinessEmployee employee)
		{
			if (employee == null || employee.State == null || (Entity)(object)employee.State.Ped == (Entity)null)
			{
				return false;
			}
			return API.HasEntityClearLosToEntity(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)employee.State.Ped).Handle, 17);
		}
		static void IncreaseIntimidation(int amount)
		{
			CurrentRobbery.IntimidationStep += amount;
		}
		bool IsPointingWeaponAtEmployee(BusinessEmployee employee, float maxDist = -1f)
		{
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			if (employee == null || employee.State == null || (Entity)(object)employee.State.Ped == (Entity)null)
			{
				return false;
			}
			bool num6 = HasClearLosToEmployee(employee);
			bool flag7 = API.IsPlayerFreeAimingAtEntity(Game.Player.Handle, ((PoolObject)employee.State.Ped).Handle);
			bool flag8 = !disallowedRobberyWeapons.Contains(Game.PlayerPed.Weapons.Current.Hash);
			float num7 = ((Vector3)(ref pPos)).DistanceToSquared(((Entity)employee.State.Ped).Position);
			if (num6 && flag7 && flag8)
			{
				if (maxDist != -1f)
				{
					return num7 <= maxDist * maxDist;
				}
				return true;
			}
			return false;
		}
		bool IsShockingEventNearby(int eventId)
		{
			return API.IsShockingEventInSphere(eventId, pPos.X, pPos.Y, pPos.Z, 10f);
		}
		static void KeepIntimidated()
		{
			CurrentRobbery.KeepIntimidatedStep++;
		}
		static bool WasPedRecentlyDamagedByPlayer(int num6)
		{
			return API.HasEntityBeenDamagedByEntity(num6, ((PoolObject)Game.PlayerPed).Handle, true);
		}
	}

	private async void StartRobbery()
	{
		if (!Gtacnr.Utils.CheckTimePassed(lastRobberyAttemptTimestamp, 10000.0))
		{
			return;
		}
		lastRobberyAttemptTimestamp = DateTime.UtcNow;
		CurrentRobbery = new RobberyState
		{
			Business = BusinessScript.ClosestBusiness,
			Employee = robberyEmployee
		};
		BusinessScript.ClosestBusiness.IsBeingRobbed = true;
		EmployeeStartBeingRobbed(CurrentRobbery.Employee);
		RobberyInfo robberyInfo = (await TriggerServerEventAsync<string>("gtacnr:businesses:robbery:start", new object[3]
		{
			CurrentRobbery.Business.Id,
			CurrentRobbery.Business.Employees.IndexOf(CurrentRobbery.Employee),
			false
		})).Unjson<RobberyInfo>();
		if (robberyInfo.Response == RobberyResponse.Started)
		{
			intimidationBox = new BarTimerBar(LocalizationController.S(Entries.Jobs.ROBBERY_INTIMIDATION_BAR))
			{
				Percentage = robberyInfo.Intimidation
			};
			counterMoneyBox = new TextTimerBar(LocalizationController.S(Entries.Jobs.ROBBERY_TAKE_COUNTER), "$0")
			{
				TextColor = TextColors.Green
			};
			TimerBarScript.AddTimerBar(intimidationBox);
			TimerBarScript.AddTimerBar(counterMoneyBox);
			if (CurrentRobbery.Business.SafeData != null)
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.ROBBERY_SAFE_HINT) + " " + LocalizationController.S(Entries.Jobs.ROBBERY_INTIMIDATION_HINT));
			}
			else
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.ROBBERY_INTIMIDATION_HINT));
			}
			ShopRobberyScript.RobberyInitiated?.Invoke(this, new EventArgs());
			return;
		}
		if (robberyInfo.Response == RobberyResponse.Joined)
		{
			OnJoined(robberyInfo);
			return;
		}
		if (robberyInfo.Response == RobberyResponse.AlreadyInProgress)
		{
			JoinRobbery();
			return;
		}
		bool attack = false;
		if (robberyInfo.Response == RobberyResponse.CannotBeRobbed)
		{
			switch (CurrentRobbery.Business.Type)
			{
			case BusinessType.Jewelry:
				if (CurrentRobbery.Business.JewelryRobbery != null)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_NOT_IDEAL));
				}
				else
				{
					Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_CANNOT_BE_ROBBED));
				}
				Utils.PlayErrorSound();
				attack = true;
				break;
			case BusinessType.GunStore:
			case BusinessType.GunStoreWithShootingRange:
			case BusinessType.Bank:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_CANNOT_BE_ROBBED_YET));
				Utils.PlayErrorSound();
				attack = true;
				break;
			default:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_CANNOT_BE_ROBBED));
				Utils.PlayErrorSound();
				break;
			}
		}
		else if (robberyInfo.Response == RobberyResponse.RecentlyRobbed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_RECENTLY_ROBBED));
			Utils.PlayErrorSound();
		}
		else if (robberyInfo.Response == RobberyResponse.NotEnoughCops)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_NOT_ENOUGH_COPS, BusinessScript.ClosestBusiness.RobberyMinCops));
			Utils.PlayErrorSound();
		}
		else
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x85-{(int)robberyInfo.Response}"));
		}
		EmployeeStopBeingRobbed(CurrentRobbery.Employee, attack);
		BusinessScript.ClosestBusiness.IsBeingRobbed = false;
		CurrentRobbery = null;
	}

	private async void JoinRobbery(bool scareEmployee = true)
	{
		if (BusinessScript.ClosestBusiness == null)
		{
			return;
		}
		RobberyInfo robberyInfo = (await TriggerServerEventAsync<string>("gtacnr:businesses:robbery:start", new object[3]
		{
			BusinessScript.ClosestBusiness.Id,
			-1,
			true
		})).Unjson<RobberyInfo>();
		if (scareEmployee && robberyEmployee != null)
		{
			EmployeeSetScared(robberyEmployee, force: true);
		}
		switch (robberyInfo.Response)
		{
		case RobberyResponse.Joined:
			CurrentRobbery = new RobberyState
			{
				Business = BusinessScript.ClosestBusiness,
				Employee = robberyEmployee
			};
			OnJoined(robberyInfo);
			break;
		case RobberyResponse.CannotBeRobbed:
			switch (BusinessScript.ClosestBusiness.Type)
			{
			case BusinessType.Jewelry:
				if (BusinessScript.ClosestBusiness.JewelryRobbery != null)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Jobs.JEWELRY_ROBBERY_NOT_IDEAL));
				}
				else
				{
					Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_CANNOT_BE_ROBBED));
				}
				Utils.PlayErrorSound();
				break;
			case BusinessType.GunStore:
			case BusinessType.GunStoreWithShootingRange:
			case BusinessType.Bank:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_CANNOT_BE_ROBBED_YET));
				Utils.PlayErrorSound();
				break;
			default:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_CANNOT_BE_ROBBED));
				Utils.PlayErrorSound();
				break;
			}
			break;
		case RobberyResponse.NotEnoughCops:
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.BUSINESS_NOT_ENOUGH_COPS, BusinessScript.ClosestBusiness.RobberyMinCops));
			Utils.PlayErrorSound();
			break;
		default:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x86-{(int)robberyInfo.Response}"));
			CurrentRobbery = null;
			break;
		case RobberyResponse.NotInProgress:
		case RobberyResponse.AlreadyInRobbery:
			break;
		}
	}

	private void OnJoined(RobberyInfo data)
	{
		intimidationBox = new BarTimerBar(LocalizationController.S(Entries.Jobs.ROBBERY_INTIMIDATION_BAR))
		{
			Percentage = data.Intimidation
		};
		counterMoneyBox = new TextTimerBar(LocalizationController.S(Entries.Jobs.ROBBERY_TAKE_COUNTER), "$0")
		{
			TextColor = TextColors.Green
		};
		TimerBarScript.AddTimerBar(intimidationBox);
		TimerBarScript.AddTimerBar(counterMoneyBox);
		if (CurrentRobbery.Business.SafeData != null)
		{
			if (Game.PlayerPed.IsInVehicle())
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.ROBBERY_WAIT_PARTNER_CRACK_SAFE));
			}
			else
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.ROBBERY_FIND_SAFE_HINT));
			}
			SafeRobberyScript.Instance.EnableSafe();
		}
		else if (Game.PlayerPed.IsInVehicle())
		{
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.ROBBERY_WAIT_PARTNER));
		}
		else if (CurrentRobbery.Business.Employees.Any((BusinessEmployee e) => e.Role == EmployeeRole.SecurityGuard))
		{
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.ROBBERY_ELIMINATE_SECURITY));
		}
		else
		{
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.ROBBERY_HELP_PARTNER));
		}
		foreach (BusinessEmployee employee in CurrentRobbery.Business.Employees)
		{
			if (employee.Role == EmployeeRole.SecurityGuard)
			{
				EmployeeAttackPlayer(employee);
			}
		}
		ShopRobberyScript.RobberyJoined?.Invoke(this, new EventArgs());
	}

	private async void EndRobbery()
	{
		if (CurrentRobbery == null)
		{
			return;
		}
		robberyEndTimestamp = DateTime.UtcNow;
		if (counterMoneyBox != null)
		{
			TimerBarScript.RemoveTimerBar(counterMoneyBox);
		}
		if (intimidationBox != null)
		{
			TimerBarScript.RemoveTimerBar(intimidationBox);
		}
		if (yourCutBox != null)
		{
			TimerBarScript.RemoveTimerBar(yourCutBox);
		}
		if (safeMoneyBox != null)
		{
			TimerBarScript.RemoveTimerBar(safeMoneyBox);
		}
		counterMoneyBox = null;
		safeMoneyBox = null;
		yourCutBox = null;
		intimidationBox = null;
		foreach (BarTimerBar value in playerHealthBoxes.Values)
		{
			TimerBarScript.RemoveTimerBar(value);
		}
		playerHealthBoxes.Clear();
		SafeRobberyScript.Instance.EndImmediately();
		lastRobbery = CurrentRobbery;
		CurrentRobbery = null;
		await BaseScript.Delay(5000);
		lastRobbery.Business.ResetRobberyState();
		ShopRobberyScript.RobberyEnded?.Invoke(this, new EventArgs());
	}

	[EventHandler("gtacnr:businesses:robbery:update")]
	private void OnRobberyUpdate(string robberyJson)
	{
		if (CurrentRobbery == null)
		{
			return;
		}
		try
		{
			CurrentRobbery.Info = robberyJson.Unjson<RobberyInfo>();
			counterMoneyBox.Text = CurrentRobbery.Info.MoneyTakenFromCounter.ToCurrencyString() ?? "";
			InterpolationTask();
			if (CurrentRobbery.Info.Players.Count > 1)
			{
				int amount = CurrentRobbery.Info.CalculateCut();
				if (yourCutBox == null)
				{
					yourCutBox = new TextTimerBar(LocalizationController.S(Entries.Jobs.ROBBERY_CUT_COUNTER), amount.ToCurrencyString() ?? "");
					yourCutBox.TextColor = (CurrentRobbery.PoliceCalled ? TextColors.Orange : TextColors.Green);
					counterMoneyBox.TextColor = TextColors.White;
					TimerBarScript.AddTimerBar(yourCutBox);
				}
				yourCutBox.Text = amount.ToCurrencyString() ?? "";
			}
			if (CurrentRobbery.Info.MoneyTakenFromSafe > 0)
			{
				if (safeMoneyBox == null)
				{
					safeMoneyBox = new TextTimerBar(LocalizationController.S(Entries.Jobs.ROBBERY_SAFE_COUNTER), CurrentRobbery.Info.MoneyTakenFromSafe.ToCurrencyString() ?? "");
					safeMoneyBox.TextColor = TextColors.White;
					counterMoneyBox.Label = LocalizationController.S(Entries.Jobs.ROBBERY_CASH_REGISTER_COUNTER);
					TimerBarScript.AddTimerBar(safeMoneyBox);
					if (yourCutBox != null)
					{
						TimerBarScript.RemoveTimerBar(yourCutBox);
						TimerBarScript.AddTimerBar(yourCutBox);
					}
				}
				safeMoneyBox.Text = CurrentRobbery.Info.MoneyTakenFromSafe.ToCurrencyString() ?? "";
			}
			foreach (int item in CurrentRobbery.Info.Players.Where((int p) => p != Game.Player.ServerId))
			{
				if (!playerHealthBoxes.ContainsKey(item))
				{
					PlayerState playerState = LatentPlayers.Get(item);
					playerHealthBoxes[item] = new BarTimerBar(playerState.ColorTextCode + playerState.Name.ToUpperInvariant());
					TimerBarScript.AddTimerBar(playerHealthBoxes[item], 0);
				}
				Player obj = ((BaseScript)this).Players[item];
				Ped val = ((obj != null) ? obj.Character : null);
				int num = ((val != null) ? ((Entity)val).Health : 0) + ((val != null) ? val.Armor : 0);
				BarTimerBar barTimerBar = playerHealthBoxes[item];
				int num2 = AntiHealthLockScript.MaxHealth - 100 + AntiHealthLockScript.MaxArmor;
				barTimerBar.Percentage = Gtacnr.Utils.ConvertRange(num, 0f, num2, 0f, 1f);
				if (barTimerBar.Percentage >= 0.6f)
				{
					barTimerBar.Color = BarColors.Blue;
				}
				else
				{
					barTimerBar.Color = BarColors.Red;
				}
			}
			if (CurrentRobbery.Info.IsCashRegisterEmpty)
			{
				TimerBarScript.RemoveTimerBar(intimidationBox);
				intimidationBox = null;
			}
			if (CurrentRobbery.Info.Players.Count <= 1)
			{
				if (yourCutBox != null)
				{
					TimerBarScript.RemoveTimerBar(yourCutBox);
					yourCutBox = null;
				}
				if (counterMoneyBox != null)
				{
					counterMoneyBox.TextColor = (CurrentRobbery.PoliceCalled ? TextColors.Orange : TextColors.Green);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		async void InterpolationTask()
		{
			if (CurrentRobbery != null && intimidationBox != null)
			{
				float percentage = intimidationBox.Percentage;
				float intimidation = CurrentRobbery.Info.Intimidation;
				float diff = intimidation - percentage;
				int steps = 10;
				for (int i = 0; i < steps; i++)
				{
					await BaseScript.Delay(Convert.ToInt32(800f / (float)steps));
					if (CurrentRobbery == null || intimidationBox == null)
					{
						break;
					}
					intimidationBox.Percentage += diff / (float)steps;
					if (intimidationBox.Percentage >= 0.99f)
					{
						intimidationBox.Color = BarColors.Green;
						intimidationBox.TextColor = TextColors.Green;
					}
					else if (intimidationBox.Percentage > 0.6f)
					{
						intimidationBox.Color = BarColors.White;
						intimidationBox.TextColor = TextColors.White;
					}
					else if (intimidationBox.Percentage > 0.4f)
					{
						intimidationBox.Color = BarColors.Orange;
						intimidationBox.TextColor = TextColors.Orange;
					}
					else
					{
						intimidationBox.Color = BarColors.Red;
						intimidationBox.TextColor = TextColors.Red;
					}
				}
			}
		}
	}

	[EventHandler("gtacnr:businesses:robbery:end")]
	private void OnRobberyEnd()
	{
		if (CurrentRobbery != null)
		{
			if (CurrentRobbery.Info.Players.Count > 1)
			{
				int amount = CurrentRobbery.Info.CalculateCut();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_COMPLETE_CUT, CurrentRobbery.Business.Name, amount.ToCurrencyString()));
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_COMPLETE, CurrentRobbery.Business.Name, CurrentRobbery.Info.TotalMoneyTaken.ToCurrencyString()));
			}
			EndRobbery();
		}
	}

	[EventHandler("gtacnr:businesses:robbery:failed")]
	private void OnRobberyFail(int reason)
	{
		if (CurrentRobbery == null)
		{
			return;
		}
		switch (reason)
		{
		case 1:
			if (CurrentRobbery.Info.Players.Count > 1)
			{
				int amount2 = CurrentRobbery.Info.CalculateCut();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CLERK_KILLED_CUT, amount2.ToCurrencyString()));
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CLERK_KILLED_COMPLETE, CurrentRobbery.Info.TotalMoneyTaken.ToCurrencyString()));
			}
			break;
		case 2:
			if (CurrentRobbery.Info.Players.Count > 1)
			{
				int amount = CurrentRobbery.Info.CalculateCut();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CLERK_NOT_INTIMIDATED_CUT, amount.ToCurrencyString()));
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_CLERK_NOT_INTIMIDATED_COMPLETE, CurrentRobbery.Info.TotalMoneyTaken.ToCurrencyString()));
			}
			break;
		}
		EndRobbery();
	}

	[EventHandler("gtacnr:businesses:robbery:fatalError")]
	private void OnRobberyFatalError()
	{
		if (CurrentRobbery != null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_ROBBERY_CANCELED));
			EndRobbery();
		}
	}

	[EventHandler("gtacnr:businesses:robbery:copsCalled")]
	private async void OnCopsCalled(int snitchPlayerId)
	{
		if (CurrentRobbery == null)
		{
			return;
		}
		CurrentRobbery.PoliceCalled = true;
		await BaseScript.Delay(2000);
		if (snitchPlayerId == 0)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_SHOTS_CALLED_POLICE));
		}
		else
		{
			PlayerState playerState = LatentPlayers.Get(snitchPlayerId);
			if (playerState.JobEnum.IsPolice())
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_PLAYER_SPOTTED, playerState.FullyFormatted));
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.ROBBERY_PLAYER_CALLED_POLICE, playerState.FullyFormatted));
			}
		}
		if (yourCutBox != null)
		{
			yourCutBox.TextColor = TextColors.Orange;
		}
		else
		{
			counterMoneyBox.TextColor = TextColors.Orange;
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onStarted")]
	private void OnRobberyStarted(string businessId, int employeeIdx, int playerId)
	{
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Expected O, but got Unknown
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		Business value = BusinessScript.Businesses.FirstOrDefault<KeyValuePair<string, Business>>((KeyValuePair<string, Business> k) => k.Key == businessId).Value;
		if (value == null)
		{
			return;
		}
		if (employeeIdx < value.Employees.Count)
		{
			value.IsBeingRobbed = true;
			EmployeeStartBeingRobbed(value.Employees[employeeIdx]);
		}
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (CurrentRobbery != null || closestBusiness == null || closestBusiness.Id != businessId || playerId == Game.Player.ServerId)
		{
			return;
		}
		PlayerState playerState = LatentPlayers.Get(playerId);
		string text = "~y~" + closestBusiness.Name + "~s~";
		Utils.SendNotification(LocalizationController.S(Entries.Jobs.ROBBING, playerState.FullyFormatted, text));
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			return;
		}
		Ped val = new Ped(API.GetPlayerPed(API.GetPlayerFromServerId(playerId)));
		if (!val.Exists())
		{
			return;
		}
		Vector3 position;
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)val.LastVehicle != (Entity)null && ((Entity)Game.PlayerPed.CurrentVehicle).NetworkId == ((Entity)val.LastVehicle).NetworkId)
		{
			position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)val).Position) < 2500f)
			{
				JoinRobbery();
			}
		}
		if (PartyScript.PartyMembers.Contains(playerId))
		{
			position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)val).Position) < 2500f)
			{
				JoinRobbery();
			}
		}
	}

	[EventHandler("gtacnr:robberies:employeeDied")]
	private void OnRobberyEmployeeDied(string businessId, int employeeIdx)
	{
		if (BusinessScript.Businesses.TryGetValue(businessId, out Business value))
		{
			BusinessEmployee businessEmployee = value.Employees[employeeIdx];
			if (businessEmployee.State != null && (Entity)(object)businessEmployee.State.Ped != (Entity)null && ((Entity)businessEmployee.State.Ped).IsAlive && !businessEmployee.State.DeathEventTriggered)
			{
				businessEmployee.State.DeathEventTriggered = true;
				businessEmployee.State.Ped.Kill();
			}
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onJoin")]
	private void OnRobberyJoin(string businessId, int playerId)
	{
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (CurrentRobbery != null && closestBusiness != null && !(closestBusiness.Id != businessId) && playerId != Game.Player.ServerId)
		{
			PlayerState playerState = LatentPlayers.Get(playerId);
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.JOINED_ROBBERY, playerState.FullyFormatted));
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onLeave")]
	private void OnRobberyLeave(string businessId, int playerId, int reason)
	{
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (CurrentRobbery != null && closestBusiness != null && !(closestBusiness.Id != businessId) && playerId != Game.Player.ServerId)
		{
			PlayerState playerState = LatentPlayers.Get(playerId);
			switch (reason)
			{
			case 1:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.DIED_ROBBERY, playerState.FullyFormatted));
				break;
			case 2:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.CUFFED_ROBBERY, playerState.FullyFormatted));
				break;
			default:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.PLAYER_ABANDONED_ROBBERY, playerState.FullyFormatted));
				break;
			}
			if (playerHealthBoxes.ContainsKey(playerId))
			{
				TimerBarScript.RemoveTimerBar(playerHealthBoxes[playerId]);
				playerHealthBoxes.Remove(playerId);
			}
		}
	}

	[EventHandler("gtacnr:businesses:robbery:onEnded")]
	private void OnRobberyEnded(string businessId, bool attack)
	{
		Business value = BusinessScript.Businesses.FirstOrDefault<KeyValuePair<string, Business>>((KeyValuePair<string, Business> k) => k.Key == businessId).Value;
		if (value == null)
		{
			return;
		}
		value.ResetRobberyState();
		if (value != BusinessScript.ClosestBusiness)
		{
			return;
		}
		foreach (BusinessEmployee employee in value.Employees)
		{
			attack = attack && lastRobbery?.Info.Players.Contains(Game.Player.ServerId) == true;
			EmployeeStopBeingRobbed(employee, attack);
		}
		BusinessScript.ClosestBusiness.IsBeingRobbed = false;
	}

	private void Intimidate(bool increaseLevel)
	{
		if (CurrentRobbery != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:intimidate", new object[2]
			{
				CurrentRobbery.Business.Id,
				increaseLevel
			});
		}
		BusinessEmployee employee = CurrentRobbery.Employee;
		if (employee != null && ((Entity)employee.State.Ped).IsAlive && Gtacnr.Utils.CheckTimePassed(employee.State.LastScaredT, 8000.0))
		{
			employee.State.LastScaredT = DateTime.UtcNow;
			employee.State.Ped.PlayAmbientSpeech("GENERIC_FRIGHTENED_MED", (SpeechModifier)3);
			PlayHandsUpAnimation(employee.State.Ped);
		}
	}

	private void CallPolice()
	{
		if (CurrentRobbery != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:callCops", new object[1] { CurrentRobbery.Business.Id });
		}
	}

	private void EmployeeStartBeingRobbed(BusinessEmployee employee)
	{
		if (!employee.State.IsBeingRobbed)
		{
			employee.State.IsBeingRobbed = true;
			EmployeeSetScared(employee);
		}
	}

	private void EmployeeStopBeingRobbed(BusinessEmployee employee, bool attack)
	{
		if (employee.State.IsBeingRobbed)
		{
			employee.State.IsBeingRobbed = false;
			if (attack || employee.Role == EmployeeRole.SecurityGuard)
			{
				EmployeeAttackPlayer(employee);
			}
			else
			{
				EmployeeSetScared(employee, force: true);
			}
		}
	}

	private async void EmployeeSetScared(BusinessEmployee employee, bool force = false)
	{
		Ped ped = employee?.State?.Ped;
		if ((Entity)(object)ped == (Entity)null || BusinessScript.ClosestBusiness == null)
		{
			return;
		}
		string key = BusinessScript.ClosestBusiness.Type.ToString();
		if (BusinessScript.BusinessTypes.ContainsKey(key) && BusinessScript.BusinessTypes[key].CashierFightsBack)
		{
			EmployeeAttackPlayer(employee);
			return;
		}
		if (Gtacnr.Utils.CheckTimePassed(employee.State.LastScaredT, 8000.0) && ((Entity)ped).IsAlive)
		{
			if (BusinessScript.ClosestBusiness.Type == BusinessType.GunStore || BusinessScript.ClosestBusiness.Type == BusinessType.GunStoreWithShootingRange)
			{
				string text;
				string text2;
				if (((Entity)ped).Model == Model.op_Implicit((PedHash)(-1643617475)))
				{
					text = "s_m_y_ammucity_01_white_01";
					text2 = new List<string> { "SHOP_SHOOTING", "STOP_SHOOTING" }.Random();
				}
				else
				{
					text = "s_m_m_ammucountry_01_white_01";
					text2 = "SHOP_SHOOTING";
				}
				API.PlayAmbientSpeechWithVoice(((PoolObject)ped).Handle, text2, text, "SPEECH_PARAMS_FORCE", false);
			}
			else
			{
				ped.PlayAmbientSpeech("GENERIC_FRIGHTENED_MED", (SpeechModifier)3);
			}
		}
		employee.State.LastScaredT = DateTime.UtcNow;
		if (!employee.State.IsScared || force)
		{
			employee.State.IsScared = true;
			if (employee.State.IsBeingRobbed)
			{
				PlayHandsUpAnimation(ped);
			}
			else
			{
				PlayScaredAnimation(ped);
			}
			while (!Gtacnr.Utils.CheckTimePassed(employee.State.LastScaredT, 30000.0))
			{
				await BaseScript.Delay(1000);
			}
			ClearScaredAnimation(ped);
			if (!employee.State.IsBeingRobbed)
			{
				ClearHandsUpAnimation(ped);
			}
			employee.State.IsScared = false;
		}
	}

	private async void EmployeeAttackPlayer(BusinessEmployee employee)
	{
		Ped enemyPed = employee.State.Ped;
		if (!((Entity)enemyPed).IsAlive || employee.State.IsAttacking)
		{
			return;
		}
		List<string> list = new List<string> { "weapon_combatpistol", "weapon_heavypistol", "weapon_pistol_mk2" };
		List<string> list2 = employee.Weapons ?? list;
		string text = list2[random.Next(list2.Count)];
		if (API.GetPedType(((PoolObject)enemyPed).Handle) != 28)
		{
			enemyPed.Weapons.Give((WeaponHash)Game.GenerateHash(text), 200, true, true);
			enemyPed.Armor = ((employee.Role == EmployeeRole.SecurityGuard) ? 300 : 0);
		}
		((Entity)enemyPed).Health = ((employee.Role == EmployeeRole.SecurityGuard) ? 300 : 200);
		((Entity)enemyPed).IsPositionFrozen = false;
		enemyPed.CanSufferCriticalHits = false;
		API.SetPedCombatAbility(((PoolObject)Game.PlayerPed).Handle, 2);
		enemyPed.Task.FightAgainst(Game.PlayerPed);
		enemyPed.AlwaysKeepTask = true;
		enemyPed.PlayAmbientSpeech("GENERIC_CURSE_HIGH", (SpeechModifier)3);
		employee.State.IsAttacking = true;
		int i = 0;
		int talkativity = Gtacnr.Utils.GetRandomInt(8, 16);
		while (true)
		{
			await BaseScript.Delay(1000);
			if (!API.DoesEntityExist(((PoolObject)enemyPed).Handle))
			{
				return;
			}
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num = ((Vector3)(ref position)).DistanceToSquared(API.GetEntityCoords(((PoolObject)enemyPed).Handle, true));
			if (!((Entity)Game.PlayerPed).IsAlive || Game.PlayerPed.IsBeingStunned || CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed || SurrenderScript.IsSurrendered || !((Entity)enemyPed).IsAlive || num > 2500f)
			{
				break;
			}
			if (i % talkativity == 0 && ((Entity)enemyPed).IsAlive)
			{
				enemyPed.PlayAmbientSpeech("GENERIC_WAR_CRY", (SpeechModifier)3);
			}
			i++;
		}
		API.ClearPedTasks(((PoolObject)enemyPed).Handle);
		API.SetCurrentPedWeapon(((PoolObject)enemyPed).Handle, (uint)API.GetHashKey("WEAPON_UNARMED"), true);
		employee.State.IsAttacking = false;
		BusinessScript.ClosestBusiness.ResetRobberyState();
	}

	private async void PlayScaredAnimation(Ped ped)
	{
		ClearHandsUpAnimation(ped);
		if (!API.IsEntityPlayingAnim(((PoolObject)ped).Handle, "anim@heists@ornate_bank@hostages@ped_c@", "flinch_intro", 3) && !API.IsEntityPlayingAnim(((PoolObject)ped).Handle, "anim@heists@ornate_bank@hostages@ped_c@", "flinch_loop", 3))
		{
			ped.Task.PlayAnimation("anim@heists@ornate_bank@hostages@ped_c@", "flinch_intro", 2f, -1, (AnimationFlags)2);
			await BaseScript.Delay(2000);
			ped.Task.ClearAnimation("anim@heists@ornate_bank@hostages@ped_c@", "flinch_intro");
			ped.Task.PlayAnimation("anim@heists@ornate_bank@hostages@ped_c@", "flinch_loop", 4f, -1, (AnimationFlags)1);
		}
	}

	private void ClearScaredAnimation(Ped ped)
	{
		ped.Task.ClearAnimation("anim@heists@ornate_bank@hostages@ped_c@", "flinch_intro");
		ped.Task.ClearAnimation("anim@heists@ornate_bank@hostages@ped_c@", "flinch_loop");
	}

	private void PlayHandsUpAnimation(Ped ped)
	{
		ClearScaredAnimation(ped);
		if (!API.IsEntityPlayingAnim(((PoolObject)ped).Handle, "missminuteman_1ig_2", "handsup_base", 3))
		{
			ped.Task.PlayAnimation("missminuteman_1ig_2", "handsup_base", 4f, -1, (AnimationFlags)1);
		}
	}

	private void ClearHandsUpAnimation(Ped ped)
	{
		ped.Task.ClearAnimation("missminuteman_1ig_2", "handsup_base");
	}
}
