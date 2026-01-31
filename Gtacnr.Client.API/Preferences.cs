using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Weapons;
using Gtacnr.Model.Enums;
using Rock.Collections;

namespace Gtacnr.Client.API;

public static class Preferences
{
	public class Preference<T>
	{
		private readonly string key;

		private readonly T? defaultValue;

		public Preference(string key, T? defaultValue = default(T?))
		{
			this.defaultValue = defaultValue;
			this.key = key;
		}

		public T? Get()
		{
			return Utils.GetPreference(key, defaultValue);
		}

		public void Set(T? value)
		{
			Utils.SetPreference(key, value);
		}
	}

	public static readonly Preference<bool> ToggleChatDMs = new Preference<bool>("gtacnr:chat:toggleDMs", defaultValue: true);

	public static readonly Preference<CameraCycleMode> CameraCycleMode = new Preference<CameraCycleMode>("camCycleMode", Gtacnr.Client.Weapons.CameraCycleMode.Normal);

	public static readonly Preference<bool> FlashlightModeEnabled = new Preference<bool>("flashlightModeEnabled", defaultValue: true);

	public static readonly Preference<bool> CrosshairEnabled = new Preference<bool>("gtacnr:crosshairEnabled", defaultValue: false);

	public static readonly Preference<string> CrosshairJson = new Preference<string>("gtacnr:crosshair");

	public static readonly Preference<float> MenuOffsetW = new Preference<float>("menuOffsetW", 0f);

	public static readonly Preference<float> MenuOffsetX = new Preference<float>("menuOffsetX", 0f);

	public static readonly Preference<float> MenuOffsetY = new Preference<float>("menuOffsetY", 0f);

	public static readonly Preference<float> MenuOffsetH = new Preference<float>("menuOffsetH", 38f);

	public static readonly Preference<bool> MenuOffsetSet = new Preference<bool>("menuOffsetsSetV2", defaultValue: false);

	public static readonly Preference<bool> HealthPercentEnabled = new Preference<bool>("healthPercentEnabled", defaultValue: false);

	public static readonly Preference<bool> OverheadSignsEnabled = new Preference<bool>("overheadSignsEnabled", defaultValue: true);

	public static readonly Preference<bool> ReticleEnabled = new Preference<bool>("reticleEnabled", defaultValue: true);

	public static readonly Preference<bool> ShowPlayerNameTagsOnBlips = new Preference<bool>("showPlayerNameTagsOnBlips", defaultValue: false);

	public static readonly Preference<bool> ThousandsSeparator = new Preference<bool>("thousandsSeparator", defaultValue: false);

	public static readonly Preference<bool> ColorBlindModeEnabled = new Preference<bool>("colorBlindModeEnabled", defaultValue: false);

	public static readonly Preference<DeathFeedMode> DeathFeedMode = new Preference<DeathFeedMode>("deathFeedMode", Gtacnr.Model.Enums.DeathFeedMode.Proximity);

	public static readonly Preference<bool> RevengeTransfers = new Preference<bool>("revengeTransfers", defaultValue: false);

	public static readonly Preference<bool> RevengeTargetMarkers = new Preference<bool>("revengeTargetMarkers", defaultValue: true);

	public static readonly Preference<bool> RevengeClaimantMarkersEnabled = new Preference<bool>("revengeClaimantMarkersEnabled", defaultValue: true);

	public static readonly Preference<bool> QrCodeBlipFound = new Preference<bool>("qrCodeBlipFound", defaultValue: false);

	public static readonly Preference<Dictionary<string, Dictionary<string, string>>> CustomWeaponTextures = new Preference<Dictionary<string, Dictionary<string, string>>>("customWeaponTextures", new Dictionary<string, Dictionary<string, string>>());

	public static readonly Preference<int> PropertyBlipsMode = new Preference<int>("propertyBlipsMode", 1);

	public static readonly Preference<DispatchFilter> PoliceCallFilter = new Preference<DispatchFilter>("policeCallFilter", DispatchFilter.All);

	public static readonly Preference<bool> MenuDoublePressOnController = new Preference<bool>("menuDoublePressOnController", defaultValue: false);

	public static readonly Preference<bool> MenusLeftAligned = new Preference<bool>("menusLeftAligned", !(API.GetScreenAspectRatio(false) < 1.8888888f));

	public static readonly Preference<string> PreferredLanguage = new Preference<string>("preferredLanguage");

	public static readonly Preference<OrderedHashSet<string>> FavoriteVehicles = new Preference<OrderedHashSet<string>>("gtacnr:favoriteVehicles", new OrderedHashSet<string>());

	public static readonly Preference<bool> RadioClicksEnabled = new Preference<bool>("radioClicksEnabled", defaultValue: true);

	public static readonly Preference<bool> FlashbangBlackoutMode = new Preference<bool>("flashbangMode", defaultValue: false);

	public static readonly Preference<bool> AltimeterEnabled = new Preference<bool>("altimeterEnabled", defaultValue: true);

	public static readonly Preference<bool> SpeedometerEnabled = new Preference<bool>("speedometerEnabled", defaultValue: true);

	public static readonly Preference<int> PoliceVoiceIdxMale = new Preference<int>("policeVoiceIdxMale", 0);

	public static readonly Preference<int> PoliceVoiceIdxFemale = new Preference<int>("policeVoiceIdxFemale", 0);

	public static readonly Preference<bool> LowFuelPrompt = new Preference<bool>("lowFuelPromptEnabled", defaultValue: true);

	public static readonly Preference<bool> AlwaysShowFuelBar = new Preference<bool>("alwaysShowFuelBar", defaultValue: false);

	public static readonly Preference<bool> XPBarHidden = new Preference<bool>("xpBarHidden", defaultValue: false);

	public static Preference<int>? PoliceVoiceIdx => Utils.GetFreemodePedSex(Game.PlayerPed) switch
	{
		Sex.Male => PoliceVoiceIdxMale, 
		Sex.Female => PoliceVoiceIdxFemale, 
		_ => null, 
	};
}
