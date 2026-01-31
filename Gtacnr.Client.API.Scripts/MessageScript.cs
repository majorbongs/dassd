using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Communication;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.API.Scripts;

public class MessageScript : Script
{
	[EventHandler("gtacnr:log")]
	private void OnLog(string message)
	{
		Print("Server: " + message);
	}

	[EventHandler("gtacnr:alert")]
	private async void OnAlert(string message, string? title = null)
	{
		if (title == null)
		{
			title = LocalizationController.S(Entries.Main.ALERT);
		}
		await Utils.ShowAlert(message, title);
	}

	[EventHandler("gtacnr:notify")]
	private void OnNotify(string message, int? color = null)
	{
		Utils.SendNotification(message, color);
	}

	[EventHandler("gtacnr:notifyLocalized")]
	private void OnNotifyLocalized(string key, List<object> args, int? color = null)
	{
		Utils.SendNotification(LocalizationController.S(key, args.ToArray()), color);
	}

	[EventHandler("gtacnr:helpText")]
	private void OnHelpText(string message, bool? playSound)
	{
		Utils.DisplayHelpText(message, playSound ?? true);
	}

	[EventHandler("gtacnr:helpTextLocalized")]
	private void OnHelpTextLocalized(string key, List<object> args, bool? playSound)
	{
		Utils.DisplayHelpText(LocalizationController.S(key, args.ToArray()), playSound ?? true);
	}

	[EventHandler("gtacnr:subtitle")]
	private void OnSubtitle(string message, int time)
	{
		if (time <= 0)
		{
			Utils.DisplaySubtitle(message);
		}
		else
		{
			Utils.DisplaySubtitle(message, time);
		}
	}

	[EventHandler("gtacnr:subtitleLocalized")]
	private void OnSubtitleLocalized(string key, List<object> args, int time)
	{
		string message = LocalizationController.S(key, args.ToArray());
		OnSubtitle(message, time);
	}

	[EventHandler("gtacnr:playSound")]
	private void OnPlaySound(string soundName, string soundSet, int sourcePlayerId)
	{
		string playerUid = "";
		PlayerState playerState = LatentPlayers.Get(sourcePlayerId);
		if (playerState != null)
		{
			playerUid = playerState.Uid;
		}
		if (!BlockScript.IsBlocked(playerUid))
		{
			Game.PlaySound(soundName, soundSet);
		}
	}

	[EventHandler("gtacnr:playAudio")]
	private void OnPlayAudio(string audioName, float volume, bool loop, int sourcePlayerId)
	{
		string playerUid = "";
		PlayerState playerState = LatentPlayers.Get(sourcePlayerId);
		if (playerState != null)
		{
			playerUid = playerState.Uid;
		}
		if (!BlockScript.IsBlocked(playerUid))
		{
			AudioScript.PlayAudio(audioName, volume, loop);
		}
	}

	[EventHandler("gtacnr:stopAudio")]
	private void OnStopAudio()
	{
		AudioScript.StopAudio();
	}

	[EventHandler("gtacnr:playAmbientSpeechAtCoords")]
	private void OnPlayAmbientSpeechAtCoords(string speechName, string voiceName, string positionJson, string speechParams)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = positionJson.Unjson<Vector3>();
		if (!((Entity)(object)Game.PlayerPed == (Entity)null))
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared2D(val) > 10000f))
			{
				API.PlayAmbientSpeechAtCoords(speechName, voiceName, val.X, val.Y, val.Z, speechParams);
			}
		}
	}

	[EventHandler("gtacnr:playAmbientSpeechFromPed")]
	private async void OnPlayAmbientSpeechFromPed(string speechName, string voiceName, int pedNetId, string speechParams)
	{
		if (!API.NetworkDoesEntityExistWithNetworkId(pedNetId))
		{
			return;
		}
		Entity obj = Entity.FromNetworkId(pedNetId);
		Ped ped = (Ped)(object)((obj is Ped) ? obj : null);
		if ((Entity)(object)Game.PlayerPed == (Entity)null || (Entity)(object)ped == (Entity)null || !ped.Exists())
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared2D(((Entity)ped).Position) > 10000f)
		{
			return;
		}
		Ped targetPed = ped;
		bool flag = false;
		if (ped.IsPlayer)
		{
			ped = await Utils.CreateLocalPed(Model.op_Implicit("mp_m_freemode_01"), ((Entity)targetPed).Position + Vector3.UnitZ * 0.5f, 0f);
			API.SetPedKeepTask(((PoolObject)ped).Handle, true);
			API.SetBlockingOfNonTemporaryEvents(((PoolObject)ped).Handle, true);
			API.SetEntityAsMissionEntity(((PoolObject)ped).Handle, true, true);
			((Entity)ped).IsCollisionEnabled = false;
			((Entity)ped).IsVisible = false;
			flag = true;
		}
		API.StopCurrentPlayingAmbientSpeech(((PoolObject)ped).Handle);
		API.PlayPedAmbientSpeechWithVoiceNative(((PoolObject)ped).Handle, speechName, voiceName, speechParams, false);
		if (flag)
		{
			DateTime t = DateTime.UtcNow;
			while (!Gtacnr.Utils.CheckTimePassed(t, 10000.0))
			{
				await BaseScript.Delay(50);
				((Entity)ped).Position = new Vector3(((Entity)targetPed).Position.X, ((Entity)targetPed).Position.Y, ((Entity)targetPed).Position.Z + 2.5f);
			}
			if ((Entity)(object)ped != (Entity)null && ped.Exists())
			{
				((PoolObject)ped).Delete();
			}
		}
	}

	[EventHandler("gtacnr:playAlarm")]
	private async void OnPlayAlarm(string alarmName, int duration = 0, float x = 0f, float y = 0f, float z = 0f, float range = 0f)
	{
		if (API.IsAlarmPlaying(alarmName))
		{
			return;
		}
		while (!API.PrepareAlarm(alarmName))
		{
			await BaseScript.Delay(0);
		}
		API.StartAlarm(alarmName, false);
		if (duration <= 0 && !(range > 0f))
		{
			return;
		}
		Vector3 alarmCoords = new Vector3(x, y, z);
		DateTime startT = DateTime.UtcNow;
		do
		{
			await BaseScript.Delay(100);
			if (range > 0f)
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				if (((Vector3)(ref position)).DistanceToSquared(alarmCoords) > range.Square())
				{
					break;
				}
			}
		}
		while (duration <= 0 || !Gtacnr.Utils.CheckTimePassed(startT, duration));
		API.StopAlarm(alarmName, true);
	}
}
