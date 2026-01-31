using System.Collections.Generic;

namespace Gtacnr.Client.Anticheat;

public class BadWordsScript : Script
{
	public static List<string> BannedWords { get; } = Gtacnr.Utils.LoadJson<List<string>>("data/autobanWords.json");
}
