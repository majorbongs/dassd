using System.Collections.Generic;

namespace Gtacnr.Model;

public class ScratchCard
{
	public string Name { get; set; }

	public int NumMax { get; set; }

	public List<int> Numbers { get; set; } = new List<int>();

	public List<int> WinningNumbers { get; set; } = new List<int>();

	public List<int> Prizes { get; set; } = new List<int>();

	public int CalculatePrize()
	{
		if (Numbers.Count == 0 || WinningNumbers.Count == 0 || Prizes.Count == 0 || Numbers.Count != WinningNumbers.Count || WinningNumbers.Count != Prizes.Count)
		{
			return 0;
		}
		int num = 0;
		foreach (int number in Numbers)
		{
			if (WinningNumbers.Contains(number))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return 0;
		}
		return Prizes[num - 1];
	}
}
