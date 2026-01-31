using CitizenFX.Core;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class PingInfo
{
	public Vector3 Position { get; set; }

	public string Label { get; set; }

	public string Author { get; set; }

	public string AuthorUid { get; set; }

	public PingType Type { get; set; }
}
