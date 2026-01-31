using System;
using System.Collections.Generic;
using System.IO;
using CitizenFX.Core;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class VehicleState
{
	public string PersonalVehicleId { get; set; } = string.Empty;

	public string DeliveryId { get; set; } = string.Empty;

	public int AttachedVehicleNetworkId { get; set; }

	public TowableState TowableState { get; set; }

	public DateTime TowTimer { get; set; } = DateTime.MinValue;

	public byte BombCharges { get; set; }

	public bool BombStarted { get; set; }

	public bool SilentSiren { get; set; }

	public float Fuel { get; set; } = -1f;

	public Vector3 Position { get; set; }

	public int NetworkId { get; set; }

	public Dictionary<VehicleDataField, uint> FieldVersions { get; set; } = new Dictionary<VehicleDataField, uint>();

	public uint GetFieldVersion(VehicleDataField field)
	{
		if (!FieldVersions.ContainsKey(field))
		{
			return 0u;
		}
		return FieldVersions[field];
	}

	public uint GetAndUpdateFieldVersion(VehicleDataField field)
	{
		uint num = GetFieldVersion(field) + 1;
		FieldVersions[field] = num;
		return num;
	}

	public byte[] Serialize()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(PersonalVehicleId ?? "");
			binaryWriter.Write(DeliveryId ?? "");
			binaryWriter.Write(AttachedVehicleNetworkId);
			binaryWriter.Write((byte)TowableState);
			binaryWriter.Write(TowTimer.Ticks);
			binaryWriter.Write(BombCharges);
			binaryWriter.Write(BombStarted);
			binaryWriter.Write(SilentSiren);
			binaryWriter.Write(Fuel);
			binaryWriter.Write(Position.X);
			binaryWriter.Write(Position.Y);
			binaryWriter.Write(Position.Z);
			return memoryStream.ToArray();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return new byte[0];
		}
	}

	public static VehicleState Deserialize(byte[] data)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			using MemoryStream input = new MemoryStream(data);
			using BinaryReader binaryReader = new BinaryReader(input);
			return new VehicleState
			{
				PersonalVehicleId = binaryReader.ReadString(),
				DeliveryId = binaryReader.ReadString(),
				AttachedVehicleNetworkId = binaryReader.ReadInt32(),
				TowableState = (TowableState)binaryReader.ReadByte(),
				TowTimer = new DateTime(binaryReader.ReadInt64()),
				BombCharges = binaryReader.ReadByte(),
				BombStarted = binaryReader.ReadBoolean(),
				SilentSiren = binaryReader.ReadBoolean(),
				Fuel = binaryReader.ReadSingle(),
				Position = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle())
			};
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return new VehicleState();
		}
	}

	public static byte[] SerializeDictionary(Dictionary<int, VehicleState> dict)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			foreach (KeyValuePair<int, VehicleState> item in dict)
			{
				binaryWriter.Write(item.Key);
				byte[] array = item.Value.Serialize();
				binaryWriter.Write(array.Length);
				binaryWriter.Write(array);
			}
			return memoryStream.ToArray();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return new byte[0];
		}
	}

	public static Dictionary<int, VehicleState> DeserializeDictionary(byte[] data)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream(data);
			using BinaryReader binaryReader = new BinaryReader(memoryStream);
			Dictionary<int, VehicleState> dictionary = new Dictionary<int, VehicleState>();
			long length = memoryStream.Length;
			while (memoryStream.Position != length)
			{
				int num = binaryReader.ReadInt32();
				int count = binaryReader.ReadInt32();
				VehicleState vehicleState = Deserialize(binaryReader.ReadBytes(count));
				vehicleState.NetworkId = num;
				dictionary[num] = vehicleState;
			}
			return dictionary;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return new Dictionary<int, VehicleState>();
		}
	}
}
