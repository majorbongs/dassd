using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Paramedic;

public class ParamedicDispatch : BaseCallDispatch<CallInfo>
{
	public ParamedicDispatch()
		: base(LocalizationController.S(Entries.Businesses.MENU_PARAMEDIC_TITLE), LocalizationController.S(Entries.Businesses.JOBMENU_CALLS))
	{
	}

	public override async void OnDispatch(int victimId, string locationJson)
	{
		if (victimId == 0 || string.IsNullOrEmpty(locationJson))
		{
			return;
		}
		Vector3 location = locationJson.Unjson<Vector3>();
		if (JurisdictionScript.IsPointOutOfJurisdiction(location))
		{
			return;
		}
		string zoneName = Utils.GetLocationName(location);
		string playerTitle = "";
		PlayerState playerInfo = LatentPlayers.Get(victimId);
		if (playerInfo == null)
		{
			await Authentication.GetAccountName(victimId);
		}
		else
		{
			_ = playerInfo.ColorTextCode;
			if (playerInfo.JobEnum == JobsEnum.Police)
			{
				playerTitle = "~b~Police Officer ~s~";
			}
			else if (playerInfo.JobEnum == JobsEnum.Paramedic)
			{
				playerTitle = "~p~Paramedic ~s~";
			}
			else if (playerInfo.JobEnum == JobsEnum.Firefighter)
			{
				playerTitle = "~p~Firefighter ~s~";
			}
			_ = playerInfo.Name;
		}
		string message = "~b~<C>Dispatch:</C> " + playerTitle + playerInfo.ColorNameAndId + " needs ~p~EMS ~s~in ~y~" + zoneName + ".";
		CallInfo callInfo = new CallInfo
		{
			PlayerId = playerInfo.Id,
			Position = location,
			DateTime = DateTime.UtcNow
		};
		AddCall(callInfo);
		await InteractiveNotificationsScript.Show(message, InteractiveNotificationType.Notification, OnAccepted, TimeSpan.FromSeconds(5.0), 0u, "Respond", "Respond (hold)", () => (Entity)(object)Game.PlayerPed == (Entity)null || ((Entity)Game.PlayerPed).IsDead);
		bool OnAccepted()
		{
			TryRespondToCall(callInfo);
			Utils.DisplaySubtitle("Head to ~y~" + zoneName + " ~s~and rescue " + playerInfo.ColorNameAndId + ".", 10000);
			return true;
		}
	}

	protected override void RespondToCall(CallInfo call)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		Utils.GetLocationName(call.Position);
		if (LatentPlayers.Get(call.PlayerId) != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:ems:respond", new object[1] { call.PlayerId });
			Vector3 position = call.Position;
			BlipSprite? sprite = (BlipSprite)162;
			Action onArrival = OnArrival;
			GPSScript.SetDestination("Call", position, 0f, shortRange: false, sprite, null, 255, autoDelete: true, 30f, onArrival);
			Utils.DisplayHelpText("Your ~b~GPS ~s~has been set to the location of the selected ~y~call~s~.");
		}
		void OnArrival()
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			Player val = PlayerList.Players[call.PlayerId];
			if (!(val == (Player)null) && !((Entity)(object)val.Character == (Entity)null))
			{
				Vector3 position2 = ((Entity)val.Character).Position;
				if (!(((Vector3)(ref position2)).DistanceToSquared2D(call.Position) > 900f))
				{
					BaseScript.TriggerServerEvent("gtacnr:ems:arrivedAtScene", new object[1] { call.PlayerId });
					lastCall = null;
					return;
				}
			}
			lastCall = null;
		}
	}

	public async void OnCancelDispatch(int playerId)
	{
		if (lastCall != null && lastCall.PlayerId == playerId)
		{
			PlayerState playerState = LatentPlayers.Get(playerId) ?? PlayerState.CreateDisconnectedPlayer(playerId);
			Utils.DisplayHelpText("Victim " + playerState.ColorNameAndId + " ~r~died~s~ and the call was canceled.");
			GPSScript.ClearDestination();
			lastCall = null;
		}
	}
}
