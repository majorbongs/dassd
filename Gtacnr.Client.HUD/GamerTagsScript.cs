using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Jobs.Hitman;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.HUD;

public class GamerTagsScript : Script
{
	public enum GamerTags : byte
	{
		GAMER_NAME,
		CREW_TAG,
		HEALTH_ARMOR,
		BIG_TEXT,
		AUDIO_ICON,
		MP_USING_MENU,
		MP_PASSIVE_MODE,
		WANTED_STARS,
		MP_DRIVER,
		MP_CO_DRIVER,
		MP_TAGGED,
		GAMER_NAME_NEARBY,
		ARROW,
		MP_PACKAGES,
		INV_IF_PED_FOLLOWING,
		RANK_TEXT,
		MP_TYPING,
		MP_BAG_LARGE,
		MP_TAG_ARROW,
		MP_GANG_CEO,
		MP_GANG_BIKER,
		BIKER_ARROW,
		MC_ROLE_PRESIDENT,
		MC_ROLE_VICE_PRESIDENT,
		MC_ROLE_ROAD_CAPTAIN,
		MC_ROLE_SARGEANT,
		MC_ROLE_ENFORCER,
		MC_ROLE_PROSPECT,
		MP_TRANSMITTER,
		MP_BOMB
	}

	private static GamerTagsScript instance;

	private static readonly DateTime StartTime = DateTime.UtcNow;

	private const uint FLASH_INTERVAL = 400u;

	private static readonly float DEFAULT_DRAW_DISTANCE = 150f;

	private static readonly float AIMING_DRAW_DISTANCE = 300f;

	private static bool renderHealthPercentageTaskAttached;

	private static bool renderHealthPercentage;

	private static bool renderOverheadSignsTasksAttached;

	private static bool renderOverheadSigns;

	private static HashSet<Player> renderedPlayers = new HashSet<Player>();

	public static bool FlashState => (long)(DateTime.UtcNow - StartTime).TotalMilliseconds / 400 % 2 == 0;

	public static bool RenderHealthPercentage
	{
		get
		{
			return renderHealthPercentage;
		}
		set
		{
			renderHealthPercentage = value;
			if (value && !renderHealthPercentageTaskAttached)
			{
				renderHealthPercentageTaskAttached = true;
				instance.Update += instance.DrawHealthPercentageTask;
			}
			else if (!value && renderHealthPercentageTaskAttached)
			{
				renderHealthPercentageTaskAttached = false;
				instance.Update -= instance.DrawHealthPercentageTask;
			}
		}
	}

	public static bool RenderOverheadSigns
	{
		get
		{
			return renderOverheadSigns;
		}
		set
		{
			renderOverheadSigns = value;
			if (value && !renderOverheadSignsTasksAttached)
			{
				renderOverheadSignsTasksAttached = true;
				instance.Update += instance.DrawOverheadSignsTask;
			}
			else if (!value && renderOverheadSignsTasksAttached)
			{
				renderOverheadSignsTasksAttached = false;
				instance.Update -= instance.DrawOverheadSignsTask;
			}
		}
	}

	public GamerTagsScript()
	{
		instance = this;
		RenderHealthPercentage = Preferences.HealthPercentEnabled.Get();
		RenderOverheadSigns = Preferences.OverheadSignsEnabled.Get();
	}

	protected override async void OnStarted()
	{
		API.SetMpGamerTagsUseVehicleBehavior(false);
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (API.IsValidMpGamerTagMovie(player.Handle) || API.IsMpGamerTagActive(player.Handle))
			{
				API.RemoveMpGamerTag(player.Handle);
			}
		}
	}

	[EventHandler("gtacnr:screenshotModeChanged")]
	private void OnScreenshotModeChanged()
	{
		Refresh();
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		if (SpawnScript.HasSpawned)
		{
			Refresh();
			await Script.Wait(100);
		}
	}

	private async Coroutine DrawHealthPercentageTask()
	{
		if (!RenderHealthPercentage || Utils.IsScreenFadingInProgress() || Utils.IsSwitchInProgress() || API.IsPauseMenuActive())
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float gameplayCamZoom = API.GetGameplayCamZoom();
		foreach (Player renderedPlayer in renderedPlayers)
		{
			float num = ((float)((Entity)renderedPlayer.Character).Health / (float)((Entity)renderedPlayer.Character).MaxHealth * 100f).Clamp(0f, 100f);
			float num2 = ((float)renderedPlayer.Character.Armor / 2f).Clamp(0f, 100f);
			Vector3 position2 = ((Entity)renderedPlayer.Character).Position;
			float num3 = (float)Math.Sqrt(((Vector3)(ref position2)).DistanceToSquared(position));
			float num4 = 1.5f + 0.08f * num3 / gameplayCamZoom;
			Utils.Draw3DText($"~r~{num:0}% ~b~{num2:0}%", ((Entity)renderedPlayer.Character).Position + new Vector3(0f, 0f, num4), new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		}
	}

	private async Coroutine DrawOverheadSignsTask()
	{
		if (!RenderOverheadSigns || Utils.IsScreenFadingInProgress() || Utils.IsSwitchInProgress() || API.IsPauseMenuActive())
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float gameplayCamZoom = API.GetGameplayCamZoom();
		foreach (Player item in renderedPlayers.Union<Player>((IEnumerable<Player>)(object)new Player[1] { Game.Player }))
		{
			Vector3 position2 = ((Entity)item.Character).Position;
			float num = (float)Math.Sqrt(((Vector3)(ref position2)).DistanceToSquared(position));
			if (num > 35f)
			{
				continue;
			}
			PlayerState playerState = LatentPlayers.Get(item);
			if (playerState != null)
			{
				float num2 = 1.5f + 0.06f * num / gameplayCamZoom;
				string signText = playerState.SignText;
				if (!string.IsNullOrEmpty(signText))
				{
					Utils.Draw3DText(signText, ((Entity)item.Character).Position + new Vector3(0f, 0f, num2), new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
				}
			}
		}
	}

	public static void Refresh()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Expected O, but got Unknown
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		int num = API.PlayerPedId();
		int num2 = API.PlayerId();
		bool flag = ModeratorMenuScript.IsOnDuty || ModeratorMenuScript.IsInGhostMode;
		JobsEnum cachedJobEnum = Gtacnr.Client.API.Jobs.CachedJobEnum;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		bool flag2 = cachedJobEnum.IsPublicService();
		bool flag3 = Game.PlayerPed.IsAiming || Game.PlayerPed.IsAimingFromCover;
		bool flag4 = Game.PlayerPed.IsInVehicle();
		float num3 = (flag3 ? AIMING_DRAW_DISTANCE : DEFAULT_DRAW_DISTANCE);
		if (MainScript.HardcoreMode)
		{
			num3 = ((flag3 || flag4) ? 400f : 50f);
		}
		float num4 = num3.Square();
		API.SetMpGamerTagsVisibleDistance(num3);
		HashSet<Player> hashSet = new HashSet<Player>(renderedPlayers);
		renderedPlayers.Clear();
		IEnumerable<PlayerState> enumerable = ((!HideHUDScript.EnableGamerTags) ? Enumerable.Empty<PlayerState>() : LatentPlayers.All);
		foreach (PlayerState item in enumerable)
		{
			int id = item.Id;
			int playerFromServerId = API.GetPlayerFromServerId(id);
			if (playerFromServerId == -1 || playerFromServerId == num2)
			{
				continue;
			}
			Player val = new Player(playerFromServerId);
			if (val == (Player)null || (Entity)(object)val.Character == (Entity)null)
			{
				continue;
			}
			float num5 = ((Vector3)(ref position)).DistanceToSquared2D(((Entity)val.Character).Position);
			if ((num5 > num4 && !flag) || !API.HasEntityClearLosToEntity(num, ((PoolObject)val.Character).Handle, 17))
			{
				continue;
			}
			bool flag5 = true;
			bool hardcoreMode = MainScript.HardcoreMode;
			if (hardcoreMode)
			{
				float z = API.GetGameplayCamRot(0).Z;
				Vector3 val2 = Vector3.Normalize(((Entity)val.Character).Position - position);
				float headingFromVector_2d = API.GetHeadingFromVector_2d(val2.X, val2.Y);
				float num6 = (z - headingFromVector_2d + 180f + 360f) % 360f - 180f;
				flag5 = num6 <= 20f && num6 >= -20f;
			}
			if (hardcoreMode && !flag5 && num5 > 100f)
			{
				continue;
			}
			bool ghostMode = item.GhostMode;
			if (ghostMode && (int)StaffLevelScript.StaffLevel < 100)
			{
				continue;
			}
			JobsEnum jobEnum = item.JobEnum;
			byte wantedLevel = item.WantedLevel;
			int num7 = item.Bounty;
			bool flag6 = cachedJobEnum == JobsEnum.Hitman && HitmanDispatch.PlayerContracts.ContainsKey(val.ServerId);
			if (flag6)
			{
				num7 += Convert.ToInt32(HitmanDispatch.PlayerContracts[val.ServerId].Reward);
			}
			bool num8 = jobEnum.IsPublicService();
			string text = $"{item.Name.RegionalIndicatorsToLetters().RemoveHealthOverlapingChars()} ({id})";
			if ((wantedLevel == 5 || flag6) && num7 > 0 && !cachedJobEnum.IsEMSOrFD() && !item.AdminDuty)
			{
				text += $" (${num7})";
			}
			byte b = Gtacnr.Utils.GetGamerTagColor(jobEnum, wantedLevel);
			if (!flag2 && jobEnum.IsPolice())
			{
				int vehiclePedIsIn = API.GetVehiclePedIsIn(API.GetPlayerPed(playerFromServerId), false);
				if (vehiclePedIsIn != 0)
				{
					int entityModel = API.GetEntityModel(vehiclePedIsIn);
					Vector3 entityCoords = API.GetEntityCoords(vehiclePedIsIn, false);
					if (UnmarkedVehiclesScript.UnmarkedCarModels.Contains(entityModel) && !API.IsVehicleSirenOn(vehiclePedIsIn))
					{
						Vector3 position2 = ((Entity)Game.PlayerPed).Position;
						if (((Vector3)(ref position2)).DistanceToSquared2D(entityCoords) > 2025f)
						{
							b = 1;
						}
					}
				}
			}
			renderedPlayers.Add(val);
			bool isDead = ((Entity)val.Character).IsDead;
			bool isCuffed = item.IsCuffed;
			bool isSurrendering = item.IsSurrendering;
			bool flag7 = !isDead && (isCuffed || isSurrendering);
			int num9 = ((!flag7 && !item.AdminDuty) ? wantedLevel : 0);
			bool spawnProtection = item.SpawnProtection;
			bool flag8 = !num8 && item.IsOnCoke;
			bool flag9 = !num8 && item.IsOnOpiates;
			bool flag10 = !num8 && item.IsOnMeth;
			bool flag11 = !num8 && item.StolenWalletOwner == Game.Player.ServerId;
			bool flag12 = !flag2 && DeathScript.RevengeTargetMarkersEnabled && (DeathScript.CachedRevengeData?.Targets.Contains(val.ServerId) ?? false);
			bool flag13 = !num8 && DeathScript.RevengeClaimantMarkersEnabled && (DeathScript.CachedRevengeData?.Claimants.Contains(val.ServerId) ?? false);
			bool voiceChatEnabled = item.VoiceChatEnabled;
			bool isTyping = item.IsTyping;
			bool flag14 = API.NetworkIsPlayerTalking(playerFromServerId);
			API.CreateMpGamerTagForNetPlayer(playerFromServerId, text, false, false, (string)null, 0, 0, 0, 0);
			API.SetMpGamerTagVisibility(playerFromServerId, 0, true);
			API.SetMpGamerTagVisibility(playerFromServerId, 1, false);
			API.SetMpGamerTagVisibility(playerFromServerId, 2, !isDead);
			API.SetMpGamerTagVisibility(playerFromServerId, 4, flag14);
			API.SetMpGamerTagVisibility(playerFromServerId, 7, num9 > 0);
			API.SetMpGamerTagVisibility(playerFromServerId, 9, !voiceChatEnabled);
			API.SetMpGamerTagVisibility(playerFromServerId, 16, isTyping);
			API.SetMpGamerTagVisibility(playerFromServerId, 22, flag8);
			API.SetMpGamerTagVisibility(playerFromServerId, 23, flag9);
			API.SetMpGamerTagVisibility(playerFromServerId, 24, flag10);
			API.SetMpGamerTagVisibility(playerFromServerId, 25, isCuffed);
			API.SetMpGamerTagVisibility(playerFromServerId, 26, flag11);
			API.SetMpGamerTagVisibility(playerFromServerId, 19, flag12);
			API.SetMpGamerTagVisibility(playerFromServerId, 8, flag13);
			API.SetMpGamerTagName(playerFromServerId, text);
			API.SetMpGamerTagWantedLevel(playerFromServerId, num9);
			API.SetMpGamerTagAlpha(playerFromServerId, 2, 255);
			API.SetMpGamerTagAlpha(playerFromServerId, 4, 255);
			API.SetMpGamerTagAlpha(playerFromServerId, 16, 255);
			API.SetMpGamerTagColour(playerFromServerId, 0, (int)b);
			API.SetMpGamerTagColour(playerFromServerId, 15, (int)b);
			API.SetMpGamerTagColour(playerFromServerId, 7, (int)b);
			API.SetMpGamerTagColour(playerFromServerId, 19, 27);
			API.SetMpGamerTagColour(playerFromServerId, 8, 12);
			API.SetMpGamerTagHealthBarColour(playerFromServerId, 6);
			if (spawnProtection)
			{
				if (FlashState)
				{
					API.SetMpGamerTagHealthBarColour(playerFromServerId, 25);
				}
				else
				{
					API.SetMpGamerTagHealthBarColour(playerFromServerId, 0);
				}
			}
			if (isDead)
			{
				if (FlashState)
				{
					API.SetMpGamerTagColour(playerFromServerId, 0, (int)b);
					API.SetMpGamerTagColour(playerFromServerId, 7, (int)b);
				}
				else
				{
					API.SetMpGamerTagColour(playerFromServerId, 0, 68);
					API.SetMpGamerTagColour(playerFromServerId, 7, 68);
				}
			}
			if (flag7)
			{
				if (FlashState)
				{
					API.SetMpGamerTagColour(playerFromServerId, 0, (int)b);
					API.SetMpGamerTagColour(playerFromServerId, 7, (int)b);
				}
				else
				{
					API.SetMpGamerTagColour(playerFromServerId, 0, 0);
					API.SetMpGamerTagColour(playerFromServerId, 7, 0);
				}
			}
			if (ghostMode)
			{
				if ((int)StaffLevelScript.StaffLevel >= 100)
				{
					if (FlashState)
					{
						API.SetMpGamerTagColour(playerFromServerId, 0, 0);
						API.SetMpGamerTagColour(playerFromServerId, 7, 0);
					}
					else
					{
						API.SetMpGamerTagColour(playerFromServerId, 0, 18);
						API.SetMpGamerTagColour(playerFromServerId, 7, 18);
					}
				}
			}
			else if (item.AdminDuty)
			{
				API.SetMpGamerTagColour(playerFromServerId, 0, 18);
				API.SetMpGamerTagColour(playerFromServerId, 7, 18);
			}
		}
		hashSet.ExceptWith(renderedPlayers);
		foreach (Player item2 in hashSet)
		{
			if (API.IsValidMpGamerTagMovie(item2.Handle) || API.IsMpGamerTagActive(item2.Handle))
			{
				API.RemoveMpGamerTag(item2.Handle);
			}
		}
	}
}
