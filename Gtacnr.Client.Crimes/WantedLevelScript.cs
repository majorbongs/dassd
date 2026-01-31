using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;

namespace Gtacnr.Client.Crimes;

public class WantedLevelScript : Script
{
	private DateTime startLosingStarsT;

	protected override void OnStarted()
	{
		LoadWantedLevel();
	}

	private async void LoadWantedLevel()
	{
		SetWantedLevel(await Crime.GetWantedLevel());
	}

	private void SetWantedLevel(int level)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setWantedLevel", new object[1] { level });
		BaseScript.TriggerEvent("gtacnr:hud:toggleWantedLevel", new object[1] { level > 0 });
	}

	[EventHandler("gtacnr:crimes:wantedLevelChanged")]
	private void OnWantedLevelChanged(int oldLevel, int newLevel)
	{
		SetWantedLevel(newLevel);
		if (newLevel < oldLevel)
		{
			startLosingStarsT = DateTime.UtcNow;
			BaseScript.TriggerEvent("gtacnr:hud:setFlashingSpeed", new object[1] { 1.0 });
		}
	}

	[EventHandler("onClientResourceStart")]
	private void OnClientResourceStart(string resourceName)
	{
		if (resourceName == "gtacnr_ui" && SpawnScript.HasSpawned)
		{
			LoadWantedLevel();
		}
	}

	[EventHandler("gtacnr:crimes:startLosingStars")]
	private void OnStartLosingStars(int wantedLevel)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setIsFlashing", new object[1] { true });
		BaseScript.TriggerEvent("gtacnr:hud:setFlashingSpeed", new object[1] { 1.0 });
		startLosingStarsT = DateTime.UtcNow;
	}

	[EventHandler("gtacnr:crimes:stopLosingStars")]
	private void OnStopLosingStars(int wantedLevel)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setIsFlashing", new object[1] { false });
	}

	[Update]
	private async Coroutine SetStarFlashingSpeedTask()
	{
		await Script.Wait(1000);
		int num = Crime.CachedWantedLevel ?? (await Crime.GetWantedLevel());
		int num2 = num;
		if (num2 > 0)
		{
			int num3 = Constants.Crime.TIMER[num2 - 1];
			if (num3 > 0)
			{
				double num4 = ((DateTime.UtcNow - startLosingStarsT).TotalSeconds / (double)num3).ConvertRange(0.0, 1.0, 0.5, 2.5);
				BaseScript.TriggerEvent("gtacnr:hud:setFlashingSpeed", new object[1] { num4 });
			}
		}
		else
		{
			BaseScript.TriggerEvent("gtacnr:hud:setIsFlashing", new object[1] { false });
		}
	}
}
