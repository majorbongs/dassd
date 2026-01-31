using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Vehicles;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Paramedic;

public class NineOneOneScript : Script
{
	[EventHandler("gtacnr:ems:receiveCall")]
	private async void OnReceiveCall(int playerId)
	{
		PlayerState playerInfo = LatentPlayers.Get(playerId);
		string areaName;
		if (playerInfo != null)
		{
			Vector3 position = playerInfo.Position;
			areaName = Utils.GetLocationName(position);
			await InteractiveNotificationsScript.Show($"{playerInfo.ColorTextCode}{playerInfo.Name} ({playerId}) ~s~called 911 in ~y~{areaName}", InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Respond", "Respond (hold)");
		}
		bool OnAccepted()
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			Utils.DisplayHelpText();
			Utils.SendNotification($"Go to ~y~{areaName} ~s~and assist {playerInfo.ColorTextCode}{playerInfo.Name} ({playerId})~s~.");
			Vector3 position2 = playerInfo.Position;
			BlipColor? color = (BlipColor)5;
			GPSScript.SetDestination("911 Call", position2, 60f, shortRange: false, null, color, 128, autoDelete: true, 60f);
			return true;
		}
	}
}
