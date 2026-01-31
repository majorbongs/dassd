using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class VehiclesAutoRemovalScript : Script
{
	public struct PositionArea
	{
		private Vector3 Position;

		private float Area;

		public PositionArea(Vector3 position, float area)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			Position = position;
			Area = area;
		}

		public bool IsPointInside(Vector3 point)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return ((Vector3)(ref Position)).DistanceToSquared(point) <= Area * Area;
		}
	}

	private static readonly List<PositionArea> vehicleRestrictedAreas = new List<PositionArea>
	{
		new PositionArea(new Vector3(17.4068f, -1115.1963f, 29.7959f), 4f),
		new PositionArea(new Vector3(299.6873f, -584.8777f, 43.2919f), 3f),
		new PositionArea(new Vector3(1855.1438f, 3683.4565f, 34.2673f), 3f),
		new PositionArea(new Vector3(-443.6278f, 6016.116f, 31.7164f), 3f),
		new PositionArea(new Vector3(434.7912f, -981.7617f, 30.6957f), 3f),
		new PositionArea(new Vector3(827.7838f, -1289.9835f, 28.2408f), 3f),
		new PositionArea(new Vector3(844.0323f, -1024.239f, 28.1499f), 3f),
		new PositionArea(new Vector3(811.9532f, -2148.2192f, 29.619f), 3f),
		new PositionArea(new Vector3(-1314.1405f, -390.3451f, 36.6958f), 3f),
		new PositionArea(new Vector3(-1632.3884f, -1015.9845f, 13.1454f), 3f),
		new PositionArea(new Vector3(-664.0685f, -944.2465f, 21.8292f), 3f)
	};

	public VehiclesAutoRemovalScript()
	{
		VehicleEvents.LeftVehicle += OnLeftVehicle;
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		MarkVehicleAsAbandoned(e.Vehicle);
	}

	public static void MarkVehicleAsAbandoned(Vehicle vehicle)
	{
		if (API.NetworkGetEntityIsNetworked(((PoolObject)vehicle).Handle))
		{
			List<int> playerPassengersInVehicle = Utils.GetPlayerPassengersInVehicle(vehicle);
			playerPassengersInVehicle.Remove(Game.Player.ServerId);
			if (playerPassengersInVehicle.Count == 0 && vehicleRestrictedAreas.Any((PositionArea p) => p.IsPointInside(((Entity)vehicle).Position)))
			{
				BaseScript.TriggerServerEvent("gtacnr:abandonedVehicle", new object[1] { ((Entity)vehicle).NetworkId });
			}
		}
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if (API.NetworkGetEntityIsNetworked(((PoolObject)e.Vehicle).Handle))
		{
			BaseScript.TriggerServerEvent("gtacnr:enteredVehicle", new object[1] { ((Entity)e.Vehicle).NetworkId });
		}
	}
}
