using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.API;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.HUD;

public class AltimeterScript : Script
{
	private float currentAltitude;

	private bool drawThisFrame;

	private DistanceMeasurementUnit measureUnit;

	private string measureUnitStr = "";

	public static bool IsAltimeterEnabled { get; set; }

	protected override void OnStarted()
	{
		IsAltimeterEnabled = Preferences.AltimeterEnabled.Get();
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		drawThisFrame = false;
		if (!IsAltimeterEnabled || !Game.PlayerPed.IsInVehicle() || (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed || !HideHUDScript.EnableHUD || Game.IsPaused || API.IsRadarHidden())
		{
			return;
		}
		VehicleClass classType = Game.PlayerPed.CurrentVehicle.ClassType;
		if ((int)classType == 16 || (int)classType == 15)
		{
			measureUnit = ((API.GetProfileSetting(227) == 1) ? DistanceMeasurementUnit.Meters : DistanceMeasurementUnit.Feet);
			measureUnitStr = Gtacnr.Utils.GetDescription(measureUnit);
			currentAltitude = ((Entity)Game.PlayerPed.CurrentVehicle).Position.Z;
			if (measureUnit == DistanceMeasurementUnit.Feet)
			{
				currentAltitude = currentAltitude.ToFeet();
			}
			drawThisFrame = true;
			await Script.Wait(50);
		}
	}

	[Update]
	private async Coroutine DrawTask()
	{
		if (drawThisFrame)
		{
			Utils.MinimapInfo minimapAnchor = Utils.GetMinimapAnchor();
			Vector2 position = default(Vector2);
			((Vector2)(ref position))._002Ector(minimapAnchor.Left + 0.006f, minimapAnchor.Bottom - 0.048f);
			Utils.Draw2DText($"{currentAltitude:0}{measureUnitStr}", position, new Color(220, 220, 220, 220), 0.43f, 4, (Alignment)1, drawOutline: false, new Color(0, 0, 0, byte.MaxValue), 5);
		}
	}
}
