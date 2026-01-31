using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Businesses.MechanicShops;
using Gtacnr.Client.Businesses.PoliceStations;
using Gtacnr.Client.Characters;
using Gtacnr.Client.Crimes.Robberies.Shop;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Businesses;

public class BusinessScript : Script
{
	public const float STREAMING_DISTANCE = 40f;

	private Random random = new Random();

	private int businessPedsTaskTickCount;

	private List<Business> cachedBusinesses = new List<Business>();

	private bool wasInGreetingRange;

	private bool shopControlsEnabled;

	public static Dictionary<string, BusinessTypeMetadata> BusinessTypes { get; } = Gtacnr.Utils.LoadJson<Dictionary<string, BusinessTypeMetadata>>("data/businesses/businessTypes.json");

	public static Dictionary<string, Business> Businesses { get; private set; }

	public static bool IsReady { get; private set; }

	public static Business ClosestBusiness { get; private set; } = null;

	public static BusinessEmployee ClosestBusinessEmployee { get; private set; } = null;

	public static BusinessEmployee RobberyEmployee => ClosestBusiness?.Employees?.FirstOrDefault((BusinessEmployee e) => e.CanBeRobbed);

	public static bool IsFacingEmployee { get; private set; } = false;

	protected override void OnStarted()
	{
		LoadBusinesses();
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChanged;
	}

	private async Task LoadBusinesses()
	{
		if (Businesses != null)
		{
			UnloadBusinesses();
		}
		Businesses = new Dictionary<string, Business>();
		foreach (string item in Gtacnr.Utils.LoadJson<List<string>>("data/businesses/locations/index.json"))
		{
			foreach (Business item2 in Gtacnr.Utils.LoadJson<List<Business>>("data/businesses/locations/" + item))
			{
				if (item2.HasRequiredResource())
				{
					Businesses[item2.Id] = item2;
					if (item2.Dealership != null)
					{
						item2.Dealership.ParentBusiness = item2;
					}
					if (item2.Mechanic != null)
					{
						item2.Mechanic.ParentBusiness = item2;
					}
					if (item2.PoliceStation != null)
					{
						item2.PoliceStation.ParentBusiness = item2;
					}
					if (item2.Hospital != null)
					{
						item2.Hospital.ParentBusiness = item2;
					}
					Print($"Loaded shop ^2{item2.Name} ^0({item2.Type})");
				}
			}
		}
		IsReady = true;
	}

	private void UnloadBusinesses()
	{
		foreach (Business value in Businesses.Values)
		{
			if (value.Blip != (Blip)null)
			{
				((PoolObject)value.Blip).Delete();
			}
			foreach (BusinessEmployee employee in value.Employees)
			{
				if ((Entity)(object)employee.State?.Ped != (Entity)null)
				{
					((PoolObject)employee.State.Ped).Delete();
				}
				employee.State.ResetState();
			}
			Print("Unloaded shop ^1" + value.Name);
		}
		Businesses = null;
	}

	private void OnJobChanged(object sender, JobArgs e)
	{
		foreach (Business value in Businesses.Values)
		{
			if (value.Blip != (Blip)null)
			{
				((PoolObject)value.Blip).Delete();
				value.Blip = null;
			}
			CreateBusinessBlip(value);
		}
	}

	private void CreateBusinessBlip(Business business)
	{
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		if (business.Blip != (Blip)null)
		{
			return;
		}
		string type = business.Type.ToString();
		BusinessTypeMetadata businessTypeMetadata = BusinessTypes[type];
		if (business.Dealership != null)
		{
			type = business.Dealership.Type.ToString();
			businessTypeMetadata = DealershipScript.DealershipTypes[type];
		}
		if (business.Mechanic != null)
		{
			type = business.Mechanic.Type.ToString();
			businessTypeMetadata = MechanicShopScript.MechanicTypes[type];
		}
		if (business.PoliceStation != null)
		{
			type = business.PoliceStation.Department;
			PoliceDepartment policeDepartment = PoliceStationsScript.Departments.FirstOrDefault((PoliceDepartment d) => d.Id == type);
			if (policeDepartment == null)
			{
				return;
			}
			businessTypeMetadata = BusinessTypeMetadata.Clone(businessTypeMetadata);
			businessTypeMetadata.Name = policeDepartment.Name;
			businessTypeMetadata.Sprite = policeDepartment.BlipSprite;
			businessTypeMetadata.Color = policeDepartment.BlipColor;
		}
		bool flag = (business.ShowBlip.HasValue ? business.ShowBlip.Value : businessTypeMetadata.ShowBlip);
		if (business.BlipJobs != null)
		{
			flag = business.BlipJobs.Contains(Gtacnr.Client.API.Jobs.CachedJob);
		}
		else if (businessTypeMetadata.BlipJobs != null)
		{
			flag = businessTypeMetadata.BlipJobs.Contains(Gtacnr.Client.API.Jobs.CachedJob);
		}
		if (flag)
		{
			Blip val = World.CreateBlip((business.BlipCoords != default(Vector3)) ? business.BlipCoords : business.Location);
			val.Sprite = (BlipSprite)(business.BlipSprite.HasValue ? business.BlipSprite.Value : businessTypeMetadata.Sprite);
			val.Color = (BlipColor)(business.BlipColor.HasValue ? business.BlipColor.Value : businessTypeMetadata.Color);
			val.Scale = (business.BlipScale.HasValue ? business.BlipScale.Value : businessTypeMetadata.Scale);
			Utils.SetBlipName(val, businessTypeMetadata.Name.AddFontTags(), type);
			business.Blip = val;
			val.IsShortRange = true;
			if (business.BlipOnlyOnMainMap)
			{
				API.SetBlipDisplay(((PoolObject)val).Handle, 3);
			}
			else if (business.BlipOnlyOnMinimap)
			{
				API.SetBlipDisplay(((PoolObject)val).Handle, 5);
			}
		}
	}

	[Update]
	private async Coroutine BusinessPedsTask()
	{
		await BaseScript.Delay(500);
		if (Businesses == null)
		{
			DetachControls();
			return;
		}
		if (businessPedsTaskTickCount % 10 == 0)
		{
			foreach (Business item in Businesses.Values.Where(delegate(Business b)
			{
				//IL_0013: Unknown result type (might be due to invalid IL or missing references)
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				if (!cachedBusinesses.Contains(b))
				{
					Vector3 position2 = ((Entity)Game.PlayerPed).Position;
					return ((Vector3)(ref position2)).DistanceToSquared2D(b.Location) < 100f.Square();
				}
				return false;
			}))
			{
				cachedBusinesses.Add(item);
			}
		}
		businessPedsTaskTickCount++;
		float minDistSq = 40f.Square();
		float pedMinDistSq = 40f.Square();
		Business closestBusiness = null;
		BusinessEmployee closestBusinessEmployee = null;
		List<Business> removedBusinesses = new List<Business>();
		Vector3 position;
		foreach (Business business in cachedBusinesses)
		{
			position = ((Entity)Game.PlayerPed).Position;
			float num = ((Vector3)(ref position)).DistanceToSquared2D(business.Location);
			float num2 = Math.Abs(((Entity)Game.PlayerPed).Position.Z - business.Location.Z);
			if (num < 40f.Square() && num2 < business.MaxHeight && business.Employees.Count > 0)
			{
				string type = business.Type.ToString();
				BusinessTypeMetadata meta = BusinessTypes[type];
				if (business.Dealership != null)
				{
					type = business.Dealership.Type.ToString();
					meta = DealershipScript.DealershipTypes[type];
				}
				if (business.Mechanic != null)
				{
					type = business.Mechanic.Type.ToString();
					meta = MechanicShopScript.MechanicTypes[type];
				}
				if (business.PoliceStation != null)
				{
					type = business.PoliceStation.Department;
					PoliceDepartment policeDepartment = PoliceStationsScript.Departments.FirstOrDefault((PoliceDepartment d) => d.Id == type);
					if (policeDepartment == null)
					{
						continue;
					}
					meta = BusinessTypeMetadata.Clone(meta);
					meta.Outfits = policeDepartment.Uniforms;
				}
				if (num < minDistSq)
				{
					minDistSq = num;
					closestBusiness = business;
					closestBusinessEmployee = null;
				}
				foreach (BusinessEmployee employee2 in business.Employees)
				{
					BusinessEmployee employee = employee2;
					bool isSecurity = employee.Role == EmployeeRole.SecurityGuard;
					if (employee.State == null)
					{
						employee.State = new BusinessEmployeeState
						{
							Business = business,
							BusinessId = business.Id
						};
					}
					if ((Entity)(object)employee.State.Ped == (Entity)null)
					{
						BusinessEmployeeState state = employee.State;
						state.Ped = await Create();
						employee.State.IsActive = true;
					}
					else if (((Entity)employee.State.Ped).IsDead)
					{
						TimeSpan timeSpan = TimeSpan.FromSeconds(isSecurity ? 300 : 20);
						ShopRobberyScript.RobberyState currentRobbery = ShopRobberyScript.CurrentRobbery;
						bool flag = (currentRobbery != null && currentRobbery.Business.Id == business.Id) || business.IsBeingRobbed;
						if (employee.State.IsActive)
						{
							employee.State.IsActive = false;
							employee.State.DeadT = DateTime.UtcNow;
						}
						else if (Gtacnr.Utils.CheckTimePassed(employee.State.DeadT, timeSpan) && !flag && !employee.State.PreventRespawn)
						{
							((PoolObject)employee.State.Ped).Delete();
							BusinessEmployeeState state = employee.State;
							state.Ped = await Create();
							employee.State.ResetState();
							employee.State.IsActive = true;
						}
					}
					else if (closestBusiness == business)
					{
						position = ((Entity)Game.PlayerPed).Position;
						float num3 = ((Vector3)(ref position)).DistanceToSquared2D(((Entity)employee.State.Ped).Position);
						if (num3 < pedMinDistSq)
						{
							pedMinDistSq = num3;
							closestBusinessEmployee = employee;
						}
					}
					async Task<Ped> Create()
					{
						string modelString = employee.PedModel ?? meta.Ped;
						int combatAbility = ((!isSecurity) ? 1 : 2);
						return await CreateBusinessPed(GetModelFromString(modelString), employee.Location.XYZ(), employee.Location.W, meta.RandomPedVariation, combatAbility, meta.Outfits);
					}
				}
				continue;
			}
			foreach (BusinessEmployee employee3 in business.Employees)
			{
				if (employee3.State != null)
				{
					if ((Entity)(object)employee3.State.Ped != (Entity)null)
					{
						((PoolObject)employee3.State.Ped).Delete();
						employee3.State.Ped = null;
					}
					employee3.State = null;
				}
			}
			removedBusinesses.Add(business);
		}
		foreach (Business item2 in removedBusinesses.Where((Business b) => cachedBusinesses.Contains(b)))
		{
			cachedBusinesses.Remove(item2);
		}
		if (ClosestBusiness != closestBusiness)
		{
			wasInGreetingRange = false;
		}
		ClosestBusiness = closestBusiness;
		ClosestBusinessEmployee = closestBusinessEmployee;
		if (closestBusinessEmployee == null)
		{
			DetachControls();
			return;
		}
		BusinessEmployee businessEmployee = closestBusinessEmployee;
		IsFacingEmployee = false;
		bool flag2 = Game.PlayerPed.IsInVehicle() && closestBusiness.Type == BusinessType.Mechanic && closestBusiness.Mechanic.Type != MechanicType.ModShop;
		Vector3 val = (flag2 ? ((Entity)Game.PlayerPed.CurrentVehicle).Position : ((Entity)Game.PlayerPed).Position);
		float num4 = (flag2 ? 15f : 2.5f).Square();
		position = ((Entity)businessEmployee.State.Ped).Position;
		float num5 = ((Vector3)(ref position)).DistanceToSquared2D(val);
		bool flag3 = Math.Abs(val.Z - ((Entity)businessEmployee.State.Ped).Position.Z) <= 1f;
		IsFacingEmployee = num5 < num4 && flag3 && (API.IsPedFacingPed(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)businessEmployee.State.Ped).Handle, 90f) || flag2);
		if (businessEmployee.State.IsBeingRobbed || businessEmployee.State.IsScared || ((Entity)businessEmployee.State.Ped).IsDead)
		{
			DetachControls();
		}
		if (IsFacingEmployee && businessEmployee.HasMenu)
		{
			AttachControls();
		}
		else
		{
			DetachControls();
		}
		GreetingTask();
	}

	private async Task<Ped> CreateBusinessPed(Model pedModel, Vector3 position, float heading, bool randomizeModel, int combatAbility, List<string> outfits)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		Ped ped = null;
		try
		{
			ped = await Utils.CreateLocalPed(pedModel, position, heading);
			API.SetPedKeepTask(((PoolObject)ped).Handle, true);
			API.SetBlockingOfNonTemporaryEvents(((PoolObject)ped).Handle, true);
			API.SetEntityAsMissionEntity(((PoolObject)ped).Handle, true, true);
			API.FreezeEntityPosition(((PoolObject)ped).Handle, true);
			API.SetPedRelationshipGroupHash(((PoolObject)ped).Handle, PvpScript.EmployeesGroupHash);
			API.SetPedCombatAbility(((PoolObject)ped).Handle, combatAbility);
			if (Utils.IsFreemodePed(ped))
			{
				PrefabAppearance randomPrefabFace = Utils.GetRandomPrefabFace(Utils.GetFreemodePedSex(ped));
				Utils.ApplyAppearance(ped, randomPrefabFace);
				if (outfits != null)
				{
					new Apparel(outfits.Random()).ApplyOnPed(ped);
				}
			}
			else if (randomizeModel)
			{
				API.SetPedRandomComponentVariation(((PoolObject)ped).Handle, false);
				if (Gtacnr.Utils.GetRandomDouble() > 0.7)
				{
					API.SetPedRandomProps(((PoolObject)ped).Handle);
				}
			}
			API.NetworkFadeInEntity(((PoolObject)ped).Handle, true);
			return ped;
		}
		catch (Exception exception)
		{
			Print(exception);
			if ((Entity)(object)ped != (Entity)null)
			{
				((PoolObject)ped).Delete();
			}
			return null;
		}
	}

	public static BusinessEmployee GetBusinessEmployeeAtCoords(Vector3 pos)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (ClosestBusinessEmployee == null || (Entity)(object)ClosestBusinessEmployee.State.Ped == (Entity)null || !ClosestBusinessEmployee.State.IsActive)
		{
			return null;
		}
		if (!(((Vector3)(ref pos)).DistanceToSquared(((Entity)ClosestBusinessEmployee.State.Ped).Position) <= 4f))
		{
			return null;
		}
		return ClosestBusinessEmployee;
	}

	private void GreetingTask()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		Business business = ClosestBusiness;
		BusinessEmployee employee = ClosestBusinessEmployee;
		Ped val = employee?.State?.Ped;
		if (business == null || employee == null || (Entity)(object)val == (Entity)null)
		{
			return;
		}
		Vector3 position = employee.Position;
		bool flag = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 6f.Square();
		if (flag && !wasInGreetingRange)
		{
			if (ShouldGreet())
			{
				string text = "SHOP_GREET";
				if (!employee.HasShopDialog)
				{
					text = new List<string> { "GENERIC_HI", "GENERIC_HOWS_IT_GOING" }.Random();
				}
				if (business.Type == BusinessType.PoliceStation)
				{
					string text2 = ((Utils.GetFreemodePedSex(val) == Sex.Male) ? "s_m_y_cop_01_white_full_01" : "s_f_y_cop_01_white_full_01");
					API.PlayAmbientSpeechWithVoice(((PoolObject)val).Handle, text, text2, "SPEECH_PARAMS_FORCE", false);
				}
				else if (business.Type == BusinessType.GunStore || business.Type == BusinessType.GunStoreWithShootingRange)
				{
					if (Gtacnr.Client.API.Crime.CachedWantedLevel == 5)
					{
						string text3 = ((((Entity)val).Model == Model.op_Implicit((PedHash)(-1643617475))) ? "s_m_y_ammucity_01_white_01" : "s_m_m_ammucountry_01_white_01");
						API.PlayAmbientSpeechWithVoice(((PoolObject)val).Handle, "SHOP_PLAYER_HAS_WANTED_LEVEL", text3, "SPEECH_PARAMS_FORCE", false);
					}
					else if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice())
					{
						string text4 = ((((Entity)val).Model == Model.op_Implicit((PedHash)(-1643617475))) ? "s_m_y_ammucity_01_white_01" : "s_m_m_ammucountry_01_white_01");
						API.PlayAmbientSpeechWithVoice(((PoolObject)val).Handle, "GUNSH_GREET3", text4, "SPEECH_PARAMS_FORCE", false);
					}
					else
					{
						val.PlayAmbientSpeech(text, (SpeechModifier)3);
					}
				}
				else
				{
					val.PlayAmbientSpeech(text, (SpeechModifier)3);
				}
			}
		}
		else if (wasInGreetingRange && !flag)
		{
			if (ShouldGreet())
			{
				if (business.Type == BusinessType.PoliceStation)
				{
					string text5 = ((Utils.GetFreemodePedSex(val) == Sex.Male) ? "s_m_y_cop_01_white_full_01" : "s_f_y_cop_01_white_full_01");
					API.PlayAmbientSpeechWithVoice(((PoolObject)val).Handle, "GENERIC_BYE", text5, "SPEECH_PARAMS_FORCE", false);
				}
				else
				{
					val.PlayAmbientSpeech(employee.HasShopDialog ? "SHOP_GOODBYE" : "GENERIC_BYE", (SpeechModifier)3);
				}
			}
			API.RemoveDecalsInRange(((Entity)val).Position.X, ((Entity)val).Position.Y, ((Entity)val).Position.Z, 20f);
		}
		wasInGreetingRange = flag;
		bool ShouldGreet()
		{
			if (!business.IsBeingRobbed && !business.EmployeesAssaulted && !employee.State.IsBeingRobbed && !employee.State.IsScared && (employee.Role == EmployeeRole.Cashier || employee.Role == EmployeeRole.Bartender))
			{
				return !MenuController.IsAnyMenuOpen();
			}
			return false;
		}
	}

	private void AttachControls()
	{
		if (!shopControlsEnabled)
		{
			shopControlsEnabled = true;
			Utils.AddInstructionalButton("shop", new InstructionalButton(LocalizationController.S(Entries.Businesses.MENU_STORE_BTN_BROWSE), 2, (Control)51));
			KeysScript.AttachListener((Control)51, OnKeyEvent, 50);
		}
	}

	private void DetachControls()
	{
		if (shopControlsEnabled)
		{
			shopControlsEnabled = false;
			Utils.RemoveInstructionalButton("shop");
			KeysScript.DetachListener((Control)51, OnKeyEvent);
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 51 && eventType == KeyEventType.JustPressed && !MenuController.IsAnyMenuOpen())
		{
			switch (ClosestBusiness.Type)
			{
			case BusinessType.Dealership:
				DealershipMenuScript.OpenDealershipMenu(ClosestBusiness);
				break;
			case BusinessType.Mechanic:
				MechanicShopScript.OpenMechanicMenu(ClosestBusiness);
				break;
			default:
				ShoppingScript.OpenShopMenu(ClosestBusiness);
				break;
			}
			return true;
		}
		return false;
	}

	private Model GetModelFromString(string modelString)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		if (modelString.Contains("|"))
		{
			string[] array = modelString.Split('|');
			int num = random.Next(array.Length);
			modelString = array[num];
		}
		return new Model(modelString);
	}
}
