using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Communication;

public class BasicCommandsScript : Script
{
	private Menu infoMenu;

	protected override void OnStarted()
	{
		infoMenu = new Menu("Player Info", "Player")
		{
			PlaySelectSound = false,
			PlayErrorSound = false
		};
		Chat.AddSuggestion("/info", "Displays some basic information about a connected player given their id or username.", new ChatParamSuggestion("player", "The id or part of username of the player you want to see the information of.", isOptional: true));
		Chat.AddSuggestion("/i", "Displays some basic information about a connected player given their id or username.", new ChatParamSuggestion("player", "The id or part of username of the player you want to see the information of.", isOptional: true));
		Chat.AddSuggestion("/q", "!Disconnects you from the server and restarts FiveM.");
	}

	[Command("info")]
	private void InfoCommand(string[] args)
	{
		int result = 0;
		if (args.Length != 0)
		{
			if (!int.TryParse(args[0], out result))
			{
				result = 0;
				string text = string.Join(" ", args).ToLowerInvariant();
				foreach (PlayerState item in LatentPlayers.All)
				{
					if (item.Name.ToLowerInvariant().Contains(text))
					{
						result = item.Id;
						break;
					}
				}
				if (result == 0)
				{
					Utils.PlayErrorSound();
					Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Player \"" + text + "\" is not connected.");
					return;
				}
			}
		}
		else
		{
			result = Game.Player.ServerId;
		}
		if (result == 0)
		{
			Utils.PlayErrorSound();
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Player not found.");
			return;
		}
		PlayerState playerState = LatentPlayers.Get(result);
		if (playerState == null)
		{
			Utils.PlayErrorSound();
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, $"Player id {result} is not connected.");
			return;
		}
		infoMenu.CloseMenu();
		infoMenu.ClearMenuItems();
		infoMenu.MenuSubtitle = playerState.ColorNameAndId;
		string text2 = Gtacnr.Data.Jobs.GetJobData(playerState.Job)?.Name ?? "N/A";
		infoMenu.CounterPreText = playerState.ColorTextCode + text2.Replace("None", "No Job");
		if (playerState.AdminDuty)
		{
			infoMenu.AddMenuItem(new MenuItem("~g~ON-DUTY STAFF"));
		}
		RevengeData cachedRevengeData = DeathScript.CachedRevengeData;
		if (cachedRevengeData != null)
		{
			bool flag = cachedRevengeData.Targets.Contains(result);
			bool flag2 = cachedRevengeData.Claimants.Contains(result);
			if (flag || flag2)
			{
				infoMenu.AddMenuItem(new MenuItem("~r~Revenge")
				{
					Label = ((flag && flag2) ? "~r~BOTH" : (flag ? "~r~ON THEM" : (flag2 ? "~r~ON YOU" : "")))
				});
			}
		}
		infoMenu.AddMenuItem(new MenuItem("~b~Level")
		{
			Label = $"~b~{playerState.Level} ({playerState.XP} XP)"
		});
		if (!playerState.JobEnum.IsPublicService())
		{
			infoMenu.AddMenuItem(new MenuItem("Wanted Level")
			{
				Label = $"{playerState.ColorTextCode}{playerState.WantedLevel}"
			});
			if (playerState.Bounty > 0 && playerState.WantedLevel == 5)
			{
				infoMenu.AddMenuItem(new MenuItem("Bounty")
				{
					Label = "~r~" + playerState.Bounty.ToCurrencyString()
				});
			}
		}
		if ((int)playerState.Tier > 0)
		{
			infoMenu.AddMenuItem(new MenuItem("~p~Membership")
			{
				Label = "~p~" + ((playerState.Tier == MembershipTier.Silver) ? "Silver" : ((playerState.Tier == MembershipTier.Gold) ? "Gold" : "Unknown"))
			});
		}
		infoMenu.OpenMenu();
	}

	[Command("i")]
	private void ICommand(string[] args)
	{
		InfoCommand(args);
	}

	[Command("q")]
	private async void QCommand()
	{
		if (await Utils.ShowConfirm("Do you really want to ~r~disconnect ~s~from ~b~Cops ~s~and ~r~Robbers ~s~V?", "Disconnect"))
		{
			API.RestartGame();
		}
	}
}
