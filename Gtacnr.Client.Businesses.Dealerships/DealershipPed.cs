using CitizenFX.Core;

namespace Gtacnr.Client.Businesses.Dealerships;

public class DealershipPed
{
	public Ped Ped { get; set; }

	public Vector3 SpawnPosition { get; set; }

	public string DealershipId { get; set; }

	public float SpawnHeading { get; set; }

	public string ModelString { get; set; }

	public string BusinessId { get; set; }

	public bool IsRespawning { get; set; }
}
