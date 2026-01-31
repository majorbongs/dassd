using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Jobs.Trucker;
using Gtacnr.Model.Enums;
using MenuAPI;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class DeliveryJob
{
	public string Id { get; set; }

	public DeliveryJobType Type { get; set; }

	public DeliveryJobLocation PickUpLocation { get; set; }

	[JsonProperty("DropOffLocations")]
	public List<DeliveryJobLocation> _DropOffLocations { get; set; }

	[JsonIgnore]
	public IReadOnlyCollection<DeliveryJobLocation> DropOffLocations => _DropOffLocations;

	public float Weight { get; set; }

	public long Value { get; set; }

	public GameTime Deadline { get; set; }

	public GameTime AutoCancelTime { get; set; }

	public long PaymentAmount { get; set; }

	public long TimeBonus { get; set; }

	[JsonIgnore]
	public bool HasTrailer => Constants.DeliveryDriver.GetRequiredVehicleType(Type) == DeliveryJobVehicleType.SemiTruck;

	public TimeSpan GetDeliveryTimeLeft()
	{
		return Deadline - GameTime.Now;
	}

	public static float CalculateDistance(Vector3 startPos, Vector3 endPos)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		float num = API.CalculateTravelDistanceBetweenPoints(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z);
		if (num == 100000f)
		{
			num = (float)Math.Sqrt(((Vector3)(ref startPos)).DistanceToSquared2D(endPos));
		}
		return num;
	}

	public string GetTotalDistanceString()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		DeliveryJobLocation deliveryJobLocation = PickUpLocation;
		foreach (DeliveryJobLocation dropOffLocation in DropOffLocations)
		{
			float num2 = CalculateDistance(deliveryJobLocation.Coordinates.XYZ(), dropOffLocation.Coordinates.XYZ());
			deliveryJobLocation = dropOffLocation;
			num += num2;
		}
		if (API.GetProfileSetting(227) != 1)
		{
			return $"{num.ToMiles():0.00}mi";
		}
		return $"{num.ToKm():0.00}km";
	}

	public string GetDeliveryLocationString()
	{
		if (DropOffLocations.Count <= 1)
		{
			return DropOffLocations.First().Name;
		}
		return $"{DropOffLocations.Count} locations";
	}

	public Tuple<string, string> GetMenuItemTitleAndPrefix()
	{
		string text = Utils.GetDescription(Type) + " to ~b~" + DropOffLocations.Last().Name + "~s~" + ((DropOffLocations.Count > 1) ? $" +{DropOffLocations.Count - 1}" : "");
		string item = "";
		if (text.Replace("~b~", "").Replace("~s~", "").Length >= 38)
		{
			item = text + "\n";
			text = text.Substring(0, 40) + "~s~...";
			if (!text.EndsWith("~s~"))
			{
				if (text.EndsWith("~s"))
				{
					text += "~";
				}
				else if (text.EndsWith("~"))
				{
					text += "s~";
				}
			}
		}
		return new Tuple<string, string>(text, item);
	}

	public MenuItem ToMenuItem()
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		TimeSpan deliveryTimeLeft = GetDeliveryTimeLeft();
		string text = ((deliveryTimeLeft.TotalHours > 15.0) ? "~g~" : ((deliveryTimeLeft.TotalHours > 8.0) ? "~y~" : "~r~"));
		string arg = $"{'\u200b'}{text}{Math.Floor(deliveryTimeLeft.TotalHours):00}:{deliveryTimeLeft.Minutes:00}{'\u200c'} left";
		float m = CalculateDistance(((Entity)Game.PlayerPed).Position, PickUpLocation.Coordinates.XYZ());
		string text2 = ((API.GetProfileSetting(227) == 1) ? $"{m.ToKm():0.00}km" : $"{m.ToMiles():0.00}mi");
		DeliveryJobVehicleType requiredVehicleType = Constants.DeliveryDriver.GetRequiredVehicleType(Type);
		DeliveryJobVehicleType? truckType = TruckerJobScript.GetTruckType(Game.PlayerPed.CurrentVehicle);
		string text3 = ((truckType.HasValue && requiredVehicleType.HasFlag(truckType)) ? "~g~" : "~r~");
		string description = Utils.GetDescription(requiredVehicleType);
		Tuple<string, string> menuItemTitleAndPrefix = GetMenuItemTitleAndPrefix();
		MenuItem menuItem = new MenuItem(menuItemTitleAndPrefix.Item1);
		menuItem.Label = "~g~" + PaymentAmount.ToCurrencyString();
		menuItem.Description = menuItemTitleAndPrefix.Item2 + $"Deadline: ~b~{Deadline} ~s~({arg}~s~)\n" + "Pick up at: ~b~" + PickUpLocation.Name + " ~s~(~y~" + text2 + "~s~)\nDeliver to: ~b~" + GetDeliveryLocationString() + " ~s~(~y~" + GetTotalDistanceString() + "~s~)\n" + $"Weight: ~b~{Weight / 1000f:0.00} tons~s~\n" + "Truck type: " + text3 + description + "~s~";
		menuItem.ItemData = this;
		return menuItem;
	}
}
