namespace Gtacnr.Model;

public interface IDisease
{
	string Id { get; }

	string Name { get; }

	int Time { get; }

	int Health { get; }

	float Spread { get; }

	float Endurance { get; }

	int Cough { get; }

	int Vomit { get; }

	int Fever { get; }

	bool Fatal { get; }

	DiseaseSpawnMode SpawnMode { get; }

	int SpawnChance { get; }

	int CureCost { get; }
}
