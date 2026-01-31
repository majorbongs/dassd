using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Mechanic;

public class MechanicDispatch : BaseCallDispatch<CallInfo>
{
	public MechanicDispatch()
		: base(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.JOBMENU_CALLS))
	{
	}

	public override async void OnDispatch(int playerId, string _)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		CallInfo callInfo;
		if (playerState != null)
		{
			string locationName = Utils.GetLocationName(playerState.Position);
			callInfo = new CallInfo
			{
				PlayerId = playerState.Id,
				Position = playerState.Position,
				DateTime = DateTime.UtcNow
			};
			AddCall(callInfo);
			await InteractiveNotificationsScript.Show(playerState.ColorNameAndId + " is looking for a mechanic in ~y~" + locationName, InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Respond", "Respond (hold)");
		}
		bool OnAccepted()
		{
			Utils.DisplayHelpText();
			TryRespondToCall(callInfo);
			return true;
		}
	}

	protected override void RespondToCall(CallInfo call)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		PlayerState playerState = LatentPlayers.Get(call.PlayerId);
		if (playerState != null)
		{
			string locationName = Utils.GetLocationName(call.Position);
			Utils.SendNotification("Go to ~y~" + locationName + " ~s~and assist " + playerState.ColorNameAndId + ".");
			GPSScript.SetDestination("Responded Call", playerState.Position, 0f, shortRange: false, (BlipSprite)162, null, 255, autoDelete: true, 60f);
		}
	}
}
