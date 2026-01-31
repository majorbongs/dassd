using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Vehicles;

public class GPSScript : Script
{
	private static Blip currentDestinationBlip;

	private static bool autoDeleteBlip;

	private static float autoDeleteBlipRange;

	private static Action OnArrival;

	public static bool IsCurrentBlipLocked { get; set; }

	public static string CurrentDestinationName { get; private set; }

	protected override void OnStarted()
	{
		Chat.AddSuggestion("/cleargps", LocalizationController.S(Entries.Main.CLEARGPS_DESC));
		Chat.AddSuggestion("/sendgps", LocalizationController.S(Entries.Main.SENDGPS_DESC), new ChatParamSuggestion("player", LocalizationController.S(Entries.Main.SENDGPS_PARAM_PLAYER_DESC)));
		Chat.AddSuggestion("/sendwaypoint", LocalizationController.S(Entries.Main.SENDWAYPOINT_DESC), new ChatParamSuggestion("player", LocalizationController.S(Entries.Main.SENDWAYPOINT_PARAM_PLAYER_DESC)));
	}

	public static Blip SetDestination(string name, Vector3 position, float radius = 0f, bool shortRange = true, BlipSprite? sprite = null, BlipColor? color = null, int alpha = 255, bool autoDelete = false, float autoDeleteRange = 15f, Action onArrival = null)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		if (IsCurrentBlipLocked)
		{
			return null;
		}
		Blip obj = currentDestinationBlip;
		if (obj != null)
		{
			((PoolObject)obj).Delete();
		}
		OnArrival = onArrival;
		Blip val = ((radius == 0f) ? World.CreateBlip(position) : World.CreateBlip(position, radius));
		if (sprite.HasValue)
		{
			val.Sprite = sprite.Value;
			val.Alpha = alpha;
		}
		else
		{
			val.Sprite = (BlipSprite)(-1);
			val.Alpha = 0;
		}
		if (color.HasValue)
		{
			val.Color = color.Value;
		}
		else
		{
			val.Color = (BlipColor)0;
		}
		val.IsShortRange = shortRange;
		Utils.SetBlipName(val, name, "gps");
		val.ShowRoute = true;
		API.SetBlipRouteColour(((PoolObject)val).Handle, 5);
		currentDestinationBlip = val;
		autoDeleteBlip = autoDelete;
		autoDeleteBlipRange = autoDeleteRange;
		CurrentDestinationName = name;
		Game.PlaySound("WAYPOINT_SET", "HUD_FRONTEND_DEFAULT_SOUNDSET");
		return val;
	}

	public static void ClearDestination(string onlyForName = null)
	{
		if (!IsCurrentBlipLocked && currentDestinationBlip != (Blip)null && (onlyForName == null || onlyForName == CurrentDestinationName))
		{
			((PoolObject)currentDestinationBlip).Delete();
			OnArrival = null;
		}
	}

	[EventHandler("gtacnr:died")]
	private void OnDied()
	{
		ClearDestination();
	}

	[EventHandler("gtacnr:police:onArrested")]
	private void OnArrested()
	{
		ClearDestination();
	}

	[EventHandler("gtacnr:clearGPS")]
	private void OnClearGPS()
	{
		ClearDestination();
	}

	[EventHandler("gtacnr:GPSShared")]
	private async void OnGPSShared(int playerId, byte sharedLocationTypeByte, Vector3 position = default(Vector3))
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		SharedLocationType sharedLocationType = (SharedLocationType)sharedLocationTypeByte;
		PlayerState playerInfo = LatentPlayers.Get(playerId);
		bool wasAccepted;
		if (playerInfo != null)
		{
			string text = default(string);
			switch (sharedLocationType)
			{
			case SharedLocationType.GPS:
				text = LocalizationController.S(Entries.Main.GPS_SHARED_PROMPT, playerInfo.ColorNameAndId);
				break;
			case SharedLocationType.Waypoint:
				text = LocalizationController.S(Entries.Main.WAYPOINT_SHARED_PROMPT, playerInfo.ColorNameAndId);
				break;
			default:
				global::_003CPrivateImplementationDetails_003E.ThrowInvalidOperationException();
				break;
			}
			string message = text;
			wasAccepted = false;
			await InteractiveNotificationsScript.Show(message, InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Accept GPS", "Accept GPS (hold)");
			if (!wasAccepted)
			{
				BaseScript.TriggerServerEvent("gtacnr:sharedGPSIgnored", new object[1] { playerId });
			}
		}
		bool OnAccepted()
		{
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			wasAccepted = true;
			Utils.DisplayHelpText();
			playerInfo = LatentPlayers.Get(playerId);
			if (playerInfo == null)
			{
				return false;
			}
			BaseScript.TriggerServerEvent("gtacnr:sharedGPSAccepted", new object[1] { playerId });
			if (position == default(Vector3))
			{
				position = playerInfo.Position;
			}
			SetDestination("Shared Position", position, 0f, shortRange: false, (BlipSprite)162, null, 255, autoDelete: true);
			return true;
		}
	}

	[Command("cleargps")]
	private void ClearGPSCommand()
	{
		if (IsCurrentBlipLocked)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, LocalizationController.S(Entries.Main.CLEARGPS_ACTIVE_MISSION));
		}
		else if (currentDestinationBlip != (Blip)null)
		{
			((PoolObject)currentDestinationBlip).Delete();
			currentDestinationBlip = null;
			Chat.AddMessage(Gtacnr.Utils.Colors.Info, LocalizationController.S(Entries.Main.CLEARGPS_SUCCESS));
		}
		else
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, LocalizationController.S(Entries.Main.CLEARGPS_SUCCESS));
		}
	}

	[Command("sendgps")]
	private void SendGPSCommand(string[] args)
	{
		ShareGPS(args);
	}

	[Command("sharegps")]
	private void ShareGPSCommand(string[] args)
	{
		ShareGPS(args);
	}

	private void ShareGPS(string[] args)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (!args.TryParseIntCollection(out List<int> result))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /sendgps [player id or ids separated by a space]");
			return;
		}
		BaseScript.TriggerServerEvent("gtacnr:shareGPS", new object[3]
		{
			result.Json(),
			(byte)0,
			(object)default(Vector3)
		});
	}

	[Command("sendwaypoint")]
	private void SendWaypointCommand(string[] args)
	{
		ShareWaypoint(args);
	}

	[Command("sharewaypoint")]
	private void ShareWaypointCommand(string[] args)
	{
		ShareWaypoint(args);
	}

	private void ShareWaypoint(string[] args)
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		List<int> list = new List<int>();
		if (args.Length != 0)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (!int.TryParse(args[i], out var result))
				{
					Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /sendwaypoint [player id or ids separated by a space]");
					return;
				}
				list.Add(result);
			}
		}
		int firstBlipInfoId = API.GetFirstBlipInfoId(8);
		if (!API.DoesBlipExist(firstBlipInfoId))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Warning, LocalizationController.S(Entries.Main.SENDWAYPOINT_NOT_SELECTED));
			return;
		}
		BaseScript.TriggerServerEvent("gtacnr:shareGPS", new object[3]
		{
			list.Json(),
			(byte)1,
			API.GetBlipInfoIdCoord(firstBlipInfoId)
		});
	}

	[Update]
	private async Coroutine ClearGPSTask()
	{
		await Script.Wait(1000);
		if (currentDestinationBlip != (Blip)null && autoDeleteBlip)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (((Vector3)(ref position)).DistanceToSquared(currentDestinationBlip.Position) <= autoDeleteBlipRange * autoDeleteBlipRange)
			{
				((PoolObject)currentDestinationBlip).Delete();
				currentDestinationBlip = null;
				OnArrival?.Invoke();
			}
		}
	}
}
