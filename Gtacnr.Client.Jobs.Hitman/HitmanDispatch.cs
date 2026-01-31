using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Hitman;

public class HitmanDispatch : BaseDispatch<KillContractInfo>
{
	private static readonly MenuItem noCallsMenuItem = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_JOB_NO_CONTRACTS_TEXT), LocalizationController.S(Entries.Imenu.IMENU_JOB_NO_CONTRACTS_DESCRIPTION))
	{
		Enabled = false
	};

	private static Dictionary<int, KillContractInfo> playerContracts = new Dictionary<int, KillContractInfo>();

	protected override MenuItem NoItemsMenuItem => noCallsMenuItem;

	public static Dictionary<int, KillContractInfo> PlayerContracts => playerContracts;

	public HitmanDispatch()
		: base(LocalizationController.S(Entries.Businesses.MENU_HITMAN_TITLE), LocalizationController.S(Entries.Businesses.MENU_HITMAN_CONTRACTS_SUBTITLE))
	{
	}

	public override async void OnDispatch(int playerId, string jData)
	{
		KillContractInfo contract = jData.Unjson<KillContractInfo>();
		if (contract == null || contract.PlayerId == Game.Player.ServerId)
		{
			return;
		}
		PlayerState playerState = LatentPlayers.Get(contract.PlayerId);
		if (playerState == null)
		{
			return;
		}
		Game.PlaySound("Event_Message_Purple", "GTAO_FM_Events_Soundset");
		if (playerContracts.ContainsKey(contract.PlayerId))
		{
			KillContractInfo killContractInfo = playerContracts[contract.PlayerId];
			killContractInfo.Reward = contract.Reward;
			killContractInfo.ExpirationDate = contract.ExpirationDate;
			if (callItems.ContainsKey(killContractInfo))
			{
				killContractInfo.UpdateMenuItem(callItems[killContractInfo]);
			}
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.HITMAN_CONTRACT_UPDATED, playerState.ColorNameAndId, contract.Reward.ToCurrencyString()));
		}
		else
		{
			AddCall(contract);
			playerContracts[contract.PlayerId] = contract;
			await InteractiveNotificationsScript.Show(LocalizationController.S(Entries.Jobs.HITMAN_NEW_CONTRACT, playerState.ColorNameAndId, contract.Reward.ToCurrencyString()), InteractiveNotificationType.Notification, OnAccepted, TimeSpan.FromSeconds(5.0), 0u, "Accept", "Accept (hold)", () => (Entity)(object)Game.PlayerPed == (Entity)null || ((Entity)Game.PlayerPed).IsDead);
		}
		bool OnAccepted()
		{
			HitmanScript.SetTarget(contract.PlayerId);
			return true;
		}
	}

	protected override void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is KillContractInfo killContractInfo)
		{
			HitmanScript.SetTarget(killContractInfo.PlayerId);
		}
	}

	public override void ResetMenu()
	{
		base.ResetMenu();
		playerContracts.Clear();
	}

	public void RemovePlayer(int playerId)
	{
		if (playerContracts.TryGetValue(playerId, out KillContractInfo value))
		{
			if (callItems.TryGetValue(value, out MenuItem value2))
			{
				base.CallsMenu.RemoveMenuItem(value2);
				callItems.Remove(value);
			}
			playerContracts.Remove(playerId);
		}
		if (callItems.Count == 0)
		{
			base.CallsMenu.ClearMenuItems();
			base.CallsMenu.AddMenuItem(NoItemsMenuItem);
		}
	}

	public void UpdateFromRetrievedContracts(List<KillContractInfo> contracts)
	{
		foreach (KillContractInfo contract in contracts)
		{
			if (contract.PlayerId != Game.Player.ServerId)
			{
				AddCall(contract);
				playerContracts[contract.PlayerId] = contract;
			}
		}
	}
}
