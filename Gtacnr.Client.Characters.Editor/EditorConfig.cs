using System.Collections.Generic;
using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace Gtacnr.Client.Characters.Editor;

public class EditorConfig
{
	public List<EditorEntry> Eyebrows { get; set; }

	public List<EditorEntry> FacialHairstyles { get; set; }

	public List<EditorEntry> Blemishes { get; set; }

	public List<EditorEntry> Agings { get; set; }

	public List<EditorEntry> Complexions { get; set; }

	public List<EditorEntry> Moles { get; set; }

	public List<EditorEntry> Damages { get; set; }

	public List<EditorEntry> EyeColors { get; set; }

	public List<EditorEntry> Makeups { get; set; }

	public List<EditorEntry> Lipsticks { get; set; }

	public List<string> Hairstyles { get; set; }

	public List<string> Outfits { get; set; }

	public List<string> Hats { get; set; }

	public List<string> Glasses { get; set; }

	public List<string> Watches { get; set; }

	public static List<EditorConfig> Default { get; private set; }

	static EditorConfig()
	{
		Default = JsonConvert.DeserializeObject<List<EditorConfig>>(API.LoadResourceFile(API.GetCurrentResourceName(), "data/characterEditor.json"));
	}
}
