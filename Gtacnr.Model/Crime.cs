using System;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class Crime
{
	public string Id { get; set; }

	public string CharacterId { get; set; }

	public string CrimeType { get; set; }

	public Vector3 Location { get; set; }

	public DateTime DateTime { get; set; }

	public string AffectedCharacterId { get; set; }

	public string AffectedBusinessId { get; set; }

	public int WantedLevelBefore { get; set; }

	public int WantedLevelAfter { get; set; }

	public int DamageValue { get; set; }

	public int FineBefore { get; set; }

	public int FineAfter { get; set; }

	public int PlayerId { get; set; }

	public string PlayerName { get; set; }

	public int AffectedPlayerId { get; set; }

	public string AffectedPlayerName { get; set; }

	public string InvolvedVehicleModelId { get; set; }

	public int CurrentVehicleModel { get; set; }

	public int CurrentVehicleColor { get; set; }
}
