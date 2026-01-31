using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Vehicles;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police;

public class NineOneOneScript : Script
{
	[EventHandler("gtacnr:police:receiveCall")]
	private async void OnReceiveCall(int playerId, int callReason)
	{
		PlayerState playerInfo = LatentPlayers.Get(playerId);
		string areaName;
		if (playerInfo != null)
		{
			Vector3 position = playerInfo.Position;
			areaName = Utils.GetLocationName(position);
			string text = "called 911 in ~y~" + areaName;
			switch (callReason)
			{
			case 1:
				text = "called 911 to report a ~o~robbery ~s~in ~y~" + areaName;
				break;
			case 2:
				text = "called 911 to report ~o~pickpocketing ~s~in ~y~" + areaName;
				break;
			}
			await InteractiveNotificationsScript.Show(playerInfo.ColorNameAndId + " " + text, InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Respond", "Respond (hold)");
		}
		bool OnAccepted()
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			Utils.DisplayHelpText();
			Utils.SendNotification($"Go to ~y~{areaName} ~s~and assist {playerInfo.ColorTextCode}{playerInfo.Name} ({playerId})~s~.");
			GPSScript.SetDestination("911 Call", playerInfo.Position, 0f, shortRange: false, (BlipSprite)162, null, 255, autoDelete: true, 60f);
			return true;
		}
	}
}
