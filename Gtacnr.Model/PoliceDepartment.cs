using System.Collections.Generic;

namespace Gtacnr.Model;

public class PoliceDepartment
{
	public string Id { get; set; }

	public string Acronym { get; set; }

	public string Name { get; set; }

	public int BlipSprite { get; set; }

	public int BlipColor { get; set; }

	public List<string> Uniforms { get; set; }
}
