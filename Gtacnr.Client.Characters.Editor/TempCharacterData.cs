using Gtacnr.Model;

namespace Gtacnr.Client.Characters.Editor;

public class TempCharacterData
{
	public Character Character { get; set; }

	public int Dad { get; set; }

	public int Mom { get; set; }

	public float HeadShape { get; set; }

	public float SkinTone { get; set; }

	public string SelectedOutfit { get; set; }

	public string SelectedHat { get; set; }

	public string SelectedGlasses { get; set; }

	public string SelectedWatch { get; set; }
}
