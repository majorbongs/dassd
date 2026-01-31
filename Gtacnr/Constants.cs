using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core.Native;
using Gtacnr.Client.Estates.Warehouses;
using Gtacnr.Data;
using Gtacnr.Model.Enums;

namespace Gtacnr;

public static class Constants
{
	public static class Menus
	{
		public const char SEP_START = '\u200b';

		public const char SEP_END = '\u200c';
	}

	public static class Jobs
	{
		public const string NONE = "none";

		public const string POLICE = "police";

		public const string PARAMEDIC = "paramedic";

		public const string FIREFIGHTER = "firefighter";

		public const string DRUG_DEALER = "drugDealer";

		public const string MECHANIC = "mechanic";

		public const string DELIVERY_DRIVER = "deliveryDriver";

		public const string HITMAN = "hitman";

		public const string PRIVATE_MEDIC = "privateMedic";

		public const string ARMS_DEALER = "armsDealer";
	}

	public static class Crime
	{
		public static readonly int[] TIMER = new int[5] { 300, 60, 80, 100, 240 };

		public static readonly float[] RANGE = new float[5] { 200f, 300f, 400f, 500f, 800f };

		public static readonly int MIN_BOUNTY = 20000;

		public static readonly int MAX_BOUNTY = 200000;

		public static readonly int MIN_FINE = 500;

		public static readonly int MAX_FINE = 200000;
	}

	public static class Pickpocketing
	{
		public static readonly int MAX_AMOUNT = 250000;
	}

	public static class ATM
	{
		public static readonly ReadonlyHashSet<uint> AtmPropHashes = new string[4] { "prop_atm_01", "prop_atm_02", "prop_atm_03", "prop_fleeca_atm" }.Select((string s) => (uint)API.GetHashKey(s)).ToHashSet().AsReadOnly();

		public static readonly TimeSpan HACK_COOLDOWN = TimeSpan.FromMinutes(5.0);

		public const int MIN_TRANSACTION_VALUE = 50000;

		public const int MAX_TRANSACTION_VALUE = 5000000;

		public const int FEE = 3500;

		public const int MIN_HACK_AMOUNT_UNLUCKY = 90000;

		public const int MAX_HACK_AMOUNT_UNLUCKY = 120000;

		public const int HACK_XP_UNLUCKY = 10;

		public const int MIN_HACK_AMOUNT_LUCKY = 155000;

		public const int MAX_HACK_AMOUNT_LUCKY = 190000;

		public const int HACK_XP_LUCKY = 15;
	}

	public static class Bank
	{
		public const float INTEREST_RATE = 0.0004f;

		public const long INTEREST_CAP = 100000L;

		public const int MIN_TRANSACTION_VALUE = 1;

		public const int MAX_TRANSACTION_VALUE = 100000000;
	}

	public static class EMS
	{
		public const float EMS_ON_SCENE_RANGE = 65f;

		public const int EMS_PING_HELP_COOLDOWN_MS = 30000;
	}

	public static class Arrest
	{
		public const int REWARD_WL_2 = 10000;

		public const int REWARD_WL_3 = 27500;

		public const int REWARD_WL_4 = 35000;

		public const int REWARD_WL_5 = 50000;

		public const int LOCKPICKING_XP_REWARD = 3;
	}

	public static class Takedown
	{
		public const int REWARD_WL_2 = 0;

		public const int REWARD_WL_3 = 8000;

		public const int REWARD_WL_4 = 15000;

		public const int REWARD_WL_5 = 27500;
	}

	public static class Revive
	{
		public const int REWARD_EMS = 20000;

		public const int REWARD_EMS_XP = 24;

		public const int REWARD_ASSIST_EMS = 10000;

		public const int REWARD_ASSIST_EMS_XP = 12;

		public const int REWARD_DOCTOR = 7500;

		public const int REWARD_DOCTOR_XP = 10;
	}

	public static class Mutes
	{
		public static readonly char[] VALID_TIME_UNITS = new char[3] { 'd', 'm', 'h' };
	}

	public static class DeliveryDriver
	{
		public const float CANCELLATION_FEE = 0.05f;

		public const float CANCELLATION_FEE_AFTER_PICKUP = 0.3f;

		public const float LATE_PENALTY_DETRACTION = 0.0012f;

		public const float LATE_PENALTY_MAX_DETRACTION = 0.6f;

		public const double COMPLETION_XP_DIVISOR = 5000.0;

		public const int COMPLETION_XP_MIN = 30;

		public const int COMPLETION_XP_MAX = 60;

		public static DeliveryJobVehicleType GetRequiredVehicleType(DeliveryJobType job)
		{
			switch (job)
			{
			case DeliveryJobType.Parcel:
				return DeliveryJobVehicleType.Van;
			case DeliveryJobType.Restock:
				return DeliveryJobVehicleType.BoxTruck;
			case DeliveryJobType.LongHaul:
				return DeliveryJobVehicleType.SemiTruck;
			case DeliveryJobType.Fuel:
				return DeliveryJobVehicleType.SemiTruck;
			case DeliveryJobType.Logs:
				return DeliveryJobVehicleType.SemiTruck;
			case DeliveryJobType.Special:
				return DeliveryJobVehicleType.SemiTruck;
			case DeliveryJobType.Trash:
				return DeliveryJobVehicleType.Trash;
			case DeliveryJobType.Food:
				return DeliveryJobVehicleType.Food;
			default:
			{
				global::_003CPrivateImplementationDetails_003E.ThrowInvalidOperationException();
				DeliveryJobVehicleType result = default(DeliveryJobVehicleType);
				return result;
			}
			}
		}
	}

	public static class Redzones
	{
		public const long COP_KILL_REWARD = 35000L;

		public const long EMT_KILL_REWARD = 15000L;
	}

	public static class HitmanContract
	{
		public const ulong MIN_REWARD = 50000uL;

		public const ulong MAX_REWARD_PER_PLAYER = 1000000uL;

		public const float HIT_FEE_ONSITE = 0.1f;

		public const float HIT_FEE_REMOTE = 0.2f;

		public static readonly TimeSpan AUTOMATIC_CONTRACT_COOLDOWN = TimeSpan.FromHours(24.0);
	}

	public static class SellMenu
	{
		public const float MIN_PRICE_MULT = 0.25f;

		public const float MAX_PRICE_MULT = 2.5f;
	}

	public static class Staff
	{
		public static readonly IReadonlyHashSet<int> StaffVehicles = new string[12]
		{
			"staffbuffalos", "staffbufsx", "staffglx", "staffvigeror", "staffgo4", "sled", "staffcara", "staffegt", "gstturc1", "bankbuf",
			"testrbufsx", "oppressor2"
		}.Select((string i) => API.GetHashKey(i)).ToReadOnlyHashSet();

		public static readonly IReadonlyHashSet<int> SharedStaffVehicles = new string[6] { "pbus2", "staffpotty", "staffcouch", "stafflimo", "staffrebla", "stockade" }.Select((string i) => API.GetHashKey(i)).ToReadOnlyHashSet();
	}

	public static class Moderation
	{
		public const int MAX_FINE_TMOD = 1000000;

		public const int MAX_FINE_MOD = 2500000;

		public const int MAX_FINE_LMOD = 5000000;
	}

	public static class SessionStats
	{
		public const string MONEY_MADE = "MONEY_MADE";

		public const string MONEY_SPENT = "MONEY_SPENT";

		public const string XP_GAINED = "XP_GAINED";

		public const string XP_LOST = "XP_LOST";

		public const string KILLS = "KILLS";

		public const string DEATHS = "DEATHS";

		public const string KD_RATIO = "KDR";

		public const string ARRESTS = "ARRESTS";

		public const string TICKETS = "TICKETS";

		public const string CONFISCATIONS = "CONFISCATIONS";

		public const string BRIBES = "BRIBES";

		public const string REVIVES = "REVIVES";

		public const string CRIMES = "CRIMES";

		public const string SALES = "SALES";

		public const string ROBBERIES = "ROBBERIES";

		public const string ATMS = "ATMS";

		public const string SAFES = "SAFES";

		public const string WALLETS = "WALLETS";

		public const string TOWS = "TOWS";

		public const string DELIVERIES = "DELIVERIES";

		public const string HITS = "HITS";

		public const string EXPORTATIONS = "EXPORTATIONS";

		public const string SCRAPPINGS = "SCRAPPINGS";

		public const string DISEASES_CONTRACTED = "DISEASES_CONTRACTED";

		public const string DISEASES_SPREAD = "DISEASES_SPREAD";

		public const string SPENT_HEALTHCARE = "SPENT_HEALTHCARE";

		public const string SPENT_BAIL = "SPENT_BAIL";

		public const string SPENT_TICKETS = "SPENT_TICKETS";

		public const string SPENT_BRIBES = "SPENT_BRIBES";
	}

	public static class DailyChallenges
	{
		public const int FETCH_COOLDOWN_MS = 30000;
	}

	public static class Food
	{
		public static readonly TimeSpan FoodConsumptionCooldown = TimeSpan.FromSeconds(2.0);
	}

	public static class Racing
	{
		public const float STARTING_POSITION_RADIUS = 20f;

		public const float MIN_DISTANCE_BETWEEN_CHECKPOINTS_SQ = 900f;
	}

	public static float GetInventoryCapacityByType(InventoryType inventoryType, string secondaryId = null)
	{
		switch (inventoryType)
		{
		case InventoryType.Primary:
			return 35000f;
		case InventoryType.Armory:
			return -1f;
		case InventoryType.Job:
			return Gtacnr.Data.Jobs.GetJobData(secondaryId)?.InventoryCapacity ?? (-1f);
		case InventoryType.Storage:
			if (WarehouseScript.GetWarehouse(secondaryId) != null)
			{
				return WarehouseScript.GetWarehouseInterior(WarehouseScript.GetWarehouse(secondaryId).InteriorId).Capacity;
			}
			return -1f;
		default:
			return -1f;
		}
	}
}
