using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CitizenFX.Core;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.API;

public class LatentVehicleStateScript : Script
{
	private static Dictionary<int, VehicleState> vehicleStates = new Dictionary<int, VehicleState>();

	public static int Count { get; private set; }

	public static VehicleState? Get(int networkId)
	{
		return vehicleStates.TryGetRefOrNull(networkId);
	}

	public static List<VehicleState> GetAll()
	{
		return vehicleStates.Values.ToList();
	}

	[EventHandler("gtacnr:vehicleData:syncAll")]
	private void OnSyncAll(byte[] state)
	{
		vehicleStates = VehicleState.DeserializeDictionary(state);
		Count = vehicleStates.Count;
	}

	[EventHandler("gtacnr:vehicleData:removeVehicles")]
	private void OnRemoveVehicles(string vehiclesJson)
	{
		foreach (int item in vehiclesJson.Unjson<List<int>>())
		{
			vehicleStates.Remove(item);
		}
	}

	[EventHandler("gtacnr:vehicleData:validateVehicles")]
	private void OnValidateVehicles(string vehiclesJson)
	{
		List<int> validIds = vehiclesJson.Unjson<List<int>>();
		foreach (int item in (from id in vehicleStates.Keys.ToList()
			where !validIds.Contains(id)
			select id).ToList())
		{
			vehicleStates.Remove(item);
		}
	}

	[EventHandler("gtacnr:vehicleData:syncVehicle")]
	private void OnSyncVehicle(int vehicleNetworkId, byte[] data)
	{
		VehicleState vehicleState = VehicleState.Deserialize(data);
		vehicleState.NetworkId = vehicleNetworkId;
		vehicleStates[vehicleNetworkId] = vehicleState;
	}

	[EventHandler("gtacnr:vehicleData:updateField")]
	private void OnUpdateField(int vehicleNetworkId, byte fieldByte, uint version, byte[] serializedValue)
	{
		VehicleDataField field = (VehicleDataField)fieldByte;
		UpdateField(vehicleNetworkId, field, version, serializedValue);
	}

	[EventHandler("gtacnr:vehicleData:updateFields")]
	private void OnUpdateFields(int vehicleNetworkId, byte[] serializedData)
	{
		if (!vehicleStates.ContainsKey(vehicleNetworkId))
		{
			vehicleStates[vehicleNetworkId] = new VehicleState
			{
				NetworkId = vehicleNetworkId
			};
		}
		foreach (KeyValuePair<VehicleDataField, Tuple<uint, byte[]>> item3 in SyncedDataSerialization.DeserializeVersionAndDataDictionary<VehicleDataField>(serializedData))
		{
			uint item = item3.Value.Item1;
			byte[] item2 = item3.Value.Item2;
			UpdateField(vehicleNetworkId, item3.Key, item, item2);
		}
	}

	[EventHandler("gtacnr:vehicleData:updateFieldsBatched")]
	private void OnUpdateFieldsBatched(byte[] serializedBatchedData)
	{
		foreach (KeyValuePair<int, byte[]> item in SyncedDataSerialization.DeserializeBatchedDataDictionary(serializedBatchedData))
		{
			OnUpdateFields(item.Key, item.Value);
		}
	}

	private void UpdateField(int vehicleNetworkId, VehicleDataField field, uint version, byte[] serializedValue)
	{
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		if (!vehicleStates.ContainsKey(vehicleNetworkId))
		{
			vehicleStates[vehicleNetworkId] = new VehicleState
			{
				NetworkId = vehicleNetworkId
			};
		}
		VehicleState vehicleState = vehicleStates[vehicleNetworkId];
		if (vehicleState.GetFieldVersion(field) < version)
		{
			vehicleState.FieldVersions[field] = version;
			switch (field)
			{
			case VehicleDataField.PersonalVehicleId:
				vehicleState.PersonalVehicleId = SyncedDataSerialization.DeserializeString(serializedValue);
				break;
			case VehicleDataField.DeliveryId:
				vehicleState.DeliveryId = SyncedDataSerialization.DeserializeString(serializedValue);
				break;
			case VehicleDataField.AttachedVehicleNetworkId:
				vehicleState.AttachedVehicleNetworkId = SyncedDataSerialization.DeserializeInt32(serializedValue);
				break;
			case VehicleDataField.TowableState:
				vehicleState.TowableState = (TowableState)SyncedDataSerialization.DeserializeByte(serializedValue);
				break;
			case VehicleDataField.TowTimer:
				vehicleState.TowTimer = new DateTime(SyncedDataSerialization.DeserializeInt64(serializedValue));
				break;
			case VehicleDataField.BombCharges:
				vehicleState.BombCharges = SyncedDataSerialization.DeserializeByte(serializedValue);
				break;
			case VehicleDataField.BombStarted:
				vehicleState.BombStarted = SyncedDataSerialization.DeserializeBool(serializedValue);
				break;
			case VehicleDataField.SilentSiren:
				vehicleState.SilentSiren = SyncedDataSerialization.DeserializeBool(serializedValue);
				break;
			case VehicleDataField.Fuel:
				vehicleState.Fuel = SyncedDataSerialization.DeserializeFloat(serializedValue);
				break;
			case VehicleDataField.Position:
				vehicleState.Position = SyncedDataSerialization.DeserializePosition(serializedValue);
				break;
			}
		}
	}

	[Command("print_vehicle_states")]
	private void PrintVehicleStatesCommand(string[] args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Current Vehicle States:");
		foreach (KeyValuePair<int, VehicleState> vehicleState in vehicleStates)
		{
			stringBuilder.AppendLine($"Network ID: {vehicleState.Key}, State: {vehicleState.Value.Json()}");
		}
		Debug.WriteLine(stringBuilder.ToString());
	}
}
