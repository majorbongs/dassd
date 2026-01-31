using System.Collections.Generic;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class RadioChannel
{
	public float Frequency { get; set; }

	public string Tag { get; set; }

	public string DisplayName { get; set; }

	public string Description { get; set; }

	public HashSet<string> Jobs { get; set; } = new HashSet<string>();

	public int RequiredLevel { get; set; }

	public StaffLevel StaffLevel { get; set; }

	public bool IsAviation { get; set; }

	public bool RequiresController { get; set; }
}
