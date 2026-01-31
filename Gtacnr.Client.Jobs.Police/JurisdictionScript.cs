using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.HUD;
using Gtacnr.Localization;
using NativeUI;

namespace Gtacnr.Client.Jobs.Police;

public class JurisdictionScript : Script
{
	private static Vector2 CayoLimits = new Vector2(3600f, -4000f);

	public static bool IsPointOutOfJurisdiction(Vector3 coords)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (coords.X > CayoLimits.X)
		{
			return coords.Y < CayoLimits.Y;
		}
		return false;
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(1000);
		if ((Entity)(object)Game.PlayerPed == (Entity)null || !Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (!IsPointOutOfJurisdiction(position))
		{
			return;
		}
		int secondsLeft = 30;
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_LEAVING_US_OFFDUTY, secondsLeft));
		TextTimerBar timerBar = new TextTimerBar(LocalizationController.S(Entries.Jobs.POLICE_OFF_DUTY_TIMER), Gtacnr.Utils.SecondsToMinutesAndSeconds(secondsLeft))
		{
			TextColor = Colors.GTARed
		};
		TimerBarScript.AddTimerBar(timerBar);
		while (secondsLeft > 0)
		{
			await Script.Wait(1000);
			secondsLeft--;
			timerBar.Text = Gtacnr.Utils.SecondsToMinutesAndSeconds(secondsLeft);
			position = ((Entity)Game.PlayerPed).Position;
			if (position.X < CayoLimits.X || position.Y > CayoLimits.Y)
			{
				TimerBarScript.RemoveTimerBar(timerBar);
				Utils.DisplayHelpText();
				return;
			}
		}
		TimerBarScript.RemoveTimerBar(timerBar);
		Utils.DisplayHelpText();
		await Gtacnr.Client.API.Jobs.TrySwitch("none", "default", null, BeforeSwitching, AfterSwitching);
		static async Task AfterSwitching()
		{
			await Utils.FadeIn(250);
		}
		static async Task BeforeSwitching()
		{
			await Utils.FadeOut(250);
		}
	}
}
