namespace Gtacnr.Model;

public class Disease : IDisease
{
	public string Id { get; set; }

	public string Name { get; set; }

	public int Time { get; set; }

	public int Health { get; set; }

	public float Spread { get; set; }

	public float Endurance { get; set; }

	public int Cough { get; set; }

	public int Vomit { get; set; }

	public int Fever { get; set; }

	public bool Fatal { get; set; }

	public DiseaseSpawnMode SpawnMode { get; set; }

	public int SpawnChance { get; set; }

	public int CureCost { get; set; }
}
