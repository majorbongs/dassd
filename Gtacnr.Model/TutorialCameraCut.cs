using CitizenFX.Core;

namespace Gtacnr.Model;

public class TutorialCameraCut
{
	public float[] From { get; set; }

	public float[] To { get; set; }

	public float[] Focus { get; set; }

	public float Speed { get; set; }

	public Vector3 VFrom => new Vector3(From[0], From[1], From[2]);

	public Vector3 VTo => new Vector3(To[0], To[1], To[2]);

	public Vector3 VFocus => new Vector3(Focus[0], Focus[1], Focus[2]);
}
