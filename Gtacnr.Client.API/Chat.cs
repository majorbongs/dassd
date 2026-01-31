using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Model;

namespace Gtacnr.Client.API;

public static class Chat
{
	private static Dictionary<string, ChatSuggestion> chatSuggestions = new Dictionary<string, ChatSuggestion>();

	public static void Clear()
	{
		BaseScript.TriggerEvent("chat:clear", new object[0]);
	}

	public static void AddMessage(string author, Color color, string text)
	{
		BaseScript.TriggerEvent("chat:addMessage", new object[1]
		{
			new
			{
				color = new int[3] { color.R, color.G, color.B },
				args = new string[2] { author, text }
			}
		});
	}

	public static void AddMessage(Color color, string text)
	{
		BaseScript.TriggerEvent("chat:addMessage", new object[1]
		{
			new
			{
				color = new int[3] { color.R, color.G, color.B },
				args = new string[1] { text }
			}
		});
	}

	public static void AddMessage(string text)
	{
		BaseScript.TriggerEvent("chat:addMessage", new object[1]
		{
			new
			{
				color = new int[3] { 255, 255, 255 },
				args = new string[1] { text }
			}
		});
	}

	public static void AddMessage(ChatMessage chatMessage)
	{
		BaseScript.TriggerEvent("gtacnr:chat:addMessage", new object[1] { chatMessage.Json() });
	}

	public static void AddSuggestion(string command, string help, params ChatParamSuggestion[] paramSuggestions)
	{
		ChatSuggestion value = new ChatSuggestion
		{
			Command = command,
			Help = help,
			ParamSuggestions = paramSuggestions.ToList()
		};
		chatSuggestions[command] = value;
	}

	public static void AddSuggestion(string[] commands, string help, params ChatParamSuggestion[] paramSuggestions)
	{
		for (int i = 0; i < commands.Length; i++)
		{
			AddSuggestion(commands[i], help, paramSuggestions);
		}
	}

	public static void AddSuggestion(ChatSuggestion suggestion)
	{
		chatSuggestions[suggestion.Command] = suggestion;
	}

	public static void RefreshSuggestions()
	{
		foreach (ChatSuggestion value in chatSuggestions.Values)
		{
			BaseScript.TriggerEvent("gtacnr:chat:addSuggestion", new object[1] { value.Json() });
		}
		BaseScript.TriggerEvent("gtacnr:chat:refreshSuggestions", new object[0]);
	}
}
