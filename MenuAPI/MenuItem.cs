using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr;
using Gtacnr.Client.Libs;

namespace MenuAPI;

public class MenuItem
{
	public enum Icon
	{
		NONE,
		LOCK,
		STAR,
		WARNING,
		CROWN,
		MEDAL_BRONZE,
		MEDAL_GOLD,
		MEDAL_SILVER,
		CASH,
		COKE,
		HEROIN,
		METH,
		WEED,
		AMMO,
		ARMOR,
		BARBER,
		CLOTHING,
		FRANKLIN,
		BIKE,
		CAR,
		GUN,
		HEALTH_HEART,
		MAKEUP_BRUSH,
		MASK,
		MICHAEL,
		TATTOO,
		TICK,
		TREVOR,
		FEMALE,
		MALE,
		LOCK_ARENA,
		ADVERSARY,
		BASE_JUMPING,
		BRIEFCASE,
		MISSION_STAR,
		DEATHMATCH,
		CASTLE,
		TROPHY,
		RACE_FLAG,
		RACE_FLAG_PLANE,
		RACE_FLAG_BICYCLE,
		RACE_FLAG_PERSON,
		RACE_FLAG_CAR,
		RACE_FLAG_BOAT_ANCHOR,
		ROCKSTAR,
		STUNT,
		STUNT_PREMIUM,
		RACE_FLAG_STUNT_JUMP,
		SHIELD,
		TEAM_DEATHMATCH,
		VEHICLE_DEATHMATCH,
		MP_AMMO_PICKUP,
		MP_AMMO,
		MP_CASH,
		MP_RP,
		MP_SPECTATING,
		SALE,
		GLOBE_WHITE,
		GLOBE_RED,
		GLOBE_BLUE,
		GLOBE_YELLOW,
		GLOBE_GREEN,
		GLOBE_ORANGE,
		INV_ARM_WRESTLING,
		INV_BASEJUMP,
		INV_MISSION,
		INV_DARTS,
		INV_DEATHMATCH,
		INV_DRUG,
		INV_CASTLE,
		INV_GOLF,
		INV_BIKE,
		INV_BOAT,
		INV_ANCHOR,
		INV_CAR,
		INV_DOLLAR,
		INV_COKE,
		INV_KEY,
		INV_DATA,
		INV_HELI,
		INV_HEORIN,
		INV_KEYCARD,
		INV_METH,
		INV_BRIEFCASE,
		INV_LINK,
		INV_PERSON,
		INV_PLANE,
		INV_PLANE2,
		INV_QUESTIONMARK,
		INV_REMOTE,
		INV_SAFE,
		INV_STEER_WHEEL,
		INV_WEAPON,
		INV_WEED,
		INV_RACE_FLAG_PLANE,
		INV_RACE_FLAG_BICYCLE,
		INV_RACE_FLAG_BOAT_ANCHOR,
		INV_RACE_FLAG_PERSON,
		INV_RACE_FLAG_CAR,
		INV_RACE_FLAG_HELMET,
		INV_SHOOTING_RANGE,
		INV_SURVIVAL,
		INV_TEAM_DEATHMATCH,
		INV_TENNIS,
		INV_VEHICLE_DEATHMATCH,
		AUDIO_MUTE,
		AUDIO_INACTIVE,
		AUDIO_VOL1,
		AUDIO_VOL2,
		AUDIO_VOL3,
		COUNTRY_USA,
		COUNTRY_UK,
		COUNTRY_SWEDEN,
		COUNTRY_KOREA,
		COUNTRY_JAPAN,
		COUNTRY_ITALY,
		COUNTRY_GERMANY,
		COUNTRY_FRANCE,
		BRAND_ALBANY,
		BRAND_ANNIS,
		BRAND_BANSHEE,
		BRAND_BENEFACTOR,
		BRAND_BF,
		BRAND_BOLLOKAN,
		BRAND_BRAVADO,
		BRAND_BRUTE,
		BRAND_BUCKINGHAM,
		BRAND_CANIS,
		BRAND_CHARIOT,
		BRAND_CHEVAL,
		BRAND_CLASSIQUE,
		BRAND_COIL,
		BRAND_DECLASSE,
		BRAND_DEWBAUCHEE,
		BRAND_DILETTANTE,
		BRAND_DINKA,
		BRAND_DUNDREARY,
		BRAND_EMPORER,
		BRAND_ENUS,
		BRAND_FATHOM,
		BRAND_GALIVANTER,
		BRAND_GROTTI,
		BRAND_GROTTI2,
		BRAND_HIJAK,
		BRAND_HVY,
		BRAND_IMPONTE,
		BRAND_INVETERO,
		BRAND_JACKSHEEPE,
		BRAND_LCC,
		BRAND_JOBUILT,
		BRAND_KARIN,
		BRAND_LAMPADATI,
		BRAND_MAIBATSU,
		BRAND_MAMMOTH,
		BRAND_MTL,
		BRAND_NAGASAKI,
		BRAND_OBEY,
		BRAND_OCELOT,
		BRAND_OVERFLOD,
		BRAND_PED,
		BRAND_PEGASSI,
		BRAND_PFISTER,
		BRAND_PRINCIPE,
		BRAND_PROGEN,
		BRAND_PROGEN2,
		BRAND_RUNE,
		BRAND_SCHYSTER,
		BRAND_SHITZU,
		BRAND_SPEEDOPHILE,
		BRAND_STANLEY,
		BRAND_TRUFFADE,
		BRAND_UBERMACHT,
		BRAND_VAPID,
		BRAND_VULCAR,
		BRAND_WEENY,
		BRAND_WESTERN,
		BRAND_WESTERNMOTORCYCLE,
		BRAND_WILLARD,
		BRAND_ZIRCONIUM,
		BRAND_VYSSER,
		BRAND_MAXWELL,
		INFO,
		GTACNR_ACCESSIBILITY,
		GTACNR_ACCOUNT,
		GTACNR_AMMO,
		GTACNR_ARMORY,
		GTACNR_BAGS,
		GTACNR_BLADES,
		GTACNR_BLUNT_WEAPS,
		GTACNR_BOTTOMS,
		GTACNR_BRACELETS,
		GTACNR_CHAINS,
		GTACNR_CHANGE_PASS,
		GTACNR_DISCORD,
		GTACNR_DISPLAY,
		GTACNR_DRINKS,
		GTACNR_DRUGS,
		GTACNR_EARRINGS,
		GTACNR_RINGS,
		GTACNR_EMS,
		GTACNR_FIREMEN,
		GTACNR_FIVEM,
		GTACNR_FOOD,
		GTACNR_GAMBLING,
		GTACNR_GARAGES,
		GTACNR_GEAR,
		GTACNR_GLASSES,
		GTACNR_HAIRSTYLES,
		GTACNR_HANDGUNS,
		GTACNR_HATS,
		GTACNR_HEAVY_WEAPS,
		GTACNR_HELP,
		GTACNR_HITMAN,
		GTACNR_HOTKEYS,
		GTACNR_HOUSES,
		GTACNR_INVENTORY,
		GTACNR_JOB,
		GTACNR_JOB_CALLS,
		GTACNR_JOB_SALES,
		GTACNR_JOB_RADIO,
		GTACNR_LINK,
		GTACNR_LINK_EMAIL,
		GTACNR_LMGS,
		GTACNR_MASKS,
		GTACNR_MECHANIC,
		GTACNR_MEMBERSHIP,
		GTACNR_MOTELS,
		GTACNR_OPTIONS,
		GTACNR_OTHER,
		GTACNR_OUTFITS,
		GTACNR_PAPERWORK,
		GTACNR_POLICE,
		GTACNR_PROPERTIES,
		GTACNR_STATS,
		GTACNR_RIFLES,
		GTACNR_SERVICES,
		GTACNR_SHOES,
		GTACNR_SHOTGUNS,
		GTACNR_SMG,
		GTACNR_SPECIAL_FOOD,
		GTACNR_SPECIAL_WEAPS,
		GTACNR_STEAM,
		GTACNR_STOCK,
		GTACNR_TAXI,
		GTACNR_THROWABLES,
		GTACNR_TOOLS,
		GTACNR_TOPS,
		GTACNR_UNIFORMS,
		GTACNR_VEHICLES,
		GTACNR_WARDROBE,
		GTACNR_WAREHOUSE,
		GTACNR_WATCHES,
		GTACNR_REGISTRATION,
		GTACNR_REFRESH,
		XBOX_A,
		XBOX_B,
		XBOX_X,
		XBOX_Y,
		XBOX_LB,
		XBOX_RB,
		XBOX_LT,
		XBOX_RT,
		XBOX_LS,
		XBOX_RS,
		XBOX_DPAD,
		XBOX_MENU,
		XBOX_VIEW
	}

	private string _text;

	private string _label;

	private string _description;

	public static float RowWidth = 500f;

	public static float RowHeight = 38f;

	public static float RowSpacing = RowHeight;

	public string Id { get; set; }

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value.RegionalIndicatorsToLetters();
		}
	}

	public string Label
	{
		get
		{
			return _label;
		}
		set
		{
			_label = value.RegionalIndicatorsToLetters();
		}
	}

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			_description = value.RegionalIndicatorsToLetters();
		}
	}

	public Icon LeftIcon { get; set; }

	public Icon RightIcon { get; set; }

	public bool Enabled { get; set; } = true;

	public int Index
	{
		get
		{
			if (ParentMenu != null)
			{
				return ParentMenu.GetMenuItems().IndexOf(this);
			}
			return -1;
		}
	}

	public bool Selected
	{
		get
		{
			if (ParentMenu != null)
			{
				return ParentMenu.CurrentIndex == Index;
			}
			return false;
		}
	}

	public Menu ParentMenu { get; set; }

	public int PositionOnScreen { get; internal set; }

	public bool PlaySelectSound { get; set; } = true;

	public bool PlayErrorSound { get; set; } = true;

	public object ItemData { get; set; }

	public MenuItem(string text)
		: this(text, null)
	{
	}

	public MenuItem(string text, string description)
	{
		Text = text;
		Description = description;
	}

	protected string GetSpriteDictionary(Icon icon)
	{
		switch (icon)
		{
		case Icon.FEMALE:
		case Icon.MALE:
		case Icon.AUDIO_MUTE:
		case Icon.AUDIO_INACTIVE:
		case Icon.AUDIO_VOL1:
		case Icon.AUDIO_VOL2:
		case Icon.AUDIO_VOL3:
			return "mpleaderboard";
		case Icon.INV_ARM_WRESTLING:
		case Icon.INV_BASEJUMP:
		case Icon.INV_MISSION:
		case Icon.INV_DARTS:
		case Icon.INV_DEATHMATCH:
		case Icon.INV_DRUG:
		case Icon.INV_CASTLE:
		case Icon.INV_GOLF:
		case Icon.INV_BIKE:
		case Icon.INV_BOAT:
		case Icon.INV_ANCHOR:
		case Icon.INV_CAR:
		case Icon.INV_DOLLAR:
		case Icon.INV_COKE:
		case Icon.INV_KEY:
		case Icon.INV_DATA:
		case Icon.INV_HELI:
		case Icon.INV_HEORIN:
		case Icon.INV_KEYCARD:
		case Icon.INV_METH:
		case Icon.INV_BRIEFCASE:
		case Icon.INV_LINK:
		case Icon.INV_PERSON:
		case Icon.INV_PLANE:
		case Icon.INV_PLANE2:
		case Icon.INV_QUESTIONMARK:
		case Icon.INV_REMOTE:
		case Icon.INV_SAFE:
		case Icon.INV_STEER_WHEEL:
		case Icon.INV_WEAPON:
		case Icon.INV_WEED:
		case Icon.INV_RACE_FLAG_PLANE:
		case Icon.INV_RACE_FLAG_BICYCLE:
		case Icon.INV_RACE_FLAG_BOAT_ANCHOR:
		case Icon.INV_RACE_FLAG_PERSON:
		case Icon.INV_RACE_FLAG_CAR:
		case Icon.INV_RACE_FLAG_HELMET:
		case Icon.INV_SHOOTING_RANGE:
		case Icon.INV_SURVIVAL:
		case Icon.INV_TEAM_DEATHMATCH:
		case Icon.INV_TENNIS:
		case Icon.INV_VEHICLE_DEATHMATCH:
			return "mpinventory";
		case Icon.ADVERSARY:
		case Icon.BASE_JUMPING:
		case Icon.BRIEFCASE:
		case Icon.MISSION_STAR:
		case Icon.DEATHMATCH:
		case Icon.CASTLE:
		case Icon.TROPHY:
		case Icon.RACE_FLAG:
		case Icon.RACE_FLAG_PLANE:
		case Icon.RACE_FLAG_BICYCLE:
		case Icon.RACE_FLAG_PERSON:
		case Icon.RACE_FLAG_CAR:
		case Icon.RACE_FLAG_BOAT_ANCHOR:
		case Icon.ROCKSTAR:
		case Icon.STUNT:
		case Icon.STUNT_PREMIUM:
		case Icon.RACE_FLAG_STUNT_JUMP:
		case Icon.SHIELD:
		case Icon.TEAM_DEATHMATCH:
		case Icon.VEHICLE_DEATHMATCH:
			return "commonmenutu";
		case Icon.MP_AMMO_PICKUP:
		case Icon.MP_AMMO:
		case Icon.MP_CASH:
		case Icon.MP_RP:
		case Icon.MP_SPECTATING:
			return "mphud";
		case Icon.SALE:
			return "mpshopsale";
		case Icon.GLOBE_WHITE:
		case Icon.GLOBE_RED:
		case Icon.GLOBE_BLUE:
		case Icon.GLOBE_YELLOW:
		case Icon.GLOBE_GREEN:
		case Icon.GLOBE_ORANGE:
			return "mprankbadge";
		case Icon.COUNTRY_USA:
		case Icon.COUNTRY_UK:
		case Icon.COUNTRY_SWEDEN:
		case Icon.COUNTRY_KOREA:
		case Icon.COUNTRY_JAPAN:
		case Icon.COUNTRY_ITALY:
		case Icon.COUNTRY_GERMANY:
		case Icon.COUNTRY_FRANCE:
		case Icon.BRAND_ALBANY:
		case Icon.BRAND_ANNIS:
		case Icon.BRAND_BANSHEE:
		case Icon.BRAND_BENEFACTOR:
		case Icon.BRAND_BF:
		case Icon.BRAND_BOLLOKAN:
		case Icon.BRAND_BRAVADO:
		case Icon.BRAND_BRUTE:
		case Icon.BRAND_BUCKINGHAM:
		case Icon.BRAND_CANIS:
		case Icon.BRAND_CHARIOT:
		case Icon.BRAND_CHEVAL:
		case Icon.BRAND_CLASSIQUE:
		case Icon.BRAND_COIL:
		case Icon.BRAND_DECLASSE:
		case Icon.BRAND_DEWBAUCHEE:
		case Icon.BRAND_DILETTANTE:
		case Icon.BRAND_DINKA:
		case Icon.BRAND_DUNDREARY:
		case Icon.BRAND_EMPORER:
		case Icon.BRAND_ENUS:
		case Icon.BRAND_FATHOM:
		case Icon.BRAND_GALIVANTER:
		case Icon.BRAND_GROTTI:
		case Icon.BRAND_HIJAK:
		case Icon.BRAND_HVY:
		case Icon.BRAND_IMPONTE:
		case Icon.BRAND_INVETERO:
		case Icon.BRAND_JACKSHEEPE:
		case Icon.BRAND_JOBUILT:
		case Icon.BRAND_KARIN:
		case Icon.BRAND_LAMPADATI:
		case Icon.BRAND_MAIBATSU:
		case Icon.BRAND_MAMMOTH:
		case Icon.BRAND_MTL:
		case Icon.BRAND_NAGASAKI:
		case Icon.BRAND_OBEY:
		case Icon.BRAND_OCELOT:
		case Icon.BRAND_OVERFLOD:
		case Icon.BRAND_PED:
		case Icon.BRAND_PEGASSI:
		case Icon.BRAND_PFISTER:
		case Icon.BRAND_PRINCIPE:
		case Icon.BRAND_PROGEN:
		case Icon.BRAND_SCHYSTER:
		case Icon.BRAND_SHITZU:
		case Icon.BRAND_SPEEDOPHILE:
		case Icon.BRAND_STANLEY:
		case Icon.BRAND_TRUFFADE:
		case Icon.BRAND_UBERMACHT:
		case Icon.BRAND_VAPID:
		case Icon.BRAND_VULCAR:
		case Icon.BRAND_WEENY:
		case Icon.BRAND_WESTERN:
		case Icon.BRAND_WESTERNMOTORCYCLE:
		case Icon.BRAND_WILLARD:
		case Icon.BRAND_ZIRCONIUM:
			return "mpcarhud";
		case Icon.BRAND_GROTTI2:
		case Icon.BRAND_LCC:
		case Icon.BRAND_PROGEN2:
		case Icon.BRAND_RUNE:
			return "mpcarhud2";
		case Icon.BRAND_VYSSER:
			return "mpcarhud3";
		case Icon.BRAND_MAXWELL:
			return "mpcarhud4";
		case Icon.INFO:
			return "shared";
		case Icon.GTACNR_ACCESSIBILITY:
		case Icon.GTACNR_ACCOUNT:
		case Icon.GTACNR_AMMO:
		case Icon.GTACNR_ARMORY:
		case Icon.GTACNR_BAGS:
		case Icon.GTACNR_BLADES:
		case Icon.GTACNR_BLUNT_WEAPS:
		case Icon.GTACNR_BOTTOMS:
		case Icon.GTACNR_BRACELETS:
		case Icon.GTACNR_CHAINS:
		case Icon.GTACNR_CHANGE_PASS:
		case Icon.GTACNR_DISCORD:
		case Icon.GTACNR_DISPLAY:
		case Icon.GTACNR_DRINKS:
		case Icon.GTACNR_DRUGS:
		case Icon.GTACNR_EARRINGS:
		case Icon.GTACNR_RINGS:
		case Icon.GTACNR_EMS:
		case Icon.GTACNR_FIREMEN:
		case Icon.GTACNR_FIVEM:
		case Icon.GTACNR_FOOD:
		case Icon.GTACNR_GAMBLING:
		case Icon.GTACNR_GARAGES:
		case Icon.GTACNR_GEAR:
		case Icon.GTACNR_GLASSES:
		case Icon.GTACNR_HAIRSTYLES:
		case Icon.GTACNR_HANDGUNS:
		case Icon.GTACNR_HATS:
		case Icon.GTACNR_HEAVY_WEAPS:
		case Icon.GTACNR_HELP:
		case Icon.GTACNR_HITMAN:
		case Icon.GTACNR_HOTKEYS:
		case Icon.GTACNR_HOUSES:
		case Icon.GTACNR_INVENTORY:
		case Icon.GTACNR_JOB:
		case Icon.GTACNR_JOB_CALLS:
		case Icon.GTACNR_JOB_SALES:
		case Icon.GTACNR_JOB_RADIO:
		case Icon.GTACNR_LINK:
		case Icon.GTACNR_LINK_EMAIL:
		case Icon.GTACNR_LMGS:
		case Icon.GTACNR_MASKS:
		case Icon.GTACNR_MECHANIC:
		case Icon.GTACNR_MEMBERSHIP:
		case Icon.GTACNR_MOTELS:
		case Icon.GTACNR_OPTIONS:
		case Icon.GTACNR_OTHER:
		case Icon.GTACNR_OUTFITS:
		case Icon.GTACNR_PAPERWORK:
		case Icon.GTACNR_POLICE:
		case Icon.GTACNR_PROPERTIES:
		case Icon.GTACNR_STATS:
		case Icon.GTACNR_RIFLES:
		case Icon.GTACNR_SERVICES:
		case Icon.GTACNR_SHOES:
		case Icon.GTACNR_SHOTGUNS:
		case Icon.GTACNR_SMG:
		case Icon.GTACNR_SPECIAL_FOOD:
		case Icon.GTACNR_SPECIAL_WEAPS:
		case Icon.GTACNR_STEAM:
		case Icon.GTACNR_STOCK:
		case Icon.GTACNR_TAXI:
		case Icon.GTACNR_THROWABLES:
		case Icon.GTACNR_TOOLS:
		case Icon.GTACNR_TOPS:
		case Icon.GTACNR_UNIFORMS:
		case Icon.GTACNR_VEHICLES:
		case Icon.GTACNR_WARDROBE:
		case Icon.GTACNR_WAREHOUSE:
		case Icon.GTACNR_WATCHES:
		case Icon.GTACNR_REGISTRATION:
		case Icon.GTACNR_REFRESH:
		case Icon.XBOX_A:
		case Icon.XBOX_B:
		case Icon.XBOX_X:
		case Icon.XBOX_Y:
		case Icon.XBOX_LB:
		case Icon.XBOX_RB:
		case Icon.XBOX_LT:
		case Icon.XBOX_RT:
		case Icon.XBOX_LS:
		case Icon.XBOX_RS:
		case Icon.XBOX_DPAD:
		case Icon.XBOX_MENU:
		case Icon.XBOX_VIEW:
			return "gtacnr_menu";
		default:
			return "commonmenu";
		}
	}

	protected string GetSpriteName(Icon icon, bool selected)
	{
		switch (icon)
		{
		case Icon.AMMO:
			if (!selected)
			{
				return "shop_ammo_icon_a";
			}
			return "shop_ammo_icon_b";
		case Icon.ARMOR:
			if (!selected)
			{
				return "shop_armour_icon_a";
			}
			return "shop_armour_icon_b";
		case Icon.BARBER:
			if (!selected)
			{
				return "shop_barber_icon_a";
			}
			return "shop_barber_icon_b";
		case Icon.BIKE:
			if (!selected)
			{
				return "shop_garage_bike_icon_a";
			}
			return "shop_garage_bike_icon_b";
		case Icon.CAR:
			if (!selected)
			{
				return "shop_garage_icon_a";
			}
			return "shop_garage_icon_b";
		case Icon.CASH:
			return "mp_specitem_cash";
		case Icon.CLOTHING:
			if (!selected)
			{
				return "shop_clothing_icon_a";
			}
			return "shop_clothing_icon_b";
		case Icon.COKE:
			return "mp_specitem_coke";
		case Icon.CROWN:
			return "mp_hostcrown";
		case Icon.FRANKLIN:
			if (!selected)
			{
				return "shop_franklin_icon_a";
			}
			return "shop_franklin_icon_b";
		case Icon.GUN:
			if (!selected)
			{
				return "shop_gunclub_icon_a";
			}
			return "shop_gunclub_icon_b";
		case Icon.HEALTH_HEART:
			if (!selected)
			{
				return "shop_health_icon_a";
			}
			return "shop_health_icon_b";
		case Icon.HEROIN:
			return "mp_specitem_heroin";
		case Icon.LOCK:
			return "shop_lock";
		case Icon.MAKEUP_BRUSH:
			if (!selected)
			{
				return "shop_makeup_icon_a";
			}
			return "shop_makeup_icon_b";
		case Icon.MASK:
			if (!selected)
			{
				return "shop_mask_icon_a";
			}
			return "shop_mask_icon_b";
		case Icon.MEDAL_BRONZE:
			return "mp_medal_bronze";
		case Icon.MEDAL_GOLD:
			return "mp_medal_gold";
		case Icon.MEDAL_SILVER:
			return "mp_medal_silver";
		case Icon.METH:
			return "mp_specitem_meth";
		case Icon.MICHAEL:
			if (!selected)
			{
				return "shop_michael_icon_a";
			}
			return "shop_michael_icon_b";
		case Icon.STAR:
			return "shop_new_star";
		case Icon.TATTOO:
			if (!selected)
			{
				return "shop_tattoos_icon_a";
			}
			return "shop_tattoos_icon_b";
		case Icon.TICK:
			return "shop_tick_icon";
		case Icon.TREVOR:
			if (!selected)
			{
				return "shop_trevor_icon_a";
			}
			return "shop_trevor_icon_b";
		case Icon.WARNING:
			return "mp_alerttriangle";
		case Icon.WEED:
			return "mp_specitem_weed";
		case Icon.MALE:
			return "leaderboard_male_icon";
		case Icon.FEMALE:
			return "leaderboard_female_icon";
		case Icon.LOCK_ARENA:
			return "shop_lock_arena";
		case Icon.ADVERSARY:
			return "adversary";
		case Icon.BASE_JUMPING:
			return "base_jumping";
		case Icon.BRIEFCASE:
			return "capture_the_flag";
		case Icon.MISSION_STAR:
			return "custom_mission";
		case Icon.DEATHMATCH:
			return "deathmatch";
		case Icon.CASTLE:
			return "gang_attack";
		case Icon.TROPHY:
			return "last_team_standing";
		case Icon.RACE_FLAG:
			return "race";
		case Icon.RACE_FLAG_PLANE:
			return "race_air";
		case Icon.RACE_FLAG_BICYCLE:
			return "race_bicycle";
		case Icon.RACE_FLAG_PERSON:
			return "race_foot";
		case Icon.RACE_FLAG_CAR:
			return "race_land";
		case Icon.RACE_FLAG_BOAT_ANCHOR:
			return "race_water";
		case Icon.ROCKSTAR:
			return "rockstar";
		case Icon.STUNT:
			return "stunt";
		case Icon.STUNT_PREMIUM:
			return "stunt_premium";
		case Icon.RACE_FLAG_STUNT_JUMP:
			return "stunt_race";
		case Icon.SHIELD:
			return "survival";
		case Icon.TEAM_DEATHMATCH:
			return "team_deathmatch";
		case Icon.VEHICLE_DEATHMATCH:
			return "vehicle_deathmatch";
		case Icon.MP_AMMO_PICKUP:
			return "ammo_pickup";
		case Icon.MP_AMMO:
			return "mp_anim_ammo";
		case Icon.MP_CASH:
			return "mp_anim_cash";
		case Icon.MP_RP:
			return "mp_anim_rp";
		case Icon.MP_SPECTATING:
			return "spectating";
		case Icon.SALE:
			return "saleicon";
		case Icon.GLOBE_WHITE:
		case Icon.GLOBE_RED:
		case Icon.GLOBE_BLUE:
		case Icon.GLOBE_YELLOW:
		case Icon.GLOBE_GREEN:
		case Icon.GLOBE_ORANGE:
			return "globe";
		case Icon.INV_ARM_WRESTLING:
			return "arm_wrestling";
		case Icon.INV_BASEJUMP:
			return "basejump";
		case Icon.INV_MISSION:
			return "custom_mission";
		case Icon.INV_DARTS:
			return "darts";
		case Icon.INV_DEATHMATCH:
			return "deathmatch";
		case Icon.INV_DRUG:
			return "drug_trafficking";
		case Icon.INV_CASTLE:
			return "gang_attack";
		case Icon.INV_GOLF:
			return "golf";
		case Icon.INV_BIKE:
			return "mp_specitem_bike";
		case Icon.INV_BOAT:
			return "mp_specitem_boat";
		case Icon.INV_ANCHOR:
			return "mp_specitem_boatpickup";
		case Icon.INV_CAR:
			return "mp_specitem_car";
		case Icon.INV_DOLLAR:
			return "mp_specitem_cash";
		case Icon.INV_COKE:
			return "mp_specitem_coke";
		case Icon.INV_KEY:
			return "mp_specitem_cuffkeys";
		case Icon.INV_DATA:
			return "mp_specitem_data";
		case Icon.INV_HELI:
			return "mp_specitem_heli";
		case Icon.INV_HEORIN:
			return "mp_specitem_heroin";
		case Icon.INV_KEYCARD:
			return "mp_specitem_keycard";
		case Icon.INV_METH:
			return "mp_specitem_meth";
		case Icon.INV_BRIEFCASE:
			return "mp_specitem_package";
		case Icon.INV_LINK:
			return "mp_specitem_partnericon";
		case Icon.INV_PERSON:
			return "mp_specitem_ped";
		case Icon.INV_PLANE:
			return "mp_specitem_plane";
		case Icon.INV_PLANE2:
			return "mp_specitem_plane2";
		case Icon.INV_QUESTIONMARK:
			return "mp_specitem_randomobject";
		case Icon.INV_REMOTE:
			return "mp_specitem_remote";
		case Icon.INV_SAFE:
			return "mp_specitem_safe";
		case Icon.INV_STEER_WHEEL:
			return "mp_specitem_steer_wheel";
		case Icon.INV_WEAPON:
			return "mp_specitem_weapons";
		case Icon.INV_WEED:
			return "mp_specitem_weed";
		case Icon.INV_RACE_FLAG_PLANE:
			return "race_air";
		case Icon.INV_RACE_FLAG_BICYCLE:
			return "race_bike";
		case Icon.INV_RACE_FLAG_BOAT_ANCHOR:
			return "race_boat";
		case Icon.INV_RACE_FLAG_PERSON:
			return "race_foot";
		case Icon.INV_RACE_FLAG_CAR:
			return "race_land";
		case Icon.INV_RACE_FLAG_HELMET:
			return "race_offroad";
		case Icon.INV_SHOOTING_RANGE:
			return "shooting_range";
		case Icon.INV_SURVIVAL:
			return "survival";
		case Icon.INV_TEAM_DEATHMATCH:
			return "team_deathmatch";
		case Icon.INV_TENNIS:
			return "tennis";
		case Icon.INV_VEHICLE_DEATHMATCH:
			return "vehicle_deathmatch";
		case Icon.AUDIO_MUTE:
			return "leaderboard_audio_mute";
		case Icon.AUDIO_INACTIVE:
			return "leaderboard_audio_inactive";
		case Icon.AUDIO_VOL1:
			return "leaderboard_audio_1";
		case Icon.AUDIO_VOL2:
			return "leaderboard_audio_2";
		case Icon.AUDIO_VOL3:
			return "leaderboard_audio_3";
		case Icon.COUNTRY_USA:
			return "vehicle_card_icons_flag_usa";
		case Icon.COUNTRY_UK:
			return "vehicle_card_icons_flag_uk";
		case Icon.COUNTRY_SWEDEN:
			return "vehicle_card_icons_flag_sweden";
		case Icon.COUNTRY_KOREA:
			return "vehicle_card_icons_flag_korea";
		case Icon.COUNTRY_JAPAN:
			return "vehicle_card_icons_flag_japan";
		case Icon.COUNTRY_ITALY:
			return "vehicle_card_icons_flag_italy";
		case Icon.COUNTRY_GERMANY:
			return "vehicle_card_icons_flag_germany";
		case Icon.COUNTRY_FRANCE:
			return "vehicle_card_icons_flag_france";
		case Icon.BRAND_ALBANY:
			return "albany";
		case Icon.BRAND_ANNIS:
			return "annis";
		case Icon.BRAND_BANSHEE:
			return "banshee";
		case Icon.BRAND_BENEFACTOR:
			return "benefactor";
		case Icon.BRAND_BF:
			return "bf";
		case Icon.BRAND_BOLLOKAN:
			return "bollokan";
		case Icon.BRAND_BRAVADO:
			return "bravado";
		case Icon.BRAND_BRUTE:
			return "brute";
		case Icon.BRAND_BUCKINGHAM:
			return "buckingham";
		case Icon.BRAND_CANIS:
			return "canis";
		case Icon.BRAND_CHARIOT:
			return "chariot";
		case Icon.BRAND_CHEVAL:
			return "cheval";
		case Icon.BRAND_CLASSIQUE:
			return "classique";
		case Icon.BRAND_COIL:
			return "coil";
		case Icon.BRAND_DECLASSE:
			return "declasse";
		case Icon.BRAND_DEWBAUCHEE:
			return "dewbauchee";
		case Icon.BRAND_DILETTANTE:
			return "dilettante";
		case Icon.BRAND_DINKA:
			return "dinka";
		case Icon.BRAND_DUNDREARY:
			return "dundreary";
		case Icon.BRAND_EMPORER:
			return "emporer";
		case Icon.BRAND_ENUS:
			return "enus";
		case Icon.BRAND_FATHOM:
			return "fathom";
		case Icon.BRAND_GALIVANTER:
			return "galivanter";
		case Icon.BRAND_GROTTI:
			return "grotti";
		case Icon.BRAND_HIJAK:
			return "hijak";
		case Icon.BRAND_HVY:
			return "hvy";
		case Icon.BRAND_IMPONTE:
			return "imponte";
		case Icon.BRAND_INVETERO:
			return "invetero";
		case Icon.BRAND_JACKSHEEPE:
			return "jacksheepe";
		case Icon.BRAND_JOBUILT:
			return "jobuilt";
		case Icon.BRAND_KARIN:
			return "karin";
		case Icon.BRAND_LAMPADATI:
			return "lampadati";
		case Icon.BRAND_MAIBATSU:
			return "maibatsu";
		case Icon.BRAND_MAMMOTH:
			return "mammoth";
		case Icon.BRAND_MTL:
			return "mtl";
		case Icon.BRAND_NAGASAKI:
			return "nagasaki";
		case Icon.BRAND_OBEY:
			return "obey";
		case Icon.BRAND_OCELOT:
			return "ocelot";
		case Icon.BRAND_OVERFLOD:
			return "overflod";
		case Icon.BRAND_PED:
			return "ped";
		case Icon.BRAND_PEGASSI:
			return "pegassi";
		case Icon.BRAND_PFISTER:
			return "pfister";
		case Icon.BRAND_PRINCIPE:
			return "principe";
		case Icon.BRAND_PROGEN:
			return "progen";
		case Icon.BRAND_SCHYSTER:
			return "schyster";
		case Icon.BRAND_SHITZU:
			return "shitzu";
		case Icon.BRAND_SPEEDOPHILE:
			return "speedophile";
		case Icon.BRAND_STANLEY:
			return "stanley";
		case Icon.BRAND_TRUFFADE:
			return "truffade";
		case Icon.BRAND_UBERMACHT:
			return "ubermacht";
		case Icon.BRAND_VAPID:
			return "vapid";
		case Icon.BRAND_VULCAR:
			return "vulcar";
		case Icon.BRAND_WEENY:
			return "weeny";
		case Icon.BRAND_WESTERN:
			return "western";
		case Icon.BRAND_WESTERNMOTORCYCLE:
			return "westernmotorcycle";
		case Icon.BRAND_WILLARD:
			return "willard";
		case Icon.BRAND_ZIRCONIUM:
			return "zirconium";
		case Icon.BRAND_GROTTI2:
			return "grotti_2";
		case Icon.BRAND_LCC:
			return "lcc";
		case Icon.BRAND_PROGEN2:
			return "progen";
		case Icon.BRAND_RUNE:
			return "rune";
		case Icon.BRAND_VYSSER:
			return "vysser";
		case Icon.BRAND_MAXWELL:
			return "maxwell";
		case Icon.INFO:
			return "info_icon_32";
		case Icon.GTACNR_ACCESSIBILITY:
			return "accessibility";
		case Icon.GTACNR_ACCOUNT:
			return "account";
		case Icon.GTACNR_AMMO:
			return "ammo";
		case Icon.GTACNR_ARMORY:
			return "armory";
		case Icon.GTACNR_BAGS:
			return "bags";
		case Icon.GTACNR_BLADES:
			return "blades";
		case Icon.GTACNR_BLUNT_WEAPS:
			return "blunt_weaps";
		case Icon.GTACNR_BOTTOMS:
			return "bottoms";
		case Icon.GTACNR_BRACELETS:
			return "bracelets";
		case Icon.GTACNR_CHAINS:
			return "chains";
		case Icon.GTACNR_CHANGE_PASS:
			return "change_pass";
		case Icon.GTACNR_DISCORD:
			return "discord";
		case Icon.GTACNR_DISPLAY:
			return "display";
		case Icon.GTACNR_DRINKS:
			return "drinks";
		case Icon.GTACNR_DRUGS:
			return "drugs";
		case Icon.GTACNR_EARRINGS:
			return "earrings";
		case Icon.GTACNR_RINGS:
			return "earrings";
		case Icon.GTACNR_EMS:
			return "ems";
		case Icon.GTACNR_FIREMEN:
			return "firemen";
		case Icon.GTACNR_FIVEM:
			return "fivem";
		case Icon.GTACNR_FOOD:
			return "food";
		case Icon.GTACNR_GAMBLING:
			return "gambling";
		case Icon.GTACNR_GARAGES:
			return "garages";
		case Icon.GTACNR_GEAR:
			return "gear";
		case Icon.GTACNR_GLASSES:
			return "glasses";
		case Icon.GTACNR_HAIRSTYLES:
			return "hairstyles";
		case Icon.GTACNR_HANDGUNS:
			return "handguns";
		case Icon.GTACNR_HATS:
			return "hats";
		case Icon.GTACNR_HEAVY_WEAPS:
			return "heavy_weaps";
		case Icon.GTACNR_HELP:
			return "help";
		case Icon.GTACNR_HITMAN:
			return "hitman";
		case Icon.GTACNR_HOTKEYS:
			return "hotkeys";
		case Icon.GTACNR_HOUSES:
			return "houses";
		case Icon.GTACNR_INVENTORY:
			return "inventory";
		case Icon.GTACNR_JOB:
			return "job";
		case Icon.GTACNR_JOB_CALLS:
			return "job_calls";
		case Icon.GTACNR_JOB_SALES:
			return "job_sales";
		case Icon.GTACNR_JOB_RADIO:
			return "job_radio";
		case Icon.GTACNR_LINK:
			return "link";
		case Icon.GTACNR_LINK_EMAIL:
			return "link_email";
		case Icon.GTACNR_LMGS:
			return "lmgs";
		case Icon.GTACNR_MASKS:
			return "'masks'";
		case Icon.GTACNR_MECHANIC:
			return "mechanic";
		case Icon.GTACNR_MEMBERSHIP:
			return "membership";
		case Icon.GTACNR_MOTELS:
			return "motels";
		case Icon.GTACNR_OPTIONS:
			return "options";
		case Icon.GTACNR_OTHER:
			return "other";
		case Icon.GTACNR_OUTFITS:
			return "outfits";
		case Icon.GTACNR_PAPERWORK:
			return "paperwork";
		case Icon.GTACNR_POLICE:
			return "police";
		case Icon.GTACNR_PROPERTIES:
			return "properties";
		case Icon.GTACNR_STATS:
			return "stats";
		case Icon.GTACNR_RIFLES:
			return "rifles";
		case Icon.GTACNR_SERVICES:
			return "services";
		case Icon.GTACNR_SHOES:
			return "shoes";
		case Icon.GTACNR_SHOTGUNS:
			return "shotguns";
		case Icon.GTACNR_SMG:
			return "smg";
		case Icon.GTACNR_SPECIAL_FOOD:
			return "special_food";
		case Icon.GTACNR_SPECIAL_WEAPS:
			return "special_weaps";
		case Icon.GTACNR_STEAM:
			return "steam";
		case Icon.GTACNR_STOCK:
			return "stock";
		case Icon.GTACNR_TAXI:
			return "taxi";
		case Icon.GTACNR_THROWABLES:
			return "throwables";
		case Icon.GTACNR_TOOLS:
			return "tools";
		case Icon.GTACNR_TOPS:
			return "tops";
		case Icon.GTACNR_UNIFORMS:
			return "uniforms";
		case Icon.GTACNR_VEHICLES:
			return "vehicles";
		case Icon.GTACNR_WARDROBE:
			return "wardrobe";
		case Icon.GTACNR_WAREHOUSE:
			return "warehouse";
		case Icon.GTACNR_WATCHES:
			return "watches";
		case Icon.GTACNR_REGISTRATION:
			return "registration";
		case Icon.GTACNR_REFRESH:
			return "refresh";
		case Icon.XBOX_A:
			return "xbox_a";
		case Icon.XBOX_B:
			return "xbox_b";
		case Icon.XBOX_X:
			return "xbox_x";
		case Icon.XBOX_Y:
			return "xbox_y";
		case Icon.XBOX_LB:
			return "xbox_lb";
		case Icon.XBOX_RB:
			return "xbox_rb";
		case Icon.XBOX_LT:
			return "xbox_lt";
		case Icon.XBOX_RT:
			return "xbox_rt";
		case Icon.XBOX_LS:
			return "xbox_ls";
		case Icon.XBOX_RS:
			return "xbox_rs";
		case Icon.XBOX_DPAD:
			return "xbox_dpad";
		case Icon.XBOX_MENU:
			return "xbox_menu";
		case Icon.XBOX_VIEW:
			return "xbox_view";
		default:
			return "";
		}
	}

	protected float GetSpriteSize(Icon icon, bool width)
	{
		switch (icon)
		{
		case Icon.CROWN:
		case Icon.CASH:
		case Icon.COKE:
		case Icon.HEROIN:
		case Icon.METH:
		case Icon.WEED:
		case Icon.ADVERSARY:
		case Icon.BASE_JUMPING:
		case Icon.BRIEFCASE:
		case Icon.MISSION_STAR:
		case Icon.DEATHMATCH:
		case Icon.CASTLE:
		case Icon.TROPHY:
		case Icon.RACE_FLAG:
		case Icon.RACE_FLAG_PLANE:
		case Icon.RACE_FLAG_BICYCLE:
		case Icon.RACE_FLAG_PERSON:
		case Icon.RACE_FLAG_CAR:
		case Icon.RACE_FLAG_BOAT_ANCHOR:
		case Icon.ROCKSTAR:
		case Icon.STUNT:
		case Icon.STUNT_PREMIUM:
		case Icon.RACE_FLAG_STUNT_JUMP:
		case Icon.SHIELD:
		case Icon.TEAM_DEATHMATCH:
		case Icon.VEHICLE_DEATHMATCH:
		case Icon.AUDIO_MUTE:
		case Icon.AUDIO_INACTIVE:
		case Icon.AUDIO_VOL1:
		case Icon.AUDIO_VOL2:
		case Icon.AUDIO_VOL3:
		case Icon.COUNTRY_USA:
		case Icon.COUNTRY_UK:
		case Icon.COUNTRY_SWEDEN:
		case Icon.COUNTRY_KOREA:
		case Icon.COUNTRY_JAPAN:
		case Icon.COUNTRY_ITALY:
		case Icon.COUNTRY_GERMANY:
		case Icon.COUNTRY_FRANCE:
		case Icon.BRAND_ALBANY:
		case Icon.BRAND_ANNIS:
		case Icon.BRAND_BANSHEE:
		case Icon.BRAND_BENEFACTOR:
		case Icon.BRAND_BF:
		case Icon.BRAND_BOLLOKAN:
		case Icon.BRAND_BRAVADO:
		case Icon.BRAND_BRUTE:
		case Icon.BRAND_BUCKINGHAM:
		case Icon.BRAND_CANIS:
		case Icon.BRAND_CHARIOT:
		case Icon.BRAND_CHEVAL:
		case Icon.BRAND_CLASSIQUE:
		case Icon.BRAND_COIL:
		case Icon.BRAND_DECLASSE:
		case Icon.BRAND_DEWBAUCHEE:
		case Icon.BRAND_DILETTANTE:
		case Icon.BRAND_DINKA:
		case Icon.BRAND_DUNDREARY:
		case Icon.BRAND_EMPORER:
		case Icon.BRAND_ENUS:
		case Icon.BRAND_FATHOM:
		case Icon.BRAND_GALIVANTER:
		case Icon.BRAND_GROTTI:
		case Icon.BRAND_GROTTI2:
		case Icon.BRAND_HIJAK:
		case Icon.BRAND_HVY:
		case Icon.BRAND_IMPONTE:
		case Icon.BRAND_INVETERO:
		case Icon.BRAND_JACKSHEEPE:
		case Icon.BRAND_LCC:
		case Icon.BRAND_JOBUILT:
		case Icon.BRAND_KARIN:
		case Icon.BRAND_LAMPADATI:
		case Icon.BRAND_MAIBATSU:
		case Icon.BRAND_MAMMOTH:
		case Icon.BRAND_MTL:
		case Icon.BRAND_NAGASAKI:
		case Icon.BRAND_OBEY:
		case Icon.BRAND_OCELOT:
		case Icon.BRAND_OVERFLOD:
		case Icon.BRAND_PED:
		case Icon.BRAND_PEGASSI:
		case Icon.BRAND_PFISTER:
		case Icon.BRAND_PRINCIPE:
		case Icon.BRAND_PROGEN:
		case Icon.BRAND_PROGEN2:
		case Icon.BRAND_RUNE:
		case Icon.BRAND_SCHYSTER:
		case Icon.BRAND_SHITZU:
		case Icon.BRAND_SPEEDOPHILE:
		case Icon.BRAND_STANLEY:
		case Icon.BRAND_TRUFFADE:
		case Icon.BRAND_UBERMACHT:
		case Icon.BRAND_VAPID:
		case Icon.BRAND_VULCAR:
		case Icon.BRAND_WEENY:
		case Icon.BRAND_WESTERN:
		case Icon.BRAND_WESTERNMOTORCYCLE:
		case Icon.BRAND_WILLARD:
		case Icon.BRAND_ZIRCONIUM:
			return 30f / (width ? MenuController.ScreenWidth : MenuController.ScreenHeight);
		case Icon.STAR:
		case Icon.LOCK_ARENA:
			return 52f / (width ? MenuController.ScreenWidth : MenuController.ScreenHeight);
		case Icon.MEDAL_SILVER:
		case Icon.MP_AMMO_PICKUP:
		case Icon.MP_AMMO:
		case Icon.MP_CASH:
		case Icon.MP_RP:
		case Icon.GLOBE_WHITE:
		case Icon.GLOBE_RED:
		case Icon.GLOBE_BLUE:
		case Icon.GLOBE_YELLOW:
		case Icon.GLOBE_GREEN:
		case Icon.GLOBE_ORANGE:
		case Icon.INV_ARM_WRESTLING:
		case Icon.INV_BASEJUMP:
		case Icon.INV_MISSION:
		case Icon.INV_DARTS:
		case Icon.INV_DEATHMATCH:
		case Icon.INV_DRUG:
		case Icon.INV_CASTLE:
		case Icon.INV_GOLF:
		case Icon.INV_BIKE:
		case Icon.INV_BOAT:
		case Icon.INV_ANCHOR:
		case Icon.INV_CAR:
		case Icon.INV_DOLLAR:
		case Icon.INV_COKE:
		case Icon.INV_KEY:
		case Icon.INV_DATA:
		case Icon.INV_HELI:
		case Icon.INV_HEORIN:
		case Icon.INV_KEYCARD:
		case Icon.INV_METH:
		case Icon.INV_BRIEFCASE:
		case Icon.INV_LINK:
		case Icon.INV_PERSON:
		case Icon.INV_PLANE:
		case Icon.INV_PLANE2:
		case Icon.INV_QUESTIONMARK:
		case Icon.INV_REMOTE:
		case Icon.INV_SAFE:
		case Icon.INV_STEER_WHEEL:
		case Icon.INV_WEAPON:
		case Icon.INV_WEED:
		case Icon.INV_RACE_FLAG_PLANE:
		case Icon.INV_RACE_FLAG_BICYCLE:
		case Icon.INV_RACE_FLAG_BOAT_ANCHOR:
		case Icon.INV_RACE_FLAG_PERSON:
		case Icon.INV_RACE_FLAG_CAR:
		case Icon.INV_RACE_FLAG_HELMET:
		case Icon.INV_SHOOTING_RANGE:
		case Icon.INV_SURVIVAL:
		case Icon.INV_TEAM_DEATHMATCH:
		case Icon.INV_TENNIS:
		case Icon.INV_VEHICLE_DEATHMATCH:
			return 22f / (width ? MenuController.ScreenWidth : MenuController.ScreenHeight);
		case Icon.GTACNR_ACCESSIBILITY:
		case Icon.GTACNR_ACCOUNT:
		case Icon.GTACNR_AMMO:
		case Icon.GTACNR_ARMORY:
		case Icon.GTACNR_BAGS:
		case Icon.GTACNR_BLADES:
		case Icon.GTACNR_BLUNT_WEAPS:
		case Icon.GTACNR_BOTTOMS:
		case Icon.GTACNR_BRACELETS:
		case Icon.GTACNR_CHAINS:
		case Icon.GTACNR_CHANGE_PASS:
		case Icon.GTACNR_DISCORD:
		case Icon.GTACNR_DISPLAY:
		case Icon.GTACNR_DRINKS:
		case Icon.GTACNR_DRUGS:
		case Icon.GTACNR_EARRINGS:
		case Icon.GTACNR_RINGS:
		case Icon.GTACNR_EMS:
		case Icon.GTACNR_FIREMEN:
		case Icon.GTACNR_FIVEM:
		case Icon.GTACNR_FOOD:
		case Icon.GTACNR_GAMBLING:
		case Icon.GTACNR_GARAGES:
		case Icon.GTACNR_GEAR:
		case Icon.GTACNR_GLASSES:
		case Icon.GTACNR_HAIRSTYLES:
		case Icon.GTACNR_HANDGUNS:
		case Icon.GTACNR_HATS:
		case Icon.GTACNR_HEAVY_WEAPS:
		case Icon.GTACNR_HELP:
		case Icon.GTACNR_HITMAN:
		case Icon.GTACNR_HOTKEYS:
		case Icon.GTACNR_HOUSES:
		case Icon.GTACNR_INVENTORY:
		case Icon.GTACNR_JOB:
		case Icon.GTACNR_JOB_CALLS:
		case Icon.GTACNR_JOB_SALES:
		case Icon.GTACNR_JOB_RADIO:
		case Icon.GTACNR_LINK:
		case Icon.GTACNR_LINK_EMAIL:
		case Icon.GTACNR_LMGS:
		case Icon.GTACNR_MASKS:
		case Icon.GTACNR_MECHANIC:
		case Icon.GTACNR_MEMBERSHIP:
		case Icon.GTACNR_MOTELS:
		case Icon.GTACNR_OPTIONS:
		case Icon.GTACNR_OTHER:
		case Icon.GTACNR_OUTFITS:
		case Icon.GTACNR_PAPERWORK:
		case Icon.GTACNR_POLICE:
		case Icon.GTACNR_PROPERTIES:
		case Icon.GTACNR_STATS:
		case Icon.GTACNR_RIFLES:
		case Icon.GTACNR_SERVICES:
		case Icon.GTACNR_SHOES:
		case Icon.GTACNR_SHOTGUNS:
		case Icon.GTACNR_SMG:
		case Icon.GTACNR_SPECIAL_FOOD:
		case Icon.GTACNR_SPECIAL_WEAPS:
		case Icon.GTACNR_STEAM:
		case Icon.GTACNR_STOCK:
		case Icon.GTACNR_TAXI:
		case Icon.GTACNR_THROWABLES:
		case Icon.GTACNR_TOOLS:
		case Icon.GTACNR_TOPS:
		case Icon.GTACNR_UNIFORMS:
		case Icon.GTACNR_VEHICLES:
		case Icon.GTACNR_WARDROBE:
		case Icon.GTACNR_WAREHOUSE:
		case Icon.GTACNR_WATCHES:
		case Icon.GTACNR_REGISTRATION:
		case Icon.GTACNR_REFRESH:
		case Icon.XBOX_A:
		case Icon.XBOX_B:
		case Icon.XBOX_X:
		case Icon.XBOX_Y:
		case Icon.XBOX_LB:
		case Icon.XBOX_RB:
		case Icon.XBOX_LT:
		case Icon.XBOX_RT:
		case Icon.XBOX_LS:
		case Icon.XBOX_RS:
		case Icon.XBOX_DPAD:
		case Icon.XBOX_MENU:
		case Icon.XBOX_VIEW:
			return 28f / (width ? MenuController.ScreenWidth : MenuController.ScreenHeight);
		default:
			return 38f / (width ? MenuController.ScreenWidth : MenuController.ScreenHeight);
		}
	}

	protected int[] GetSpriteColour(Icon icon, bool selected)
	{
		switch (icon)
		{
		case Icon.LOCK:
		case Icon.CROWN:
		case Icon.TICK:
		case Icon.FEMALE:
		case Icon.MALE:
		case Icon.LOCK_ARENA:
		case Icon.ADVERSARY:
		case Icon.BASE_JUMPING:
		case Icon.BRIEFCASE:
		case Icon.MISSION_STAR:
		case Icon.DEATHMATCH:
		case Icon.CASTLE:
		case Icon.TROPHY:
		case Icon.RACE_FLAG:
		case Icon.RACE_FLAG_PLANE:
		case Icon.RACE_FLAG_BICYCLE:
		case Icon.RACE_FLAG_PERSON:
		case Icon.RACE_FLAG_CAR:
		case Icon.RACE_FLAG_BOAT_ANCHOR:
		case Icon.ROCKSTAR:
		case Icon.STUNT:
		case Icon.STUNT_PREMIUM:
		case Icon.RACE_FLAG_STUNT_JUMP:
		case Icon.SHIELD:
		case Icon.TEAM_DEATHMATCH:
		case Icon.VEHICLE_DEATHMATCH:
		case Icon.MP_SPECTATING:
		case Icon.GLOBE_WHITE:
		case Icon.AUDIO_MUTE:
		case Icon.AUDIO_INACTIVE:
		case Icon.AUDIO_VOL1:
		case Icon.AUDIO_VOL2:
		case Icon.AUDIO_VOL3:
		case Icon.BRAND_ALBANY:
		case Icon.BRAND_ANNIS:
		case Icon.BRAND_BANSHEE:
		case Icon.BRAND_BENEFACTOR:
		case Icon.BRAND_BF:
		case Icon.BRAND_BOLLOKAN:
		case Icon.BRAND_BRAVADO:
		case Icon.BRAND_BRUTE:
		case Icon.BRAND_BUCKINGHAM:
		case Icon.BRAND_CANIS:
		case Icon.BRAND_CHARIOT:
		case Icon.BRAND_CHEVAL:
		case Icon.BRAND_CLASSIQUE:
		case Icon.BRAND_COIL:
		case Icon.BRAND_DECLASSE:
		case Icon.BRAND_DEWBAUCHEE:
		case Icon.BRAND_DILETTANTE:
		case Icon.BRAND_DINKA:
		case Icon.BRAND_DUNDREARY:
		case Icon.BRAND_EMPORER:
		case Icon.BRAND_ENUS:
		case Icon.BRAND_FATHOM:
		case Icon.BRAND_GALIVANTER:
		case Icon.BRAND_GROTTI:
		case Icon.BRAND_GROTTI2:
		case Icon.BRAND_HIJAK:
		case Icon.BRAND_HVY:
		case Icon.BRAND_IMPONTE:
		case Icon.BRAND_INVETERO:
		case Icon.BRAND_JACKSHEEPE:
		case Icon.BRAND_LCC:
		case Icon.BRAND_JOBUILT:
		case Icon.BRAND_KARIN:
		case Icon.BRAND_LAMPADATI:
		case Icon.BRAND_MAIBATSU:
		case Icon.BRAND_MAMMOTH:
		case Icon.BRAND_MTL:
		case Icon.BRAND_NAGASAKI:
		case Icon.BRAND_OBEY:
		case Icon.BRAND_OCELOT:
		case Icon.BRAND_OVERFLOD:
		case Icon.BRAND_PED:
		case Icon.BRAND_PEGASSI:
		case Icon.BRAND_PFISTER:
		case Icon.BRAND_PRINCIPE:
		case Icon.BRAND_PROGEN:
		case Icon.BRAND_PROGEN2:
		case Icon.BRAND_RUNE:
		case Icon.BRAND_SCHYSTER:
		case Icon.BRAND_SHITZU:
		case Icon.BRAND_SPEEDOPHILE:
		case Icon.BRAND_STANLEY:
		case Icon.BRAND_TRUFFADE:
		case Icon.BRAND_UBERMACHT:
		case Icon.BRAND_VAPID:
		case Icon.BRAND_VULCAR:
		case Icon.BRAND_WEENY:
		case Icon.BRAND_WESTERN:
		case Icon.BRAND_WESTERNMOTORCYCLE:
		case Icon.BRAND_WILLARD:
		case Icon.BRAND_ZIRCONIUM:
			if (!selected)
			{
				if (Enabled)
				{
					return new int[3] { 255, 255, 255 };
				}
				return new int[3] { 109, 109, 109 };
			}
			if (!Enabled)
			{
				return new int[3] { 50, 50, 50 };
			}
			return new int[3];
		case Icon.GTACNR_ACCESSIBILITY:
		case Icon.GTACNR_ACCOUNT:
		case Icon.GTACNR_AMMO:
		case Icon.GTACNR_ARMORY:
		case Icon.GTACNR_BAGS:
		case Icon.GTACNR_BLADES:
		case Icon.GTACNR_BLUNT_WEAPS:
		case Icon.GTACNR_BOTTOMS:
		case Icon.GTACNR_BRACELETS:
		case Icon.GTACNR_CHAINS:
		case Icon.GTACNR_CHANGE_PASS:
		case Icon.GTACNR_DISCORD:
		case Icon.GTACNR_DISPLAY:
		case Icon.GTACNR_DRINKS:
		case Icon.GTACNR_DRUGS:
		case Icon.GTACNR_EARRINGS:
		case Icon.GTACNR_RINGS:
		case Icon.GTACNR_EMS:
		case Icon.GTACNR_FIREMEN:
		case Icon.GTACNR_FIVEM:
		case Icon.GTACNR_FOOD:
		case Icon.GTACNR_GAMBLING:
		case Icon.GTACNR_GARAGES:
		case Icon.GTACNR_GEAR:
		case Icon.GTACNR_GLASSES:
		case Icon.GTACNR_HAIRSTYLES:
		case Icon.GTACNR_HANDGUNS:
		case Icon.GTACNR_HATS:
		case Icon.GTACNR_HEAVY_WEAPS:
		case Icon.GTACNR_HELP:
		case Icon.GTACNR_HITMAN:
		case Icon.GTACNR_HOTKEYS:
		case Icon.GTACNR_HOUSES:
		case Icon.GTACNR_INVENTORY:
		case Icon.GTACNR_JOB:
		case Icon.GTACNR_JOB_CALLS:
		case Icon.GTACNR_JOB_SALES:
		case Icon.GTACNR_JOB_RADIO:
		case Icon.GTACNR_LINK:
		case Icon.GTACNR_LINK_EMAIL:
		case Icon.GTACNR_LMGS:
		case Icon.GTACNR_MASKS:
		case Icon.GTACNR_MECHANIC:
		case Icon.GTACNR_MEMBERSHIP:
		case Icon.GTACNR_MOTELS:
		case Icon.GTACNR_OPTIONS:
		case Icon.GTACNR_OTHER:
		case Icon.GTACNR_OUTFITS:
		case Icon.GTACNR_PAPERWORK:
		case Icon.GTACNR_POLICE:
		case Icon.GTACNR_PROPERTIES:
		case Icon.GTACNR_STATS:
		case Icon.GTACNR_RIFLES:
		case Icon.GTACNR_SERVICES:
		case Icon.GTACNR_SHOES:
		case Icon.GTACNR_SHOTGUNS:
		case Icon.GTACNR_SMG:
		case Icon.GTACNR_SPECIAL_FOOD:
		case Icon.GTACNR_SPECIAL_WEAPS:
		case Icon.GTACNR_STEAM:
		case Icon.GTACNR_STOCK:
		case Icon.GTACNR_TAXI:
		case Icon.GTACNR_THROWABLES:
		case Icon.GTACNR_TOOLS:
		case Icon.GTACNR_TOPS:
		case Icon.GTACNR_UNIFORMS:
		case Icon.GTACNR_VEHICLES:
		case Icon.GTACNR_WARDROBE:
		case Icon.GTACNR_WAREHOUSE:
		case Icon.GTACNR_WATCHES:
		case Icon.GTACNR_REGISTRATION:
		case Icon.GTACNR_REFRESH:
		case Icon.XBOX_A:
		case Icon.XBOX_B:
		case Icon.XBOX_X:
		case Icon.XBOX_Y:
		case Icon.XBOX_LB:
		case Icon.XBOX_RB:
		case Icon.XBOX_LT:
		case Icon.XBOX_RT:
		case Icon.XBOX_LS:
		case Icon.XBOX_RS:
		case Icon.XBOX_DPAD:
		case Icon.XBOX_MENU:
		case Icon.XBOX_VIEW:
			if (Enabled)
			{
				return new int[3] { 255, 255, 255 };
			}
			return new int[3] { 109, 109, 109 };
		case Icon.GLOBE_BLUE:
			if (Enabled)
			{
				return new int[3] { 10, 103, 166 };
			}
			return new int[3] { 11, 62, 117 };
		case Icon.GLOBE_GREEN:
			if (Enabled)
			{
				return new int[3] { 10, 166, 85 };
			}
			return new int[3] { 5, 71, 22 };
		case Icon.GLOBE_ORANGE:
			if (Enabled)
			{
				return new int[3] { 232, 145, 14 };
			}
			return new int[3] { 133, 77, 12 };
		case Icon.GLOBE_RED:
			if (Enabled)
			{
				return new int[3] { 207, 43, 31 };
			}
			return new int[3] { 110, 7, 7 };
		case Icon.GLOBE_YELLOW:
			if (Enabled)
			{
				return new int[3] { 232, 207, 14 };
			}
			return new int[3] { 131, 133, 12 };
		default:
			if (Enabled)
			{
				return new int[3] { 255, 255, 255 };
			}
			return new int[3] { 109, 109, 109 };
		}
	}

	protected float GetSpriteX(Icon icon, bool leftAligned, bool leftSide)
	{
		if (icon == Icon.NONE)
		{
			return 0f;
		}
		if (!leftSide)
		{
			if (!leftAligned)
			{
				return API.GetSafeZoneSize() - 20f / MenuController.ScreenWidth;
			}
			return (RowWidth - 20f) / MenuController.ScreenWidth;
		}
		if (!leftAligned)
		{
			return API.GetSafeZoneSize() - (RowWidth - 20f) / MenuController.ScreenWidth;
		}
		return 20f / MenuController.ScreenWidth;
	}

	protected float GetSpriteY(Icon icon)
	{
		return 0f;
	}

	internal virtual async Task Draw(int indexOffset)
	{
		if (ParentMenu == null)
		{
			return;
		}
		float num = ParentMenu.MenuItemsYOffset + 1f - RowSpacing * (float)MathUtil.Clamp(ParentMenu.Size, 0, ParentMenu.MaxItemsOnScreen);
		API.SetScriptGfxAlign(ParentMenu.LeftAligned ? 76 : 82, 84);
		API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
		float num2 = (ParentMenu.Position.X + RowWidth / 2f) / MenuController.ScreenWidth;
		float y = (ParentMenu.Position.Y + (float)(Index - indexOffset) * RowSpacing + 20f + num) / MenuController.ScreenHeight;
		float num3 = RowWidth / MenuController.ScreenWidth;
		float num4 = RowHeight / MenuController.ScreenHeight;
		if (Selected)
		{
			API.DrawRect(num2, y, num3, num4, 255, 255, 255, 225);
		}
		API.ResetScriptGfxAlign();
		float textXOffset = 0f;
		if (LeftIcon != Icon.NONE)
		{
			textXOffset = 25f;
			API.SetScriptGfxAlign(76, 84);
			API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
			string name = GetSpriteName(LeftIcon, Selected);
			float spriteY = y;
			float spriteX = GetSpriteX(LeftIcon, ParentMenu.LeftAligned, leftSide: true);
			float spriteHeight = GetSpriteSize(LeftIcon, width: false);
			float spriteWidth = GetSpriteSize(LeftIcon, width: true);
			int[] spriteColor = GetSpriteColour(LeftIcon, Selected);
			string textureDictionary = GetSpriteDictionary(LeftIcon);
			if (!API.HasStreamedTextureDictLoaded(textureDictionary))
			{
				API.RequestStreamedTextureDict(textureDictionary, false);
				while (!API.HasStreamedTextureDictLoaded(textureDictionary))
				{
					await BaseScript.Delay(0);
				}
			}
			API.DrawSprite(textureDictionary, name, spriteX, spriteY, spriteWidth, spriteHeight, 0f, spriteColor[0], spriteColor[1], spriteColor[2], 255);
			API.ResetScriptGfxAlign();
		}
		float num5 = 0f;
		if (RightIcon != Icon.NONE)
		{
			num5 = 25f;
			API.SetScriptGfxAlign(76, 84);
			API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
			string spriteName = GetSpriteName(RightIcon, Selected);
			float num6 = y;
			float spriteX2 = GetSpriteX(RightIcon, ParentMenu.LeftAligned, leftSide: false);
			float spriteSize = GetSpriteSize(RightIcon, width: false);
			float spriteSize2 = GetSpriteSize(RightIcon, width: true);
			int[] spriteColour = GetSpriteColour(RightIcon, Selected);
			API.DrawSprite(GetSpriteDictionary(RightIcon), spriteName, spriteX2, num6, spriteSize2, spriteSize, 0f, spriteColour[0], spriteColour[1], spriteColour[2], 255);
			API.ResetScriptGfxAlign();
		}
		int textFont = (int)ParentMenu.TextFont;
		float textScale = ParentMenu.TextFontSize / MenuController.ScreenHeight;
		float num7 = textXOffset / MenuController.ScreenWidth + 10f / MenuController.ScreenWidth;
		float num8 = (RowWidth - 10f) / MenuController.ScreenWidth;
		float y2 = y - RowHeight / 2f / MenuController.ScreenHeight;
		int num9 = ((!Selected) ? (Enabled ? 255 : 109) : ((!Enabled) ? 50 : 0));
		if (!string.IsNullOrEmpty(Label))
		{
			BetterDrawTextHelper.BeginTextCommandDisplayText(Label);
			BetterDrawTextHelper.SetScriptGfxAlign(GfxAlign.Left, GfxAlign.Top);
			BetterDrawTextHelper.SetTextFont((Font)textFont);
			BetterDrawTextHelper.SetTextScale(textScale);
			BetterDrawTextHelper.SetTextJustification(TextJustification.Right);
			if (Selected || !Enabled)
			{
				BetterDrawTextHelper.SetTextColour((byte)num9, (byte)num9, (byte)num9, byte.MaxValue);
			}
			float num10 = 0f;
			float num11;
			float num12;
			if (ParentMenu.LeftAligned)
			{
				num11 = (RowWidth + 10f - num5) / MenuController.ScreenWidth;
				num12 = API.GetSafeZoneSize() - (RowWidth + 10f - num5) / MenuController.ScreenWidth;
				num10 = 20f / MenuController.ScreenWidth;
				num12 += 0.01f;
				num10 += 0.01f;
				num11 += 0.01f;
			}
			else
			{
				num11 = API.GetSafeZoneSize() - (10f + num5) / MenuController.ScreenWidth;
				num12 = num5 / MenuController.ScreenWidth;
				num12 += 0.005f;
				num10 += 0.005f;
				num11 += 0.005f;
			}
			BetterDrawTextHelper.SetTextWrap(num10, num11);
			BetterDrawTextHelper.EndTextCommandDisplayText(num12, y2);
		}
		BetterDrawTextHelper.BeginTextCommandDisplayText(Text ?? "N/A");
		BetterDrawTextHelper.SetScriptGfxAlign(GfxAlign.Left, GfxAlign.Top);
		float num13 = num7;
		float end = num8;
		if (Selected || !Enabled)
		{
			BetterDrawTextHelper.SetTextColour((byte)num9, (byte)num9, (byte)num9, byte.MaxValue);
		}
		if (!ParentMenu.LeftAligned)
		{
			num13 = textXOffset / MenuController.ScreenWidth + API.GetSafeZoneSize() - (RowWidth - 10f) / MenuController.ScreenWidth;
			end = API.GetSafeZoneSize() - 10f / MenuController.ScreenWidth;
		}
		BetterDrawTextHelper.SetTextWrap(num13, end);
		BetterDrawTextHelper.SetTextScale(textScale);
		BetterDrawTextHelper.EndTextCommandDisplayText(num13, y2);
	}
}
