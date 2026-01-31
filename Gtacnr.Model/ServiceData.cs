using System.Collections.Generic;

namespace Gtacnr.Model;

public class ServiceData
{
	public Dictionary<string, float> Prices { get; set; } = new Dictionary<string, float>();

	public List<string> Certifications { get; set; } = new List<string>();

	public List<string> InStock { get; set; } = new List<string>();
}
