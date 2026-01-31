using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin.Menus;
using Gtacnr.Client.Admin.Menus.EntitiesManagement;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Crimes;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Inventory;
using Gtacnr.Client.Jobs.Trucker;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Admin;

public class ModeratorMenuScript : Script
{
	private PlayerState selectedPlayerState;

	private readonly Random random = new Random();

	private DateTime lastSummonTimestamp = DateTime.MinValue;

	private DateTime lastFreezeTimestamp = DateTime.MinValue;

	private bool isVisible = true;

	private bool isFakeUsernameActive;

	private DateTime noclipEnableTimestamp;

	private Vector3 noclipEnablePosition;

	private VehicleHash noclipEnableVehicle;

	private bool menusCreated;

	private bool commandsRegistered;

	public static EventHandler<EventArgs> ModeratorCommandsRegistered;

	private static readonly Dictionary<string, string> ambientPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "a_f_y_tourist_01", "Tourist" },
		{ "a_m_m_ktown_01", "Old Korean Man" },
		{ "a_f_o_ktown_01", "Old Korean Woman" },
		{ "a_m_m_genfat_01", "Fat Guy" },
		{ "a_f_m_fatwhite_01", "Fat Woman" },
		{ "a_m_y_beach_03", "Beach Man" },
		{ "a_f_y_beach_01", "Beach Woman" },
		{ "a_f_y_fitness_01", "Fit Woman" },
		{ "a_m_m_soucent_01", "Davis Man" },
		{ "a_f_o_soucent_02", "Davis Woman" },
		{ "a_m_m_bevhills_02", "Rockford Man" },
		{ "a_f_y_bevhills_02", "Rockford Woman" },
		{ "ig_old_man1a", "Country Guy" },
		{ "g_m_y_famdnf_01", "Families" },
		{ "g_f_y_vagos_01", "Vagos" },
		{ "g_m_y_ballaeast_01", "Ballas" },
		{ "mp_m_shopkeep_01", "Shopkeeper" },
		{ "s_m_y_ammucity_01", "Ammu-Nation" }
	};

	private static readonly Dictionary<string, string> animalPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "a_c_rat", "Rat" },
		{ "a_c_rabbit_01", "Rabbit" },
		{ "a_c_pigeon", "Pigeon" },
		{ "a_c_hen", "Chicken" },
		{ "a_c_crow", "Crow" },
		{ "a_c_chickenhawk", "Hawk" },
		{ "a_c_rottweiler", "Rottweiler" },
		{ "a_c_retriever", "Retriever" },
		{ "a_c_shepherd", "Shepherd" },
		{ "a_c_husky", "Husky" },
		{ "a_c_westy", "Westy" },
		{ "a_c_pug", "Pug" },
		{ "a_c_coyote", "Coyote" },
		{ "a_c_cat_01", "Cat" },
		{ "a_c_mtlion", "Cougar" },
		{ "a_c_panther", "Black Panther" },
		{ "a_c_pig", "Pig" },
		{ "a_c_boar", "Boar" },
		{ "a_c_cow", "Cow" },
		{ "a_c_deer", "Deer" }
	};

	private static readonly Dictionary<string, string> specialPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "u_m_y_pogo_01", "Pogo" },
		{ "u_m_m_streetart_01", "Pogo 2" },
		{ "u_m_y_juggernaut_01", "Juggernaut" },
		{ "u_m_y_zombie_01", "Zombie" },
		{ "ig_orleans", "Bigfoot" },
		{ "u_m_y_rsranger_01", "Space Ranger" },
		{ "s_m_m_movspace_01", "Astronaut" },
		{ "s_m_m_strperf_01", "Human Statue" },
		{ "s_m_m_movalien_01", "Green Alien" },
		{ "grayalien", "Gray Alien" },
		{ "kermit", "Kermit the Frog" },
		{ "shaolin", "Shaolin Monk" },
		{ "somalipirate", "Somali Pirate" },
		{ "teslabot", "Coil Bot" },
		{ "tung", "Tung Tung Tung Sahur" }
	};

	private static readonly Dictionary<string, string> gtaPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "player_zero", "Michael" },
		{ "player_one", "Franklin" },
		{ "player_two", "Trevor" },
		{ "ig_lamardavis", "Lamar" },
		{ "ig_tanisha", "Tanisha" },
		{ "ig_wade", "Wade" },
		{ "ig_lestercrest", "Lester" },
		{ "ig_amandatownley", "Amanda" },
		{ "ig_jimmydisanto", "Jimmy" },
		{ "ig_tracydisanto", "Tracy" }
	};

	private static readonly Dictionary<string, string> vipPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "khaby", "Khaby Lame" },
		{ "elon", "Elon Musk" },
		{ "the_rock", "The Rock" },
		{ "trump", "Donald Trump" },
		{ "obama", "Barack Obama" }
	};

	private static readonly Dictionary<string, string> moviePeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "shrek", "Shrek" },
		{ "puss", "Puss in Boots" },
		{ "mr_incredible", "Mr. Incredible" },
		{ "neo", "Neo" },
		{ "agentsmith", "Agent Smith" },
		{ "ellie", "Ellie Williams" },
		{ "squid_doll", "Squid Game Doll" },
		{ "squid_guard", "Squid Game Guard" },
		{ "nailong", "NaiLong" }
	};

	private static readonly Dictionary<string, string> gamePeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "laracroft", "Lara Croft" },
		{ "codghost", "COD Ghosts" },
		{ "valojett", "Jett" },
		{ "valoneon", "Neon" }
	};

	private static readonly Dictionary<string, string> marvelDcPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "batman", "Batman" },
		{ "catwoman", "Catwoman" },
		{ "spiderman", "Spiderman" },
		{ "deadpool", "Deadpool" },
		{ "cpt_america", "Captain America" },
		{ "wintersoldier", "Winter Soldier" },
		{ "joker", "The Joker" },
		{ "harley", "Harley Quinn" },
		{ "blackwidow", "Black Widow" }
	};

	private static readonly Dictionary<string, string> breakingBadPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "heisenberg", "Walter White" },
		{ "pinkman", "Jesse Pinkman" },
		{ "saul", "Saul Goodman" },
		{ "ehrmantraut", "Mike Ehrmantraut" },
		{ "schrader", "Hank Schrader" }
	};

	private static readonly Dictionary<string, string> dragonballPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "goku", "Goku" },
		{ "vegeta", "Vegeta" },
		{ "piccolo", "Piccolo" },
		{ "kid_goku", "Kid Goku" },
		{ "kidbuu", "Kid Buu" }
	};

	private static readonly Dictionary<string, string> doaPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "kasumi", "Kasumi" },
		{ "ayane", "Ayane" },
		{ "honoka", "Honoka" },
		{ "kokoro", "Kokoro" },
		{ "ryu", "Ryu" }
	};

	private static readonly Dictionary<string, string> halloweenPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "Annabelle", "Annabelle" },
		{ "billysaw", "Billy the Puppet" },
		{ "ghostface", "Ghostface" },
		{ "jason", "Jason Voorhees" },
		{ "leatherface", "Leatherface" },
		{ "MMyers", "Michael Myers" },
		{ "pennywise", "Pennywise" },
		{ "SlenderMan", "Slenderman" },
		{ "Skeleton", "Skeleton" },
		{ "jackskellington", "Jack Skellington" }
	};

	private static readonly Dictionary<string, string> christmasPeds = new Dictionary<string, string>
	{
		{ "", "Normal" },
		{ "Santaclaus", "Santa Claus" },
		{ "Mrsclaus", "Mrs. Claus" }
	};

	public static bool StreamingMode { get; private set; }

	public static bool IsOnDuty { get; private set; }

	public static bool IsInGhostMode { get; private set; }

	public static bool IsInUndercoverMode { get; private set; }

	private Dictionary<string, Menu> menus { get; } = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems { get; } = new Dictionary<string, MenuItem>();

	private void RegisterCommands()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100 && !commandsRegistered)
		{
			API.RegisterCommand("aplayers", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				OpenPlayersMenu();
			}), false);
			Chat.AddSuggestion("/aplayers", "Opens the menu to moderate players.");
			API.RegisterCommand("atools", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				OpenToolsMenu();
			}), false);
			Chat.AddSuggestion("/atools", "Opens the moderator tools menu.");
			API.RegisterCommand("anoclip", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				ToggleNoClip();
			}), false);
			Chat.AddSuggestion("/anoclip", "Toggles admin no-clip mode.");
			API.RegisterCommand("aghost", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				ToggleGhostMode();
			}), false);
			Chat.AddSuggestion("/aghost", "Toggles ghost mode.");
			API.RegisterCommand("aundercover", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				ToggleUndercoverMode();
			}), false);
			Chat.AddSuggestion("/aundercover", "Toggles undercover mode.");
			API.RegisterCommand("aduty", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				ToggleDuty();
			}), false);
			Chat.AddSuggestion("/aduty", "Toggles on-duty mode.");
			API.RegisterCommand("afakeid", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				ToggleFakeName();
			}), false);
			Chat.AddSuggestion("/afakeid", "Toggles fake identity mode.");
			API.RegisterCommand("ainv", InputArgument.op_Implicit((Delegate)(Action<int, List<object>, string>)delegate
			{
				ToggleInvisible();
			}), false);
			Chat.AddSuggestion("/ainv", "Toggles invisible mode.");
			commandsRegistered = true;
			ModeratorCommandsRegistered?.Invoke(this, EventArgs.Empty);
		}
	}

	public ModeratorMenuScript()
	{
		StaffLevelScript.StaffLevelInitializedOrChanged += OnStaffLevelInitializedOrChanged;
		StaffLevelScript.StaffLevelChanged += OnStaffLevelChanged;
	}

	private void OnStaffLevelInitializedOrChanged(object sender, StaffLevelArgs e)
	{
		if ((int)e.PreviousStaffLevel >= 100 && (int)e.NewStaffLevel < 100)
		{
			SpectateScript.EndSpectate();
			StreamingMode = false;
			IsOnDuty = false;
			((Entity)Game.PlayerPed).IsInvincible = IsOnDuty;
			BaseScript.TriggerEvent("gtacnr:hud:toggleAdminDuty", new object[1] { IsOnDuty });
			IsInGhostMode = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleAdminGhost", new object[1] { IsInGhostMode });
			IsInUndercoverMode = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleAdminUndercover", new object[1] { IsInUndercoverMode });
			if (!isVisible)
			{
				API.SetEntityVisible(((PoolObject)Game.PlayerPed).Handle, true, false);
				isVisible = true;
			}
			isFakeUsernameActive = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleAdminFakeName", new object[1] { false });
			try
			{
				menuItems["duty"].Label = "~r~OFF";
				menuItems["noclip"].Label = "~r~OFF";
				menuItems["streaming"].Label = "~r~OFF";
				menuItems["fakeIdentity"].Label = "~r~OFF";
				menuItems["undercoverMode"].Label = "~r~OFF";
				menuItems["ghostMode"].Label = "~r~OFF";
				menuItems["invisibleMode"].Label = "~r~OFF";
			}
			catch (Exception)
			{
			}
		}
		if ((int)e.NewStaffLevel >= 100)
		{
			RegisterCommands();
			CreateAllMenus();
		}
	}

	private void CreateAllMenus()
	{
		TeleportMenuScript.Instance.CreateMenus();
		EntitiesManagementScript.Instance.CreateMenus();
		CreateMainMenus();
	}

	private void CreateMainMenus()
	{
		if (!menusCreated)
		{
			menusCreated = true;
			menus["players"] = new Menu("Moderator Menu", "Select a player")
			{
				CloseWhenDead = false
			};
			menus["players"].OnItemSelect += OnItemSelect;
			menus["players"].InstructionalButtons.Add((Control)206, LocalizationController.S(Entries.Main.BTN_SEARCH));
			menus["players"].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)206, Menu.ControlPressCheckType.JUST_PRESSED, async delegate
			{
				await EnterIdManually();
			}, disableControl: true));
			MenuController.AddMenu(menus["players"]);
			menus["actions"] = new Menu("Moderator Menu", "Select an action")
			{
				CloseWhenDead = false
			};
			menus["actions"].OnItemSelect += OnItemSelect;
			Menu menu = menus["actions"];
			MenuItem item = (menuItems["spectate"] = new MenuItem("Spectate", "Spectate the player. ~r~Attention: ~s~You must enable ~g~Ghost Mode ~s~before spectating, to make your blip invisible on the minimap."));
			menu.AddMenuItem(item);
			Menu menu2 = menus["actions"];
			item = (menuItems["goto"] = new MenuItem("Go to", "Teleports you to the player's location."));
			menu2.AddMenuItem(item);
			Menu menu3 = menus["actions"];
			item = (menuItems["summon"] = new MenuItem("Summon", "Summons the player to your location."));
			menu3.AddMenuItem(item);
			Menu menu4 = menus["actions"];
			item = (menuItems["freeze"] = new MenuItem("Freeze/Unfreeze", "Stops player movement."));
			menu4.AddMenuItem(item);
			Menu menu5 = menus["actions"];
			item = (menuItems["crimes"] = new MenuItem("View crimes", "View crimes that this player committed."));
			menu5.AddMenuItem(item);
			Menu menu6 = menus["actions"];
			item = (menuItems["copyUID"] = new MenuItem("Copy UID", "Shows player UID in a text box."));
			menu6.AddMenuItem(item);
			Menu menu7 = menus["actions"];
			item = (menuItems["warn"] = new MenuItem("Warn", "Warns the player with a full-screen message."));
			menu7.AddMenuItem(item);
			Menu menu8 = menus["actions"];
			item = (menuItems["kill"] = new MenuItem("Detonate", "Creates an explosion that kills the player."));
			menu8.AddMenuItem(item);
			Menu menu9 = menus["actions"];
			item = (menuItems["cayo"] = new MenuItem("Send to Cayo Perico", "Teleports the player to the Cayo Perico island redzone."));
			menu9.AddMenuItem(item);
			Menu menu10 = menus["actions"];
			item = (menuItems["mute"] = new MenuItem("Mute", "Prevents the player from using the global text chat."));
			menu10.AddMenuItem(item);
			Menu menu11 = menus["actions"];
			item = (menuItems["fine"] = new MenuItem("Fine", "Fines the player of a specified amount of money."));
			menu11.AddMenuItem(item);
			Menu menu12 = menus["actions"];
			item = (menuItems["xpfine"] = new MenuItem("XP Fine", "Fines the player of a specified amount of XP."));
			menu12.AddMenuItem(item);
			Menu menu13 = menus["actions"];
			item = (menuItems["kick"] = new MenuItem("Kick", "Kicks the player from the server."));
			menu13.AddMenuItem(item);
			Menu menu14 = menus["actions"];
			item = (menuItems["jobBan"] = new MenuItem("Job Ban", "Bans the player from playing their current job.\nThe player's job has to be a public service (i.e. police or paramedic)."));
			menu14.AddMenuItem(item);
			Menu menu15 = menus["actions"];
			item = (menuItems["ban"] = new MenuItem("~r~Ban", "Bans the player from the server."));
			menu15.AddMenuItem(item);
			MenuController.AddSubmenu(menus["players"], menus["actions"]);
			menus["options"] = new Menu("Moderator Menu", LocalizationController.S(Entries.Main.MENU_CHOOSE_OPTION))
			{
				CloseWhenDead = false
			};
			menus["options"].OnItemSelect += OnItemSelect;
			Menu menu16 = menus["options"];
			Dictionary<string, MenuItem> dictionary = menuItems;
			MenuItem obj = new MenuItem("Duty Mode", "Enable or disable moderator duty mode, which makes your blip appear green, removes you from the police/paramedic jobs, and enables godmode.")
			{
				Label = "~r~OFF"
			};
			item = obj;
			dictionary["duty"] = obj;
			menu16.AddMenuItem(item);
			Menu menu17 = menus["options"];
			Dictionary<string, MenuItem> dictionary2 = menuItems;
			MenuItem obj2 = new MenuItem("Noclip", "Enable or disable noclip.")
			{
				Label = "~r~OFF"
			};
			item = obj2;
			dictionary2["noclip"] = obj2;
			menu17.AddMenuItem(item);
			Menu menu18 = menus["options"];
			Dictionary<string, MenuItem> dictionary3 = menuItems;
			MenuItem obj3 = new MenuItem("Streaming Mode", "Enable or disable streaming mode. All moderators ~r~must ~s~enable this mode when broadcasting or sharing their screen with non-moderators.")
			{
				Label = "~r~OFF"
			};
			item = obj3;
			dictionary3["streaming"] = obj3;
			menu18.AddMenuItem(item);
			Menu menu19 = menus["options"];
			Dictionary<string, MenuItem> dictionary4 = menuItems;
			MenuItem obj4 = new MenuItem("Online Staff", "See the list of the online staff members.")
			{
				Label = Utils.MENU_ARROW
			};
			item = obj4;
			dictionary4["staffList"] = obj4;
			menu19.AddMenuItem(item);
			Menu menu20 = menus["options"];
			Dictionary<string, MenuItem> dictionary5 = menuItems;
			MenuItem obj5 = new MenuItem("Reports", "Manage user reports.")
			{
				Label = Utils.MENU_ARROW
			};
			item = obj5;
			dictionary5["reports"] = obj5;
			menu20.AddMenuItem(item);
			Menu menu21 = menus["options"];
			Dictionary<string, MenuItem> dictionary6 = menuItems;
			MenuItem obj6 = new MenuItem("Teleport Options", "Quick teleport, map teleport and more.")
			{
				Label = Utils.MENU_ARROW
			};
			item = obj6;
			dictionary6["teleport"] = obj6;
			menu21.AddMenuItem(item);
			Menu menu22 = menus["options"];
			Dictionary<string, MenuItem> dictionary7 = menuItems;
			MenuItem obj7 = new MenuItem("Incognito Options", "Incognito mode, ghost, fake identity.")
			{
				Label = Utils.MENU_ARROW
			};
			item = obj7;
			dictionary7["incognito"] = obj7;
			menu22.AddMenuItem(item);
			Menu menu23 = menus["options"];
			Dictionary<string, MenuItem> dictionary8 = menuItems;
			MenuItem obj8 = new MenuItem("Server Options", "Manage the server in general.")
			{
				Label = Utils.MENU_ARROW
			};
			item = obj8;
			dictionary8["server"] = obj8;
			menu23.AddMenuItem(item);
			Menu menu24 = menus["options"];
			Dictionary<string, MenuItem> dictionary9 = menuItems;
			MenuItem obj9 = new MenuItem("Entities Management", "Allows you to manage entities spawned on the server.")
			{
				Label = Utils.MENU_ARROW
			};
			item = obj9;
			dictionary9["entities"] = obj9;
			menu24.AddMenuItem(item);
			MenuController.BindMenuItem(menus["options"], EntitiesManagementScript.Instance.GetMenu(), menuItems["entities"]);
			menus["staffListMenu"] = new Menu("Moderator Menu", "Staff list")
			{
				CloseWhenDead = false
			};
			MenuController.BindMenuItem(menus["options"], menus["staffListMenu"], menuItems["staffList"]);
			menus["reports"] = new Menu("Moderator Menu", "Reports")
			{
				CloseWhenDead = false
			};
			Menu menu25 = menus["reports"];
			item = (menuItems["pendingReports"] = new MenuItem("Pending", "View the pending reports on online players."));
			menu25.AddMenuItem(item);
			Menu menu26 = menus["reports"];
			item = (menuItems["assignedReports"] = new MenuItem("Assigned", "View the reports that you've assigned to yourself."));
			menu26.AddMenuItem(item);
			Menu menu27 = menus["reports"];
			item = (menuItems["previousReports"] = new MenuItem("Closed", "View the reports that you've closed."));
			menu27.AddMenuItem(item);
			menus["reports"].OnItemSelect += OnItemSelect;
			MenuController.BindMenuItem(menus["options"], menus["reports"], menuItems["reports"]);
			menus["pendingReports"] = new Menu("Moderator Menu", "Pending Reports")
			{
				CloseWhenDead = false
			};
			menus["assignedReports"] = new Menu("Moderator Menu", "Assigned Reports")
			{
				CloseWhenDead = false
			};
			menus["previousReports"] = new Menu("Moderator Menu", "Closed Reports")
			{
				CloseWhenDead = false
			};
			MenuController.BindMenuItem(menus["reports"], menus["pendingReports"], menuItems["pendingReports"]);
			MenuController.BindMenuItem(menus["reports"], menus["assignedReports"], menuItems["assignedReports"]);
			MenuController.BindMenuItem(menus["reports"], menus["previousReports"], menuItems["previousReports"]);
			menus["pendingReports"].OnItemSelect += OnItemSelect;
			menus["assignedReports"].OnItemSelect += OnItemSelect;
			menus["previousReports"].PlaySelectSound = false;
			menus["assignedReports"].InstructionalButtons.Clear();
			menus["assignedReports"].InstructionalButtons.Add((Control)201, "Respond");
			menus["assignedReports"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
			menus["assignedReports"].InstructionalButtons.Add((Control)204, "View Player");
			menus["assignedReports"].InstructionalButtons.Add((Control)214, "Unassign");
			menus["assignedReports"].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)204, Menu.ControlPressCheckType.JUST_PRESSED, OnPendingReportViewPlayer, disableControl: true));
			menus["assignedReports"].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)214, Menu.ControlPressCheckType.JUST_PRESSED, UnassignReport, disableControl: true));
			MenuController.BindMenuItem(menus["options"], TeleportMenuScript.Instance.GetMenu(), menuItems["teleport"]);
			menus["incognitoOptions"] = new Menu("Moderator Menu", "Incognito options")
			{
				CloseWhenDead = false
			};
			menus["incognitoOptions"].OnItemSelect += OnItemSelect;
			menus["incognitoOptions"].OnListItemSelect += OnListItemSelect;
			Menu menu28 = menus["incognitoOptions"];
			Dictionary<string, MenuItem> dictionary10 = menuItems;
			MenuItem obj10 = new MenuItem("Fake Identity", "Temporarily change your display username, to do undercover work without being detected.")
			{
				Label = "~r~OFF"
			};
			item = obj10;
			dictionary10["fakeIdentity"] = obj10;
			menu28.AddMenuItem(item);
			Menu menu29 = menus["incognitoOptions"];
			Dictionary<string, MenuItem> dictionary11 = menuItems;
			MenuItem obj11 = new MenuItem("Undercover Mode", "Hides your Staff role from leaderboard.")
			{
				Label = "~r~OFF"
			};
			item = obj11;
			dictionary11["undercoverMode"] = obj11;
			menu29.AddMenuItem(item);
			Menu menu30 = menus["incognitoOptions"];
			Dictionary<string, MenuItem> dictionary12 = menuItems;
			MenuItem obj12 = new MenuItem("Ghost Mode", "Hides your blip from the radar and your nametag from your head.")
			{
				Label = "~r~OFF"
			};
			item = obj12;
			dictionary12["ghostMode"] = obj12;
			menu30.AddMenuItem(item);
			Menu menu31 = menus["incognitoOptions"];
			Dictionary<string, MenuItem> dictionary13 = menuItems;
			MenuItem obj13 = new MenuItem("Invisible", "Makes your character invisible.")
			{
				Label = "~r~OFF"
			};
			item = obj13;
			dictionary13["invisibleMode"] = obj13;
			menu31.AddMenuItem(item);
			Menu menu32 = menus["incognitoOptions"];
			Dictionary<string, MenuItem> dictionary14 = menuItems;
			MenuItem obj14 = new MenuItem("Peds", "Changes your character to one of the many available peds.")
			{
				Label = Utils.MENU_ARROW
			};
			item = obj14;
			dictionary14["peds"] = obj14;
			menu32.AddMenuItem(item);
			MenuController.BindMenuItem(menus["options"], menus["incognitoOptions"], menuItems["incognito"]);
			menus["peds"] = new Menu("Moderator Menu", "Peds")
			{
				CloseWhenDead = false
			};
			menus["peds"].OnItemSelect += OnItemSelect;
			menus["peds"].OnListItemSelect += OnListItemSelect;
			Menu menu33 = menus["peds"];
			item = (menuItems["ambientPed"] = new MenuListItem("Ambient", ambientPeds.Values.ToList(), 0));
			menu33.AddMenuItem(item);
			Menu menu34 = menus["peds"];
			item = (menuItems["animal"] = new MenuListItem("Animals", animalPeds.Values.ToList(), 0));
			menu34.AddMenuItem(item);
			Menu menu35 = menus["peds"];
			item = (menuItems["gtaPed"] = new MenuListItem("GTA", gtaPeds.Values.ToList(), 0));
			menu35.AddMenuItem(item);
			MenuController.BindMenuItem(menus["incognitoOptions"], menus["peds"], menuItems["peds"]);
			menus["serverOptions"] = new Menu("Moderator Menu", "Server options")
			{
				CloseWhenDead = false
			};
			menus["serverOptions"].OnItemSelect += OnItemSelect;
			menus["serverOptions"].OnListItemSelect += OnListItemSelect;
			Menu menu36 = menus["serverOptions"];
			item = (menuItems["announce"] = new MenuItem("Announce", "Make a public announcement in the chat as a moderator (t-mods, mods) or admin (admins, managers)."));
			menu36.AddMenuItem(item);
			Menu menu37 = menus["serverOptions"];
			item = (menuItems["clearChat"] = new MenuItem("Clear chat", "Clears the text chat."));
			menu37.AddMenuItem(item);
			Menu menu38 = menus["serverOptions"];
			item = (menuItems["clearVehs"] = new MenuItem("Clear vehicles", "Clears all the unused and NPC vehicles in the area."));
			menu38.AddMenuItem(item);
			Menu menu39 = menus["serverOptions"];
			item = (menuItems["toggleED"] = new MenuItem("Toggle entity detector", "Enable/disable CnR Shield's Entity Detector, which automatically detects and bans cheaters who spawn vehicles and other entities (NPCs, props). It's disabled by default because it's ~r~VERY DEMANDING ~s~on server performance."));
			menu39.AddMenuItem(item);
			Menu menu40 = menus["serverOptions"];
			item = (menuItems["resetBucket"] = new MenuItem("Reset universe", "Returns you to the main universe. Use this if you're stuck inside a property or the dealership with no exit and when you noclip out you can't see anyone."));
			menu40.AddMenuItem(item);
			Menu menu41 = menus["serverOptions"];
			item = (menuItems["restartGameMode"] = new MenuItem("Restart game mode", "Restarts the game scripts for everyone (aka soft-restart).\n~r~Warning~s~: only use this when absolutely necessary!"));
			menu41.AddMenuItem(item);
			MenuController.BindMenuItem(menus["options"], menus["serverOptions"], menuItems["server"]);
		}
	}

	private void OnStaffLevelChanged(object sender, StaffLevelArgs e)
	{
		if (e.NewStaffLevel == StaffLevel.None)
		{
			if (e.PreviousStaffLevel != StaffLevel.None)
			{
				Utils.DisplayHelpText("You have been ~r~removed from the ~g~staff~s~.");
				MenuController.CloseAllMenus();
			}
		}
		else if (e.PreviousStaffLevel == StaffLevel.None)
		{
			Utils.DisplayHelpText($"You have been added to the ~g~staff ~s~as part of group ~g~{e.NewStaffLevel}~s~.");
			CreateAllMenus();
		}
		else
		{
			Utils.DisplayHelpText($"Your ~g~staff group ~s~has been changed to ~g~{e.NewStaffLevel}~s~.");
		}
	}

	[EventHandler("gtacnr:halloween:initialize")]
	private async void OnHalloweenStarted()
	{
		while (!menus.ContainsKey("incognitoOptions"))
		{
			await BaseScript.Delay(1000);
		}
		Menu menu = menus["peds"];
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuListItem obj = new MenuListItem("~o~Halloween", halloweenPeds.Values.ToList(), 0)
		{
			Description = "Temporarily replaces your character with a Halloween ped."
		};
		MenuItem item = obj;
		dictionary["halloweenPed"] = obj;
		menu.AddMenuItem(item);
	}

	[EventHandler("gtacnr:christmas:initialize")]
	private async void OnChristmasStarted()
	{
		while (!menus.ContainsKey("incognitoOptions"))
		{
			await BaseScript.Delay(1000);
		}
		Menu menu = menus["peds"];
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuListItem obj = new MenuListItem("~g~Christ~r~mas", christmasPeds.Values.ToList(), 0)
		{
			Description = "Temporarily replaces your character with a Christmas ped."
		};
		MenuItem item = obj;
		dictionary["christmasPed"] = obj;
		menu.AddMenuItem(item);
	}

	private void RefreshPlayersMenu()
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		menus["players"].ClearMenuItems();
		foreach (PlayerState item in LatentPlayers.All.OrderBy((PlayerState ps) => ps.Id))
		{
			string text = Gtacnr.Data.Jobs.GetJobData(item.Job)?.Name ?? "N/A";
			string locationName = Utils.GetLocationName(item.Position);
			string colorTextCode = item.ColorTextCode;
			string text2 = colorTextCode + item.Name;
			string description = "FiveM/Steam Username: " + colorTextCode + item.ActualUsername + "~s~~n~" + (((int)StaffLevelScript.StaffLevel >= 120) ? ("Location: " + colorTextCode + locationName + "~s~~n~") : "") + "Job: " + colorTextCode + text + "~s~ - " + $"Wanted Level: {colorTextCode}{item.WantedLevel}~s~~n~" + $"XP: {colorTextCode}{item.XP}~s~ - " + $"Level: {colorTextCode}{item.Level}~s~ -" + "Cash: ~g~" + item.Cash.ToCurrencyString() + "~s~ - Bank: ~g~" + item.Bank.ToCurrencyString();
			MenuItem menuItem = new MenuItem(text2, description)
			{
				Label = $"{colorTextCode}{item.Id} ›",
				ItemData = item
			};
			MenuController.BindMenuItem(menus["players"], menus["actions"], menuItem);
			menus["players"].AddMenuItem(menuItem);
		}
		menus["players"].CounterPreText = $"{LatentPlayers.Count} players";
	}

	private async void RefreshStaffMenu()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		Dictionary<int, string> dictionary = (await TriggerServerEventAsync<string>("gtacnr:admin:fetchRealUsernames", new object[0]))?.Unjson<Dictionary<int, string>>() ?? new Dictionary<int, string>();
		menus["staffListMenu"].ClearMenuItems();
		List<PlayerState> list = (from ps in LatentPlayers.All
			where (int)ps.StaffLevel > 0
			orderby ps.Id
			select ps).ToList();
		int num = 0;
		foreach (PlayerState item2 in list)
		{
			string text = "~g~";
			switch (item2.StaffLevel)
			{
			case StaffLevel.Helper:
				text = "~o~";
				break;
			case StaffLevel.TrialTester:
			case StaffLevel.Tester:
			case StaffLevel.LeadTester:
				text = "~y~";
				break;
			case StaffLevel.Designer:
			case StaffLevel.Developer:
				text = "~b~";
				break;
			case StaffLevel.TrialModerator:
				text = "~HUD_COLOUR_GREENDARK~";
				break;
			case StaffLevel.Moderator:
			case StaffLevel.LeadModerator:
			case StaffLevel.Admin:
				text = "~g~";
				break;
			case StaffLevel.Manager:
			case StaffLevel.CommunityManager:
			case StaffLevel.StaffManager:
				text = "~HUD_COLOUR_G12~";
				break;
			case StaffLevel.Coowner:
				text = "~HUD_COLOUR_REDDARK~";
				break;
			case StaffLevel.Owner:
				text = "~r~";
				break;
			}
			string text2 = Gtacnr.Utils.GetDescription(item2.StaffLevel);
			if (dictionary.ContainsKey(item2.Id))
			{
				text2 = text2 + " (" + dictionary[item2.Id] + ")";
			}
			MenuItem item = new MenuItem(text + item2.Name)
			{
				Label = $"{text}{item2.Id}",
				ItemData = item2,
				Description = text2
			};
			menus["staffListMenu"].AddMenuItem(item);
			num++;
		}
		menus["staffListMenu"].CounterPreText = $"{num} staff members";
	}

	private async void RefreshPendingReportsMenu()
	{
		menus["pendingReports"].ClearMenuItems();
		menus["pendingReports"].AddLoadingMenuItem();
		List<Report> reports = new List<Report>();
		string text = await TriggerServerEventAsync<string>("gtacnr:fetchReportsOnConnectedPlayers", new object[0]);
		menus["pendingReports"].ClearMenuItems();
		if (!string.IsNullOrEmpty(text))
		{
			reports = (from r in text.Unjson<IEnumerable<Report>>()
				where r.State == ReportState.Pending
				orderby r.DateTime descending
				select r).ToList();
		}
		foreach (Report item2 in reports)
		{
			string text2 = Gtacnr.Utils.CalculateTimeAgo(item2.DateTime);
			int num = 0;
			foreach (PlayerState item3 in LatentPlayers.All)
			{
				if (item3.Name == item2.ReportedUserName)
				{
					num = item3.Id;
				}
			}
			MenuItem menuItem = new MenuItem((num == 0) ? item2.ReportedUserName : $"{item2.ReportedUserName} ({num})");
			menuItem.Description = "~b~Time: ~s~" + text2 + " (" + item2.DateTime.ToFormalDateTime() + ")\n~b~Reporter: ~s~" + item2.ReporterUserName + "\n~b~Reason: ~s~" + Gtacnr.Utils.GetDescription(item2.Reason) + "\n~b~Details: ~s~" + item2.Details;
			menuItem.Label = "~b~by " + item2.ReporterUserName;
			menuItem.ItemData = item2;
			MenuItem item = menuItem;
			menus["pendingReports"].AddMenuItem(item);
		}
		menus["pendingReports"].CounterPreText = $"{reports.Count} reports";
		if (reports.Count == 0)
		{
			menus["pendingReports"].AddMenuItem(new MenuItem("No reports :D", "The server is clean! No reports to handle."));
		}
	}

	private async void RefreshAssignedReportsMenu()
	{
		menus["assignedReports"].ClearMenuItems();
		menus["assignedReports"].AddLoadingMenuItem();
		List<Report> reports = new List<Report>();
		string text = await TriggerServerEventAsync<string>("gtacnr:fetchOpenReportsAssignedToMe", new object[0]);
		menus["assignedReports"].ClearMenuItems();
		if (!string.IsNullOrEmpty(text))
		{
			reports = (from r in text.Unjson<IEnumerable<Report>>()
				orderby r.DateTime descending
				select r).ToList();
		}
		foreach (Report item2 in reports)
		{
			string text2 = Gtacnr.Utils.CalculateTimeAgo(item2.DateTime);
			int num = 0;
			foreach (PlayerState item3 in LatentPlayers.All)
			{
				if (item3.Name == item2.ReportedUserName)
				{
					num = item3.Id;
				}
			}
			MenuItem menuItem = new MenuItem((num == 0) ? item2.ReportedUserName : $"{item2.ReportedUserName} ({num})");
			menuItem.Description = "~b~Time: ~s~" + text2 + " (" + item2.DateTime.ToFormalDateTime() + ")\n~b~Reporter: ~s~" + item2.ReporterUserName + "\n~b~Reason: ~s~" + Gtacnr.Utils.GetDescription(item2.Reason) + "\n~b~Details: ~s~" + item2.Details;
			menuItem.Label = "~b~by " + item2.ReporterUserName;
			menuItem.ItemData = item2;
			MenuItem item = menuItem;
			menus["assignedReports"].AddMenuItem(item);
		}
		menus["assignedReports"].CounterPreText = $"{reports.Count()} reports";
		if (reports.Count == 0)
		{
			menus["assignedReports"].AddMenuItem(new MenuItem("No assigned reports :(", "Take some reports from the pending reports menu."));
		}
	}

	private async void RefreshPreviousReportsMenu()
	{
		menus["previousReports"].ClearMenuItems();
		menus["previousReports"].AddLoadingMenuItem();
		List<Report> reports = new List<Report>();
		string text = await TriggerServerEventAsync<string>("gtacnr:fetchClosedReportsAssignedToMe", new object[0]);
		menus["previousReports"].ClearMenuItems();
		if (!string.IsNullOrEmpty(text))
		{
			reports = (from r in text.Unjson<IEnumerable<Report>>()
				orderby r.DateTime descending
				select r).ToList();
		}
		foreach (Report item2 in reports)
		{
			string text2 = Gtacnr.Utils.CalculateTimeAgo(item2.DateTime);
			int num = 0;
			foreach (PlayerState item3 in LatentPlayers.All)
			{
				if (item3.Name == item2.ReportedUserName)
				{
					num = item3.Id;
				}
			}
			MenuItem menuItem = new MenuItem((num == 0) ? item2.ReportedUserName : $"{item2.ReportedUserName} ({num})");
			menuItem.Description = "~b~Time: ~s~" + text2 + " (" + item2.DateTime.ToFormalDateTime() + ")\n~b~Reporter: ~s~" + item2.ReporterUserName + "\n~b~Reason: ~s~" + Gtacnr.Utils.GetDescription(item2.Reason) + "\n~b~Details: ~s~" + item2.Details + "\n~b~Response: ~s~" + item2.ClosingResponse;
			menuItem.Label = text2 ?? "";
			menuItem.ItemData = item2;
			MenuItem item = menuItem;
			menus["previousReports"].AddMenuItem(item);
		}
		menus["previousReports"].CounterPreText = $"{reports.Count()} reports";
		if (reports.Count == 0)
		{
			menus["previousReports"].AddMenuItem(new MenuItem("No assigned reports :(", "Complete some reports from the assigned reports menu."));
		}
	}

	private void OnPendingReportViewPlayer(Menu menu, Control control)
	{
		if (menu.GetCurrentMenuItem().ItemData is Report report)
		{
			foreach (PlayerState item in LatentPlayers.All)
			{
				if (item.Name == report.ReportedUserName)
				{
					selectedPlayerState = item;
					menus["actions"].MenuSubtitle = $"{item.Name} ({item.Id})";
					MenuController.CloseAllMenus();
					menus["actions"].ParentMenu = menu;
					menus["actions"].OpenMenu();
					Utils.PlaySelectSound();
					return;
				}
			}
		}
		Utils.PlayErrorSound();
	}

	private async void UnassignReport(Menu menu, Control control)
	{
		MenuItem menuItem = menu.GetCurrentMenuItem();
		object itemData = menuItem.ItemData;
		if (itemData is Report report)
		{
			if (report.State != ReportState.Assigned)
			{
				Utils.PlayErrorSound();
			}
			else if (await TriggerServerEventAsync<bool>("gtacnr:untakeReport", new object[1] { report.Id }))
			{
				Utils.DisplayHelpText("You unassigned the ~g~" + Gtacnr.Utils.GetDescription(report.Reason) + " report ~s~on ~r~" + report.ReportedUserName + "~s~.");
				menu.RemoveMenuItem(menuItem);
			}
			else
			{
				Utils.DisplayErrorMessage(92, 2);
			}
		}
	}

	public static async Task RestoreCharacter()
	{
		Character character = await Gtacnr.Client.API.Characters.GetActiveCharacter();
		if (character == null)
		{
			Debug.WriteLine("Your active character is null!");
			return;
		}
		int num = 0;
		if (character.Sex == Sex.Male)
		{
			num = API.GetHashKey("mp_m_freemode_01");
		}
		else if (character.Sex == Sex.Female)
		{
			num = API.GetHashKey("mp_f_freemode_01");
		}
		AntiHealthLockScript.JustHealed();
		await Game.Player.ChangeModel(Model.op_Implicit(num));
		Utils.ApplyAppearance(Game.PlayerPed, character.Appearance);
		Clothes.CurrentApparel = character.Apparel;
		API.SetEntityHealth(((PoolObject)Game.PlayerPed).Handle, 400);
		API.SetEntityInvincible(((PoolObject)Game.PlayerPed).Handle, false);
		await ArmoryScript.ReloadLoadout();
	}

	private async void OnListItemSelect(Menu menu, MenuListItem menuItem, int selectedIndex, int itemIndex)
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		if (IsSelected("ambientPed") || IsSelected("animal") || IsSelected("specialPed") || IsSelected("gtaPed") || IsSelected("vipPed") || IsSelected("moviePed") || IsSelected("gamePed") || IsSelected("marvelDcPed") || IsSelected("breakingBadPed") || IsSelected("dragonballPed") || IsSelected("doaPed") || IsSelected("halloweenPed") || IsSelected("christmasPed"))
		{
			bool flag = Utils.IsFreemodePed(Game.PlayerPed);
			if (selectedIndex == 0 && flag)
			{
				return;
			}
			Dictionary<string, string> dictionary = null;
			if (IsSelected("ambientPed"))
			{
				dictionary = ambientPeds;
			}
			else if (IsSelected("animal"))
			{
				dictionary = animalPeds;
			}
			else if (IsSelected("specialPed"))
			{
				dictionary = specialPeds;
			}
			else if (IsSelected("gtaPed"))
			{
				dictionary = gtaPeds;
			}
			else if (IsSelected("vipPed"))
			{
				dictionary = vipPeds;
			}
			else if (IsSelected("moviePed"))
			{
				dictionary = moviePeds;
			}
			else if (IsSelected("gamePed"))
			{
				dictionary = gamePeds;
			}
			else if (IsSelected("marvelDcPed"))
			{
				dictionary = marvelDcPeds;
			}
			else if (IsSelected("breakingBadPed"))
			{
				dictionary = breakingBadPeds;
			}
			else if (IsSelected("dragonballPed"))
			{
				dictionary = dragonballPeds;
			}
			else if (IsSelected("doaPed"))
			{
				dictionary = doaPeds;
			}
			else if (IsSelected("halloweenPed"))
			{
				dictionary = halloweenPeds;
			}
			else if (IsSelected("christmasPed"))
			{
				dictionary = christmasPeds;
			}
			if (dictionary == null)
			{
				return;
			}
			int newPed = API.GetHashKey(dictionary.ElementAt(selectedIndex).Key);
			if (selectedIndex == 0)
			{
				await RestoreCharacter();
			}
			else
			{
				if (flag)
				{
					string text = Gtacnr.Client.API.Jobs.CachedJob;
					if (!Gtacnr.Data.Jobs.GetJobData(text).SeparateOutfit)
					{
						text = "none";
					}
					await Outfits.SaveOutfit(text, 0, Clothes.CurrentApparel);
				}
				AntiHealthLockScript.JustHealed();
				await Game.Player.ChangeModel(Model.op_Implicit(newPed));
				API.SetPedDefaultComponentVariation(((PoolObject)Game.PlayerPed).Handle);
				AntiHealthLockScript.JustHealed();
				API.SetEntityHealth(((PoolObject)Game.PlayerPed).Handle, 400);
				API.SetEntityInvincible(((PoolObject)Game.PlayerPed).Handle, true);
				if (IsOnDuty)
				{
					Utils.DisplayHelpText("~g~You got some futuristic moderator weapons! COOL!");
					Game.PlayerPed.Weapons.Give((WeaponHash)(-1355376991), 10000, true, true);
					Game.PlayerPed.Weapons.Give((WeaponHash)1198256469, 10000, false, true);
					Game.PlayerPed.Weapons.Give((WeaponHash)(-1238556825), 10000, false, true);
				}
			}
			API.SetGameplayCamRelativeHeading(180f);
		}
		else if (menuItem == menuItems["setWeather"])
		{
			BaseScript.TriggerServerEvent("gtacnr:admin:setWeather", new object[1] { selectedIndex });
		}
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		if (menu == menus["players"] && menuItem.ItemData is PlayerState playerState)
		{
			selectedPlayerState = playerState;
			menus["actions"].ParentMenu = menus["players"];
			menus["actions"].MenuSubtitle = $"{playerState.Name} ({playerState.Id})";
			return;
		}
		if (menu == menus["actions"])
		{
			if (selectedPlayerState != null)
			{
				if (IsSelected("spectate"))
				{
					Spectate();
				}
				else if (IsSelected("goto"))
				{
					Goto();
				}
				else if (IsSelected("summon"))
				{
					Summon();
				}
				else if (IsSelected("freeze"))
				{
					FreezeSelectedPlayer();
				}
				else if (IsSelected("crimes"))
				{
					ShowSelectedPlayerCrimes(menu);
				}
				else if (IsSelected("copyUID"))
				{
					CopyUID();
				}
				else if (IsSelected("warn"))
				{
					Warn();
				}
				else if (IsSelected("kill"))
				{
					Kill();
				}
				else if (IsSelected("cayo"))
				{
					CayoPerico();
				}
				else if (IsSelected("mute"))
				{
					Mute();
				}
				else if (IsSelected("fine"))
				{
					Fine();
				}
				else if (IsSelected("xpfine"))
				{
					XPFine();
				}
				else if (IsSelected("kick"))
				{
					Kick();
				}
				else if (IsSelected("jobBan"))
				{
					JobBan();
				}
				else if (IsSelected("ban"))
				{
					Ban();
				}
				else
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED));
				}
			}
			return;
		}
		if (menu == menus["options"])
		{
			if (IsSelected("duty"))
			{
				ToggleDuty();
			}
			else if (IsSelected("noclip"))
			{
				ToggleNoClip();
			}
			else if (IsSelected("streaming"))
			{
				ToggleStreamingMode();
			}
			else if (IsSelected("staffList"))
			{
				RefreshStaffMenu();
			}
			return;
		}
		if (menu == menus["incognitoOptions"])
		{
			if (IsSelected("ghostMode"))
			{
				ToggleGhostMode();
			}
			else if (IsSelected("undercoverMode"))
			{
				ToggleUndercoverMode();
			}
			else if (IsSelected("fakeIdentity"))
			{
				ToggleFakeName();
			}
			else if (IsSelected("invisibleMode"))
			{
				ToggleInvisible();
			}
			else if (!IsSelected("peds"))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_IMPLEMENTED));
			}
			return;
		}
		if (menu == menus["serverOptions"])
		{
			if (IsSelected("announce"))
			{
				MakeAnnouncement();
			}
			else if (IsSelected("clearChat"))
			{
				ClearChat();
			}
			else if (IsSelected("clearPeds"))
			{
				ClearPeds();
			}
			else if (IsSelected("clearVehs"))
			{
				ClearVehicles();
			}
			else if (IsSelected("clearProps"))
			{
				ClearProp();
			}
			else if (IsSelected("resetBucket"))
			{
				ResetBucket();
			}
			else if (IsSelected("toggleED"))
			{
				ToggleEntityDetector();
			}
			else if (menuItem == menuItems["restartGameMode"] && await Utils.ShowConfirm("Are you sure you want to restart the whole game mode?", "Restart"))
			{
				RestartGameMode();
				MenuController.CloseAllMenus();
			}
			return;
		}
		if (menu == menus["reports"])
		{
			if (menuItem == menuItems["pendingReports"])
			{
				RefreshPendingReportsMenu();
			}
			else if (menuItem == menuItems["assignedReports"])
			{
				RefreshAssignedReportsMenu();
			}
			else if (menuItem == menuItems["previousReports"])
			{
				RefreshPreviousReportsMenu();
			}
			return;
		}
		object itemData;
		if (menu == menus["pendingReports"])
		{
			itemData = menuItem.ItemData;
			if (itemData is Report report1)
			{
				if (await TriggerServerEventAsync<bool>("gtacnr:takeReport", new object[1] { report1.Id }))
				{
					Utils.DisplayHelpText("You assigned yourself the ~g~" + Gtacnr.Utils.GetDescription(report1.Reason) + " report ~s~on ~r~" + report1.ReportedUserName + "~s~.");
					menu.RemoveMenuItem(menuItem);
				}
				else
				{
					Utils.DisplayErrorMessage(94, 2);
				}
				return;
			}
		}
		if (menu != menus["assignedReports"])
		{
			return;
		}
		itemData = menuItem.ItemData;
		if (!(itemData is Report report2))
		{
			return;
		}
		string text = await Utils.GetUserInput("Response", "Enter a response for the user.", "", 200);
		if (text != null)
		{
			text = text.Trim();
			if (text.Length < 4)
			{
				Utils.DisplayHelpText("~r~Please, enter a longer response!");
			}
			else if (!(await TriggerServerEventAsync<bool>("gtacnr:closeReport", new object[2] { report2.Id, text })))
			{
				Utils.DisplayErrorMessage(95, 2);
			}
			else
			{
				menu.RemoveMenuItem(menuItem);
			}
		}
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
	}

	private async void MakeAnnouncement()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		string text = await Utils.GetUserInput("Announcement", "Enter an announcement to make.", "", 200);
		if (text != null)
		{
			text = text.Trim();
			if (text.Length < 5)
			{
				Utils.DisplayHelpText("~r~Please, enter a longer announcement!");
				return;
			}
			BaseScript.TriggerServerEvent("gtacnr:admin:makeAnnouncement", new object[1] { text });
		}
	}

	private void ClearChat()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			BaseScript.TriggerServerEvent("gtacnr:admin:clearChat", new object[0]);
		}
	}

	private void ClearPeds()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			string locationName = Utils.GetLocationName(((Entity)Game.PlayerPed).Position);
			BaseScript.TriggerServerEvent("gtacnr:admin:clearEntities", new object[2] { 1, locationName });
		}
	}

	private void ClearVehicles()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			string locationName = Utils.GetLocationName(((Entity)Game.PlayerPed).Position);
			BaseScript.TriggerServerEvent("gtacnr:admin:clearEntities", new object[2] { 2, locationName });
		}
	}

	private void ClearProp()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			string locationName = Utils.GetLocationName(((Entity)Game.PlayerPed).Position);
			BaseScript.TriggerServerEvent("gtacnr:admin:clearEntities", new object[2] { 3, locationName });
		}
	}

	private void ResetBucket()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			BaseScript.TriggerServerEvent("gtacnr:admin:resetMyRoutingBucket", new object[0]);
		}
	}

	private void ToggleEntityDetector()
	{
		if ((int)StaffLevelScript.StaffLevel < 110)
		{
			Utils.DisplayHelpText("Moderators in Training cannot use this feature.");
		}
		else
		{
			BaseScript.TriggerServerEvent("gtacnr:admin:toggleEntityDetector", new object[0]);
		}
	}

	private void RestartGameMode()
	{
		if ((int)StaffLevelScript.StaffLevel < 110)
		{
			Utils.DisplayHelpText("Moderators in Training cannot use this feature.");
		}
		else
		{
			BaseScript.TriggerServerEvent("gtacnr:admin:restartGameMode", new object[0]);
		}
	}

	private async void Spectate()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			if (!IsInGhostMode)
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("~r~You must enable Ghost Mode before spectating players.", playSound: false);
			}
			else
			{
				await SpectateScript.StartSpectate(selectedPlayerState.Id);
			}
		}
	}

	private async void Goto()
	{
		if ((int)StaffLevelScript.StaffLevel < 100 || selectedPlayerState == null)
		{
			return;
		}
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed)
			{
				Utils.DisplayHelpText("~r~You cannot teleport when you're not the driver of a vehicle.", playSound: false);
				return;
			}
			if (Game.PlayerPed.CurrentVehicle.PassengerCount > 0)
			{
				Utils.DisplayHelpText("~r~You cannot teleport when you have passengers.", playSound: false);
				return;
			}
		}
		if (!(await TriggerServerEventAsync<bool>("gtacnr:admin:goto", new object[1] { selectedPlayerState.Id })))
		{
			Utils.DisplayHelpText($"Unable to teleport you to {selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id})~s~.");
			return;
		}
		selectedPlayerState = LatentPlayers.Get(selectedPlayerState.Id);
		await Utils.TeleportToCoords(selectedPlayerState.Position, -1f, Utils.TeleportFlags.TeleportVehicle | Utils.TeleportFlags.VisualEffects);
	}

	private async void Summon()
	{
		if ((int)StaffLevelScript.StaffLevel < 100 || selectedPlayerState == null)
		{
			return;
		}
		if (!Gtacnr.Utils.CheckTimePassed(lastSummonTimestamp, 1000.0))
		{
			Utils.DisplayHelpText("~r~Wait before summoning another player.");
			return;
		}
		lastSummonTimestamp = DateTime.UtcNow;
		if (!(await TriggerServerEventAsync<bool>("gtacnr:admin:summon", new object[1] { selectedPlayerState.Id })))
		{
			Utils.DisplayHelpText($"Unable to summon {selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id})~s~.");
		}
	}

	private async void FreezeSelectedPlayer()
	{
		if ((int)StaffLevelScript.StaffLevel < 100 || selectedPlayerState == null)
		{
			return;
		}
		int playerFromServerId = API.GetPlayerFromServerId(selectedPlayerState.Id);
		if (playerFromServerId == -1)
		{
			Utils.DisplayHelpText("~r~Player is too far away.");
			return;
		}
		Player val = new Player(playerFromServerId);
		if (!(val == (Player)null) && !((Entity)(object)val.Character == (Entity)null))
		{
			Vector3 position = ((Entity)val.Character).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position) > 25f.Square()))
			{
				if (!Gtacnr.Utils.CheckTimePassed(lastFreezeTimestamp, 1000.0))
				{
					Utils.DisplayHelpText("~r~Wait before freezing another player.");
					return;
				}
				lastFreezeTimestamp = DateTime.UtcNow;
				if (!(await TriggerServerEventAsync<bool>("gtacnr:admin:freeze", new object[1] { selectedPlayerState.Id })))
				{
					Utils.DisplayHelpText($"Unable to freeze {selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id})~s~.");
				}
				return;
			}
		}
		Utils.DisplayHelpText("~r~Player is too far away.");
	}

	private async void ShowSelectedPlayerCrimes(Menu parentMenu)
	{
		if ((int)StaffLevelScript.StaffLevel >= 100 && selectedPlayerState != null)
		{
			await CrimeScript.Instance.OpenOtherPlayerCrimes(selectedPlayerState.Id, delegate(string error)
			{
				Utils.DisplayHelpText("~r~" + error);
			}, parentMenu);
		}
	}

	private async Coroutine<string> GetInputReason(string title, string message, string placeholder = "", int maxInputLength = 0, string type = "text")
	{
		string text = await Utils.GetUserInput(title, message, placeholder, maxInputLength, type);
		if (text == null)
		{
			return "";
		}
		text = text.Replace("  ", " ").Trim();
		string text2 = text;
		text = ((text == "/age") ? "This account has been banned on suspicion of being under the minimum age while not being allowed by a parent. If you are the parent or guardian of this user, please e-mail support@strazzullo.net and mention:\n- the account name (shown above)\n- the date of birth\n- declaration that they are allowed to play" : (text switch
		{
			"/wcrdm5" => "Do not use lethal force on suspects who are cuffed", 
			"/wcrdm4" => "Do not use lethal force on suspects who aren't a threat", 
			"/wcrdm3" => "Do not attack other cops", 
			"/wcrdm2" => "Do not attack innocent civilians", 
			"/wcrdm" => "Do not kill random players as a cop", 
			"/whrdm" => "Do not kill random players at hospitals", 
			"/wrdm" => "Do not kill random players", 
			"/wspam3" => "Do not be annoying on radio", 
			"/wspam2" => "Do not be annoying on voice chat", 
			"/wspam" => "Do not spam/flood the chat", 
			"/crdm5" => "RDM (Attacking Cuffed Suspects as a Cop)", 
			"/crdm4" => "RDM (Attacking Non-Threat as a Cop)", 
			"/crdm3" => "RDM (Attacking Cops as a Cop)", 
			"/crdm2" => "RDM (Attacking Civilians as a Cop)", 
			"/crdm" => "RDM (RDM as a Cop)", 
			"/erdm2" => "RDM (RDM as EMS)", 
			"/erdm" => "RDM (RDM as EMS)", 
			"/hunt" => "RDM (Cop Hunting)", 
			"/rrdm" => "RDM (Repeated RDM)", 
			"/mrdm" => "RDM (Mass RDM)", 
			"/cl" => "Combat Log", 
			"/ct" => "Cross-Teaming", 
			"/gp" => "Exploiting (Ghost Peeking)", 
			"/cuff" => "Exploiting (Shooting While Cuffed)", 
			"/star" => "Exploiting (Wanted Level System)", 
			"/spam3" => "Bad Conduct (Disturbing Radio)", 
			"/spam2" => "Bad Conduct (Disturbing Voice Chat)", 
			"/spam" => "Bad Conduct (Flooding Chat)", 
			"/sex" => "Bad Conduct (Sexual Harassment)", 
			"/suic" => "Bad Conduct (Suicide Encouragement)", 
			"/nat2" => "Bad Conduct (Xenophobic Slurs)", 
			"/nat" => "Bad Conduct (National Origin Discrimination)", 
			"/relig2" => "Bad Conduct (Blasphemy/Insulting Religion)", 
			"/relig" => "Bad Conduct (Religious Discrimination)", 
			"/disab2" => "Bad Conduct (Disability Slurs)", 
			"/disab" => "Bad Conduct (Disability Discrimination)", 
			"/homo2" => "Bad Conduct (Homophobic Slurs)", 
			"/homo" => "Bad Conduct (Sexual Orientation Discrimination)", 
			"/racism2" => "Bad Conduct (Racial Slurs)", 
			"/racism" => "Bad Conduct (Racial Discrimination)", 
			_ => text, 
		}));
		if (text2 != text)
		{
			text = await Utils.GetUserInput(title, message, placeholder, maxInputLength, type, text);
		}
		else if (text.StartsWith("/"))
		{
			return await GetInputReason(title, message.Substring(0, message.IndexOf("<br/>")) + "<br/>Invalid shortcut (" + text + ")", placeholder, maxInputLength, type);
		}
		return text;
	}

	private async void CopyUID()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			await Utils.GetUserInput("UID", selectedPlayerState.Name + "'s UID.", "", 200, "text", selectedPlayerState.Uid);
		}
	}

	private async void Warn()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			string text = await GetInputReason("Warn", "Enter a message for the player (full screen).", "", 200);
			if (text.Length < 3)
			{
				Utils.DisplayHelpText("~r~The warning message was too short!");
			}
			else if (await TriggerServerEventAsync<bool>("gtacnr:admin:sendWarning", new object[2] { selectedPlayerState.Id, text }))
			{
				Utils.DisplayHelpText($"{selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id}) ~s~has been warned!");
			}
			else
			{
				Utils.DisplayHelpText("~r~The warning couldn't be sent due to an error");
			}
		}
	}

	private async void Kill()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			string text = await GetInputReason("Detonate", "Enter a message for the player (only shows as a notification on the bottom left).", "", 200);
			if (text.Length < 3)
			{
				Utils.DisplayHelpText("~r~The reason was too short!");
			}
			else if (await TriggerServerEventAsync<bool>("gtacnr:admin:killPlayer", new object[2] { selectedPlayerState.Id, text }))
			{
				Utils.DisplayHelpText(selectedPlayerState.ColorNameAndId + " ~s~has been killed!");
			}
			else
			{
				Utils.DisplayHelpText("~r~The player couldn't be killed due to an error");
			}
		}
	}

	private async void CayoPerico()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			string text = await GetInputReason("Send to Cayo Perico", "Enter a message for the player (full screen)", "", 200);
			if (text.Length < 3)
			{
				Utils.DisplayHelpText("~r~The reason was too short!");
			}
			else if (await TriggerServerEventAsync<bool>("gtacnr:admin:cayoPerico", new object[2] { selectedPlayerState.Id, text }))
			{
				Utils.DisplayHelpText(selectedPlayerState.ColorNameAndId + " has been sent to Cayo Perico!");
			}
			else
			{
				Utils.DisplayHelpText("~r~The player couldn't be sent to Cayo Perico due to an error");
			}
		}
	}

	private async void Fine()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		int maxFine = (((int)StaffLevelScript.StaffLevel >= 115) ? 5000000 : (((int)StaffLevelScript.StaffLevel >= 110) ? 2500000 : 1000000));
		if (!int.TryParse(await Utils.GetUserInput("Fine", "Enter the fine amount (" + 1.ToCurrencyString() + " to " + maxFine.ToCurrencyString() + ")", "", 10), out var amount))
		{
			Utils.DisplayHelpText("~r~The fine amount must be numeric!");
			return;
		}
		if (amount < 1 || amount > maxFine)
		{
			Utils.DisplayHelpText("~r~The fine amount must be between " + 1.ToCurrencyString() + " and " + maxFine.ToCurrencyString() + "!");
			return;
		}
		string text = await GetInputReason("Fine", "Enter a message for the player (full screen)", "", 200);
		if (text.Length < 3)
		{
			Utils.DisplayHelpText("~r~The reason was too short!");
		}
		else if (await TriggerServerEventAsync<bool>("gtacnr:admin:fine", new object[3] { selectedPlayerState.Id, amount, text }))
		{
			Utils.DisplayHelpText($"{selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id}) ~s~has been fined {amount.ToCurrencyString()}!");
		}
		else
		{
			Utils.DisplayHelpText("~r~The fine couldn't be given due to an error");
		}
	}

	private async void XPFine()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		if (!int.TryParse(await Utils.GetUserInput("XP Fine", $"Enter an XP fine amount ({5}XP to {100}XP)", "", 3), out var amount))
		{
			Utils.DisplayHelpText("~r~The XP fine amount must be numeric!");
			return;
		}
		if (amount < 5 || amount > 100)
		{
			Utils.DisplayHelpText($"~r~The XP fine amount must be between {5}XP and {100}XP!");
			return;
		}
		string text = await GetInputReason("XP Fine", "Enter a message for the player (full screen).", "", 200);
		if (text.Length < 3)
		{
			Utils.DisplayHelpText("~r~The reason was too short!");
		}
		else if (await TriggerServerEventAsync<bool>("gtacnr:admin:xpfine", new object[3] { selectedPlayerState.Id, amount, text }))
		{
			Utils.DisplayHelpText($"{selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id}) ~s~has been fined {amount} XP!");
		}
		else
		{
			Utils.DisplayHelpText("~r~The fine couldn't be given due to an error");
		}
	}

	private async void Kick()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			string text = await GetInputReason("Kick", "Enter a message for the player.", "", 200);
			if (text.Length < 3)
			{
				Utils.DisplayHelpText("~r~The reason was too short!");
			}
			else if (await TriggerServerEventAsync<bool>("gtacnr:admin:kick", new object[2] { selectedPlayerState.Id, text }))
			{
				Utils.DisplayHelpText($"{selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id}) ~s~has been kicked!");
			}
			else
			{
				Utils.DisplayHelpText("~r~The player couldn't be kicked due to an error");
			}
		}
	}

	private async void JobBan()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		if (!selectedPlayerState.JobEnum.IsPublicService())
		{
			Utils.DisplayHelpText("~r~The player has to be on a public service job (police, paramedic).");
			return;
		}
		int maxDays = 0;
		switch (StaffLevelScript.StaffLevel)
		{
		case StaffLevel.TrialModerator:
			maxDays = 14;
			break;
		case StaffLevel.Moderator:
			maxDays = 30;
			break;
		case StaffLevel.LeadModerator:
			maxDays = 90;
			break;
		}
		string text = "";
		if (maxDays > 0)
		{
			text = $" (1-{maxDays})";
		}
		Job jobData = Gtacnr.Data.Jobs.GetJobData(selectedPlayerState.Job);
		if (!int.TryParse(await Utils.GetUserInput("Job ban (" + jobData.Name + ")", "Enter a number of days" + text + ".", "", 9), out var days))
		{
			Utils.DisplayHelpText("~r~The number of days must be numeric!");
			return;
		}
		if (days < 1 || (days > maxDays && maxDays > 0))
		{
			Utils.DisplayHelpText($"~r~The number of days must be between 1 and {maxDays}!");
			return;
		}
		string text2 = await GetInputReason("Job ban", "Enter a description of the broken rules (max 500 characters).", "", 500);
		if (text2.Length < 3)
		{
			Utils.DisplayHelpText("~r~The reason was too short!");
		}
		else if (await TriggerServerEventAsync<bool>("gtacnr:admin:jobBan", new object[3] { selectedPlayerState.Id, days, text2 }))
		{
			Utils.DisplayHelpText($"{selectedPlayerState.ColorTextCode}{selectedPlayerState.Name} ({selectedPlayerState.Id}) " + $"~s~has been banned from the {selectedPlayerState.Job} for {days} days!");
		}
		else
		{
			Utils.DisplayHelpText("~r~The player couldn't be job-banned due to an error");
		}
	}

	private async void Ban()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		int maxDays = 0;
		switch (StaffLevelScript.StaffLevel)
		{
		case StaffLevel.TrialModerator:
			maxDays = 14;
			break;
		case StaffLevel.Moderator:
			maxDays = 30;
			break;
		case StaffLevel.LeadModerator:
			maxDays = 90;
			break;
		}
		string text = "";
		if (maxDays > 0)
		{
			text = $" (1-{maxDays})";
		}
		if (!int.TryParse(await Utils.GetUserInput("Ban", "Enter a number of days" + text + ".", "", 9), out var days))
		{
			Utils.DisplayHelpText("~r~The number of days must be numeric!");
			return;
		}
		if (days < 1 || (days > maxDays && maxDays > 0))
		{
			Utils.DisplayHelpText($"~r~The number of days must be between 1 and {maxDays}!");
			return;
		}
		string text2 = await GetInputReason("Ban", "Enter a description of the broken rules (max 500 characters).", "", 500);
		if (text2.Length < 3)
		{
			Utils.DisplayHelpText("~r~The reason was too short!");
			return;
		}
		int num = await TriggerServerEventAsync<int>("gtacnr:admin:ban", new object[3] { selectedPlayerState.Id, days, text2 });
		switch (num)
		{
		case 1:
			Utils.DisplayHelpText($"{selectedPlayerState.ColorNameAndId} has been ~r~banned ~s~for {days} days!");
			break;
		case 6:
			Utils.DisplayHelpText(selectedPlayerState.ColorNameAndId + " is a ~g~moderator~s~!");
			break;
		case 9:
			Utils.DisplayHelpText(selectedPlayerState.ColorNameAndId + " is ~y~already banned~s~!");
			break;
		default:
			Utils.DisplayErrorMessage(255, num);
			break;
		}
	}

	private async void Mute()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		int num = 0;
		switch (StaffLevelScript.StaffLevel)
		{
		case StaffLevel.TrialModerator:
			num = 14;
			break;
		case StaffLevel.Moderator:
			num = 30;
			break;
		case StaffLevel.LeadModerator:
			num = 90;
			break;
		}
		string text = "";
		if (num > 0)
		{
			text = $" (1m-{num}d)";
		}
		string amountStr = await Utils.GetUserInput("Mute", "Enter a duration " + text + ".", "", 9);
		string text2 = await GetInputReason("Mute", "Enter a description of the broken rules (max 500 characters).", "", 500);
		if (text2.Length < 3)
		{
			Utils.DisplayHelpText("~r~The reason was too short!");
			return;
		}
		TimeSpan duration;
		try
		{
			duration = Gtacnr.Utils.ParseTimeSpan(amountStr, Constants.Mutes.VALID_TIME_UNITS);
		}
		catch (Exception ex)
		{
			Utils.DisplayErrorMessage(250, 0, ex.Message);
			return;
		}
		int num2 = await TriggerServerEventAsync<int>("gtacnr:admin:mute", new object[3] { selectedPlayerState.Id, duration.Ticks, text2 });
		switch (num2)
		{
		case 1:
			Utils.DisplayHelpText(selectedPlayerState.ColorNameAndId + " has been ~r~muted ~s~for " + Gtacnr.Utils.FormatTimeSpanString(duration) + "!");
			break;
		case 6:
			Utils.DisplayHelpText(selectedPlayerState.ColorNameAndId + " is a ~g~moderator~s~!");
			break;
		case 9:
			Utils.DisplayHelpText(selectedPlayerState.ColorNameAndId + " is ~y~already muted~s~!");
			break;
		default:
			Utils.DisplayErrorMessage(255, num2);
			break;
		}
	}

	private async void ToggleDuty()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		if (!Gtacnr.Utils.CheckTimePassed(DamagedScript.LastDamageReceivedTimestamp, 10000.0))
		{
			Utils.DisplayHelpText("~r~You cannot go on duty if you've been recently damaged!");
			return;
		}
		IsOnDuty = await TriggerServerEventAsync<bool>("gtacnr:admin:toggleDuty", new object[0]);
		if (IsOnDuty)
		{
			Utils.DisplayHelpText("~g~You are now on moderation duty!");
			menuItems["duty"].Label = "~g~ON";
		}
		else
		{
			Utils.DisplayHelpText("~g~You are ~r~no longer ~g~on moderation duty!");
			menuItems["duty"].Label = "~r~OFF";
			if (Game.PlayerPed.Weapons.HasWeapon((WeaponHash)(-1355376991)))
			{
				Game.PlayerPed.Weapons.Remove((WeaponHash)(-1355376991));
				Game.PlayerPed.Weapons.Remove((WeaponHash)1198256469);
				Game.PlayerPed.Weapons.Remove((WeaponHash)(-1238556825));
			}
		}
		((Entity)Game.PlayerPed).IsInvincible = IsOnDuty;
		BaseScript.TriggerEvent("gtacnr:hud:toggleAdminDuty", new object[1] { IsOnDuty });
	}

	private void ToggleNoClip()
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected I4, but got Unknown
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			return;
		}
		if (!NoClipScript.IsNoClipActive && (Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed)
			{
				Utils.DisplayHelpText("~r~You cannot enable noclip when you're not the driver of a vehicle.", playSound: false);
				return;
			}
			if (Game.PlayerPed.CurrentVehicle.PassengerCount > 0)
			{
				Utils.DisplayHelpText("~r~You cannot enable noclip when you have passengers.", playSound: false);
				return;
			}
		}
		NoClipScript.IsNoClipActive = !NoClipScript.IsNoClipActive;
		if (NoClipScript.IsNoClipActive)
		{
			noclipEnableTimestamp = DateTime.UtcNow;
			noclipEnablePosition = ((Entity)Game.PlayerPed).Position;
			if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
			{
				noclipEnableVehicle = (VehicleHash)((Entity)Game.PlayerPed.CurrentVehicle).Model.Hash;
			}
			Utils.DisplayHelpText("~g~You enabled noclip.", playSound: false);
			menuItems["noclip"].Label = "~g~ON";
			isVisible = false;
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = ((Vector3)(ref noclipEnablePosition)).DistanceToSquared(position);
		if (Gtacnr.Utils.CheckTimePassed(noclipEnableTimestamp, 2000.0) && num > 10000f)
		{
			string locationName = Utils.GetLocationName(noclipEnablePosition);
			string locationName2 = Utils.GetLocationName(position);
			string text = "";
			if ((int)noclipEnableVehicle != 0)
			{
				text = Game.GetGXTEntry(API.GetDisplayNameFromVehicleModel((uint)(int)noclipEnableVehicle));
			}
			BaseScript.TriggerServerEvent("gtacnr:admin:usedNoClip", new object[5]
			{
				noclipEnablePosition.Json(),
				position.Json(),
				locationName,
				locationName2,
				text
			});
		}
		noclipEnableTimestamp = default(DateTime);
		noclipEnablePosition = default(Vector3);
		noclipEnableVehicle = (VehicleHash)0;
		Utils.DisplayHelpText("~g~You disabled noclip.", playSound: false);
		isVisible = true;
		menuItems["noclip"].Label = "~r~OFF";
	}

	private async void ToggleStreamingMode()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			StreamingMode = !StreamingMode;
			await TriggerServerEventAsync<int>("gtacnr:admin:toggleStreamingMode", new object[1] { StreamingMode });
			if (StreamingMode)
			{
				Utils.DisplayHelpText("~g~You enabled streaming mode.", playSound: false);
				menuItems["streaming"].Label = "~g~ON";
			}
			else
			{
				Utils.DisplayHelpText("~g~You disabled streaming mode.", playSound: false);
				menuItems["streaming"].Label = "~r~OFF";
			}
		}
	}

	private async void ToggleGhostMode()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			switch (await TriggerServerEventAsync<int>("gtacnr:admin:toggleGhostMode", new object[0]))
			{
			case 1:
				Utils.DisplayHelpText("~r~You're not authorized to use this feature.");
				break;
			case 2:
				Utils.DisplayHelpText("You ~g~enabled ~s~ghost mode.", playSound: false);
				BaseScript.TriggerEvent("gtacnr:hud:toggleAdminGhost", new object[1] { true });
				menuItems["ghostMode"].Label = "~g~ON";
				IsInGhostMode = true;
				break;
			case 3:
				Utils.DisplayHelpText("You ~r~disabled ~s~ghost mode.", playSound: false);
				BaseScript.TriggerEvent("gtacnr:hud:toggleAdminGhost", new object[1] { false });
				menuItems["ghostMode"].Label = "~r~OFF";
				IsInGhostMode = false;
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
				break;
			}
		}
	}

	private async void ToggleUndercoverMode()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			switch (await TriggerServerEventAsync<int>("gtacnr:admin:toggleUndercoverMode", new object[0]))
			{
			case 1:
				Utils.DisplayHelpText("~r~You're not authorized to use this feature.");
				break;
			case 2:
				Utils.DisplayHelpText("You ~g~enabled ~s~undercover mode.", playSound: false);
				BaseScript.TriggerEvent("gtacnr:hud:toggleAdminUndercover", new object[1] { true });
				menuItems["undercoverMode"].Label = "~g~ON";
				IsInUndercoverMode = true;
				break;
			case 3:
				Utils.DisplayHelpText("You ~r~disabled ~s~undercover mode.", playSound: false);
				BaseScript.TriggerEvent("gtacnr:hud:toggleAdminUndercover", new object[1] { false });
				menuItems["undercoverMode"].Label = "~r~OFF";
				IsInUndercoverMode = false;
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
				break;
			}
		}
	}

	private async void ToggleFakeName()
	{
		if (!isFakeUsernameActive)
		{
			string newName = await Utils.GetUserInput("Fake username", "Enter your new fake username.", "", 30);
			if (string.IsNullOrWhiteSpace(newName))
			{
				Utils.DisplayHelpText("~r~You must enter a new fake username.");
				return;
			}
			if (newName.Contains("|") || newName.Contains("[") || newName.Contains("]"))
			{
				Utils.DisplayHelpText("~r~Your name cannot contain crew tags.");
				return;
			}
			if (newName.ToLowerInvariant().Contains("sasino"))
			{
				Utils.DisplayHelpText("~r~Your name cannot contain that word.");
				return;
			}
			int num = random.Next(1910, 9500);
			switch (await TriggerServerEventAsync<int>("gtacnr:admin:setFakeUsernameAndXP", new object[2] { newName, num }))
			{
			case 1:
				Utils.DisplayHelpText("~r~You're not authorized to use this feature.");
				break;
			case 2:
				Utils.DisplayHelpText("~r~You can't use the username of a registered user.");
				break;
			case 3:
				Utils.DisplayHelpText("~g~Your fake username has been set to ~s~" + newName + "~g~.", playSound: false);
				BaseScript.TriggerEvent("gtacnr:hud:setAdminFakeName", new object[1] { newName });
				BaseScript.TriggerEvent("gtacnr:hud:toggleAdminFakeName", new object[1] { true });
				isFakeUsernameActive = true;
				menuItems["fakeIdentity"].Label = newName;
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
				break;
			}
		}
		else if (await TriggerServerEventAsync<bool>("gtacnr:admin:resetFakeUsernameAndXP", new object[0]))
		{
			isFakeUsernameActive = false;
			Utils.DisplayHelpText("~g~Your fake username has been reset.", playSound: false);
			BaseScript.TriggerEvent("gtacnr:hud:toggleAdminFakeName", new object[1] { false });
			menuItems["fakeIdentity"].Label = "~r~OFF";
		}
		else
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
		}
	}

	private void ToggleInvisible()
	{
		isVisible = !isVisible;
		API.SetEntityVisible(((PoolObject)Game.PlayerPed).Handle, isVisible, false);
		if (!isVisible)
		{
			Utils.DisplayHelpText("~g~You are invisible.", playSound: false);
			menuItems["invisibleMode"].Label = "~g~ON";
		}
		else
		{
			Utils.DisplayHelpText("~g~You are visible.", playSound: false);
			menuItems["invisibleMode"].Label = "~r~OFF";
		}
	}

	[Update]
	private async Coroutine ControlTick()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100 && !((Entity)(object)Game.PlayerPed == (Entity)null) && ((Entity)Game.PlayerPed).Health > 0 && Utils.IsUsingKeyboard())
		{
			if (API.IsControlJustPressed(0, 121))
			{
				OpenPlayersMenu();
			}
			else if (API.IsControlJustPressed(0, 214))
			{
				OpenToolsMenu();
			}
			else if (API.IsControlJustPressed(0, 212))
			{
				menus["options"].Visible = false;
				await Script.Wait(100);
				ToggleNoClip();
			}
			else if (API.IsControlJustPressed(0, 316))
			{
				TeleportMenuScript.Instance.SavePos();
			}
			else if (API.IsControlJustPressed(0, 317))
			{
				TeleportMenuScript.Instance.GotoPos();
			}
		}
	}

	private void OpenPlayersMenu()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			if (!menus["players"].Visible)
			{
				RefreshPlayersMenu();
				menus["players"].ResetFilter();
			}
			menus["players"].Visible = !menus["players"].Visible;
		}
	}

	private void OpenToolsMenu()
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			menus["options"].Visible = !menus["options"].Visible;
		}
	}

	private async Task EnterIdManually()
	{
		string input = await Utils.GetUserInput("Search", "Player id or part of their name (only connected players).", "", 23);
		int targetId;
		bool isId = int.TryParse(input, out targetId);
		if (string.IsNullOrWhiteSpace(input))
		{
			menus["players"].ResetFilter();
		}
		else
		{
			menus["players"].FilterMenuItems(delegate(MenuItem item)
			{
				if (item.ItemData is PlayerState playerState)
				{
					if (isId)
					{
						return playerState.Id == targetId;
					}
					if (!(playerState.Id.ToString() == input))
					{
						return playerState.Name.ToLowerInvariant().Contains(input.ToLowerInvariant());
					}
					return true;
				}
				return false;
			});
		}
		Utils.PlaySelectSound();
	}

	[EventHandler("gtacnr:reportTaken")]
	private void OnReportTaken(string reportId, int takerId)
	{
		if (!menus.ContainsKey("pendingReports"))
		{
			return;
		}
		foreach (MenuItem menuItem in menus["pendingReports"].GetMenuItems())
		{
			if (menuItem.ItemData is Report report && report.Id == reportId)
			{
				menus["pendingReports"].RemoveMenuItem(menuItem);
				break;
			}
		}
	}

	[EventHandler("gtacnr:reportClosed")]
	private void OnReportClosed(int closerId, string username)
	{
		Utils.DisplayHelpText("~g~" + username + " ~s~has responded to your report. View your reports in " + MainMenuScript.OpenMenuControlString + " ~y~Menu ~s~> ~y~Help ~s~> ~y~Reports~s~.");
	}

	[EventHandler("gtacnr:admin:gotSummoned")]
	private async void OnGotSummoned(int playerId)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState != null)
		{
			await Utils.TeleportToCoords(playerState.Position, -1f, Utils.TeleportFlags.TeleportVehicle | Utils.TeleportFlags.VisualEffects);
			Utils.DisplayHelpText($"~g~Moderator {playerState.Name} ({playerState.Id}) ~s~summoned you.");
		}
	}

	[EventHandler("gtacnr:admin:gotFrozen")]
	private async void OnGotFrozen(int playerId, bool shouldFreeze)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState != null)
		{
			if (shouldFreeze)
			{
				Utils.Freeze(freeze: true, freezeCamControls: false);
			}
			else
			{
				Utils.Freeze(freeze: false, freezeCamControls: false);
			}
			string arg = (shouldFreeze ? "froze" : "unfroze");
			Utils.DisplayHelpText($"~g~Moderator {playerState.Name} ({playerState.Id}) ~s~{arg} you.");
		}
	}

	[EventHandler("gtacnr:admin:gotExploded")]
	private async void OnGotExploded(string reason)
	{
		DeathScript.HasSpawnProtection = false;
		((Entity)Game.PlayerPed).IsInvincible = false;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		((Entity)Game.PlayerPed).Position = new Vector3(position.X, position.Y, position.Z + 20f);
		await BaseScript.Delay(100);
		position = ((Entity)Game.PlayerPed).Position;
		API.AddExplosion(position.X, position.Y, position.Z, 1, 1f, true, false, 1f);
		((Entity)Game.PlayerPed).Health = 0;
		Utils.SendNotification("~g~A moderator ~s~detonated you. Reason: " + reason + ".");
	}

	[EventHandler("gtacnr:admin:gotCayoPericoed")]
	private async void OnCayoPericoed(string reason)
	{
		Vector4 val = default(Vector4);
		((Vector4)(ref val))._002Ector(5154.1313f, -5138.44f, 2.3162f, 263.5233f);
		Task alertT = Utils.ShowAlert(reason + "\nYou have been sent to the deathmatch island by a moderator.\n\nRead the rules at " + ExternalLinks.Collection.Rules, "~r~moderator action");
		await Utils.TeleportToCoords(val.XYZ(), val.W, Utils.TeleportFlags.VisualEffects);
		await alertT;
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, "You've been sent here by a moderator, RDM is allowed on this island. When you're ready to follow the rules, get a boat or a plane and go back to San Andreas.");
	}

	[EventHandler("gtacnr:admin:entitiesCleared")]
	private void OnEntitiesCleared(int entityType)
	{
		switch (entityType)
		{
		case 1:
		{
			Ped[] allPeds = World.GetAllPeds();
			foreach (Ped val3 in allPeds)
			{
				if (API.NetworkHasControlOfEntity(((PoolObject)val3).Handle) && !val3.IsPlayer)
				{
					((PoolObject)val3).Delete();
				}
			}
			Utils.SendNotification("An ~g~admin ~s~has cleared peds in the area.");
			break;
		}
		case 2:
		{
			Vehicle[] allVehicles = World.GetAllVehicles();
			foreach (Vehicle val2 in allVehicles)
			{
				if (API.NetworkHasControlOfEntity(((PoolObject)val2).Handle) && !((Entity)(object)val2 == (Entity)(object)Game.PlayerPed.CurrentVehicle) && !((Entity)(object)val2 == (Entity)(object)Game.PlayerPed.LastVehicle) && !((Entity)(object)val2 == (Entity)(object)TruckerJobScript.DeliveryVehicle))
				{
					((PoolObject)val2).Delete();
				}
			}
			Utils.SendNotification("An ~g~admin ~s~has cleared vehicles in the area.");
			break;
		}
		case 3:
		{
			Prop[] allProps = World.GetAllProps();
			foreach (Prop val in allProps)
			{
				if (API.NetworkHasControlOfEntity(((PoolObject)val).Handle) && API.IsEntityAMissionEntity(((PoolObject)val).Handle))
				{
					((PoolObject)val).Delete();
				}
			}
			Utils.SendNotification("An ~g~admin ~s~has cleared props in the area.");
			break;
		}
		}
	}

	[Command("gotocoords")]
	private async void GotoCoords(string[] args)
	{
		if ((int)StaffLevelScript.StaffLevel >= 100)
		{
			if (args.Length < 3)
			{
				Chat.AddMessage(Gtacnr.Utils.Colors.Warning, "Usage: /gotocoords [x] [y] [z]");
			}
			else
			{
				await Utils.TeleportToCoords(new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2])), -1f, Utils.TeleportFlags.None, 0);
			}
		}
	}

	[EventHandler("gtacnr:admin:dispatch")]
	private void OnCrimeDispatch(int suspectId, string crimeJson)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (suspectId != 0 && !string.IsNullOrEmpty(crimeJson))
			{
				Gtacnr.Model.Crime crime = crimeJson.Unjson<Gtacnr.Model.Crime>();
				CrimeType definition = Gtacnr.Data.Crimes.GetDefinition(crime.CrimeType);
				PlayerState playerState = LatentPlayers.Get(suspectId);
				Vector3 location = crime.Location;
				((Vector3)(ref location)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
				string locationName = Utils.GetLocationName(crime.Location);
				Utils.SendNotification("~g~" + playerState.NameAndId + " committed " + definition.Description + " in " + locationName + ".");
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}
}
