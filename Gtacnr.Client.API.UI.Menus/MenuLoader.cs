using MenuAPI;

namespace Gtacnr.Client.API.UI.Menus;

public static class MenuLoader
{
	public static Menu FromJson(string json)
	{
		return json.Unjson<MenuBuilder>().Build();
	}

	public static Menu FromJsonFile(string filename)
	{
		return FromJson(Gtacnr.Utils.LoadCurrentResourceFile(filename));
	}
}
