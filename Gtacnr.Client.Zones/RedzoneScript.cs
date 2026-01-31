using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Zones;

public class RedzoneScript : Script
{
	private static readonly List<Redzone> staticRedzones = Gtacnr.Utils.LoadJson<List<Redzone>>("data/staticRedzones.json");

	private static Redzone? activeBonusRedzone;

	private static Blip? activeBonusRedzoneBlipArea;

	private static Blip? activeBonusRedzoneBlipSprite;

	private static bool isInRedzone;

	private static bool redzoneChanged;

	private List<Redzone> redzonesToDraw = new List<Redzone>();

	private bool _drawRedzones;

	private static IEnumerable<Redzone> allRedzones => staticRedzones.UnionNotNull(new Redzone[1] { activeBonusRedzone });

	public static Redzone? ActiveBonusRedzone => activeBonusRedzone;

	private bool DrawRedzones
	{
		get
		{
			return _drawRedzones;
		}
		set
		{
			if (_drawRedzones != value)
			{
				if (value)
				{
					base.Update += DrawTask;
				}
				else
				{
					base.Update -= DrawTask;
				}
				_drawRedzones = value;
			}
		}
	}

	public RedzoneScript()
	{
		if (MainScript.HardcoreMode)
		{
			List<Redzone> collection = Gtacnr.Utils.LoadJson<List<Redzone>>("data/hardcore/staticRedzones.json");
			staticRedzones.AddRange(collection);
		}
		foreach (Redzone staticRedzone in staticRedzones)
		{
			CreateRedzoneBlip(staticRedzone);
		}
	}

	protected override async void OnStarted()
	{
		string text = await TriggerServerEventAsync<string>("gtacnr:redzones:getActive", new object[0]);
		if (!string.IsNullOrEmpty(text))
		{
			BaseScript.TriggerEvent("gtacnr:redzones:activeRedzoneChanged", new object[1] { text });
		}
		Chat.AddSuggestion("/redzone", "View the current bonus redzone.");
		Chat.AddSuggestion("/redzone-vote", "Vote for the next bonus redzone.", new ChatParamSuggestion("index", "Index of the redzone you are voting for"));
	}

	private void SetBonusRedzoneBlip(Redzone redzone)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Blip? obj = activeBonusRedzoneBlipArea;
		if (obj != null)
		{
			((PoolObject)obj).Delete();
		}
		Blip? obj2 = activeBonusRedzoneBlipSprite;
		if (obj2 != null)
		{
			((PoolObject)obj2).Delete();
		}
		activeBonusRedzoneBlipArea = CreateRedzoneBlip(redzone);
		activeBonusRedzoneBlipSprite = World.CreateBlip(redzone.Position);
		activeBonusRedzoneBlipSprite.IsShortRange = false;
		activeBonusRedzoneBlipSprite.Sprite = (BlipSprite)276;
		activeBonusRedzoneBlipSprite.Color = (BlipColor)1;
		activeBonusRedzoneBlipSprite.Alpha = 255;
		Utils.SetBlipName(activeBonusRedzoneBlipSprite, "Bonus Redzone", "bonusRedzone");
		API.SetBlipDisplay(((PoolObject)activeBonusRedzoneBlipSprite).Handle, 3);
	}

	private Blip CreateRedzoneBlip(Redzone redzone)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		Blip obj = World.CreateBlip(redzone.Position, redzone.Radius);
		obj.IsShortRange = false;
		obj.Sprite = (BlipSprite)(-1);
		obj.Color = (BlipColor)1;
		obj.Alpha = 64;
		Utils.SetBlipName(obj, "Redzone", "redzone");
		API.SetBlipDisplay(((PoolObject)obj).Handle, 8);
		return obj;
	}

	public static bool IsPlayerInBonusRedzone()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (activeBonusRedzone == null)
		{
			return false;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		return activeBonusRedzone.IsPointInside(position);
	}

	private static Redzone? GetRedzonePlayerIsIn()
	{
		if (IsPlayerInBonusRedzone())
		{
			return activeBonusRedzone;
		}
		return staticRedzones.Where((Redzone r) => r.IsPointInside(((Entity)Game.PlayerPed).Position)).FirstOrDefault();
	}

	[Update]
	private async Coroutine GetRedzonesToDrawTask()
	{
		await Script.Wait(250);
		redzonesToDraw = allRedzones.Where(delegate(Redzone redzone)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			return ((Vector3)(ref position)).DistanceToSquared2D(redzone.Position) <= (redzone.Radius * 4f).Square();
		}).ToList();
		DrawRedzones = redzonesToDraw.Count > 0;
	}

	private async Coroutine DrawTask()
	{
		foreach (Redzone item in redzonesToDraw)
		{
			World.DrawMarker((MarkerType)1, new Vector3(item.Position.X, item.Position.Y, item.Position.Z - 30f), Vector3.Zero, Vector3.Zero, new Vector3(item.Radius * 2f, item.Radius * 2f, 60f), System.Drawing.Color.FromArgb(549126144), false, false, false, (string)null, (string)null, false);
		}
	}

	[Update]
	private async Coroutine RefreshTask()
	{
		await Script.Wait(30);
		Redzone redzonePlayerIsIn = GetRedzonePlayerIsIn();
		bool flag = isInRedzone;
		isInRedzone = redzonePlayerIsIn != null;
		if (!flag && isInRedzone)
		{
			string text = LocalizationController.S(Entries.Player.REDZONE_ENTERED);
			if (redzonePlayerIsIn == activeBonusRedzone)
			{
				text = text + " " + (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice() ? LocalizationController.S(Entries.Player.REDZONE_ENTERED_COP) : (Gtacnr.Client.API.Jobs.CachedJobEnum.IsEMSOrFD() ? LocalizationController.S(Entries.Player.REDZONE_ENTERED_EMS) : LocalizationController.S(Entries.Player.REDZONE_ENTERED_CRIM)));
			}
			Utils.DisplayHelpText(text);
		}
		else if (flag && !isInRedzone)
		{
			if (redzoneChanged)
			{
				redzoneChanged = false;
			}
			else
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.REDZONE_LEFT));
			}
		}
	}

	[EventHandler("gtacnr:redzones:activeRedzoneChanged")]
	private void OnRedzoneChanged(string jRedzone)
	{
		if (IsPlayerInBonusRedzone())
		{
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Player.REDZONE_CHANGED), 2000);
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			redzoneChanged = true;
		}
		activeBonusRedzone = jRedzone.Unjson<Redzone>();
		SetBonusRedzoneBlip(activeBonusRedzone);
		Utils.SendNotification(LocalizationController.S(Entries.Player.REDZONE_ANNOUNCEMENT, activeBonusRedzone.Description) + " " + (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice() ? LocalizationController.S(Entries.Player.REDZONE_ANNOUNCEMENT_COP) : (Gtacnr.Client.API.Jobs.CachedJobEnum.IsEMSOrFD() ? LocalizationController.S(Entries.Player.REDZONE_ANNOUNCEMENT_EMS) : LocalizationController.S(Entries.Player.REDZONE_ANNOUNCEMENT_CRIM))));
		Game.PlaySound("Event_Message_Purple", "GTAO_FM_Events_Soundset");
	}

	[Command("redzone")]
	private void RedzoneCommand()
	{
		Utils.SendNotification(LocalizationController.S(Entries.Player.REDZONE_COMMAND, activeBonusRedzone.Description));
		Utils.PlaySelectSound();
	}
}
