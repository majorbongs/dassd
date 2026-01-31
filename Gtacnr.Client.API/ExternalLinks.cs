namespace Gtacnr.Client.API;

public static class ExternalLinks
{
	public static LinksCollection Collection { get; private set; } = Gtacnr.Utils.LoadJson<LinksCollection>("data/urls.json");
}
