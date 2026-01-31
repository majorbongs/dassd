using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Newtonsoft.Json;

namespace Gtacnr.Client.Items;

public class FlashBangScript : Script
{
	private readonly uint FLASHBANG_MODEL = (uint)Game.GenerateHash("w_ex_flashbang");

	private int flashCount;

	private DateTime flashBangTimestamp;

	public static bool BlackoutMode { get; set; }

	protected override void OnStarted()
	{
		BlackoutMode = Preferences.FlashbangBlackoutMode.Get();
	}

	[Update]
	private async Coroutine CheckTask()
	{
		if (flashBangTimestamp != default(DateTime) && Gtacnr.Utils.CheckTimePassed(flashBangTimestamp, 10000.0))
		{
			flashBangTimestamp = default(DateTime);
			Utils.Unblur();
		}
		bool num = (int)Game.Player.Character.Weapons.Current.Hash == Game.GenerateHash("WEAPON_FLASHBANG");
		bool isShooting = Game.Player.Character.IsShooting;
		int fbHandle;
		if (num && isShooting)
		{
			await Script.Wait(100);
			Vector3 position = ((Entity)Game.Player.Character).Position;
			fbHandle = API.GetClosestObjectOfType(position.X, position.Y, position.Z, 50f, FLASHBANG_MODEL, false, false, false);
			if (fbHandle != 0)
			{
				Trigger();
			}
		}
		async void Trigger()
		{
			Prop flashbang = (Prop)Entity.FromHandle(fbHandle);
			API.SetEntityAsMissionEntity(((PoolObject)flashbang).Handle, true, true);
			API.NetworkRegisterEntityAsNetworked(((PoolObject)flashbang).Handle);
			int t = 0;
			while (!API.NetworkGetEntityIsNetworked(((PoolObject)flashbang).Handle))
			{
				t++;
				if (t > 50)
				{
					return;
				}
				await BaseScript.Delay(100);
			}
			await BaseScript.Delay(2000);
			World.AddExplosion(((Entity)flashbang).Position, (ExplosionType)25, 0.3f, 1f, (Ped)null, true, false);
			BaseScript.TriggerServerEvent("gtacnr:flashbang:thrown", new object[4]
			{
				((Entity)flashbang).Position.X,
				((Entity)flashbang).Position.Y,
				((Entity)flashbang).Position.Z,
				((Entity)flashbang).NetworkId
			});
		}
	}

	[EventHandler("gtacnr:flashbang:exploded")]
	private async void OnFlashbangExploded(float x, float y, float z, int propNetId)
	{
		Ped ped = Game.PlayerPed;
		Vector3 fbPos = new Vector3(x, y, z);
		Vector3 position = ((Entity)ped).Position;
		if (((Vector3)(ref position)).DistanceToSquared(fbPos) > 3600f || !API.NetworkDoesEntityExistWithNetworkId(propNetId))
		{
			return;
		}
		Vector3 pedPos = API.GetPedBoneCoords(((PoolObject)ped).Handle, 31086, 0f, 0f, 0f);
		Entity flashbang = Entity.FromNetworkId(propNetId);
		int fbHandle = ((PoolObject)flashbang).Handle;
		Vector3 distVector = new Vector3(pedPos.X - x, pedPos.Y - y, pedPos.Z - z);
		API.RequestNamedPtfxAsset("core");
		while (!API.HasNamedPtfxAssetLoaded("core"))
		{
			await BaseScript.Delay(0);
		}
		API.UseParticleFxAssetNextCall("core");
		API.StartParticleFxLoopedAtCoord("ent_anim_paparazzi_flash", fbPos.X, fbPos.Y, fbPos.Z + 0.25f, 0f, 0f, 0f, 30f, false, false, false, false);
		int num = 5;
		float radius = 15f;
		float distance = World.GetDistance(((Entity)ped).Position, fbPos);
		float faceDistance = World.GetDistance(pedPos, fbPos);
		float num2 = distance * distance;
		float num3 = 0.015f / (radius / 8f);
		int actualStunTime = (((float)num - (float)num * num3 * num2) * 1000f).ToInt();
		if (actualStunTime < 1)
		{
			actualStunTime = 1;
		}
		bool hit = false;
		int entityHit = 0;
		int result = 1;
		Vector3 hitPos = default(Vector3);
		Vector3 surfaceNormal = default(Vector3);
		int lostTest = API.StartShapeTestLosProbe(x, y, z, pedPos.X + 10f * distVector.X, pedPos.Y + 10f * distVector.Y, pedPos.Z + 10f * distVector.Z, 159, fbHandle, 4);
		int num4;
		while (true)
		{
			switch (result)
			{
			case 1:
				result = API.GetShapeTestResult(lostTest, ref hit, ref hitPos, ref surfaceNormal, ref entityHit);
				await BaseScript.Delay(0);
				continue;
			case 2:
				num4 = ((entityHit == ((PoolObject)ped).Handle) ? 1 : 0);
				break;
			default:
				num4 = 0;
				break;
			}
			break;
		}
		bool flag = (byte)num4 != 0;
		((PoolObject)flashbang).Delete();
		if (faceDistance <= radius && flag)
		{
			Flash(actualStunTime);
		}
	}

	private async void Flash(int time)
	{
		flashCount++;
		int thisFlash = flashCount;
		flashBangTimestamp = DateTime.UtcNow;
		Utils.Blur(1);
		AudioScript.PlayAudio("tinnitus.wav", 0.35f);
		API.AnimpostfxPlay("BeastTransition", 0, true);
		Utils.ShakeGamepad();
		DateTime nuiT = DateTime.Now;
		DateTime flashT = DateTime.Now;
		float flashIntensity = 1f;
		int flashDelay = ((double)time * 0.3).ToInt();
		SetFlashIntensity(flashIntensity);
		while (!Gtacnr.Utils.CheckTimePassed(flashT, time))
		{
			await BaseScript.Delay(0);
			if (flashCount > thisFlash)
			{
				return;
			}
			API.SetAudioSpecialEffectMode(1);
			if (Gtacnr.Utils.CheckTimePassed(nuiT, flashDelay))
			{
				flashDelay = 100;
				flashIntensity -= 0.035f;
				nuiT = DateTime.Now;
				SetFlashIntensity(flashIntensity);
			}
		}
		SetFlashIntensity(0f);
		Utils.Unblur(250);
		API.AnimpostfxStop("BeastTransition");
		AudioScript.StopAudio();
		static void SetFlashIntensity(float intensity)
		{
			API.SendNuiMessage(JsonConvert.SerializeObject(new
			{
				method = "setFlashBang",
				intensity = intensity,
				blackoutMode = BlackoutMode
			}));
		}
	}
}
