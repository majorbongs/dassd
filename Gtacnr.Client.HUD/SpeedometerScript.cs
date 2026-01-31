using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.API;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.HUD;

public class SpeedometerScript : Script
{
	private SpeedMeasurementUnit measureUnit;

	private string measureUnitStr;

	private float currentSpeed;

	private Utils.MinimapInfo minimapInfo;

	private Vector2 textPosition;

	private bool drawThisFrame;

	public static bool IsSpeedometerEnabled { get; set; }

	protected override void OnStarted()
	{
		IsSpeedometerEnabled = Preferences.SpeedometerEnabled.Get();
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(500);
		bool flag = drawThisFrame;
		drawThisFrame = true;
		if (!IsSpeedometerEnabled || !Game.PlayerPed.IsInVehicle() || (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed || !HideHUDScript.EnableHUD || Game.IsPaused || API.IsRadarHidden())
		{
			drawThisFrame = false;
		}
		if (drawThisFrame && !flag)
		{
			await CalculateTask();
			base.Update += CalculateTask;
			base.Update += DrawTask;
		}
		else if (!drawThisFrame && flag)
		{
			base.Update -= CalculateTask;
			base.Update -= DrawTask;
		}
	}

	private async Coroutine CalculateTask()
	{
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if (!((Entity)(object)currentVehicle == (Entity)null))
		{
			VehicleClass classType = currentVehicle.ClassType;
			measureUnit = ((API.GetProfileSetting(227) == 1) ? SpeedMeasurementUnit.Kmh : SpeedMeasurementUnit.Mph);
			if (((int)classType == 14 || (int)classType == 16 || (int)classType == 15) && measureUnit == SpeedMeasurementUnit.Mph)
			{
				measureUnit = SpeedMeasurementUnit.Kts;
			}
			switch (measureUnit)
			{
			case SpeedMeasurementUnit.Mph:
				currentSpeed = currentVehicle.Speed.ToMph();
				break;
			case SpeedMeasurementUnit.Kmh:
				currentSpeed = currentVehicle.Speed.ToKmh();
				break;
			case SpeedMeasurementUnit.Kts:
				currentSpeed = Game.PlayerPed.CurrentVehicle.Speed.ToKts();
				break;
			}
			measureUnitStr = Gtacnr.Utils.GetDescription(measureUnit);
			minimapInfo = Utils.GetMinimapAnchor();
			textPosition.X = minimapInfo.Right - 0.006f;
			textPosition.Y = minimapInfo.Bottom - 0.048f;
			await Script.Wait(50);
		}
	}

	private async Coroutine DrawTask()
	{
		Utils.Draw2DText($"{currentSpeed:0}{measureUnitStr}", textPosition, new Color(220, 220, 220, 220), 0.43f, 4, (Alignment)2, drawOutline: false, new Color(0, 0, 0, byte.MaxValue), 5);
	}
}
