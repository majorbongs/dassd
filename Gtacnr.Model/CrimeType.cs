using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class CrimeType
{
	public string Id { get; set; }

	public string Description { get; set; } = "";

	public string Emoji { get; set; }

	public int MinWantedLevel { get; set; }

	public int MaxWantedLevel { get; set; }

	public int Value { get; set; }

	public int RadioCode { get; set; }

	public float RangeMultiplier { get; set; } = 1f;

	public bool IsViolent { get; set; }

	public CrimeSeverity CrimeSeverity { get; set; }

	public string ColorStr { get; set; } = "~s~";
}
