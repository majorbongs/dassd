using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Communication;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Client.Zones;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.HUD;

public class RadarBlipsScript : Script
{
	private static bool colorBlindMode = false;

	private static RadarBlipsScript instance;

	private static Dictionary<int, Blip> farPlayerBlips = new Dictionary<int, Blip>();

	public static bool ColorBlindMode
	{
		get
		{
			return colorBlindMode;
		}
		set
		{
			colorBlindMode = value;
			ForceRefresh();
		}
	}

	public RadarBlipsScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		ColorBlindMode = Preferences.ColorBlindModeEnabled.Get();
	}

	private static async void ForceRefresh()
	{
		await instance.RefreshTask();
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		await Script.Wait(450);
		JobsEnum cachedJobEnum = Gtacnr.Client.API.Jobs.CachedJobEnum;
		int num = Gtacnr.Client.API.Crime.CachedWantedLevel ?? (-1);
		if (num == -1)
		{
			return;
		}
		PlayerState playerState = LatentPlayers.Get(Game.Player.ServerId);
		bool flag = ModeratorMenuScript.IsOnDuty || ModeratorMenuScript.IsInGhostMode;
		HashSet<int> hashSet = new HashSet<int>();
		foreach (PlayerState item in LatentPlayers.All)
		{
			int id = item.Id;
			int playerFromServerId = API.GetPlayerFromServerId(id);
			if (playerFromServerId == API.PlayerId() || item.RoutingBucket != playerState.RoutingBucket)
			{
				continue;
			}
			JobsEnum jobEnum = item.JobEnum;
			byte wantedLevel = item.WantedLevel;
			Player val = new Player(playerFromServerId);
			float playerBlipDrawDistance = GetPlayerBlipDrawDistance(cachedJobEnum, num, jobEnum, wantedLevel);
			Blip val2 = null;
			BlipColor color = (BlipColor)Gtacnr.Utils.GetRadarBlipColor(jobEnum, wantedLevel);
			BlipSprite sprite = (BlipSprite)Gtacnr.Utils.GetRadarBlipSprite(jobEnum, wantedLevel);
			BlipSprite val3 = (BlipSprite)1;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool ghostMode;
			if (playerFromServerId != -1)
			{
				if (val == (Player)null || (Entity)(object)val.Character == (Entity)null || API.NetworkIsPlayerConcealed(val.Handle))
				{
					continue;
				}
				int playerPed = API.GetPlayerPed(playerFromServerId);
				if (playerPed == 0)
				{
					continue;
				}
				int num2 = API.GetBlipFromEntity(playerPed);
				ghostMode = item.GhostMode;
				if (ShouldDrawBlip(item, ((Entity)val.Character).Position, playerBlipDrawDistance, ghostMode) || flag)
				{
					if (num2 == 0)
					{
						num2 = API.AddBlipForEntity(playerPed);
					}
					val2 = new Blip(num2);
					if (!cachedJobEnum.IsPublicService() && jobEnum.IsPolice())
					{
						int vehiclePedIsIn = API.GetVehiclePedIsIn(playerPed, false);
						if (vehiclePedIsIn != 0)
						{
							int entityModel = API.GetEntityModel(vehiclePedIsIn);
							Vector3 entityCoords = API.GetEntityCoords(vehiclePedIsIn, false);
							if (UnmarkedVehiclesScript.UnmarkedCarModels.Contains(entityModel) && !API.IsVehicleSirenOn(vehiclePedIsIn))
							{
								Vector3 position = ((Entity)Game.PlayerPed).Position;
								if (((Vector3)(ref position)).DistanceToSquared2D(entityCoords) > 2025f)
								{
									color = (BlipColor)0;
									sprite = (BlipSprite)1;
								}
							}
						}
					}
					bool isCuffed = item.IsCuffed;
					bool isSurrendering = item.IsSurrendering;
					flag3 = !flag2 && (isCuffed || isSurrendering);
					flag2 = ((Entity)val.Character).IsDead;
					Vehicle currentVehicle = val.Character.CurrentVehicle;
					switch ((currentVehicle != null) ? new VehicleClass?(currentVehicle.ClassType) : ((VehicleClass?)null))
					{
					case (VehicleClass)1L:
						val3 = (BlipSprite)422;
						sprite = val3;
						break;
					case (VehicleClass)2L:
						val3 = (BlipSprite)423;
						sprite = val3;
						break;
					case (VehicleClass)0L:
						val3 = (BlipSprite)427;
						sprite = val3;
						break;
					}
					if (val.Character.IsInPlane || val.Character.IsInBoat)
					{
						val2.Rotation = ((Entity)val.Character).Heading.ToInt();
					}
					if (ColorBlindMode && val2.Sprite != val3)
					{
						val2.Scale = 0.9f;
					}
					else
					{
						val2.Scale = 1.2f;
					}
					if (DeathScript.CachedRevengeData != null)
					{
						bool num3 = DeathScript.CachedRevengeData.Targets.Contains(item.Id);
						bool flag5 = DeathScript.CachedRevengeData.Claimants.Contains(item.Id);
						if (num3 && !playerState.JobEnum.IsPublicService() && DeathScript.RevengeTargetMarkersEnabled)
						{
							API.ShowOutlineIndicatorOnBlip(((PoolObject)val2).Handle, true);
							Function.Call((Hash)1479754035503172075L, (InputArgument[])(object)new InputArgument[4]
							{
								InputArgument.op_Implicit(((PoolObject)val2).Handle),
								InputArgument.op_Implicit(255),
								InputArgument.op_Implicit(61),
								InputArgument.op_Implicit(56)
							});
							flag4 = true;
						}
						else if (flag5 && !item.JobEnum.IsPublicService() && DeathScript.RevengeClaimantMarkersEnabled)
						{
							API.ShowOutlineIndicatorOnBlip(((PoolObject)val2).Handle, true);
							Function.Call((Hash)1479754035503172075L, (InputArgument[])(object)new InputArgument[4]
							{
								InputArgument.op_Implicit(((PoolObject)val2).Handle),
								InputArgument.op_Implicit(235),
								InputArgument.op_Implicit(201),
								InputArgument.op_Implicit(88)
							});
							flag4 = true;
						}
					}
				}
				else if (num2 != 0)
				{
					((PoolObject)new Blip(num2)).Delete();
				}
			}
			else
			{
				ghostMode = item.GhostMode;
				if (ShouldDrawBlip(item, item.Position, playerBlipDrawDistance, ghostMode) || flag)
				{
					if (!farPlayerBlips.ContainsKey(item.Id))
					{
						farPlayerBlips[item.Id] = World.CreateBlip(item.Position);
					}
					val2 = farPlayerBlips[item.Id];
					val2.Position = item.Position;
					if (ColorBlindMode && val2.Sprite != val3)
					{
						val2.Scale = 0.8f;
					}
					else
					{
						val2.Scale = 0.9f;
					}
					hashSet.Add(item.Id);
				}
			}
			if (!(val2 != (Blip)null))
			{
				continue;
			}
			if (ColorBlindMode)
			{
				val2.Sprite = sprite;
			}
			else
			{
				val2.Sprite = val3;
			}
			val2.Name = $"{item.Name} ({id})";
			val2.Color = color;
			API.SetBlipCategory(((PoolObject)val2).Handle, 7);
			if (HideHUDScript.ShowPlayerNameTagsOnBlips)
			{
				API.SetBlipPriority(((PoolObject)val2).Handle, 99);
			}
			if (flag2)
			{
				ApplyBlipDeadStyle(val2, color);
			}
			if (flag3)
			{
				ApplyBlipCustodyStyle(val2, color);
			}
			if (ghostMode)
			{
				if ((int)StaffLevelScript.StaffLevel >= 100)
				{
					ApplyBlipGhostStyle(val2);
				}
			}
			else if (item.AdminDuty)
			{
				ApplyBlipDutyStyle(val2);
			}
			if (PartyScript.PartyMembers.Contains(item.Id))
			{
				API.ShowOutlineIndicatorOnBlip(((PoolObject)val2).Handle, true);
				Function.Call((Hash)1479754035503172075L, (InputArgument[])(object)new InputArgument[4]
				{
					InputArgument.op_Implicit(((PoolObject)val2).Handle),
					InputArgument.op_Implicit(114),
					InputArgument.op_Implicit(204),
					InputArgument.op_Implicit(114)
				});
				flag4 = true;
			}
			if (!flag4)
			{
				API.ShowOutlineIndicatorOnBlip(((PoolObject)val2).Handle, false);
			}
		}
		HashSet<int> hashSet2 = new HashSet<int>(farPlayerBlips.Keys);
		hashSet2.ExceptWith(hashSet);
		foreach (int item2 in hashSet2)
		{
			((PoolObject)farPlayerBlips[item2]).Delete();
			farPlayerBlips.Remove(item2);
		}
	}

	private bool ShouldDrawBlip(PlayerState otherState, Vector3 otherPos, float drawDistance, bool isInGhostMode = false)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared2D(otherPos) < drawDistance * drawDistance || (otherState.JobEnum == JobsEnum.Mechanic && otherState.WantedLevel == 0 && BusinessScript.Businesses.Values.Any(delegate(Business b)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			if (b.Mechanic != null)
			{
				Vector3 location = b.Location;
				return ((Vector3)(ref location)).DistanceToSquared2D(otherPos) < 900f;
			}
			return false;
		})) || PartyScript.PartyMembers.Contains(otherState.Id))
		{
			goto IL_008c;
		}
		if (otherState.WantedLevel == 5)
		{
			Redzone? activeBonusRedzone = RedzoneScript.ActiveBonusRedzone;
			if (activeBonusRedzone != null && activeBonusRedzone.IsPointInside(otherPos))
			{
				goto IL_008c;
			}
		}
		goto IL_00ba;
		IL_008c:
		if (!isInGhostMode || (int)StaffLevelScript.StaffLevel >= 100)
		{
			if (MainScript.HardcoreMode && !ModeratorMenuScript.IsOnDuty)
			{
				return PartyScript.PartyMembers.Contains(otherState.Id);
			}
			return true;
		}
		goto IL_00ba;
		IL_00ba:
		return false;
	}

	private float GetPlayerBlipDrawDistance(JobsEnum myJob, int myWantedLevel, JobsEnum targetJob, int targetWantedLevel)
	{
		if (targetWantedLevel == 5)
		{
			return 500f;
		}
		if (myJob.IsPublicService())
		{
			if (targetJob.IsPublicService())
			{
				return 999999f;
			}
			if (myJob.IsPolice())
			{
				switch (targetWantedLevel)
				{
				case 2:
					return 300f;
				case 3:
					return 350f;
				case 4:
					return 400f;
				}
			}
		}
		else if (targetJob.IsPolice())
		{
			switch (myWantedLevel)
			{
			case 2:
				return 300f;
			case 3:
				return 350f;
			case 4:
				return 400f;
			case 5:
				return 500f;
			}
		}
		return 200f;
	}

	private void ApplyBlipDeadStyle(Blip blip, BlipColor color)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		if (GamerTagsScript.FlashState)
		{
			blip.Color = color;
		}
		else
		{
			blip.Color = (BlipColor)40;
		}
		if (ColorBlindMode)
		{
			blip.Sprite = (BlipSprite)364;
		}
	}

	private void ApplyBlipCustodyStyle(Blip blip, BlipColor color)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		if (GamerTagsScript.FlashState)
		{
			blip.Color = color;
		}
		else
		{
			blip.Color = (BlipColor)0;
		}
		if (ColorBlindMode)
		{
			blip.Sprite = (BlipSprite)609;
		}
	}

	private void ApplyBlipGhostStyle(Blip blip)
	{
		if (GamerTagsScript.FlashState)
		{
			blip.Color = (BlipColor)0;
		}
		else
		{
			blip.Color = (BlipColor)2;
		}
		if (ColorBlindMode)
		{
			blip.Sprite = (BlipSprite)646;
		}
	}

	private void ApplyBlipDutyStyle(Blip blip)
	{
		blip.Color = (BlipColor)2;
		if (ColorBlindMode)
		{
			blip.Sprite = (BlipSprite)634;
		}
	}
}
