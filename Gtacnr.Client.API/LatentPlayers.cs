using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.Model.PrefixedGUIDs;

namespace Gtacnr.Client.API;

public static class LatentPlayers
{
	public class LatentPlayerStateScript : Script
	{
		private static SortedDictionary<int, PlayerState> playerStates = new SortedDictionary<int, PlayerState>();

		public static int Count { get; private set; }

		public static PlayerState? Get(int id)
		{
			return playerStates.TryGetRefOrNull(id);
		}

		public static List<PlayerState> GetAll()
		{
			return playerStates.Values.ToList();
		}

		[EventHandler("gtacnr:playerData:syncAll")]
		private void OnSyncAll(byte[] stateB)
		{
			playerStates = PlayerState.DeserializeDictionary(stateB);
			Count = playerStates.Count;
		}

		[EventHandler("gtacnr:playerData:syncPlayer")]
		private void OnSyncPlayer(int playerId, byte[] data)
		{
			PlayerState playerState = PlayerState.Deserialize(data);
			playerState.Id = playerId;
			playerStates[playerId] = playerState;
		}

		[EventHandler("gtacnr:playerData:removePlayer")]
		private void OnRemovePlayer(int playerId)
		{
			playerStates.Remove(playerId);
		}

		[EventHandler("gtacnr:playerData:updateField")]
		private void OnUpdateField(int playerId, byte fieldByte, uint version, byte[] serializedValue)
		{
			PlayerDataField field = (PlayerDataField)fieldByte;
			UpdateField(playerId, field, version, serializedValue);
		}

		[EventHandler("gtacnr:playerData:updateFields")]
		private void OnUpdateFields(int playerId, byte[] serializedData)
		{
			if (!playerStates.ContainsKey(playerId))
			{
				return;
			}
			foreach (KeyValuePair<PlayerDataField, Tuple<uint, byte[]>> item3 in SyncedDataSerialization.DeserializeVersionAndDataDictionary<PlayerDataField>(serializedData))
			{
				uint item = item3.Value.Item1;
				byte[] item2 = item3.Value.Item2;
				UpdateField(playerId, item3.Key, item, item2);
			}
		}

		[EventHandler("gtacnr:playerData:updateFieldsBatched")]
		private void OnUpdateFieldsBatched(byte[] serializedBatchedData)
		{
			foreach (KeyValuePair<int, byte[]> item in SyncedDataSerialization.DeserializeBatchedDataDictionary(serializedBatchedData))
			{
				OnUpdateFields(item.Key, item.Value);
			}
		}

		private void UpdateField(int playerId, PlayerDataField field, uint version, byte[] serializedValue)
		{
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			if (!playerStates.ContainsKey(playerId))
			{
				return;
			}
			PlayerState playerState = playerStates[playerId];
			if (playerState.GetFieldVersion(field) < version)
			{
				playerState.FieldVersions[field] = version;
				switch (field)
				{
				case PlayerDataField.Uuid:
					playerState.Uid = SyncedDataSerialization.DeserializeString(serializedValue);
					break;
				case PlayerDataField.Name:
					playerState.Name = SyncedDataSerialization.DeserializeString(serializedValue);
					break;
				case PlayerDataField.ActualUsername:
					playerState.ActualUsername = SyncedDataSerialization.DeserializeString(serializedValue);
					break;
				case PlayerDataField.Ping:
					playerState.Ping = SyncedDataSerialization.DeserializeUInt16(serializedValue);
					break;
				case PlayerDataField.WantedLevel:
					playerState.WantedLevel = SyncedDataSerialization.DeserializeByte(serializedValue);
					break;
				case PlayerDataField.JobEnum:
					playerState.JobEnum = (JobsEnum)SyncedDataSerialization.DeserializeByte(serializedValue);
					break;
				case PlayerDataField.XP:
					playerState.XP = SyncedDataSerialization.DeserializeInt32(serializedValue);
					playerState.Level = Gtacnr.Utils.GetLevelByXP(playerState.XP);
					break;
				case PlayerDataField.Cash:
					playerState.Cash = SyncedDataSerialization.DeserializeInt64(serializedValue);
					break;
				case PlayerDataField.Bank:
					playerState.Bank = SyncedDataSerialization.DeserializeInt64(serializedValue);
					break;
				case PlayerDataField.Position:
					playerState.Position = SyncedDataSerialization.DeserializePosition(serializedValue);
					break;
				case PlayerDataField.Bounty:
					playerState.Bounty = SyncedDataSerialization.DeserializeInt32(serializedValue);
					break;
				case PlayerDataField.RoutingBucket:
					playerState.RoutingBucket = SyncedDataSerialization.DeserializeInt32(serializedValue);
					break;
				case PlayerDataField.Tier:
					playerState.Tier = (MembershipTier)SyncedDataSerialization.DeserializeByte(serializedValue);
					break;
				case PlayerDataField.StaffLevel:
					playerState.StaffLevel = (StaffLevel)SyncedDataSerialization.DeserializeUInt16(serializedValue);
					break;
				case PlayerDataField.AdminDuty:
					playerState.AdminDuty = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.GhostMode:
					playerState.GhostMode = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.IsSurrendering:
					playerState.IsSurrendering = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.IsCuffed:
					playerState.IsCuffed = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.IsInCustody:
					playerState.IsInCustody = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.VoiceChatEnabled:
					playerState.VoiceChatEnabled = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.SpawnProtection:
					playerState.SpawnProtection = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.StolenWalletOwner:
					playerState.StolenWalletOwner = SyncedDataSerialization.DeserializeInt32(serializedValue);
					break;
				case PlayerDataField.SignText:
					playerState.SignText = SyncedDataSerialization.DeserializeString(serializedValue);
					break;
				case PlayerDataField.IsTyping:
					playerState.IsTyping = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.IsOnCoke:
					playerState.IsOnCoke = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.IsOnOpiates:
					playerState.IsOnOpiates = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.IsOnMeth:
					playerState.IsOnMeth = SyncedDataSerialization.DeserializeBool(serializedValue);
					break;
				case PlayerDataField.CrewId:
					playerState.CrewId = new CrewId(SyncedDataSerialization.DeserializeGuid(serializedValue));
					break;
				}
			}
		}

		[Update]
		private async Coroutine LegacyDataTask()
		{
			await Script.Wait(1000);
			BaseScript.TriggerEvent("gtacnr:latentPlayerStateB", new object[1] { PlayerState.SerializeDictionaryLegacy(playerStates) });
		}
	}

	public static List<PlayerState> All => LatentPlayerStateScript.GetAll();

	public static int Count => LatentPlayerStateScript.Count;

	public static PlayerState? Get(int playerServerId)
	{
		return LatentPlayerStateScript.Get(playerServerId);
	}

	public static PlayerState? Get(Player player)
	{
		return LatentPlayerStateScript.Get(player.ServerId);
	}

	public static IEnumerable<PlayerState> Get(IEnumerable<int> playerIds)
	{
		return from id in playerIds
			select Get(id) into ps
			where ps != null
			select ps;
	}
}
