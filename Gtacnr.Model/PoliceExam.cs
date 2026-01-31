using System.Collections.Generic;
using System.Linq;

namespace Gtacnr.Model;

public class PoliceExam
{
	public string Title { get; set; }

	public int PassingScore { get; set; }

	public int TimeMinutes { get; set; }

	public List<PoliceExamQuestion> Questions { get; set; } = new List<PoliceExamQuestion>();

	public void ShuffleQuestions()
	{
		foreach (PoliceExamQuestion question in Questions)
		{
			question.Options = question.Options.Shuffle().ToList();
		}
		Questions = Questions.Shuffle().ToList();
	}

	public void InitQuestionsIndex()
	{
		for (int i = 0; i < Questions.Count; i++)
		{
			Questions[i].OriginalIndex = i;
			Questions[i].InitOptionsIndex();
		}
	}
}
