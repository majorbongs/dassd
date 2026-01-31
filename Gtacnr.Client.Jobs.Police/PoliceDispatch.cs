using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Vehicles;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police;

public class PoliceDispatch : BaseCallDispatch<CrimeCallInfo>
{
	private Dictionary<int, DateTime> lastCrimeTimestampByPlayer = new Dictionary<int, DateTime>();

	public PoliceDispatch()
		: base(LocalizationController.S(Entries.Businesses.MENU_POLICEOFFICER_TITLE), LocalizationController.S(Entries.Businesses.JOBMENU_CALLS))
	{
	}

	public override async void OnDispatch(int suspectId, string? crimeJson)
	{
		if (suspectId == 0 || string.IsNullOrEmpty(crimeJson))
		{
			return;
		}
		Gtacnr.Model.Crime crime = crimeJson.Unjson<Gtacnr.Model.Crime>();
		CrimeType definition = Gtacnr.Data.Crimes.GetDefinition(crime.CrimeType);
		if ((lastCrimeTimestampByPlayer.ContainsKey(suspectId) && !Gtacnr.Utils.CheckTimePassed(lastCrimeTimestampByPlayer[suspectId], 10000.0)) || InteractiveNotificationsScript.IsAnyNotificationWithHigherPriorityActive(0u))
		{
			return;
		}
		lastCrimeTimestampByPlayer[suspectId] = DateTime.UtcNow;
		if (JurisdictionScript.IsPointOutOfJurisdiction(crime.Location))
		{
			return;
		}
		string text = definition.ColorStr;
		if (definition.IsViolent)
		{
			text = "~r~";
		}
		PlayerState playerState = LatentPlayers.Get(suspectId);
		string colorTextCode = Gtacnr.Utils.GetColorTextCode(JobsEnum.None, crime.WantedLevelAfter);
		CrimeCallInfo callInfo;
		try
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			Vector3 location = crime.Location;
			float num = ((Vector3)(ref location)).DistanceToSquared(position);
			string text2 = Utils.LocalizeCrimeSeverity(definition.CrimeSeverity);
			string text3 = text + (definition.IsViolent ? LocalizationController.S(Entries.Crime.CRIME_MODIFIER_VIOLENT, text2) : text2) + "~s~";
			if (playerState.WantedLevel == 1 || num <= 2500f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Crime.DISPATCH_CRIME_SPOTTED, colorTextCode + playerState.NameAndId, text3, definition.Description), Gtacnr.Utils.Colors.HudBlueDark);
				Game.PlaySound("Event_Message_Purple", "GTAO_FM_Events_Soundset");
				await Utils.Delay(3000);
				return;
			}
			switch (PoliceMenuScript.CallFilter)
			{
			case DispatchFilter.Violent:
				if (!definition.IsViolent)
				{
					return;
				}
				break;
			case DispatchFilter.Major:
				if (definition.CrimeSeverity != CrimeSeverity.MajorFelony)
				{
					return;
				}
				break;
			case DispatchFilter.None:
				return;
			}
			string locationName = Utils.GetLocationName(crime.Location);
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			uint num2 = ((currentVehicle != null) ? ((uint)((Entity)currentVehicle).Model.Hash) : 0u);
			float num3 = API.CalculateTravelDistanceBetweenPoints(position.X, position.Y, position.Z, crime.Location.X, crime.Location.Y, crime.Location.Z);
			if (API.IsThisModelAPlane(num2) || API.IsThisModelAHeli(num2) || API.IsThisModelABoat(num2) || num3 == 100000f || num3 <= 0f)
			{
				num3 = num;
			}
			string text4 = Utils.FormatDistanceString(num3);
			string text5 = LocalizationController.S(Entries.Crime.DISPATCH_CRIME_REPORTED, colorTextCode + playerState.NameAndId, text3, definition.Description, locationName, text4);
			if (crime.CurrentVehicleModel != 0)
			{
				string vehicleModelName = Utils.GetVehicleModelName(crime.CurrentVehicleModel);
				string text6 = "";
				if (DealershipScript.VehicleColors.TryGetValue(crime.CurrentVehicleColor, out VehicleColorInfo value))
				{
					text6 = value.Description;
					text6 = text6.Replace("Metallic", "").Replace("Matte", "").Replace("Util", "")
						.Replace("Worn Off", "")
						.Replace("Worn", "")
						.Replace("Pure", "")
						.Replace("Brushed", "")
						.Replace("Police Car", "")
						.Replace("Modshop", "")
						.Replace("#1", "")
						.Replace("Frost", "")
						.Replace("Midnight", "")
						.Trim();
				}
				text5 = text5 + "~n~" + LocalizationController.S(Entries.Crime.DISPATCH_CRIME_REPORTED_VEHICLE, "~y~" + text6 + " " + vehicleModelName);
			}
			callInfo = new CrimeCallInfo
			{
				PlayerId = playerState.Id,
				Position = crime.Location,
				DateTime = DateTime.UtcNow,
				Details = definition.Description,
				Crime = crime
			};
			AddCall(callInfo);
			string message = text5;
			Func<bool> onAccept = OnAccepted;
			string keyboardMessage = LocalizationController.S(Entries.Crime.DISPATCH_RESPOND);
			string controllerMessage = LocalizationController.S(Entries.Businesses.BTN_STP_LABEL_HOLD, LocalizationController.S(Entries.Crime.DISPATCH_RESPOND));
			await InteractiveNotificationsScript.Show(message, InteractiveNotificationType.Notification, onAccept, null, 0u, keyboardMessage, controllerMessage, () => ((Entity)Game.PlayerPed).IsDead);
		}
		catch (Exception exception)
		{
			Gtacnr.Utils.PrintException(exception);
		}
		bool OnAccepted()
		{
			TryRespondToCall(callInfo);
			return true;
		}
	}

	protected override void RespondToCall(CrimeCallInfo call)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		string locationName = Utils.GetLocationName(call.Crime.Location);
		PlayerState playerState = LatentPlayers.Get(call.Crime.PlayerId);
		BaseScript.TriggerServerEvent("gtacnr:police:respond", new object[1] { call.Crime.Id });
		Utils.DisplaySubtitle(LocalizationController.S(Entries.Crime.DISPATCH_RESPOND_MESSAGE, locationName, playerState.ColorNameAndId), 10000);
		Vector3 location = call.Crime.Location;
		BlipSprite? sprite = (BlipSprite)162;
		Action onArrival = OnArrival;
		GPSScript.SetDestination("Call", location, 0f, shortRange: false, sprite, null, 255, autoDelete: true, 60f, onArrival);
		void OnArrival()
		{
			BaseScript.TriggerServerEvent("gtacnr:police:arrivedAtScene", new object[1] { call.Crime.Id });
		}
	}
}
