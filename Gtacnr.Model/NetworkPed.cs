using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class NetworkPed
{
	public int NetworkHandle { get; set; }

	public int LocalHandle { get; set; }

	public bool CreationInProgress { get; set; }

	public Vector3 Location { get; set; }

	public float Heading { get; set; }

	public Sex Sex { get; set; }

	public uint ModelHash { get; set; }

	public Appearance Appearance { get; set; }

	public Apparel Apparel { get; set; }

	public List<KeyValuePair<string, float>> DecorsFloat { get; set; } = new List<KeyValuePair<string, float>>();

	public List<KeyValuePair<string, bool>> DecorsBool { get; set; } = new List<KeyValuePair<string, bool>>();

	public List<KeyValuePair<string, int>> DecorsInt { get; set; } = new List<KeyValuePair<string, int>>();

	public List<KeyValuePair<string, DateTime>> DecorsTime { get; set; } = new List<KeyValuePair<string, DateTime>>();

	[JsonIgnore]
	public bool Exists => NetworkHandle != 0;
}
