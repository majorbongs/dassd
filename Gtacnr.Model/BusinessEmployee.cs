using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CitizenFX.Core;
using Gtacnr.Client.Businesses;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class BusinessEmployee
{
	public Dictionary<string, Vector4> LocationResourceOverrides = new Dictionary<string, Vector4>();

	public EmployeeRole Role { get; set; }

	public string PedModel { get; set; }

	public List<string> Weapons { get; set; }

	public bool HasMenu { get; set; } = true;

	public bool CanBeRobbed { get; set; } = true;

	public bool HasShopDialog { get; set; } = true;

	[JsonProperty("Location_")]
	public Vector4 Location { get; set; }

	public Vector3 Position => Location.XYZ();

	public float Heading => Location.W;

	public BusinessEmployeeState State { get; set; }

	public string Id { get; set; }

	public string BusinessId { get; set; }

	public string Name { get; set; }

	public DateTime HireDateTime { get; set; }

	public int Paycheck { get; set; }

	public float Experience { get; set; }

	public float FightOrFlight { get; set; }

	public float Happiness { get; set; }

	public Sex Sex { get; set; }

	public Appearance Appearance { get; set; }

	public Apparel Apparel { get; set; }

	public override string ToString()
	{
		return Name;
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		foreach (string key in LocationResourceOverrides.Keys)
		{
			Vector4 location = LocationResourceOverrides[key];
			if (Utils.IsResourceLoadedOrLoading(key))
			{
				Location = location;
			}
		}
	}
}
