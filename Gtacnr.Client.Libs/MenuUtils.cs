using CitizenFX.Core;

namespace Gtacnr.Client.Libs;

public static class MenuUtils
{
	public static string[] SplitString(string inputString)
	{
		int num = (inputString.Length - 1) / 50 + 1;
		string[] array = new string[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = inputString.Substring(i * 50, MathUtil.Clamp(inputString.Substring(i * 50).Length, 0, 50));
		}
		return array;
	}
}
