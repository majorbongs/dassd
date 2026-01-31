using System;
using System.Collections.Generic;
using System.IO;
using CitizenFX.Core;
using Gtacnr.Client.Crews;
using Gtacnr.Data;
using Gtacnr.Model.Enums;
using Gtacnr.Model.PrefixedGUIDs;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class PlayerState
{
	[JsonIgnore]
	private byte _wantedLevel;

	[JsonIgnore]
	private CrewId _crewId = new CrewId(Guid.Empty);

	[JsonIgnore]
	private JobsEnum _jobEnum;

	public string Uid { get; set; } = "";

	[JsonIgnore]
	public string _name { get; set; } = "";

	[JsonIgnore]
	public string _nameWithCrewTag { get; set; } = "";

	public string Name
	{
		get
		{
			return _nameWithCrewTag;
		}
		set
		{
			_name = value;
			Tuple<string, AcronymStyleData> crewAcronymData = CrewScript.GetCrewAcronymData(CrewId);
			if (crewAcronymData != null)
			{
				_nameWithCrewTag = crewAcronymData.Item2.ApplyToUsername(_name, crewAcronymData.Item1) ?? "";
			}
			else
			{
				_nameWithCrewTag = _name;
			}
		}
	}

	public string ActualUsername { get; set; } = "";

	public ushort Ping { get; set; }

	public byte WantedLevel
	{
		get
		{
			return _wantedLevel;
		}
		set
		{
			_wantedLevel = value;
			ColorRGB = Utils.GetColorRGB(_jobEnum, WantedLevel);
			ColorTextCode = Utils.GetColorTextCode(_jobEnum, WantedLevel);
		}
	}

	public string Job { get; set; } = string.Empty;

	public string JobDescription { get; set; } = "";

	public int XP { get; set; }

	public int Level { get; set; }

	public long Cash { get; set; }

	public long Bank { get; set; }

	public Vector3 Position { get; set; }

	public int Bounty { get; set; }

	public int RoutingBucket { get; set; }

	public MembershipTier Tier { get; set; }

	public Color ColorRGB { get; set; }

	public string ColorTextCode { get; set; } = "";

	public StaffLevel StaffLevel { get; set; }

	public bool AdminDuty { get; set; }

	public bool GhostMode { get; set; }

	public bool IsSurrendering { get; set; }

	public bool IsCuffed { get; set; }

	public bool IsInCustody { get; set; }

	[JsonIgnore]
	public bool CanBeCuffed
	{
		get
		{
			if (!JobEnum.IsPublicService() && WantedLevel >= 2 && !IsCuffed && !IsInCustody)
			{
				return !AdminDuty;
			}
			return false;
		}
	}

	[JsonIgnore]
	public bool CanBeStopped
	{
		get
		{
			if (!JobEnum.IsPublicService() && WantedLevel >= 1 && !IsCuffed && !IsInCustody)
			{
				return !AdminDuty;
			}
			return false;
		}
	}

	public bool VoiceChatEnabled { get; set; }

	public bool SpawnProtection { get; set; }

	public int StolenWalletOwner { get; set; }

	public string SignText { get; set; } = "";

	public bool IsTyping { get; set; }

	public bool IsOnCoke { get; set; }

	public bool IsOnOpiates { get; set; }

	public bool IsOnMeth { get; set; }

	public CrewId CrewId
	{
		get
		{
			return _crewId;
		}
		set
		{
			_crewId = value ?? new CrewId(Guid.Empty);
			Tuple<string, AcronymStyleData> crewAcronymData = CrewScript.GetCrewAcronymData(CrewId);
			if (crewAcronymData != null)
			{
				_nameWithCrewTag = crewAcronymData.Item2.ApplyToUsername(_name, crewAcronymData.Item1) ?? "";
			}
			else
			{
				_nameWithCrewTag = _name;
			}
		}
	}

	public Dictionary<PlayerDataField, uint> FieldVersions { get; set; } = new Dictionary<PlayerDataField, uint>();

	public int Id { get; set; }

	[JsonIgnore]
	public string NameAndId => $"{Name} ({Id})";

	[JsonIgnore]
	public string ColorNameAndId => $"{ColorTextCode}{Name} ({Id})~s~";

	[JsonIgnore]
	public string FullyFormatted => $"<C>{ColorTextCode}{Name} ({Id})</C>~s~";

	public JobsEnum JobEnum
	{
		get
		{
			return _jobEnum;
		}
		set
		{
			_jobEnum = value;
			Job = Utils.JobMapper.EnumToJob(_jobEnum);
			JobDescription = ((_jobEnum == JobsEnum.Staff) ? "Staff" : (Jobs.GetJobData(Job)?.Name ?? "Not Spawned/Unknown"));
			ColorRGB = Utils.GetColorRGB(_jobEnum, WantedLevel);
			ColorTextCode = Utils.GetColorTextCode(_jobEnum, WantedLevel);
		}
	}

	public uint GetFieldVersion(PlayerDataField field)
	{
		if (!FieldVersions.ContainsKey(field))
		{
			return 0u;
		}
		return FieldVersions[field];
	}

	public uint GetAndUpdateFieldVersion(PlayerDataField field)
	{
		uint num = GetFieldVersion(field) + 1;
		FieldVersions[field] = num;
		return num;
	}

	public string ColorNameAndIdWithWantedLevel(int wantedLevel)
	{
		return $"{Utils.GetColorTextCode(_jobEnum, wantedLevel)}{Name} ({Id})~s~";
	}

	public override string ToString()
	{
		return NameAndId;
	}

	public static PlayerState CreateDisconnectedPlayer(int playerId)
	{
		return new PlayerState
		{
			ColorTextCode = "~HUD_COLOUR_GREYDARK~",
			Name = "Disconnected Player",
			Id = playerId
		};
	}

	public byte[] SerializeLegacy()
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(Uid ?? "");
		binaryWriter.Write(Name ?? "");
		binaryWriter.Write(ActualUsername ?? "");
		binaryWriter.Write((int)Ping);
		binaryWriter.Write((int)WantedLevel);
		binaryWriter.Write(Job ?? "");
		binaryWriter.Write(XP);
		binaryWriter.Write(Level);
		binaryWriter.Write(Cash);
		binaryWriter.Write(Bank);
		binaryWriter.Write(Position.X);
		binaryWriter.Write(Position.Y);
		binaryWriter.Write(Position.Z);
		binaryWriter.Write(Bounty);
		binaryWriter.Write(RoutingBucket);
		binaryWriter.Write((int)Tier);
		binaryWriter.Write(ColorRGB.R);
		binaryWriter.Write(ColorRGB.G);
		binaryWriter.Write(ColorRGB.B);
		binaryWriter.Write(ColorTextCode ?? "");
		binaryWriter.Write((int)StaffLevel);
		binaryWriter.Write(AdminDuty);
		binaryWriter.Write((byte)JobEnum);
		return memoryStream.ToArray();
	}

	public byte[] Serialize()
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(Uid ?? "");
			binaryWriter.Write(_name ?? "");
			binaryWriter.Write(ActualUsername ?? "");
			binaryWriter.Write(Ping);
			binaryWriter.Write(WantedLevel);
			binaryWriter.Write((byte)JobEnum);
			binaryWriter.Write(XP);
			binaryWriter.Write(Cash);
			binaryWriter.Write(Bank);
			binaryWriter.Write(Position.X);
			binaryWriter.Write(Position.Y);
			binaryWriter.Write(Position.Z);
			binaryWriter.Write(Bounty);
			binaryWriter.Write(RoutingBucket);
			binaryWriter.Write((byte)Tier);
			binaryWriter.Write((ushort)StaffLevel);
			binaryWriter.Write(AdminDuty);
			binaryWriter.Write(GhostMode);
			binaryWriter.Write(IsSurrendering);
			binaryWriter.Write(IsCuffed);
			binaryWriter.Write(IsInCustody);
			binaryWriter.Write(VoiceChatEnabled);
			binaryWriter.Write(SpawnProtection);
			binaryWriter.Write(StolenWalletOwner);
			binaryWriter.Write(SignText);
			binaryWriter.Write(IsTyping);
			binaryWriter.Write(IsOnCoke);
			binaryWriter.Write(IsOnOpiates);
			binaryWriter.Write(IsOnMeth);
			binaryWriter.Write(((Guid)CrewId).ToByteArray());
			return memoryStream.ToArray();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return new byte[0];
		}
	}

	public static PlayerState Deserialize(byte[] data)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			using MemoryStream input = new MemoryStream(data);
			using BinaryReader binaryReader = new BinaryReader(input);
			PlayerState obj = new PlayerState
			{
				Uid = binaryReader.ReadString(),
				Name = binaryReader.ReadString(),
				ActualUsername = binaryReader.ReadString(),
				Ping = binaryReader.ReadUInt16(),
				WantedLevel = binaryReader.ReadByte(),
				JobEnum = (JobsEnum)binaryReader.ReadByte(),
				XP = binaryReader.ReadInt32(),
				Cash = binaryReader.ReadInt64(),
				Bank = binaryReader.ReadInt64(),
				Position = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle()),
				Bounty = binaryReader.ReadInt32(),
				RoutingBucket = binaryReader.ReadInt32(),
				Tier = (MembershipTier)binaryReader.ReadByte(),
				StaffLevel = (StaffLevel)binaryReader.ReadUInt16(),
				AdminDuty = binaryReader.ReadBoolean(),
				GhostMode = binaryReader.ReadBoolean(),
				IsSurrendering = binaryReader.ReadBoolean(),
				IsCuffed = binaryReader.ReadBoolean(),
				IsInCustody = binaryReader.ReadBoolean(),
				VoiceChatEnabled = binaryReader.ReadBoolean(),
				SpawnProtection = binaryReader.ReadBoolean(),
				StolenWalletOwner = binaryReader.ReadInt32(),
				SignText = binaryReader.ReadString(),
				IsTyping = binaryReader.ReadBoolean(),
				IsOnCoke = binaryReader.ReadBoolean(),
				IsOnOpiates = binaryReader.ReadBoolean(),
				IsOnMeth = binaryReader.ReadBoolean(),
				CrewId = new CrewId(new Guid(binaryReader.ReadBytes(16)))
			};
			obj.Level = Utils.GetLevelByXP(obj.XP);
			return obj;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return new PlayerState();
		}
	}

	public static byte[] SerializeDictionary(SortedDictionary<int, PlayerState> dict)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			foreach (KeyValuePair<int, PlayerState> item in dict)
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

	public static byte[] SerializeDictionaryLegacy(SortedDictionary<int, PlayerState> dict)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			foreach (KeyValuePair<int, PlayerState> item in dict)
			{
				binaryWriter.Write(item.Key);
				byte[] array = item.Value.SerializeLegacy();
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

	public static SortedDictionary<int, PlayerState> DeserializeDictionary(byte[] data)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream(data);
			using BinaryReader binaryReader = new BinaryReader(memoryStream);
			SortedDictionary<int, PlayerState> sortedDictionary = new SortedDictionary<int, PlayerState>();
			long length = memoryStream.Length;
			while (memoryStream.Position != length)
			{
				int num = binaryReader.ReadInt32();
				int count = binaryReader.ReadInt32();
				PlayerState playerState = Deserialize(binaryReader.ReadBytes(count));
				playerState.Id = num;
				sortedDictionary[num] = playerState;
			}
			return sortedDictionary;
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.ToString());
			return new SortedDictionary<int, PlayerState>();
		}
	}
}
