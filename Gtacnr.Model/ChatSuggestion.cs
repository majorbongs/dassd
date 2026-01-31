using System.Collections.Generic;
using System.Linq;

namespace Gtacnr.Model;

public class ChatSuggestion
{
	public string Command { get; set; }

	public string Help { get; set; }

	public List<ChatParamSuggestion> ParamSuggestions { get; set; } = new List<ChatParamSuggestion>();

	public ChatSuggestion()
	{
	}

	public ChatSuggestion(string command, string help, params ChatParamSuggestion[] suggestions)
	{
		Command = command;
		Help = help;
		if (suggestions != null)
		{
			ParamSuggestions = suggestions.ToList();
		}
	}

	public ChatSuggestion(string command, string help)
		: this(command, help, (ChatParamSuggestion[])null)
	{
	}
}
