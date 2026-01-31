using System;
using Gtacnr.Data;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class Character
{
	private string _job = "none";

	public string Id { get; set; }

	public string OwnerUserId { get; set; }

	public int Slot { get; set; }

	public DateTime? CreationDate { get; set; }

	public DateTime? LastPlayDate { get; set; }

	public Sex Sex { get; set; }

	public Appearance Appearance { get; set; }

	[JsonConverter(typeof(ApparelConverter))]
	public Apparel Apparel { get; set; }

	public string Job
	{
		get
		{
			return _job;
		}
		set
		{
			_job = value;
			if (string.IsNullOrWhiteSpace(_job))
			{
				_job = "none";
			}
		}
	}

	public int WantedLevel { get; set; }

	public int Bounty { get; set; }

	public int Health { get; set; }

	public int Armor { get; set; }

	public int SyncResult { get; set; }

	public string CreationDateS => CreationDate?.ToFormalDate();

	public string LastPlayDateS => LastPlayDate?.ToFormalDate();

	public string SexS
	{
		get
		{
			if (Sex != Sex.Male)
			{
				return "Female";
			}
			return "Male";
		}
	}

	public string JobS => Jobs.GetJobData(Job).Name;
}
