namespace Gtacnr.Model;

public class DailyChallengeEntry
{
	public int Day { get; set; }

	public string Id { get; set; }

	public uint PointsNeeded { get; set; }

	public DailyChallengeEntry MakeFromDefinition(DailyChallenge challenge, int day)
	{
		Day = day;
		Id = challenge.Id;
		uint step = challenge.Step;
		uint num = (challenge.Max - challenge.Min) / step;
		int randomInt = Utils.GetRandomInt(0, (int)(num + 1));
		PointsNeeded = (uint)(challenge.Min + randomInt * step);
		return this;
	}
}
