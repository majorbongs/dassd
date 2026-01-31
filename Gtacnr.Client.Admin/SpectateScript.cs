using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Model;

namespace Gtacnr.Client.Admin;

public class SpectateScript : Script
{
	public static bool IsSpectating { get; private set; }

	public static int SpectatingPlayerId { get; private set; }

	public static SpectateScript Instance { get; private set; }

	private static Vector3 previousLocation { get; set; }

	public SpectateScript()
	{
		Instance = this;
	}

	public static async Task StartSpectate(int targetId)
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		if (NoClipScript.IsNoClipActive)
		{
			Utils.PlayErrorSound();
			return;
		}
		if (targetId == Game.Player.ServerId)
		{
			Utils.DisplayHelpText("You cannot ~r~spectate ~s~yourself!", playSound: false);
			Utils.PlayErrorSound();
			return;
		}
		if (IsSpectating)
		{
			await EndSpectate();
			await BaseScript.Delay(1000);
		}
		if (await Instance.TriggerServerEventAsync<int>("gtacnr:admin:startSpectate", new object[1] { targetId }) == 1)
		{
			await Instance.SpectateInternal(targetId);
		}
		else
		{
			Utils.DisplayErrorMessage();
		}
	}

	public static async Task EndSpectate(bool triggerServer = true)
	{
		if (!IsSpectating)
		{
			return;
		}
		if (triggerServer)
		{
			int num = await Instance.TriggerServerEventAsync<int>("gtacnr:admin:endSpectate", new object[0]);
			if (num != 1)
			{
				Utils.DisplayErrorMessage(0, -1, $"End spectate - server returned {num}");
			}
		}
		API.NetworkSetInSpectatorMode(false, 0);
		API.SetMinimapInSpectatorMode(false, 0);
		Instance.DetachTasks();
		((Entity)Game.PlayerPed).IsCollisionEnabled = true;
		await Utils.TeleportToCoords(previousLocation, -1f, Utils.TeleportFlags.VisualEffects);
		((Entity)Game.PlayerPed).IsVisible = true;
		((Entity)Game.PlayerPed).IsPositionFrozen = false;
		IsSpectating = false;
	}

	private async Task SpectateInternal(int targetId)
	{
		PlayerState playerState = LatentPlayers.Get(targetId);
		if (playerState == null)
		{
			Utils.DisplayErrorMessage(0, -1, "Unable to get target info.");
			return;
		}
		IsSpectating = true;
		SpectatingPlayerId = targetId;
		previousLocation = ((Entity)Game.PlayerPed).Position;
		((Entity)Game.PlayerPed).IsVisible = false;
		((Entity)Game.PlayerPed).IsCollisionEnabled = false;
		((Entity)Game.PlayerPed).IsPositionFrozen = true;
		await SpectateTeleport(playerState.Position);
		AttachTasks();
	}

	private void AttachTasks()
	{
		base.Update += SpectateTick;
		KeysScript.AttachListener((Control)205, OnKeyEvent, 100);
		Utils.AddInstructionalButton("spectateCancel", new InstructionalButton("Stop spectating", 2, (Control)205));
	}

	private void DetachTasks()
	{
		base.Update -= SpectateTick;
		KeysScript.DetachListener((Control)205, OnKeyEvent);
		Utils.RemoveInstructionalButton("spectateCancel");
	}

	private async Coroutine SpectateTick()
	{
		if (IsSpectating && SpectatingPlayerId != 0)
		{
			await Script.Wait(1000);
			Player val = ((IEnumerable<Player>)((BaseScript)this).Players).FirstOrDefault((Player p) => p.ServerId == SpectatingPlayerId);
			if (!(val == (Player)null))
			{
				Vector3 position = ((Entity)val.Character).Position;
				((Entity)Game.PlayerPed).Position = new Vector3(position.X, position.Y, position.Z - 25f);
			}
		}
	}

	private static bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (eventType == KeyEventType.JustPressed)
		{
			EndSpectate();
			return true;
		}
		return false;
	}

	[EventHandler("gtacnr:admin:stopSpectate")]
	private async void OnStopSpectate()
	{
		await EndSpectate(triggerServer: false);
	}

	[EventHandler("gtacnr:admin:updateSpectatingPosition")]
	private async void OnUpdateSpectatingPosition(int targetId, Vector3 position)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (IsSpectating && SpectatingPlayerId == targetId)
		{
			await SpectateTeleport(position);
		}
	}

	private async Task SpectateTeleport(Vector3 position)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		await Utils.TeleportToCoords(new Vector3(position.X, position.Y, position.Z - 25f), -1f, Utils.TeleportFlags.VisualEffects);
		Player val = ((IEnumerable<Player>)((BaseScript)this).Players).FirstOrDefault((Player p) => p.ServerId == SpectatingPlayerId);
		if (val == (Player)null)
		{
			Utils.DisplayErrorMessage(0, -1, "Unable to get target player.");
			return;
		}
		API.NetworkSetInSpectatorMode(true, ((PoolObject)val.Character).Handle);
		API.SetMinimapInSpectatorMode(true, ((PoolObject)val.Character).Handle);
	}
}
