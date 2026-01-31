namespace Gtacnr.Model;

public class PoliceExamOption
{
	public string Option { get; set; }

	public bool IsCorrect { get; set; }

	public int Points { get; set; } = 1;

	public bool TerminateExamIfSelected { get; set; }

	public int? OriginalIndex { get; set; }
}
