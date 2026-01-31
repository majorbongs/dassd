using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Events.Holidays.Christmas;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Sync;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Microsoft.CSharp.RuntimeBinder;
using NativeUI;

namespace Gtacnr.Client.Jobs.Trucker;

public class TruckerJobScript : Script
{
	[CompilerGenerated]
	private static class _003C_003Eo__51
	{
		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__0;

		public static CallSite<Func<CallSite, object, bool, object>> _003C_003Ep__1;

		public static CallSite<Func<CallSite, object, bool>> _003C_003Ep__2;

		public static CallSite<Action<CallSite, object, bool>> _003C_003Ep__3;

		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__4;

		public static CallSite<Func<CallSite, Type, object, Vector3, bool, bool, object>> _003C_003Ep__5;

		public static CallSite<Func<CallSite, object, Prop>> _003C_003Ep__6;

		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__7;

		public static CallSite<Func<CallSite, object, int, object>> _003C_003Ep__8;

		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__9;

		public static CallSite<Func<CallSite, object, int, object>> _003C_003Ep__10;

		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__11;

		public static CallSite<Func<CallSite, object, int, object>> _003C_003Ep__12;

		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__13;

		public static CallSite<Func<CallSite, object, int, object>> _003C_003Ep__14;

		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__15;

		public static CallSite<Func<CallSite, object, int, object>> _003C_003Ep__16;

		public static CallSite<Func<CallSite, object, object>> _003C_003Ep__17;

		public static CallSite<Func<CallSite, object, int, object>> _003C_003Ep__18;

		public static CallSite<_003C_003EA<CallSite, Type, int, int, int, object, object, object, object, object, object, bool, bool, bool, bool, int, bool>> _003C_003Ep__19;

		public static CallSite<Action<CallSite, object, bool>> _003C_003Ep__20;
	}

	private static TruckerJobScript instance;

	private static DeliveryJob currentDelivery;

	private static Vehicle deliveryVehicle;

	private static int currentDeliveryIndex = -1;

	private Blip deliveryVehicleBlip;

	private static Blip parkingBlip;

	private TextTimerBar _timerBox;

	private TextTimerBar _payoutBox;

	private TextTimerBar _deliveriesBox;

	private BarTimerBar _vehicleHealthBar;

	private GameTime startGameTime;

	private bool hasIntentionallyDamagedGoods;

	private float currentWeight;

	private float markRot;

	private Dictionary<Model, float[]> defaultHandling = new Dictionary<Model, float[]>();

	private Dictionary<Vehicle, float> LastSetWeight = new Dictionary<Vehicle, float>();

	private const float parkingDistance = 3f;

	public static DeliveryJob CurrentDelivery => currentDelivery;

	public static Vehicle DeliveryVehicle => deliveryVehicle;

	public static int CurrentDeliveryIndex => currentDeliveryIndex;

	private TextTimerBar timerBox
	{
		get
		{
			return _timerBox;
		}
		set
		{
			_timerBox = TimerBarScript.SetTimerBar(_timerBox, value);
		}
	}

	private TextTimerBar payoutBox
	{
		get
		{
			return _payoutBox;
		}
		set
		{
			_payoutBox = TimerBarScript.SetTimerBar(_payoutBox, value);
		}
	}

	private TextTimerBar deliveriesBox
	{
		get
		{
			return _deliveriesBox;
		}
		set
		{
			_deliveriesBox = TimerBarScript.SetTimerBar(_deliveriesBox, value);
		}
	}

	private BarTimerBar vehicleHealthBar
	{
		get
		{
			return _vehicleHealthBar;
		}
		set
		{
			_vehicleHealthBar = TimerBarScript.SetTimerBar(_vehicleHealthBar, value);
		}
	}

	public static bool IsPlayerCloseToDropOff
	{
		get
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			if (currentDeliveryIndex != -1 && (Entity)(object)deliveryVehicle != (Entity)null && parkingBlip != (Blip)null)
			{
				Vector3 position = parkingBlip.Position;
				return ((Vector3)(ref position)).DistanceToSquared2D(((Entity)deliveryVehicle).Position) < 9f;
			}
			return false;
		}
	}

	public TruckerJobScript()
	{
		instance = this;
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		Vehicle vehicle = e.Vehicle;
		VehicleSeat seat = e.Seat;
		if (CanUseVehicleForTrucking(vehicle) && (int)seat == -1 && currentDelivery == null)
		{
			ShowActiveJobsPrompt();
		}
	}

	private async void ShowActiveJobsPrompt()
	{
		if (Gtacnr.Client.API.Jobs.CachedJob != "deliveryDriver")
		{
			return;
		}
		await TruckerMenuScript.ReloadAvailableJobs();
		IEnumerable<DeliveryJob> deliveryJobCache = TruckerMenuScript.DeliveryJobCache;
		if (deliveryJobCache.Count() > 0)
		{
			await InteractiveNotificationsScript.Show(LocalizationController.S(Entries.Jobs.DDRIVER_N_JOBS_AVAILABLE, deliveryJobCache.Count(), Utils.IsUsingKeyboard() ? LocalizationController.S(Entries.Businesses.STP_PRESS, "~INPUT_MP_TEXT_CHAT_TEAM~") : LocalizationController.S(Entries.Businesses.STP_HOLD, "~INPUT_REPLAY_SCREENSHOT~")), InteractiveNotificationType.HelpText, delegate
			{
				TruckerMenuScript.RefreshDeliveriesMenu(forceReload: false);
				TruckerMenuScript.OpenDeliveriesMenu();
				return true;
			}, TimeSpan.FromSeconds(10.0), 0u, LocalizationController.S(Entries.Jobs.DDRIVER_VIEW_JOBS), LocalizationController.S(Entries.Main.BTN_HOLD, LocalizationController.S(Entries.Jobs.DDRIVER_VIEW_JOBS)));
		}
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		if (e.CurrentJobEnum != JobsEnum.DeliveryDriver)
		{
			_ResetDeliveryJob();
		}
	}

	[EventHandler("gtacnr:time")]
	private void OnTime(int hour, int minute, int dayOfWeek)
	{
		if (currentDelivery == null)
		{
			return;
		}
		RefreshHealthBox();
		RefreshTimerBox();
		TruckerMenuScript.RefreshCurrentDeliveryMenu();
		RefreshVehicleWeight();
		if (deliveryVehicleBlip != (Blip)null)
		{
			if (Game.PlayerPed.IsInVehicle(deliveryVehicle))
			{
				deliveryVehicleBlip.Alpha = 0;
			}
			else
			{
				deliveryVehicleBlip.Alpha = 255;
			}
		}
	}

	[EventHandler("gtacnr:trucker:completed")]
	private void OnDeliveryJobCompleted(int totalEarned)
	{
		if (currentDelivery != null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_SUCCESS_MESSAGE, totalEarned.ToCurrencyString()));
			TruckerMenuScript.AddPastDelivery(currentDelivery, startGameTime, TimeSyncScript.GameTime);
			_ResetDeliveryJob();
			ShowActiveJobsPrompt();
		}
	}

	public static async Task AssignJob(DeliveryJob deliveryJob)
	{
		instance._AssignJob(deliveryJob);
	}

	private async Task _AssignJob(DeliveryJob deliveryJob)
	{
		ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:trucker:assignJob", deliveryJob.Id);
		switch (responseCode)
		{
		case ResponseCode.Success:
			StartDeliveryJob(deliveryJob);
			break;
		case ResponseCode.AlreadyOnMission:
			Utils.DisplayHelpText("~r~" + LocalizationController.S(Entries.Jobs.DDRIVER_ALREADY_ON_JOB), playSound: false);
			Utils.PlayErrorSound();
			break;
		case ResponseCode.MissionNoLongerAvailable:
			Utils.DisplayHelpText("~r~" + LocalizationController.S(Entries.Jobs.DDRIVER_JOB_NO_LONGER_AVAILABLE), playSound: false);
			Utils.PlayErrorSound();
			await TruckerMenuScript.ReloadAvailableJobs();
			break;
		default:
			Utils.DisplayError(responseCode, "", "_AssignJob");
			break;
		case ResponseCode.Cooldown:
			break;
		}
	}

	private void StartDeliveryJob(DeliveryJob deliveryJob, bool isResume = false)
	{
		currentDelivery = deliveryJob;
		timerBox = new TextTimerBar(LocalizationController.S(Entries.Main.MISSION_TIME_LEFT), "");
		TimerBarScript.AddTimerBar(timerBox);
		RefreshTimerBox();
		TruckerMenuScript.AddCurrentDeliveryMenuItem();
		if (!isResume)
		{
			NavigateToPickUpLocation();
			startGameTime = TimeSyncScript.GameTime;
		}
	}

	private void NavigateToPickUpLocation()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = currentDelivery.PickUpLocation.Coordinates.XYZ();
		Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_GO_TO_PICKUP, currentDelivery.PickUpLocation.Name));
		GPSScript.SetDestination("Pickup", position, 0f, shortRange: true, (BlipSprite)(currentDelivery.HasTrailer ? 479 : 478), (BlipColor)5, 255, autoDelete: true, 100f, OnArrivedToPickupLocation);
		GPSScript.IsCurrentBlipLocked = true;
	}

	public static void ResetDeliveryJob()
	{
		instance._ResetDeliveryJob();
	}

	private void _ResetDeliveryJob()
	{
		if (deliveryVehicleBlip != (Blip)null)
		{
			((PoolObject)deliveryVehicleBlip).Delete();
			deliveryVehicleBlip = null;
		}
		if (parkingBlip != (Blip)null)
		{
			((PoolObject)parkingBlip).Delete();
			parkingBlip = null;
		}
		timerBox = null;
		payoutBox = null;
		deliveriesBox = null;
		vehicleHealthBar = null;
		if ((Entity)(object)deliveryVehicle != (Entity)null)
		{
			ResetVehicleWeight(deliveryVehicle);
			if (((Entity)deliveryVehicle).IsAttached())
			{
				Entity entityAttachedTo = ((Entity)deliveryVehicle).GetEntityAttachedTo();
				Vehicle vehicle = (Vehicle)(object)((entityAttachedTo is Vehicle) ? entityAttachedTo : null);
				ResetVehicleWeight(vehicle);
			}
			if ((Entity)(object)deliveryVehicle != (Entity)(object)ActiveVehicleScript.ActiveVehicle)
			{
				int handle = ((PoolObject)deliveryVehicle).Handle;
				API.SetEntityAsNoLongerNeeded(ref handle);
			}
		}
		currentDelivery = null;
		currentDeliveryIndex = -1;
		deliveryVehicle = null;
		hasIntentionallyDamagedGoods = false;
		GPSScript.IsCurrentBlipLocked = false;
		GPSScript.ClearDestination();
		TruckerMenuScript.RemoveCurrentDeliveryMenuItem();
	}

	private async void OnArrivedToPickupLocation()
	{
		Vector4 pickupLoc = currentDelivery.PickUpLocation.Coordinates;
		if (currentDelivery.HasTrailer)
		{
			string[] collection = ((currentDelivery.Type != DeliveryJobType.Fuel) ? ((currentDelivery.Type != DeliveryJobType.Logs) ? ((currentDelivery.Type != DeliveryJobType.Special) ? ((!ChristmasScript.IsChristmas) ? new string[3] { "trailers", "trailers2", "trailers4" } : new string[1] { "trailers5" }) : new string[1] { "trailerlarge" }) : new string[1] { "trailerlogs" }) : new string[2] { "tanker", "tanker2" });
			API.ClearAreaOfVehicles(pickupLoc.X, pickupLoc.Y, pickupLoc.Z, 5f, false, false, false, false, false);
			deliveryVehicle = await World.CreateVehicle(Model.op_Implicit(collection.Random()), pickupLoc.XYZ(), pickupLoc.W);
			AntiEntitySpawnScript.RegisterEntity((Entity)(object)deliveryVehicle);
			API.SetEntityAsMissionEntity(((PoolObject)deliveryVehicle).Handle, true, true);
			deliveryVehicleBlip = ((Entity)deliveryVehicle).AttachBlip();
			deliveryVehicleBlip.Sprite = (BlipSprite)479;
			deliveryVehicleBlip.Color = (BlipColor)5;
			deliveryVehicleBlip.Scale = 0.9f;
			Utils.SetBlipName(deliveryVehicleBlip, "Trailer", "delivery_vehicle");
			vehicleHealthBar = new BarTimerBar(LocalizationController.S(Entries.Jobs.DDRIVER_TRAILER_HEALTH));
			TimerBarScript.AddTimerBar(vehicleHealthBar);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_PICK_UP_TRAILER));
			base.Update += DrawVehicleMarkerTask;
			try
			{
				while (true)
				{
					Vehicle obj = deliveryVehicle;
					if (obj != null && !((Entity)obj).IsAttached())
					{
						if (currentDelivery == null || (Entity)(object)deliveryVehicle == (Entity)null || ((Entity)deliveryVehicle).IsDead || !deliveryVehicle.Exists())
						{
							return;
						}
						await BaseScript.Delay(100);
						continue;
					}
					break;
				}
			}
			finally
			{
				base.Update -= DrawVehicleMarkerTask;
			}
		}
		else
		{
			bool notified = false;
			DeliveryJobVehicleType requiredType = Constants.DeliveryDriver.GetRequiredVehicleType(currentDelivery.Type);
			while (true)
			{
				if (!((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null))
				{
					DeliveryJobVehicleType? truckType;
					DeliveryJobVehicleType? deliveryJobVehicleType = (truckType = GetTruckType(Game.PlayerPed.CurrentVehicle));
					if (deliveryJobVehicleType.HasValue && requiredType.HasFlag(truckType))
					{
						break;
					}
				}
				if (!notified)
				{
					notified = true;
					Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_NEED_TRUCK_TYPE, Gtacnr.Utils.GetDescription(requiredType)));
				}
				await BaseScript.Delay(1000);
			}
			deliveryVehicle = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)ActiveVehicleScript.ActiveVehicle != (Entity)(object)deliveryVehicle)
			{
				API.SetEntityAsMissionEntity(((PoolObject)deliveryVehicle).Handle, true, true);
				deliveryVehicleBlip = ((Entity)deliveryVehicle).AttachBlip();
				deliveryVehicleBlip.Sprite = (BlipSprite)477;
				deliveryVehicleBlip.Color = (BlipColor)5;
				deliveryVehicleBlip.Scale = 0.8f;
				Utils.SetBlipName(deliveryVehicleBlip, "Truck");
			}
			vehicleHealthBar = new BarTimerBar(LocalizationController.S(Entries.Jobs.DDRIVER_TRUCK_HEALTH));
			TimerBarScript.AddTimerBar(vehicleHealthBar);
			Blip obj2 = parkingBlip;
			if (obj2 != null)
			{
				((PoolObject)obj2).Delete();
			}
			parkingBlip = World.CreateBlip(pickupLoc.XYZ());
			parkingBlip.Sprite = (BlipSprite)12;
			parkingBlip.Color = (BlipColor)5;
			parkingBlip.Scale = 1f;
			Utils.SetBlipName(parkingBlip, "Pickup", "delivery_pickup");
			base.Update += DrawParkingMarkerTask;
			try
			{
				Vector3 position;
				while (true)
				{
					position = ((Entity)deliveryVehicle).Position;
					if (!(((Vector3)(ref position)).DistanceToSquared(pickupLoc.XYZ()) > 3f.Square()))
					{
						break;
					}
					await BaseScript.Delay(1000);
					if (currentDelivery == null)
					{
						return;
					}
				}
				notified = false;
				int min = (pickupLoc.W - 15f).ToInt();
				int max = (pickupLoc.W + 15f).ToInt();
				while (true)
				{
					if (Gtacnr.Utils.IsAngleInRange(min, max, ((Entity)deliveryVehicle).Heading.ToInt()))
					{
						position = ((Entity)deliveryVehicle).Position;
						if (!(((Vector3)(ref position)).DistanceToSquared(pickupLoc.XYZ()) > 3f.Square()))
						{
							break;
						}
					}
					if (!notified)
					{
						Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_PARK_TRUCK_CORRECT_HEADING));
						notified = true;
					}
					await BaseScript.Delay(1000);
					if (currentDelivery == null)
					{
						return;
					}
				}
				while (deliveryVehicle.Speed.ToKmh() > 1f)
				{
					await BaseScript.Delay(100);
					if (currentDelivery == null)
					{
						return;
					}
				}
				((PoolObject)parkingBlip).Delete();
			}
			finally
			{
				base.Update -= DrawParkingMarkerTask;
			}
		}
		currentWeight = CurrentDelivery.Weight;
		SetVehicleWeight(deliveryVehicle, currentWeight);
		NavigateToDropOffLocation(currentDeliveryIndex = 0);
		ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:trucker:pickUpGoods", ((Entity)deliveryVehicle).NetworkId);
		if (responseCode != ResponseCode.Success)
		{
			Utils.DisplayError(responseCode, "", "OnArrivedToPickupLocation");
			_ResetDeliveryJob();
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_CANCELED_ERROR));
		}
	}

	private void NavigateToDropOffLocation(int locIdx)
	{
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		if (currentDelivery == null)
		{
			return;
		}
		if (currentDelivery.DropOffLocations.Count() > 1)
		{
			if (deliveriesBox == null)
			{
				deliveriesBox = new TextTimerBar(LocalizationController.S(Entries.Jobs.DDRIVER_NUM_DELIVERIES), $"0/{currentDelivery.DropOffLocations.Count()}");
				TimerBarScript.AddTimerBar(deliveriesBox);
			}
			else
			{
				deliveriesBox.Text = $"{locIdx}/{currentDelivery.DropOffLocations.Count()}";
			}
		}
		List<DeliveryJobLocation> list = currentDelivery.DropOffLocations.ToList();
		if (locIdx < list.Count)
		{
			if (locIdx != 0)
			{
				currentWeight -= CurrentDelivery.Weight / (float)list.Count;
				SetVehicleWeight(deliveryVehicle, currentWeight);
			}
			DeliveryJobLocation deliveryJobLocation = list[locIdx];
			Utils.DisplaySubtitle(LocalizationController.S((CurrentDelivery.Type == DeliveryJobType.Parcel) ? Entries.Jobs.DDRIVER_JOB_GO_TO_DROPOFF_PARCEL : Entries.Jobs.DDRIVER_JOB_GO_TO_DROPOFF, deliveryJobLocation.Name));
			float autoDeleteRange = ((CurrentDelivery.Type == DeliveryJobType.Parcel) ? 50f : ((CurrentDelivery.Type == DeliveryJobType.Restock) ? 75f : 100f));
			GPSScript.IsCurrentBlipLocked = false;
			GPSScript.SetDestination("Dropoff", deliveryJobLocation.Coordinates.XYZ(), 0f, shortRange: true, (BlipSprite)38, (BlipColor)5, 255, autoDelete: true, autoDeleteRange, OnArrivedToDropoffLocation);
			GPSScript.IsCurrentBlipLocked = true;
		}
	}

	private async void OnArrivedToDropoffLocation()
	{
		if ((Entity)(object)deliveryVehicle == (Entity)null || !deliveryVehicle.Exists())
		{
			Utils.DisplayError(ResponseCode.InvalidVehicle, "", "OnArrivedToDropoffLocation");
			_ResetDeliveryJob();
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_CANCELED_ERROR));
			return;
		}
		bool notified = false;
		Vector3 position;
		while (true)
		{
			position = ((Entity)deliveryVehicle).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position) > 20f.Square()))
			{
				break;
			}
			if (!notified)
			{
				notified = true;
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_RECOVER_VEHICLE));
			}
			await BaseScript.Delay(1000);
			if (currentDelivery == null)
			{
				return;
			}
		}
		Vector4 currentDeliveryLocation = currentDelivery.DropOffLocations.ToArray()[currentDeliveryIndex].Coordinates;
		Prop boxProp;
		dynamic package;
		if (currentDelivery.Type == DeliveryJobType.Parcel)
		{
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_DELIVER_PACKAGE));
			Blip obj = parkingBlip;
			if (obj != null)
			{
				((PoolObject)obj).Delete();
			}
			parkingBlip = World.CreateBlip(currentDeliveryLocation.XYZ());
			parkingBlip.Sprite = (BlipSprite)12;
			parkingBlip.Color = (BlipColor)5;
			parkingBlip.Scale = 1f;
			Utils.SetBlipName(parkingBlip, "Dropoff", "delivery_dropoff");
			base.Update += DrawPackageDropMarkerTask;
			boxProp = null;
			try
			{
				package = new List<object>
				{
					new
					{
						PropModel = "prop_cs_box_clothes",
						Offsets = new float[6] { 0.23f, 0.03f, -0.21f, -122f, 66f, -13f },
						IsHeavy = false
					},
					new
					{
						PropModel = "prop_cs_package_01",
						Offsets = new float[6] { 0.205f, 0.065f, -0.24f, -114f, 74.5f, -11f },
						IsHeavy = false
					},
					new
					{
						PropModel = "v_serv_abox_02",
						Offsets = new float[6] { 0.155f, -0.065f, -0.215f, -103.2f, -14.5f, 21.4f },
						IsHeavy = false
					},
					new
					{
						PropModel = "prop_cardbordbox_02a",
						Offsets = new float[6] { 0.05f, 0f, -0.25f, -105.6f, -18.3f, 4.4f },
						IsHeavy = false
					},
					new
					{
						PropModel = "prop_cardbordbox_03a",
						Offsets = new float[6] { 0.02f, 0.5f, -0.4f, 101f, 164f, -26f },
						IsHeavy = true
					}
				}.Random();
				while (true)
				{
					IL_0329:
					notified = false;
					if ((Entity)(object)boxProp != (Entity)null)
					{
						((PoolObject)boxProp).Delete();
						boxProp = null;
						while (!Game.PlayerPed.IsInVehicle())
						{
							if (!notified)
							{
								notified = true;
								Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_RECOVER_PACKAGE));
							}
							await BaseScript.Delay(500);
							if (currentDelivery == null)
							{
								return;
							}
						}
					}
					while (Game.PlayerPed.IsInVehicle())
					{
						await BaseScript.Delay(1000);
						if (currentDelivery == null)
						{
							return;
						}
					}
					await BaseScript.Delay(500);
					SetWalkStyle();
					PlayAnim();
					await CreateBox();
					AttachBox();
					Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
					while (true)
					{
						position = ((Entity)Game.PlayerPed).Position;
						if (!(((Vector3)(ref position)).DistanceToSquared(currentDeliveryLocation.XYZ()) > 1.2f.Square()))
						{
							break;
						}
						await BaseScript.Delay(50);
						if ((int)Weapon.op_Implicit(Game.PlayerPed.Weapons.Current) != -1569615261 && ((Entity)boxProp).IsAttached())
						{
							Game.PlayerPed.Task.ClearAnimation("anim@heists@box_carry@", "idle");
							API.ResetPedMovementClipset(((PoolObject)Game.PlayerPed).Handle, 0.2f);
							goto IL_0329;
						}
						if ((int)Weapon.op_Implicit(Game.PlayerPed.Weapons.Current) == -1569615261 && !((Entity)boxProp).IsAttached())
						{
							AttachBox();
							PlayAnim();
							SetWalkStyle();
						}
						if (currentDelivery == null)
						{
							return;
						}
					}
					break;
				}
			}
			finally
			{
				base.Update -= DrawPackageDropMarkerTask;
				((PoolObject)parkingBlip).Delete();
				if ((Entity)(object)boxProp != (Entity)null)
				{
					((Entity)boxProp).IsCollisionEnabled = true;
					((Entity)boxProp).Detach();
					((Entity)boxProp).MarkAsNoLongerNeeded();
				}
				API.ResetPedMovementClipset(((PoolObject)Game.PlayerPed).Handle, 0.2f);
				Game.PlayerPed.Task.ClearAnimation("anim@heists@box_carry@", "idle");
				((dynamic)((BaseScript)this).Exports["dpemotes"]).SetWalkingStyleLocked(false);
			}
		}
		else
		{
			if (currentDelivery.HasTrailer)
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_PARK_TRAILER_IN_AREA));
			}
			else
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_PARK_TRUCK_IN_AREA));
			}
			base.Update += DrawParkingMarkerTask;
			Blip obj2 = parkingBlip;
			if (obj2 != null)
			{
				((PoolObject)obj2).Delete();
			}
			parkingBlip = World.CreateBlip(currentDeliveryLocation.XYZ());
			parkingBlip.Sprite = (BlipSprite)12;
			parkingBlip.Color = (BlipColor)5;
			parkingBlip.Scale = 1f;
			Utils.SetBlipName(parkingBlip, "Dropoff", "delivery_dropoff");
			try
			{
				while (true)
				{
					position = ((Entity)deliveryVehicle).Position;
					float num;
					float distance = (num = ((Vector3)(ref position)).DistanceToSquared(currentDeliveryLocation.XYZ()));
					if (num > 6f.Square())
					{
						await BaseScript.Delay(500);
						if (distance < (float)40.Square())
						{
							API.ClearAreaOfVehicles(currentDeliveryLocation.X, currentDeliveryLocation.Y, currentDeliveryLocation.Z, 5f, false, false, false, false, false);
						}
						if (currentDelivery == null)
						{
							return;
						}
						continue;
					}
					while (true)
					{
						position = ((Entity)deliveryVehicle).Position;
						if (!(((Vector3)(ref position)).DistanceToSquared(currentDeliveryLocation.XYZ()) > 3f.Square()))
						{
							break;
						}
						await BaseScript.Delay(1000);
						if (currentDelivery == null)
						{
							return;
						}
					}
					notified = false;
					int min = (currentDeliveryLocation.W - 15f).ToInt();
					int max = (currentDeliveryLocation.W + 15f).ToInt();
					while (true)
					{
						if (Gtacnr.Utils.IsAngleInRange(min, max, ((Entity)deliveryVehicle).Heading.ToInt()))
						{
							position = ((Entity)deliveryVehicle).Position;
							if (!(((Vector3)(ref position)).DistanceToSquared(currentDeliveryLocation.XYZ()) > 3f.Square()))
							{
								break;
							}
						}
						if (!notified)
						{
							Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_PARK_TRUCK_CORRECT_HEADING));
							notified = true;
						}
						await BaseScript.Delay(1000);
						if (currentDelivery == null)
						{
							return;
						}
					}
					while (deliveryVehicle.Speed.ToKmh() > 1f)
					{
						await BaseScript.Delay(100);
						if (currentDelivery == null)
						{
							return;
						}
					}
					if (!Gtacnr.Utils.IsAngleInRange(min, max, ((Entity)deliveryVehicle).Heading.ToInt()))
					{
						continue;
					}
					position = ((Entity)deliveryVehicle).Position;
					if (((Vector3)(ref position)).DistanceToSquared(currentDeliveryLocation.XYZ()) > 3f.Square())
					{
						continue;
					}
					if (!currentDelivery.HasTrailer || currentDeliveryIndex != currentDelivery.DropOffLocations.Count() - 1)
					{
						break;
					}
					Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_DETACH_TRAILER));
					Utils.AddInstructionalButton("trailerDetach", new Gtacnr.Client.API.UI.InstructionalButton("Detach (hold)", 2, (Control)74));
					while (((Entity)deliveryVehicle).IsAttached())
					{
						await BaseScript.Delay(100);
						if (currentDelivery == null)
						{
							return;
						}
					}
					Utils.RemoveInstructionalButton("trailerDetach");
					if (Gtacnr.Utils.IsAngleInRange(min, max, ((Entity)deliveryVehicle).Heading.ToInt()))
					{
						position = ((Entity)deliveryVehicle).Position;
						if (!(((Vector3)(ref position)).DistanceToSquared(currentDeliveryLocation.XYZ()) > 3f.Square()))
						{
							break;
						}
					}
				}
			}
			finally
			{
				base.Update -= DrawParkingMarkerTask;
				((PoolObject)parkingBlip).Delete();
			}
		}
		NavigateToDropOffLocation(++currentDeliveryIndex);
		ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:trucker:dropOffGoods");
		if (responseCode != ResponseCode.Success)
		{
			Utils.DisplayError(responseCode, "", "OnArrivedToDropoffLocation");
			_ResetDeliveryJob();
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_CANCELED_ERROR));
		}
		void AttachBox()
		{
			API.AttachEntityToEntity(((PoolObject)boxProp).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 57005), package.Offsets[0], package.Offsets[1], package.Offsets[2], package.Offsets[3], package.Offsets[4], package.Offsets[5], true, true, false, true, 1, true);
		}
		unsafe async Task CreateBox()
		{
			Prop obj3 = boxProp;
			if (obj3 != null)
			{
				((PoolObject)obj3).Delete();
			}
			if (_003C_003Eo__51._003C_003Ep__6 == null)
			{
				_003C_003Eo__51._003C_003Ep__6 = CallSite<Func<CallSite, object, Prop>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(Prop), typeof(TruckerJobScript)));
			}
			Func<CallSite, object, Prop> target = _003C_003Eo__51._003C_003Ep__6.Target;
			CallSite<Func<CallSite, object, Prop>> _003C_003Ep__ = _003C_003Eo__51._003C_003Ep__6;
			dynamic awaiter = World.CreateProp(package.PropModel, ((Entity)Game.PlayerPed).Position, false, false).GetAwaiter();
			if (!(bool)awaiter.IsCompleted)
			{
				ICriticalNotifyCompletion awaiter2 = awaiter as ICriticalNotifyCompletion;
				AsyncTaskMethodBuilder asyncTaskMethodBuilder = default(AsyncTaskMethodBuilder);
				if (awaiter2 == null)
				{
					INotifyCompletion awaiter3 = (INotifyCompletion)(object)awaiter;
					asyncTaskMethodBuilder.AwaitOnCompleted(ref awaiter3, ref *(_003C_003Ec__DisplayClass51_0._003C_003COnArrivedToDropoffLocation_003Eg__CreateBox_007C2_003Ed*)/*Error near IL_024a: stateMachine*/);
				}
				else
				{
					asyncTaskMethodBuilder.AwaitUnsafeOnCompleted(ref awaiter2, ref *(_003C_003Ec__DisplayClass51_0._003C_003COnArrivedToDropoffLocation_003Eg__CreateBox_007C2_003Ed*)/*Error near IL_025d: stateMachine*/);
				}
				/*Error near IL_0266: leave MoveNext - await not detected correctly*/;
			}
			object result = awaiter.GetResult();
			boxProp = target(_003C_003Ep__, result);
			AntiEntitySpawnScript.RegisterEntity((Entity)(object)boxProp);
		}
		static async void PlayAnim()
		{
			await Game.PlayerPed.Task.PlayAnimation("anim@heists@box_carry@", "idle", 4f, -4f, -1, (AnimationFlags)51, 0f);
		}
		async void SetWalkStyle()
		{
			if (package.IsHeavy == true)
			{
				((dynamic)((BaseScript)this).Exports["dpemotes"]).SetWalkingStyleLocked(true);
				API.RequestAnimSet("anim_group_move_ballistic");
				while (!API.HasAnimSetLoaded("anim_group_move_ballistic"))
				{
					await BaseScript.Delay(10);
				}
				API.SetPedMovementClipset(((PoolObject)Game.PlayerPed).Handle, "anim_group_move_ballistic", 0.2f);
				API.RemoveAnimSet("anim_group_move_ballistic");
			}
		}
	}

	private void SetVehicleWeight(Vehicle vehicle, float weight)
	{
		if (!((Entity)(object)vehicle == (Entity)null) && (!LastSetWeight.ContainsKey(vehicle) || LastSetWeight[vehicle] != weight))
		{
			if (!defaultHandling.ContainsKey(((Entity)vehicle).Model))
			{
				defaultHandling[((Entity)vehicle).Model] = new float[3]
				{
					API.GetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fMass"),
					API.GetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fInitialDriveForce"),
					API.GetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fBrakeForce")
				};
			}
			float num = defaultHandling[((Entity)vehicle).Model][0];
			API.SetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fMass", num + weight);
			float num2 = defaultHandling[((Entity)vehicle).Model][1];
			float toB = num2 * 0.66f;
			float num3 = weight.ConvertRange(1000f, 30000f, num2, toB);
			API.SetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fInitialDriveForce", num3);
			float num4 = defaultHandling[((Entity)vehicle).Model][2];
			float toB2 = num4 * 0.001f;
			float num5 = weight.ConvertRange(1000f, 30000f, num4, toB2);
			API.SetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fBrakeForce", num5);
			LastSetWeight[vehicle] = weight;
		}
	}

	private void ResetVehicleWeight(Vehicle vehicle)
	{
		if (!((Entity)(object)vehicle == (Entity)null) && defaultHandling.ContainsKey(((Entity)vehicle).Model) && (!LastSetWeight.ContainsKey(vehicle) || LastSetWeight[vehicle] != defaultHandling[((Entity)vehicle).Model][0]))
		{
			API.SetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fMass", defaultHandling[((Entity)vehicle).Model][0]);
			API.SetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fInitialDriveForce", defaultHandling[((Entity)vehicle).Model][1]);
			API.SetVehicleHandlingFloat(((PoolObject)vehicle).Handle, "CHandlingData", "fBrakeForce", defaultHandling[((Entity)vehicle).Model][2]);
			LastSetWeight[vehicle] = defaultHandling[((Entity)vehicle).Model][0];
		}
	}

	private async Coroutine DrawVehicleMarkerTask()
	{
		if (currentDelivery == null || (Entity)(object)deliveryVehicle == (Entity)null || !deliveryVehicle.Exists() || Game.PlayerPed.IsInVehicle(deliveryVehicle))
		{
			return;
		}
		if (((Entity)deliveryVehicle).IsAttached())
		{
			Entity entityAttachedTo = ((Entity)deliveryVehicle).GetEntityAttachedTo();
			Entity obj = ((entityAttachedTo is Vehicle) ? entityAttachedTo : null);
			if ((Entity)(object)((obj != null) ? ((Vehicle)obj).Driver : null) == (Entity)(object)Game.PlayerPed)
			{
				return;
			}
		}
		Vector3 val = ((Entity)deliveryVehicle).Position + new Vector3(0f, 0f, 2.5f);
		Vector3 val2 = default(Vector3);
		((Vector3)(ref val2))._002Ector(1.2f, 1.2f, 1.2f);
		Color color = Color.FromUint(2863267968u);
		API.DrawMarker(0, val.X, val.Y, val.Z, 0f, 0f, 0f, 0f, 0f, 0f, val2.X, val2.Y, val2.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, true, true, 2, false, (string)null, (string)null, false);
	}

	private async Coroutine DrawPackageDropMarkerTask()
	{
		if (currentDelivery != null)
		{
			Vector4 val = ((currentDeliveryIndex < 0) ? currentDelivery.PickUpLocation.Coordinates : currentDelivery.DropOffLocations.ToArray()[currentDeliveryIndex].Coordinates);
			Vector3 val2 = default(Vector3);
			((Vector3)(ref val2))._002Ector(1f, 1f, 0.75f);
			Color color = Color.FromUint(2863267968u);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(val.X, val.Y, val.Z, ref z, false))
			{
				val.Z = z;
			}
			API.DrawMarker(1, val.X, val.Y, val.Z, 0f, 0f, 0f, 0f, 0f, 0f, val2.X, val2.Y, val2.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}

	private async Coroutine DrawParkingMarkerTask()
	{
		if (currentDelivery != null)
		{
			Vector4 val = ((currentDeliveryIndex < 0) ? currentDelivery.PickUpLocation.Coordinates : currentDelivery.DropOffLocations.ToArray()[currentDeliveryIndex].Coordinates);
			Vector3 val2 = default(Vector3);
			((Vector3)(ref val2))._002Ector(val.X, val.Y, val.Z + 0.7f);
			Vector3 val3 = default(Vector3);
			((Vector3)(ref val3))._002Ector(5f, 3.5f, 9f);
			Color color = Color.FromUint(2863267936u);
			API.DrawMarker(22, val2.X, val2.Y, val2.Z, 0f, 0f, 1f, 0f, 0f - val.W, markRot, val3.X, val3.Y, val3.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, false, 2, false, (string)null, (string)null, false);
			markRot += 2f;
		}
	}

	private async void RefreshHealthBox()
	{
		if (currentDelivery == null || (Entity)(object)deliveryVehicle == (Entity)null)
		{
			return;
		}
		if (!deliveryVehicle.Exists())
		{
			GoodsDestroyed();
			return;
		}
		float num = ((float)((Entity)deliveryVehicle).Health / (float)((Entity)deliveryVehicle).MaxHealth).Clamp(0f, 1f);
		if (((Entity)deliveryVehicle).IsDead || ((Entity)deliveryVehicle).IsOnFire)
		{
			num = 0f;
		}
		if (num == 0f)
		{
			GoodsDestroyed();
			return;
		}
		vehicleHealthBar.Percentage = num;
		vehicleHealthBar.TextColor = ((num > 0.7f) ? TextColors.White : ((num > 0.3f) ? TextColors.Orange : TextColors.Red));
		vehicleHealthBar.Color = ((num > 0.7f) ? BarColors.White : ((num > 0.3f) ? BarColors.Orange : BarColors.Red));
		async void GoodsDestroyed()
		{
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_GOODS_DESTROYED));
			_ResetDeliveryJob();
			await BaseScript.Delay(500);
			BaseScript.TriggerServerEvent("gtacnr:trucker:goodsDestroyed", new object[1] { hasIntentionallyDamagedGoods });
		}
	}

	public static void OnDamageVehicle(Vehicle vehicle, int weapon)
	{
		if ((Entity)(object)vehicle == (Entity)(object)deliveryVehicle)
		{
			instance.hasIntentionallyDamagedGoods = true;
		}
	}

	private void RefreshTimerBox()
	{
		if (currentDelivery == null)
		{
			return;
		}
		TimeSpan timeSpan = currentDelivery.GetDeliveryTimeLeft();
		if (timeSpan.TotalHours < 0.0)
		{
			timeSpan = timeSpan.Negate();
			timerBox.Label = LocalizationController.S(Entries.Main.MISSION_LATE);
			timerBox.TextColor = TextColors.Red;
			float val = (float)((TimeSyncScript.GameTime - currentDelivery.Deadline).TotalMinutes.ToInt() * currentDelivery.PaymentAmount) * 0.0012f;
			val = val.Clamp(0f, (float)currentDelivery.PaymentAmount * 0.6f);
			long amount = currentDelivery.PaymentAmount - val.ToLong();
			if (payoutBox == null)
			{
				payoutBox = new TextTimerBar(LocalizationController.S(Entries.Main.MISSION_PAYOUT), "");
				payoutBox.TextColor = TextColors.Red;
				TimerBarScript.AddTimerBar(payoutBox, TimerBarScript.TimerBars.ToList().IndexOf(timerBox));
			}
			payoutBox.Text = amount.ToCurrencyString();
		}
		else
		{
			timerBox.Label = LocalizationController.S(Entries.Main.MISSION_TIME_LEFT);
			timerBox.TextColor = ((timeSpan.TotalHours > 10.0) ? TextColors.White : ((timeSpan.TotalHours > 5.0) ? TextColors.Orange : TextColors.Red));
		}
		string text = $"{Math.Floor(timeSpan.TotalHours):00}:{timeSpan.Minutes:00}";
		timerBox.Text = text;
	}

	private void RefreshVehicleWeight()
	{
		if (currentDelivery == null || (Entity)(object)deliveryVehicle == (Entity)null || !deliveryVehicle.Exists())
		{
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle != (Entity)(object)deliveryVehicle && (Entity)(object)currentVehicle != (Entity)null)
		{
			if (((Entity)currentVehicle).IsAttachedTo((Entity)(object)deliveryVehicle) || ((Entity)deliveryVehicle).IsAttachedTo((Entity)(object)currentVehicle))
			{
				SetVehicleWeight(currentVehicle, currentWeight);
			}
			else
			{
				ResetVehicleWeight(currentVehicle);
			}
		}
	}

	public static bool CanUseVehicleForTrucking(Vehicle vehicle)
	{
		PersonalVehicleModel personalVehicleModel = DealershipScript.FindVehicleModelData(Model.op_Implicit(((Entity)vehicle).Model));
		if (personalVehicleModel == null)
		{
			return false;
		}
		if (!personalVehicleModel.HasExtraData("CanUseForTruckingJob"))
		{
			return false;
		}
		return personalVehicleModel.GetExtraDataBool("CanUseForTruckingJob");
	}

	public static DeliveryJobVehicleType? GetTruckType(Vehicle vehicle)
	{
		if ((Entity)(object)vehicle == (Entity)null)
		{
			return null;
		}
		PersonalVehicleModel personalVehicleModel = DealershipScript.FindVehicleModelData(Model.op_Implicit(((Entity)vehicle).Model));
		if (personalVehicleModel == null)
		{
			return null;
		}
		if (!personalVehicleModel.HasExtraData("TruckType"))
		{
			return null;
		}
		if (!Enum.TryParse<DeliveryJobVehicleType>(personalVehicleModel.GetExtraDataString("TruckType"), ignoreCase: false, out var result))
		{
			return null;
		}
		return result;
	}
}
