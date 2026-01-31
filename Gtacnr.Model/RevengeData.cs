using System;
using System.Collections.Generic;
using System.IO;

namespace Gtacnr.Model;

public class RevengeData
{
	public HashSet<int> Targets { get; set; } = new HashSet<int>();

	public HashSet<int> Claimants { get; set; } = new HashSet<int>();

	public DateTime T { get; set; }

	public byte[] Serialize()
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((ushort)Targets.Count);
		foreach (int target in Targets)
		{
			binaryWriter.Write(target);
		}
		binaryWriter.Write((ushort)Claimants.Count);
		foreach (int claimant in Claimants)
		{
			binaryWriter.Write(claimant);
		}
		binaryWriter.Write(T.Ticks);
		return memoryStream.ToArray();
	}

	public static RevengeData Deserialize(byte[] data)
	{
		RevengeData revengeData = new RevengeData();
		using MemoryStream input = new MemoryStream(data);
		using BinaryReader binaryReader = new BinaryReader(input);
		ushort num = binaryReader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			revengeData.Targets.Add(binaryReader.ReadInt32());
		}
		ushort num2 = binaryReader.ReadUInt16();
		for (int j = 0; j < num2; j++)
		{
			revengeData.Claimants.Add(binaryReader.ReadInt32());
		}
		revengeData.T = new DateTime(binaryReader.ReadInt64(), DateTimeKind.Utc);
		return revengeData;
	}
}
