using System;
using System.Collections.Generic;
using Gtacnr.ResponseCodes;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class RobberyInfo
{
	private float _intimidation;

	public RobberyResponse Response { get; set; }

	public List<int> Players { get; set; }

	public float Intimidation
	{
		get
		{
			return _intimidation;
		}
		set
		{
			if (value < 0f)
			{
				value = 0f;
			}
			if (value > 1f)
			{
				value = 1f;
			}
			_intimidation = value;
		}
	}

	public int EmployeeIndex { get; set; }

	public int MoneyTakenFromCounter { get; set; }

	public int MoneyTakenFromSafe { get; set; }

	[JsonIgnore]
	public int TotalMoneyTaken => MoneyTakenFromCounter + MoneyTakenFromSafe;

	public bool IsCashRegisterEmpty { get; set; }

	public bool IsSafeEmpty { get; set; }

	public bool WasSafeOpened { get; set; }

	public bool WasSafeCanceled { get; set; }

	public int PlayerRobbingSafe { get; set; }

	public List<int> DisabledLootIndices { get; set; } = new List<int>();

	public bool PoliceCalled { get; set; }

	public int CalculateCut()
	{
		try
		{
			if (Players.Count == 0)
			{
				return 0;
			}
			return Convert.ToInt32(Math.Ceiling((double)TotalMoneyTaken / (double)Players.Count));
		}
		catch (Exception)
		{
			return 0;
		}
	}
}
