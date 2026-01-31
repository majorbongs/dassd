using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Vehicles;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Client.Vehicles.Fuel;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using NativeUI;

namespace Gtacnr.Client.Jobs.Mechanic;

public class TowingScript : Script
{
	private class FlatbedInfo
	{
		public string Model { get; set; }

		public float[] Offsets_ { get; set; }

		public Vector3 Offsets => new Vector3(Offsets_[0], Offsets_[1], Offsets_[2]);
	}

	private List<Impound> impounds = Gtacnr.Utils.LoadJson<List<Impound>>("data/mechanic/impounds.json");

	private List<float[][]> noParkingAreas = Gtacnr.Utils.LoadJson<List<float[][]>>("data/mechanic/noParkingAreas.json");

	private Dictionary<Vehicle, TowableState> towableVehicles = new Dictionary<Vehicle, TowableState>();

	private static Dictionary<int, FlatbedInfo> flatbedInfo = Gtacnr.Utils.LoadJson<List<FlatbedInfo>>("data/mechanic/flatbeds.json").ToDictionary((FlatbedInfo i) => Gtacnr.Utils.GenerateHash(i.Model), (FlatbedInfo i) => i);

	private static readonly HashSet<VehicleClass> ignoredClassTypes = new HashSet<VehicleClass>
	{
		(VehicleClass)14,
		(VehicleClass)15,
		(VehicleClass)16,
		(VehicleClass)21,
		(VehicleClass)19
	};

	private bool areMechanicTasksAttached;

	private bool isDrawTaskAttached;

	private bool isImpoundGpsKeyListenerAttached;

	private bool areFlatbedAttachInstructionsAttached;

	private bool areFlatbedDetachInstructionsAttached;

	private bool isFlatbedAttachKeyListenerAttached;

	private bool isInTowTruck;

	private bool isTowingVehicle;

	private bool canImpoundCurrentVehicle;

	private bool dropOffHelpShown;

	private bool canDropOff;

	private bool towTimerHelpShown;

	private Impound currentImpound;

	private Vehicle vehicleToImpound;

	private Vehicle flatbedTargetVehicle;

	private TextTimerBar towTimerBar = new TextTimerBar("TIMER", Gtacnr.Utils.SecondsToMinutesAndSeconds(0))
	{
		TextColor = Colors.GTAYellow
	};

	public static bool IsTowTruck(VehicleHash vehicleHash)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected I4, but got Unknown
		return flatbedInfo.ContainsKey((int)vehicleHash);
	}

	public TowingScript()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		VehicleEvents.LeftVehicle += OnLeftVehicle;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	protected override void OnStarted()
	{
		CreateBlips();
	}

	private void CreateBlips()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		foreach (Impound impound in impounds)
		{
			Blip obj = World.CreateBlip(impound.Position.XYZ());
			obj.Sprite = (BlipSprite)68;
			obj.Color = (BlipColor)17;
			Utils.SetBlipName(obj, "Impound", "impound");
			obj.Scale = 0.85f;
			obj.IsShortRange = true;
		}
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		bool flag = e.PreviousJobEnum == JobsEnum.Mechanic;
		bool flag2 = e.CurrentJobEnum == JobsEnum.Mechanic;
		if (!flag && flag2)
		{
			AttachMechanicTasks();
		}
		else if (flag && !flag2)
		{
			DetachMechanicTasks();
		}
	}

	[EventHandler("gtacnr:mechanic:illegalVehicleParked")]
	private async void OnIllegalVehicleParked(Vector3 coords)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		string areaName;
		if (isInTowTruck)
		{
			areaName = Utils.GetLocationName(coords);
			Game.PlaySound("Event_Message_Purple", "GTAO_FM_Events_Soundset");
			await InteractiveNotificationsScript.Show("There's a ~r~crime vehicle ~s~to impound in ~y~" + areaName + "~s~.", InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Respond", "Respond (hold)");
		}
		bool OnAccepted()
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Utils.DisplayHelpText();
			Utils.SendNotification("Go to ~y~" + areaName + " ~s~and tow the ~r~vehicle~s~.");
			GPSScript.SetDestination("Responded Call", coords, 0f, shortRange: false, (BlipSprite)162, null, 255, autoDelete: true, 30f);
			return true;
		}
	}

	private void AttachMechanicTasks()
	{
		if (!areMechanicTasksAttached)
		{
			base.Update += DrawImpoundableVehiclesTask;
			areMechanicTasksAttached = true;
		}
	}

	private void DetachMechanicTasks()
	{
		if (areMechanicTasksAttached)
		{
			base.Update -= DrawImpoundableVehiclesTask;
			areMechanicTasksAttached = false;
			ResetVars();
		}
	}

	private void ResetVars()
	{
		isInTowTruck = false;
		isTowingVehicle = false;
		canImpoundCurrentVehicle = false;
		canDropOff = false;
		currentImpound = null;
		flatbedTargetVehicle = null;
	}

	private async void DropOff()
	{
		if (!canDropOff || Gtacnr.Client.API.Jobs.CachedJobEnum != JobsEnum.Mechanic)
		{
			return;
		}
		if ((Entity)(object)vehicleToImpound == (Entity)null || !vehicleToImpound.Exists())
		{
			Utils.DisplayErrorMessage(127, 12, "Invalid vehicle to impound.");
			return;
		}
		try
		{
			int num = await TriggerServerEventAsync<int>("gtacnr:mechanic:impound", new object[2]
			{
				((Entity)vehicleToImpound).NetworkId,
				impounds.IndexOf(currentImpound)
			});
			switch (num)
			{
			case 1:
				if (vehicleToImpound.Exists())
				{
					((PoolObject)vehicleToImpound).Delete();
				}
				break;
			case 6:
				Utils.DisplayHelpText("~r~You can't impound this vehicle.");
				break;
			default:
				Utils.DisplayErrorMessage(127, num);
				break;
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private void AddFlatbedAttachInstructions()
	{
		if (!areFlatbedAttachInstructionsAttached)
		{
			Utils.AddInstructionalButton("towtruckAttach", new Gtacnr.Client.API.UI.InstructionalButton("Attach (hold)", 2, (Control)74));
			areFlatbedAttachInstructionsAttached = true;
		}
	}

	private void RemoveFlatbedAttachInstructions()
	{
		if (areFlatbedAttachInstructionsAttached)
		{
			Utils.RemoveInstructionalButton("towtruckAttach");
			areFlatbedAttachInstructionsAttached = false;
		}
	}

	private void AddFlatbedDetachInstructions()
	{
		if (!areFlatbedDetachInstructionsAttached)
		{
			Utils.AddInstructionalButton("towtruckDetach", new Gtacnr.Client.API.UI.InstructionalButton("Detach (hold)", 2, (Control)74));
			areFlatbedDetachInstructionsAttached = true;
		}
	}

	private void RemoveFlatbedDetachInstructions()
	{
		if (areFlatbedDetachInstructionsAttached)
		{
			Utils.RemoveInstructionalButton("towtruckDetach");
			areFlatbedDetachInstructionsAttached = false;
		}
	}

	private void AttachFlatbedKeyListener()
	{
		if (!isFlatbedAttachKeyListenerAttached)
		{
			KeysScript.AttachListener((Control)74, OnFlatbedKeyEvent, 100);
			isFlatbedAttachKeyListenerAttached = true;
		}
	}

	private void DetachFlatbedKeyListener()
	{
		if (isFlatbedAttachKeyListenerAttached)
		{
			KeysScript.DetachListener((Control)74, OnFlatbedKeyEvent);
			isFlatbedAttachKeyListenerAttached = false;
		}
	}

	private bool OnFlatbedKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (!isFlatbedAttachKeyListenerAttached)
		{
			return false;
		}
		if (eventType == KeyEventType.Held)
		{
			Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)currentVehicle == (Entity)null || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
			{
				return false;
			}
			Vehicle towedVehicle = GetTowedVehicle();
			if ((Entity)(object)towedVehicle != (Entity)null && towedVehicle.Exists())
			{
				DetachFromFlatbed();
				RemoveFlatbedDetachInstructions();
				return true;
			}
			if ((Entity)(object)flatbedTargetVehicle != (Entity)null && flatbedTargetVehicle.Exists())
			{
				AttachToFlatbed();
				RemoveFlatbedAttachInstructions();
				AddFlatbedDetachInstructions();
				return true;
			}
		}
		return false;
	}

	private Vehicle GetTowedVehicle()
	{
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null || !isInTowTruck)
		{
			return null;
		}
		Vehicle[] allVehicles = World.GetAllVehicles();
		foreach (Vehicle val in allVehicles)
		{
			Entity entityAttachedTo = ((Entity)val).GetEntityAttachedTo();
			Vehicle val2 = (Vehicle)(object)((entityAttachedTo is Vehicle) ? entityAttachedTo : null);
			if ((Entity)(object)val2 != (Entity)null && (Entity)(object)val2 == (Entity)(object)Game.PlayerPed.CurrentVehicle)
			{
				return val;
			}
		}
		return null;
	}

	private async void AttachToFlatbed()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || (Entity)(object)flatbedTargetVehicle == (Entity)null || !IsTowTruck(Model.op_Implicit(((Entity)currentVehicle).Model)))
		{
			return;
		}
		if (currentVehicle.Speed > 5f)
		{
			Utils.SendNotification("You cannot attach a vehicle while ~r~moving~s~.");
			return;
		}
		if (ignoredClassTypes.Contains(flatbedTargetVehicle.ClassType))
		{
			Utils.SendNotification("Vehicles of this type ~r~cannot be towed~s~.");
			return;
		}
		if ((Entity)(object)flatbedTargetVehicle.Driver != (Entity)null && flatbedTargetVehicle.Driver.Exists())
		{
			Utils.SendNotification("The vehicle is ~r~occupied~s~.");
			return;
		}
		Vector3 modelDim = ((Entity)flatbedTargetVehicle).Model.GetDimensions();
		if (modelDim.X > 2.9f || modelDim.Y > 6.8f)
		{
			Utils.SendNotification("The vehicle is ~r~too big ~s~for this truck.");
		}
		else if (((Entity)flatbedTargetVehicle).NetworkId != 0 && await TriggerServerEventAsync<bool>("gtacnr:mechanic:attachVehicle", new object[1] { ((Entity)flatbedTargetVehicle).NetworkId }) && await Utils.GetNetworkControlOfEntity((Entity)(object)flatbedTargetVehicle))
		{
			Vector3 offsets = flatbedInfo[Model.op_Implicit(((Entity)currentVehicle).Model)].Offsets;
			offsets.Z += modelDim.Z;
			float num = Math.Abs(((Entity)flatbedTargetVehicle).Heading - ((Entity)currentVehicle).Heading);
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector(0f, 0f, (num < 15f) ? 0f : 180f);
			((Entity)flatbedTargetVehicle).AttachTo((Entity)(object)currentVehicle, offsets, val);
			BaseScript.TriggerServerEvent("gtacnr:mechanic:setTowState", new object[2]
			{
				((Entity)flatbedTargetVehicle).NetworkId,
				1
			});
		}
	}

	private void DetachFromFlatbed()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null)
		{
			return;
		}
		TimerBarScript.RemoveTimerBar(towTimerBar);
		towTimerHelpShown = false;
		Vector3 position = ((Entity)currentVehicle).Position - ((Entity)currentVehicle).ForwardVector * 7.5f;
		Vehicle towedVehicle = GetTowedVehicle();
		if ((Entity)(object)towedVehicle == (Entity)null || !towedVehicle.Exists())
		{
			return;
		}
		if (currentVehicle.Speed > 5f)
		{
			Utils.SendNotification("You cannot detach the vehicle while ~r~moving~s~.");
			return;
		}
		((Entity)towedVehicle).Detach();
		((Entity)towedVehicle).Position = position;
		towedVehicle.PlaceOnGround();
		VehiclesAutoRemovalScript.MarkVehicleAsAbandoned(towedVehicle);
		DropOff();
		if (((Entity)towedVehicle).NetworkId > 0)
		{
			BaseScript.TriggerServerEvent("gtacnr:mechanic:setTowState", new object[2]
			{
				((Entity)towedVehicle).NetworkId,
				0
			});
		}
	}

	private void AttachDrawImpoundTask()
	{
		if (!isDrawTaskAttached)
		{
			base.Update += DrawImpoundTask;
			isDrawTaskAttached = true;
		}
	}

	private void DetachDrawImpoundTask()
	{
		if (isDrawTaskAttached)
		{
			base.Update -= DrawImpoundTask;
			isDrawTaskAttached = false;
		}
	}

	private async Coroutine DrawImpoundTask()
	{
		if (canImpoundCurrentVehicle && currentImpound != null)
		{
			Vector3 dropOffPosition = currentImpound.DropOffPosition;
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector(5f, 5f, 1f);
			Color color = Color.FromUint(2863267968u);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(dropOffPosition.X, dropOffPosition.Y, dropOffPosition.Z, ref z, false))
			{
				dropOffPosition.Z = z;
			}
			API.DrawMarker(1, dropOffPosition.X, dropOffPosition.Y, dropOffPosition.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		await Script.Wait(1000);
		bool flag = isInTowTruck;
		bool flag2 = isTowingVehicle;
		bool flag3 = canImpoundCurrentVehicle;
		ResetVars();
		if (((Entity)Game.PlayerPed).IsDead || CuffedScript.IsCuffed || CuffedScript.IsInCustody)
		{
			TimerBarScript.RemoveTimerBar(towTimerBar);
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		bool flag4 = (Entity)(object)currentVehicle != (Entity)null && (Entity)(object)currentVehicle.Driver == (Entity)(object)Game.PlayerPed;
		string vehicleType;
		Vehicle[] allVehicles;
		if ((Entity)(object)currentVehicle != (Entity)null && IsTowTruck(Model.op_Implicit(((Entity)currentVehicle).Model)))
		{
			isInTowTruck = true;
			Vehicle towedVehicle = GetTowedVehicle();
			Vector3 position;
			if (Gtacnr.Client.API.Jobs.CachedJobEnum == JobsEnum.Mechanic && flag4)
			{
				foreach (Impound impound2 in impounds)
				{
					position = ((Entity)Game.PlayerPed).Position;
					if (!(((Vector3)(ref position)).DistanceToSquared(impound2.DropOffPosition) < 2500f))
					{
						continue;
					}
					currentImpound = impound2;
					if (!((Entity)(object)towedVehicle != (Entity)null) || !towedVehicle.Exists())
					{
						break;
					}
					position = ((Entity)towedVehicle).Position;
					if (((Vector3)(ref position)).DistanceToSquared(impound2.DropOffPosition) < 64f)
					{
						canDropOff = true;
						vehicleToImpound = towedVehicle;
						if (!dropOffHelpShown && (Entity)(object)vehicleToImpound != (Entity)null && vehicleToImpound.Exists())
						{
							dropOffHelpShown = true;
							Utils.DisplayHelpText("Hold ~INPUT_VEH_HEADLIGHT~ to drop the ~r~vehicle ~s~off.");
						}
					}
					else
					{
						dropOffHelpShown = false;
					}
					break;
				}
				if ((Entity)(object)towedVehicle != (Entity)null && towedVehicle.Exists())
				{
					isTowingVehicle = true;
					TowableState? towableState = LatentVehicleStateScript.Get(((Entity)towedVehicle).NetworkId)?.TowableState;
					if (towableState.HasValue && towableState != TowableState.None)
					{
						canImpoundCurrentVehicle = true;
						if (!flag3)
						{
							vehicleType = ((towableState == TowableState.IllegallyParked) ? "~y~illegally parked vehicle" : ((towableState == TowableState.UsedInCrime) ? "~r~vehicle used in a crime" : "vehicle"));
							ShowPrompt();
						}
					}
					else if (!flag2)
					{
						Utils.DisplaySubtitle("You can't ~r~impound ~s~this vehicle. Find a vehicle marked by a ~y~yellow cone~s~.");
					}
					DateTime? dateTime = LatentVehicleStateScript.Get(((Entity)towedVehicle).NetworkId)?.TowTimer;
					if (dateTime.HasValue && dateTime != DateTime.MinValue)
					{
						int seconds = Math.Max(0, 120 - (int)(DateTime.UtcNow - dateTime.Value).TotalSeconds);
						towTimerBar.Text = Gtacnr.Utils.SecondsToMinutesAndSeconds(seconds);
						TimerBarScript.AddTimerBar(towTimerBar);
						if (!towTimerHelpShown && Gtacnr.Client.API.Jobs.CachedJobEnum == JobsEnum.Mechanic)
						{
							towTimerHelpShown = true;
							Utils.SendNotification("You are towing a ~y~player's vehicle ~s~for ~g~extra money~s~, but you have limited time. When the timer ~r~runs out~s~, the owner will be able to ~y~recall ~s~it.");
						}
					}
					else
					{
						TimerBarScript.RemoveTimerBar(towTimerBar);
						towTimerHelpShown = false;
					}
				}
				if (!flag)
				{
					Utils.DisplayHelpText("Tow illegally parked ~y~vehicles ~s~marked by a yellow cone to an ~g~impound lot~s~.");
				}
			}
			if (flag4 && ((Entity)(object)towedVehicle == (Entity)null || !towedVehicle.Exists()))
			{
				Vehicle val = null;
				Vector3 val2 = ((Entity)currentVehicle).Position - ((Entity)currentVehicle).ForwardVector * 7.5f;
				allVehicles = World.GetAllVehicles();
				foreach (Vehicle val3 in allVehicles)
				{
					float num = Math.Abs(((Entity)val3).Heading - ((Entity)currentVehicle).Heading);
					bool num2 = num < 15f || (num > 165f && num < 195f);
					position = ((Entity)val3).Position;
					float num3 = ((Vector3)(ref position)).DistanceToSquared(val2);
					bool flag5 = (Entity)(object)val3.Driver != (Entity)null && val3.Driver.Exists();
					if (num2 && num3 <= 6.25f && !flag5)
					{
						val = val3;
						break;
					}
				}
				if ((Entity)(object)val != (Entity)null)
				{
					AddFlatbedAttachInstructions();
					AttachFlatbedKeyListener();
					flatbedTargetVehicle = val;
				}
				else
				{
					RemoveFlatbedAttachInstructions();
				}
			}
			else if ((Entity)(object)towedVehicle != (Entity)null && towedVehicle.Exists())
			{
				AddFlatbedDetachInstructions();
				AttachFlatbedKeyListener();
			}
		}
		if ((!isInTowTruck && flag) || !flag4)
		{
			RemoveFlatbedAttachInstructions();
			RemoveFlatbedDetachInstructions();
			DetachFlatbedKeyListener();
			TimerBarScript.RemoveTimerBar(towTimerBar);
			towTimerHelpShown = false;
		}
		if (canImpoundCurrentVehicle && !flag3)
		{
			AttachDrawImpoundTask();
		}
		if (!canImpoundCurrentVehicle && flag3)
		{
			DetachDrawImpoundTask();
		}
		towableVehicles.Clear();
		allVehicles = World.GetAllVehicles();
		foreach (Vehicle val4 in allVehicles)
		{
			if (!API.NetworkGetEntityIsNetworked(((PoolObject)val4).Handle))
			{
				continue;
			}
			VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)val4).NetworkId);
			if (vehicleState != null)
			{
				TowableState towableState2 = vehicleState.TowableState;
				if (towableState2 != TowableState.None)
				{
					towableVehicles[val4] = towableState2;
				}
			}
		}
		bool OnAccepted()
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			Impound impound = null;
			float num4 = 999999f;
			foreach (Impound impound3 in impounds)
			{
				Vector3 position2 = ((Entity)Game.PlayerPed).Position;
				float num5 = ((Vector3)(ref position2)).DistanceToSquared(impound3.DropOffPosition);
				if (num5 < num4)
				{
					num4 = num5;
					impound = impound3;
				}
			}
			if (impound != null)
			{
				GPSScript.SetDestination("Impound", impound.DropOffPosition, 20f, shortRange: true, null, null, 255, autoDelete: true);
			}
			return true;
		}
		async void ShowPrompt()
		{
			await InteractiveNotificationsScript.Show("Tow this " + vehicleType + " ~s~to an ~g~impound lot~s~.", InteractiveNotificationType.Subtitle, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Set GPS", "Set GPS (hold)", () => !canImpoundCurrentVehicle);
		}
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		BaseScript.TriggerServerEvent("gtacnr:mechanic:setVehicleTowable", new object[2]
		{
			((Entity)e.Vehicle).NetworkId,
			0
		});
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		bool towable = false;
		if (ignoredClassTypes.Contains(e.Vehicle.ClassType) || (GasStationsScript.CurrentGasStation != null && GasStationsScript.CurrentPumpIndex != -1))
		{
			return;
		}
		if (API.IsPointOnRoad(((Entity)e.Vehicle).Position.X, ((Entity)e.Vehicle).Position.Y, ((Entity)e.Vehicle).Position.Z, ((PoolObject)e.Vehicle).Handle))
		{
			SetTowable(value: true);
		}
		else
		{
			foreach (float[][] noParkingArea in noParkingAreas)
			{
				if (Gtacnr.Utils.IsPointInPolygon(noParkingArea, ((Entity)e.Vehicle).Position))
				{
					SetTowable(value: true);
					break;
				}
			}
		}
		BaseScript.TriggerServerEvent("gtacnr:mechanic:setVehicleTowable", new object[2]
		{
			((Entity)e.Vehicle).NetworkId,
			towable ? 1 : 0
		});
		void SetTowable(bool value)
		{
			towable = value;
			if (e.Vehicle.IsSirenActive)
			{
				towable = false;
			}
			if (towable)
			{
				Utils.SendNotification("You have parked your vehicle ~r~illegally ~s~and it might be towed at your expense.");
			}
		}
	}

	private async Coroutine DrawImpoundableVehiclesTask()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if (!isInTowTruck || (Entity)(object)currentVehicle == (Entity)null)
		{
			return;
		}
		Vector3 val2 = default(Vector3);
		foreach (KeyValuePair<Vehicle, TowableState> towableVehicle in towableVehicles)
		{
			Vehicle key = towableVehicle.Key;
			TowableState value = towableVehicle.Value;
			if (key.Exists() && (!((Entity)(object)key.Driver != (Entity)null) || !key.Driver.Exists()) && key.PassengerCount <= 0)
			{
				Vector3 val = ((Entity)key).Position + new Vector3(0f, 0f, 2f);
				((Vector3)(ref val2))._002Ector(0.7f, 0.7f, 0.7f);
				Color color = Color.FromUint((value == TowableState.IllegallyParked) ? 2863267968u : 2856583296u);
				API.DrawMarker(0, val.X, val.Y, val.Z, 0f, 0f, 0f, 0f, 0f, 0f, val2.X, val2.Y, val2.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, true, true, 2, false, (string)null, (string)null, false);
			}
		}
	}

	[EventHandler("gtacnr:mechanic:abandonTowing")]
	private void OnAbandon(int netVehId)
	{
		ResetVars();
		TimerBarScript.RemoveTimerBar(towTimerBar);
		RemoveFlatbedDetachInstructions();
		if (netVehId != 0 && API.NetworkDoesEntityExistWithNetworkId(netVehId))
		{
			Entity obj = Entity.FromNetworkId(netVehId);
			if (obj != null)
			{
				((PoolObject)obj).Delete();
			}
		}
	}
}
