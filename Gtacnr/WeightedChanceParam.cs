using System;

namespace Gtacnr;

public class WeightedChanceParam
{
	public Action Func { get; }

	public double Ratio { get; }

	public WeightedChanceParam(Action func, double ratio)
	{
		Func = func;
		Ratio = ratio;
	}
}
public class WeightedChanceParam<T>
{
	public Func<T> Func { get; }

	public double Ratio { get; }

	public WeightedChanceParam(Func<T> func, double ratio)
	{
		Func = func;
		Ratio = ratio;
	}
}
