using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Jobs.DrugDealer;

public class CornerScript : Script
{
	private readonly TimeSpan SNITCH_COOLDOWN = TimeSpan.FromMinutes(30.0);

	private readonly TimeSpan SPAWN_RATE = TimeSpan.FromSeconds(40.0);

	private readonly float CIRCLE_RADIUS = 5f;

	private readonly string keyboardControlStr = "INPUT_MP_TEXT_CHAT_TEAM";

	private readonly string gamepadControlStr = "INPUT_REPLAY_SCREENSHOT";

	private List<DrugCorner> drugCorners = Gtacnr.Utils.LoadJson<List<DrugCorner>>("data/drugDealer/corners.json");

	private List<Blip> cornerBlips = new List<Blip>();

	private HashSet<Model> invalidSkins = new HashSet<Model>
	{
		Model.op_Implicit("s_m_y_cop_01"),
		Model.op_Implicit("s_f_y_cop_01"),
		Model.op_Implicit("s_m_y_hwaycop_01"),
		Model.op_Implicit("s_m_y_sheriff_01"),
		Model.op_Implicit("s_f_y_sheriff_01"),
		Model.op_Implicit("s_m_y_ranger_01"),
		Model.op_Implicit("s_f_y_ranger_01"),
		Model.op_Implicit("s_m_m_security_01"),
		Model.op_Implicit("s_m_m_ciasec_01"),
		Model.op_Implicit("s_m_y_swat_01"),
		Model.op_Implicit("a_f_y_topless_01"),
		Model.op_Implicit("a_m_y_musclbeac_01"),
		Model.op_Implicit("a_m_m_acult_01"),
		Model.op_Implicit("a_m_o_acult_01"),
		Model.op_Implicit("a_m_y_acult_01"),
		Model.op_Implicit("a_f_m_bodybuild_01"),
		Model.op_Implicit("a_m_m_tranvest_01"),
		Model.op_Implicit("a_m_m_tranvest_02")
	};

	private List<Model> randomSkins = new List<Model>
	{
		Model.op_Implicit("a_f_m_fatbla_01"),
		Model.op_Implicit("a_f_m_eastsa_01"),
		Model.op_Implicit("a_f_m_eastsa_02"),
		Model.op_Implicit("a_f_m_fatwhite_01"),
		Model.op_Implicit("a_f_m_salton_01"),
		Model.op_Implicit("a_f_m_soucent_02"),
		Model.op_Implicit("a_f_m_soucentmc_01"),
		Model.op_Implicit("a_f_o_salton_01"),
		Model.op_Implicit("a_f_y_eastsa_03"),
		Model.op_Implicit("a_f_y_rurmeth_01"),
		Model.op_Implicit("a_f_y_skater_01"),
		Model.op_Implicit("a_f_y_soucent_03"),
		Model.op_Implicit("a_m_m_afriamer_01"),
		Model.op_Implicit("a_m_m_eastsa_01"),
		Model.op_Implicit("a_m_m_genfat_02"),
		Model.op_Implicit("a_m_m_hillbilly_02"),
		Model.op_Implicit("a_m_m_mexcntry_01"),
		Model.op_Implicit("a_m_m_mexlabor_01"),
		Model.op_Implicit("a_m_m_polynesian_01"),
		Model.op_Implicit("a_m_m_salton_01"),
		Model.op_Implicit("a_m_m_salton_03"),
		Model.op_Implicit("a_m_m_skater_01"),
		Model.op_Implicit("a_m_m_skidrow_01"),
		Model.op_Implicit("a_m_m_stlat_02"),
		Model.op_Implicit("a_m_m_tramp_01"),
		Model.op_Implicit("a_m_m_trampbeac_01"),
		Model.op_Implicit("a_m_y_eastsa_01"),
		Model.op_Implicit("a_m_y_gay_01"),
		Model.op_Implicit("a_m_y_hipster_01"),
		Model.op_Implicit("a_m_y_latino_01"),
		Model.op_Implicit("a_m_y_runner_02"),
		Model.op_Implicit("a_m_y_skater_01"),
		Model.op_Implicit("a_m_y_skater_02"),
		Model.op_Implicit("a_m_y_soucent_02"),
		Model.op_Implicit("a_m_y_soucent_03"),
		Model.op_Implicit("g_f_y_ballas_01"),
		Model.op_Implicit("g_f_y_families_01"),
		Model.op_Implicit("g_f_y_vagos_01")
	};

	private bool areDrugCornerTasksAttached;

	private bool isDrugActionInProgress;

	private bool isDrugDealer;

	private DateTime lastDrugActionTimestamp;

	private DrugCorner closestCorner;

	private DrugCorner currentCorner;

	private Menu offerMenu;

	private DateTime weapMsgT;

	public CornerScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	protected override void OnStarted()
	{
		CreateOfferMenus();
	}

	private void CreateOfferMenus()
	{
		offerMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_NPCOFFER_TITLE), LocalizationController.S(Entries.Businesses.MENU_NPCOFFER_SUBTITLE))
		{
			MaxDistance = 7.5f
		};
		offerMenu.OnItemSelect += OnMenuItemSelect;
		MenuController.AddMenu(offerMenu);
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		isDrugDealer = e.CurrentJobEnum == JobsEnum.DrugDealer;
		if (isDrugDealer)
		{
			if (!areDrugCornerTasksAttached)
			{
				areDrugCornerTasksAttached = true;
				base.Update += UpdateDrugCornerTask;
				base.Update += DrawDrugCornerTask;
			}
			CreateCornerBlips();
		}
		else if (!isDrugDealer && e.PreviousJobEnum == JobsEnum.DrugDealer)
		{
			if (areDrugCornerTasksAttached)
			{
				areDrugCornerTasksAttached = false;
				base.Update -= UpdateDrugCornerTask;
				base.Update -= DrawDrugCornerTask;
			}
			RemoveCornerBlips();
		}
	}

	private void CreateCornerBlips()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		foreach (DrugCorner drugCorner in drugCorners)
		{
			Blip val = World.CreateBlip(drugCorner.Position);
			val.Sprite = (BlipSprite)500;
			val.Color = (BlipColor)49;
			Utils.SetBlipName(val, "Drug Spot", "drug_spot");
			val.Scale = 0.6f;
			val.IsShortRange = true;
			cornerBlips.Add(val);
		}
	}

	private void RemoveCornerBlips()
	{
		foreach (Blip cornerBlip in cornerBlips)
		{
			((PoolObject)cornerBlip).Delete();
		}
		cornerBlips.Clear();
	}

	private void EnterDrugCorner()
	{
		Utils.DisplayHelpText("You are in a ~r~drug spot~s~. Stay here and ~y~NPCs ~s~will ask you for drugs.");
		Print("Entered drug spot `" + currentCorner.Id + "`");
	}

	private void ExitDrugCorner(bool forced)
	{
		if (forced)
		{
			Utils.DisplayHelpText($"Someone has called the ~b~cops~s~, you can't use this ~r~drug spot ~s~for the next ~p~{SNITCH_COOLDOWN.TotalMinutes} minutes~s~.");
		}
		else
		{
			Utils.DisplayHelpText("You left the ~r~drug spot~s~.");
		}
	}

	private async void DrugCornerTick()
	{
		double milliseconds = SPAWN_RATE.TotalMilliseconds / (double)currentCorner.DemandMult;
		if (Utils.IsAnActualWeapon(Game.PlayerPed.Weapons.Current.Hash))
		{
			if (Gtacnr.Utils.CheckTimePassed(weapMsgT, 20000.0))
			{
				Print($"{Game.PlayerPed.Weapons.Current.Hash}");
				Utils.DisplayHelpText("You have a weapon! Fewer ~y~NPCs ~s~will buy drugs.", playSound: false, 5000);
				weapMsgT = DateTime.UtcNow;
			}
		}
		else
		{
			weapMsgT = default(DateTime);
		}
		if (isDrugActionInProgress || !Gtacnr.Utils.CheckTimePassed(lastDrugActionTimestamp, milliseconds))
		{
			return;
		}
		try
		{
			isDrugActionInProgress = true;
			await RandomNPCGoBuyDrugs();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isDrugActionInProgress = false;
			lastDrugActionTimestamp = DateTime.UtcNow;
		}
	}

	private async Task RandomNPCGoBuyDrugs()
	{
		Vector3 playerPos = ((Entity)Game.PlayerPed).Position;
		if (IsShockingEventNearby(90) || IsShockingEventNearby(91))
		{
			Utils.DisplayHelpText("There's an active ~r~shooting~s~! ~y~NPCs ~s~will not buy drugs.", playSound: false, 5000);
			return;
		}
		Ped[] allPeds = World.GetAllPeds();
		Vector3 position;
		foreach (Ped val in allPeds)
		{
			position = ((Entity)val).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position) > 400f) && ((Entity)val).IsDead)
			{
				Utils.DisplayHelpText("There is a ~r~dead body ~s~nearby. ~y~NPCs ~s~will not buy drugs.", playSound: false, 5000);
				return;
			}
		}
		Vector3 val2 = default(Vector3);
		double num = 20.0 * Gtacnr.Utils.GetRandomDouble(0.6, 1.2);
		if (currentCorner.NPCSpawnPoints != null && currentCorner.NPCSpawnPoints.Count > 0)
		{
			val2 = currentCorner.NPCSpawnPoints.Random();
		}
		else if (!API.FindSpawnPointInDirection(playerPos.X + (float)(3.0 * Gtacnr.Utils.GetRandomDouble(0.2, 1.2)), playerPos.Y + (float)(3.0 * Gtacnr.Utils.GetRandomDouble(0.2, 1.2)), playerPos.Z, playerPos.X + (float)(3.0 * Gtacnr.Utils.GetRandomDouble(0.2, 1.2)), playerPos.Y + (float)(3.0 * Gtacnr.Utils.GetRandomDouble(0.2, 1.2)), playerPos.Z, (float)num, ref val2))
		{
			Print("^3Warning: ^0unable to find a suitable spawn position for the NPC.");
			return;
		}
		Ped val3 = (from p in World.GetAllPeds()
			where !p.IsPlayer && !invalidSkins.Contains(Model.op_Implicit(((Entity)p).Model.Hash)) && !API.IsEntityAMissionEntity(((PoolObject)p).Handle) && API.GetPedType(((PoolObject)p).Handle) != 28
			select p).Random();
		Ped ped;
		if ((Entity)(object)val3 != (Entity)null)
		{
			ped = new Ped(API.ClonePed(((PoolObject)val3).Handle, 1f, false, true));
			((Entity)ped).Position = val2;
		}
		else
		{
			ped = await World.CreatePed(randomSkins.Random(), val2, 0f);
		}
		await AntiEntitySpawnScript.RegisterEntity((Entity)(object)ped);
		int pedHandle = ((PoolObject)ped).Handle;
		ped.AlwaysKeepTask = true;
		ped.Task.ClearAll();
		API.TaskGoToEntity(((PoolObject)ped).Handle, ((PoolObject)Game.PlayerPed).Handle, -1, 1.5f, 1f, 0f, 0);
		DateTime startTaskDateTime = DateTime.UtcNow;
		do
		{
			await BaseScript.Delay(100);
			if (IsShockingEventNearby(90) || IsShockingEventNearby(91))
			{
				Utils.DisplayHelpText("There's an active ~r~shooting~s~! ~y~NPCs ~s~will not buy drugs.", playSound: false, 5000);
				WalkAway();
				return;
			}
			if (currentCorner == null || Gtacnr.Utils.CheckTimePassed(startTaskDateTime, 60000.0) || ((Entity)Game.PlayerPed).IsDead || CuffedScript.IsCuffed || ((Entity)ped).IsDead || ped.IsFleeing || ped.IsInCombat || ped.IsInMeleeCombat)
			{
				WalkAway();
				return;
			}
			position = ((Entity)ped).Position;
		}
		while (!(((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 2.25f));
		ped.PlayAmbientSpeech((Gtacnr.Utils.GetRandomDouble() > 0.5) ? "GENERIC_HI" : "GENERIC_HOWS_IT_GOING", (SpeechModifier)0);
		ped.Task.ChatTo(Game.PlayerPed);
		int num2 = drugCorners.IndexOf(currentCorner);
		NpcDrugOffer offer = (await TriggerServerEventAsync<string>("gtacnr:drugdealer:createNpcOffer", new object[2]
		{
			num2,
			((Entity)ped).NetworkId
		})).Unjson<NpcDrugOffer>();
		switch (offer.Response)
		{
		case NpcDrugOfferResponse.Success:
		case NpcDrugOfferResponse.InsufficientAmount:
		case NpcDrugOfferResponse.ExcessivePrice:
			await BeginSale();
			break;
		case NpcDrugOfferResponse.TooManyPlayers:
			Utils.DisplayHelpText("There are too many ~r~drug dealers ~s~in the drug spot.", playSound: false, 5000);
			WalkAway();
			break;
		case NpcDrugOfferResponse.SnitchCooldown:
			Utils.DisplayHelpText("You have been ~r~caught ~s~selling in this spot. Please find another ~y~spot~s~.", playSound: false, 5000);
			WalkAway();
			break;
		default:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x34-{(int)offer.Response}"));
			WalkAway();
			break;
		}
		async Task BeginSale()
		{
			InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(offer.DrugId);
			string text = (Utils.IsUsingKeyboard() ? ("Press ~" + keyboardControlStr + "~") : ("Hold ~" + gamepadControlStr + "~"));
			string arg = ((offer.Price > 0) ? (" " + text + " to offer it for ~g~" + offer.Price.ToCurrencyString() + ".") : "");
			bool responded = false;
			bool walkAway = true;
			await InteractiveNotificationsScript.Show($"The ~y~NPC ~s~wants ~p~{offer.Amount:0.##}g ~s~of ~r~{itemInfo.Name}~s~.{arg}", InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Offer", "Offer (hold)");
			if (!responded)
			{
				Utils.DisplayHelpText("The ~y~NPC ~s~is no longer interested.", playSound: false);
			}
			if (walkAway)
			{
				WalkAway();
			}
			async void ModifyOffer()
			{
				int num3 = ((offer.Response == NpcDrugOfferResponse.InsufficientAmount) ? 1 : ((offer.Response == NpcDrugOfferResponse.ExcessivePrice) ? 2 : 0));
				NpcDrugOffer offer3 = (await TriggerServerEventAsync<string>("gtacnr:drugdealer:sellToNpc", new object[1] { num3 })).Unjson<NpcDrugOffer>();
				switch (offer3.Response)
				{
				case NpcDrugOfferResponse.Success:
				case NpcDrugOfferResponse.UndercoverCop:
				{
					Utils.DisplayHelpText();
					DrugCorner drugCorner = currentCorner;
					offer = offer3;
					itemInfo = Gtacnr.Data.Items.GetItemDefinition(offer3.DrugId);
					await PlayDealAnimation();
					if (offer3.Response == NpcDrugOfferResponse.UndercoverCop)
					{
						await BaseScript.Delay(2000);
						drugCorner.LastSnitchTimestamp = DateTime.UtcNow;
					}
					break;
				}
				case NpcDrugOfferResponse.SecondOfferDeclined:
					ped.PlayAmbientSpeech("GENERIC_INSULT_HIGH", (SpeechModifier)3);
					Utils.DisplayHelpText("The ~y~NPC ~s~is not interested in your counteroffer.", playSound: false);
					WalkAway();
					break;
				case NpcDrugOfferResponse.Expired:
					Utils.PlayErrorSound();
					break;
				default:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x35-{(int)offer3.Response}"));
					break;
				}
			}
			bool OnAccepted()
			{
				bool responded2;
				if (offer.Response == NpcDrugOfferResponse.Success)
				{
					Utils.DisplayHelpText();
					Sell();
				}
				else if (offer.Response == NpcDrugOfferResponse.InsufficientAmount || offer.Response == NpcDrugOfferResponse.ExcessivePrice)
				{
					responded = true;
					walkAway = false;
					ped.PlayAmbientSpeech("GENERIC_CURSE_MED", (SpeechModifier)3);
					responded2 = false;
					AskModifyOffer();
				}
				return true;
				async void AskModifyOffer()
				{
					await BaseScript.Delay(100);
					await InteractiveNotificationsScript.Show((offer.Response == NpcDrugOfferResponse.InsufficientAmount) ? ("You do not have enough ~r~" + itemInfo.Name + "~s~. Do you want to offer a ~y~different ~s~drug?") : ((offer.Response == NpcDrugOfferResponse.ExcessivePrice) ? "The ~y~NPC ~s~said your price is ~r~too high~s~. Do you want to ~r~reduce ~s~your offer?" : ""), InteractiveNotificationType.HelpText, OnModifyOffer, TimeSpan.FromSeconds(10.0), 0u, "Counteroffer", "Counteroffer (hold)");
					if (!responded2)
					{
						Utils.DisplayHelpText("The ~y~NPC ~s~is no longer interested.", playSound: false);
					}
					WalkAway();
				}
				bool OnModifyOffer()
				{
					responded2 = true;
					ModifyOffer();
					return true;
				}
			}
			async Task PlayDealAnimation()
			{
				API.TaskTurnPedToFaceEntity(API.PlayerPedId(), ((PoolObject)ped).Handle, 1000);
				await BaseScript.Delay(1000);
				string dict = "mp_ped_interaction";
				API.RequestAnimDict(dict);
				while (!API.HasAnimDictLoaded(dict))
				{
					await BaseScript.Delay(0);
				}
				ped.Task.ClearAll();
				ped.AlwaysKeepTask = true;
				ped.Task.PlayAnimation(dict, "handshake_guy_b", 4f, 3500, (AnimationFlags)51);
				Game.PlayerPed.Task.PlayAnimation(dict, "handshake_guy_a", 4f, 3500, (AnimationFlags)51);
				await BaseScript.Delay(500);
				Vector3 position2 = ((Entity)Game.PlayerPed).Position;
				Tuple<int, string, float, float, float, float, float, Tuple<float>> propInfo = ((offer.Price >= 100000) ? Tuple.Create(57005, "prop_anim_cash_pile_02", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f) : ((offer.Price >= 50000) ? Tuple.Create(57005, "prop_anim_cash_pile_01", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f) : ((offer.Price >= 10000) ? Tuple.Create(57005, "xs_prop_arena_cash_pile_s", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f) : Tuple.Create(57005, "prop_cash_note_01", 0.13f, 0.028f, -0.01f, 21.24f, -1.54f, 0f))));
				int propDrug = API.CreateObject(API.GetHashKey(itemInfo.Model), position2.X, position2.Y, position2.Z + 0.2f, true, true, true);
				int propCash = API.CreateObject(API.GetHashKey(propInfo.Item2), position2.X, position2.Y, position2.Z + 0.2f, true, true, true);
				int playerBoneIndex = API.GetPedBoneIndex(API.PlayerPedId(), 57005);
				int pedBoneIndex = API.GetPedBoneIndex(((PoolObject)ped).Handle, 57005);
				API.AttachEntityToEntity(propDrug, API.PlayerPedId(), playerBoneIndex, 0.145f, 0f, -0.075f, 312.3f, -0.5f, 0f, true, true, false, true, 1, true);
				API.AttachEntityToEntity(propCash, ((PoolObject)ped).Handle, pedBoneIndex, propInfo.Item3, propInfo.Item4, propInfo.Item5, propInfo.Item6, propInfo.Item7, propInfo.Rest.Item1, true, true, false, true, 1, true);
				await BaseScript.Delay(1000);
				Game.PlaySound("ROBBERY_MONEY_TOTAL", "HUD_FRONTEND_CUSTOM_SOUNDSET");
				Utils.SendNotification($"You sold ~p~{offer.Amount}g ~s~of ~r~{itemInfo.Name} ~s~to an NPC for ~g~{offer.Price.ToCurrencyString()}~s~.");
				API.AttachEntityToEntity(propDrug, ((PoolObject)ped).Handle, pedBoneIndex, 0.145f, 0f, -0.075f, 312.3f, -0.5f, 0f, true, true, false, true, 1, true);
				API.AttachEntityToEntity(propCash, API.PlayerPedId(), playerBoneIndex, propInfo.Item3, propInfo.Item4, propInfo.Item5, propInfo.Item6, propInfo.Item7, propInfo.Rest.Item1, true, true, false, true, 1, true);
				await BaseScript.Delay(2000);
				API.ClearPedTasks(API.PlayerPedId());
				API.ClearPedTasks(((PoolObject)ped).Handle);
				API.DeleteEntity(ref propDrug);
				API.DeleteEntity(ref propCash);
				ped.PlayAmbientSpeech("GENERIC_THANKS", (SpeechModifier)3);
				await BaseScript.Delay(500);
				WalkAway();
			}
			async void Sell()
			{
				if (currentCorner == null)
				{
					Utils.PlayErrorSound();
				}
				else
				{
					NpcDrugOffer offer2 = (await TriggerServerEventAsync<string>("gtacnr:drugdealer:sellToNpc", new object[1] { 0 })).Unjson<NpcDrugOffer>();
					switch (offer2.Response)
					{
					case NpcDrugOfferResponse.Success:
					case NpcDrugOfferResponse.UndercoverCop:
					{
						responded = true;
						DrugCorner drugCorner = currentCorner;
						await PlayDealAnimation();
						if (offer2.Response == NpcDrugOfferResponse.UndercoverCop)
						{
							await BaseScript.Delay(2000);
							drugCorner.LastSnitchTimestamp = DateTime.UtcNow;
						}
						break;
					}
					case NpcDrugOfferResponse.Expired:
						Utils.PlayErrorSound();
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x35-{(int)offer2.Response}"));
						break;
					}
				}
			}
		}
		bool IsShockingEventNearby(int eventId)
		{
			return API.IsShockingEventInSphere(eventId, playerPos.X, playerPos.Y, playerPos.Z, 50f);
		}
		async void WalkAway()
		{
			ped.Task.ClearAll();
			ped.Task.WanderAround();
			ped.AlwaysKeepTask = false;
			API.SetEntityAsNoLongerNeeded(ref pedHandle);
			await BaseScript.Delay(20000);
			if (API.DoesEntityExist(((PoolObject)ped).Handle))
			{
				pedHandle = ((PoolObject)ped).Handle;
				API.DeleteEntity(ref pedHandle);
			}
		}
	}

	private async Coroutine UpdateDrugCornerTask()
	{
		await Script.Wait(1000);
		float num = 10000f;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		DrugCorner drugCorner = currentCorner;
		_ = closestCorner;
		closestCorner = null;
		currentCorner = null;
		foreach (DrugCorner drugCorner2 in drugCorners)
		{
			if (DateTime.UtcNow - drugCorner2.LastSnitchTimestamp < SNITCH_COOLDOWN)
			{
				continue;
			}
			Vector3 position2 = drugCorner2.Position;
			float num2 = ((Vector3)(ref position2)).DistanceToSquared(position);
			if (num2 < num)
			{
				closestCorner = drugCorner2;
				if (num2 < CIRCLE_RADIUS * CIRCLE_RADIUS)
				{
					currentCorner = drugCorner2;
				}
			}
		}
		if (drugCorner != currentCorner)
		{
			if (currentCorner != null)
			{
				EnterDrugCorner();
			}
			else
			{
				bool forced = DateTime.UtcNow - drugCorner.LastSnitchTimestamp < SNITCH_COOLDOWN;
				ExitDrugCorner(forced);
			}
		}
		if (currentCorner != null)
		{
			DrugCornerTick();
		}
	}

	private async Coroutine DrawDrugCornerTask()
	{
		if (closestCorner != null && closestCorner != currentCorner && !(DateTime.UtcNow - closestCorner.LastSnitchTimestamp < SNITCH_COOLDOWN))
		{
			Vector3 position = closestCorner.Position;
			Vector3 val = default(Vector3);
			((Vector3)(ref val))._002Ector(CIRCLE_RADIUS, CIRCLE_RADIUS, 0.4f);
			Color color = Color.FromInt(1790108288);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(position.X, position.Y, position.Z, ref z, false))
			{
				position.Z = z;
			}
			API.DrawMarker(1, position.X, position.Y, position.Z, 0f, 0f, 0f, 0f, 0f, 0f, val.X, val.Y, val.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
	}
}
