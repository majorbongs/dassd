using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class DrugLab
{
	public string Id { get; set; }

	public string Name { get; set; }

	public DrugLabType Type { get; set; }

	public int RequiredLevel { get; set; }

	public float[] Position_ { get; set; }

	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	public List<float[]> WorkPositions_ { get; set; }

	public List<Vector4> WorkPositions => ((IEnumerable<float[]>)WorkPositions_).Select((Func<float[], Vector4>)((float[] wp) => new Vector4(wp[0], wp[1], wp[2], wp[3]))).ToList();

	public float[] ComputerPosition_ { get; set; }

	public Vector4 ComputerPosition => new Vector4(ComputerPosition_[0], ComputerPosition_[1], ComputerPosition_[2], ComputerPosition_[3]);
}
