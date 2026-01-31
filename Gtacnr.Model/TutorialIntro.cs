using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class TutorialIntro
{
	public List<TutorialIntroStep> Steps { get; set; } = new List<TutorialIntroStep>();

	public float[] TeleportTo { get; set; }

	public Vector4 VTeleportTo => new Vector4(TeleportTo[0], TeleportTo[1], TeleportTo[2], TeleportTo[3]);
}
