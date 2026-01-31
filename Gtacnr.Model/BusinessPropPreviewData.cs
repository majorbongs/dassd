using CitizenFX.Core;

namespace Gtacnr.Model;

public class BusinessPropPreviewData
{
	public float[] ObjectPos_ { get; set; }

	public float[] CameraPos_ { get; set; }

	public Vector3 ObjectPos => new Vector3(ObjectPos_[0], ObjectPos_[1], ObjectPos_[2]);

	public Vector3 CameraPos => new Vector3(CameraPos_[0], CameraPos_[1], CameraPos_[2]);

	public float RotationSpeed { get; set; } = 0.2f;

	public float ObjectDistance { get; set; } = 1f;

	public float CameraDistance { get; set; } = 1.5f;

	public float ZOffset { get; set; }
}
