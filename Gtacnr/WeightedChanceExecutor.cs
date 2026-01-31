using System;
using System.Linq;

namespace Gtacnr;

public class WeightedChanceExecutor
{
	private Random r;

	public WeightedChanceParam[] Parameters { get; set; }

	public double RatioSum => Parameters.Sum((WeightedChanceParam p) => p.Ratio);

	public WeightedChanceExecutor(params WeightedChanceParam[] parameters)
	{
		Parameters = parameters;
		r = new Random();
	}

	public void Execute()
	{
		double num = r.NextDouble() * RatioSum;
		WeightedChanceParam[] parameters = Parameters;
		foreach (WeightedChanceParam weightedChanceParam in parameters)
		{
			num -= weightedChanceParam.Ratio;
			if (num <= 0.0)
			{
				weightedChanceParam.Func();
				break;
			}
		}
	}
}
public class WeightedChanceExecutor<T>
{
	private Random r;

	public WeightedChanceParam<T>[] Parameters { get; set; }

	public double RatioSum => Parameters.Sum((WeightedChanceParam<T> p) => p.Ratio);

	public WeightedChanceExecutor(params WeightedChanceParam<T>[] parameters)
	{
		Parameters = parameters;
		r = new Random();
	}

	public T Execute()
	{
		double num = r.NextDouble() * RatioSum;
		WeightedChanceParam<T>[] parameters = Parameters;
		foreach (WeightedChanceParam<T> weightedChanceParam in parameters)
		{
			num -= weightedChanceParam.Ratio;
			if (num <= 0.0)
			{
				return weightedChanceParam.Func();
			}
		}
		return default(T);
	}
}
