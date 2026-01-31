using System.Collections.Generic;

namespace Gtacnr.Model;

public class FireDepartment
{
	public string Id { get; set; }

	public string Name { get; set; }

	public Dictionary<string, string> Uniforms { get; set; } = new Dictionary<string, string>();
}
