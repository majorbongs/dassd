using CitizenFX.Core;

namespace Gtacnr.Model;

public class BusinessClothingPreviewData
{
	public float[] PedPos_ { get; set; }

	public float[] CameraPos_ { get; set; }

	public Vector4 PedPos => new Vector4(PedPos_[0], PedPos_[1], PedPos_[2], PedPos_[3]);

	public Vector3 CameraPos => new Vector3(CameraPos_[0], CameraPos_[1], CameraPos_[2]);
}
