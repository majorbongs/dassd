using Newtonsoft.Json;

namespace Gtacnr.Model;

public class ChatParamSuggestion
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("help")]
	public string Help { get; set; }

	[JsonProperty("optional")]
	public bool IsOptional { get; set; }

	public ChatParamSuggestion(string name, string help, bool isOptional)
	{
		Name = name;
		Help = help;
		IsOptional = isOptional;
	}

	public ChatParamSuggestion(string name, string help)
		: this(name, help, isOptional: false)
	{
	}

	public ChatParamSuggestion()
	{
	}
}
