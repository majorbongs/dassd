using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Premium;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Communication;

public class ChatScript : Script
{
	private static bool isChatInputOpen;

	public static bool IsChatInputOpen => isChatInputOpen;

	protected override async void OnStarted()
	{
		Chat.AddSuggestion("/me", "Tells in the global chat that you are performing a certain action or task.", new ChatParamSuggestion("action", "The action that you're performing."));
		Chat.AddSuggestion("/pm", "Sends a direct message to another player.", new ChatParamSuggestion("receiver", "The id of the player you want to send the message to."), new ChatParamSuggestion("message", "The content of the message."));
		Chat.AddSuggestion("/dm", "Sends a direct message to another player.", new ChatParamSuggestion("receiver", "The id of the player you want to send the message to."), new ChatParamSuggestion("message", "The content of the message."));
		Chat.AddSuggestion("/r", "Replies to the last sender.", new ChatParamSuggestion("message", "The content of the message."));
		Chat.AddSuggestion("/dms", "Blocks all incoming direct messages. Use again to cancel.");
		Chat.AddSuggestion("/help", "Shows a link to help information.");
		Chat.AddSuggestion("/discord", "Shows the Discord link.");
		Chat.AddSuggestion("/rules", "Shows a link to the server rules.");
		Chat.AddSuggestion("/e", "Plays an emote.", new ChatParamSuggestion("name", "The name of the emote."));
		Chat.AddSuggestion("/emote", "Plays an emote.", new ChatParamSuggestion("name", "The name of the emote."));
		Chat.AddSuggestion("/emotes", "Shows all emotes.");
		Chat.AddSuggestion("/emotemenu", "Shows the emote menu.");
		Chat.AddSuggestion("/walk", "Changes your walking style.", new ChatParamSuggestion("name", "The name of the walking style."));
		Chat.AddSuggestion("/walks", "Shows all walking styles.");
		KeysScript.AttachListener((Control)245, OnKeyEvent, 100);
		await BaseScript.Delay(5000);
		Chat.RefreshSuggestions();
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (canOpenChat())
		{
			BaseScript.TriggerEvent("gtacnr:chat:input", new object[0]);
			isChatInputOpen = true;
			return true;
		}
		return false;
		bool canOpenChat()
		{
			if (eventType == KeyEventType.JustPressed && SpawnScript.HasSpawned && !Utils.IsScreenFadingInProgress() && !API.IsScreenFadedOut() && !API.IsPauseMenuActive() && !API.IsPlayerSwitchInProgress() && !Utils.IsOnScreenKeyboardActive)
			{
				return !MenuController.IsAnyMenuOpen();
			}
			return false;
		}
	}

	[Command("help")]
	private void HelpCommand()
	{
		Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.NOTI_FANDOM));
	}

	[Command("discord")]
	private void DiscordCommand()
	{
		Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.NOTI_DISCORD));
	}

	[Command("rules")]
	private void RulesCommand()
	{
		Utils.SendNotification("Read the server rules at ~b~" + ExternalLinks.Collection.Rules + "~s~. We have translated the rules in multiple languages.");
	}

	[Command("refresh-suggestions")]
	private void RefreshSuggestionsCommand()
	{
		Chat.RefreshSuggestions();
	}

	[EventHandler("gtacnr:chat:inputClosed")]
	private void OnInputClosed()
	{
		isChatInputOpen = false;
	}

	[EventHandler("gtacnr:chat:ready")]
	private void OnChatReady()
	{
		Chat.RefreshSuggestions();
		BaseScript.TriggerEvent("gtacnr:membershipUpdated", new object[1] { (int)MembershipScript.GetCurrentMembershipTier() });
	}

	[EventHandler("gtacnr:spawned")]
	private void OnSpawned()
	{
		if (!Preferences.ToggleChatDMs.Get())
		{
			API.ExecuteCommand("dms");
		}
	}

	[EventHandler("gtacnr:chat:toggleDMs")]
	private void OnDMStateToggled(bool value)
	{
		Preferences.ToggleChatDMs.Set(value);
		if (value)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Message, "!! You are now accepting direct messages.");
		}
		else
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Message, "!! You are blocking all direct messages. Type /dms again to cancel.");
		}
		Game.PlaySound("CHALLENGE_UNLOCKED", "HUD_AWARDS");
	}
}
