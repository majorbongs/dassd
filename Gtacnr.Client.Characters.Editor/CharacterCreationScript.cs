using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.HUD;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using NativeUI;
using Newtonsoft.Json;

namespace Gtacnr.Client.Characters.Editor;

public class CharacterCreationScript : Script
{
	private static CharacterCreationScript instance;

	private const string creatorTempCharKey = "char_creator_v8";

	private const string editorTempCharKey = "char_editor_v8";

	private Character editingCharacter;

	private MenuPool menuPool;

	private UIMenu mainMenu;

	private UIMenu heritageMenu;

	private UIMenu featuresMenu;

	private UIMenu appearanceMenu;

	private UIMenu apparelMenu;

	private Dictionary<string, UIMenuItem> menuItems = new Dictionary<string, UIMenuItem>();

	private UIMenuHeritageWindow heritageWindow;

	private Dictionary<Sex, List<EditorParent>> parentInfo = Gtacnr.Utils.LoadJson<Dictionary<Sex, List<EditorParent>>>("data/parents.json");

	private bool isInCreator;

	private bool isAwaitingCreation;

	private int camera1;

	private int camera2;

	private int dad;

	private int mom;

	private float headShape;

	private float skinTone;

	private string selectedOutfit;

	private string selectedHat;

	private string selectedGlasses;

	private string selectedWatch;

	private bool isEditMode;

	private Vector3 cam1Coords;

	private Vector3 cam2Coords;

	private Vector3 cam3Coords;

	private Vector4 pedCoords;

	private bool detailZoomInProgress;

	public static bool IsInCreator => instance.isInCreator;

	public CharacterCreationScript()
	{
		instance = this;
	}

	private async Coroutine MenuTick()
	{
		if (isInCreator && (Entity)(object)Game.PlayerPed != (Entity)null)
		{
			if (menuPool != null)
			{
				menuPool.ProcessMenus();
			}
			if (Game.IsControlJustPressed(2, (Control)205))
			{
				Game.PlayerPed.Task.LookAt(API.GetOffsetFromEntityInWorldCoords(((PoolObject)Game.PlayerPed).Handle, 1.2f, 0.5f, 0.7f), 1100);
			}
			else if (Game.IsControlJustPressed(2, (Control)206))
			{
				Game.PlayerPed.Task.LookAt(API.GetOffsetFromEntityInWorldCoords(((PoolObject)Game.PlayerPed).Handle, -1.2f, 0.5f, 0.7f), 1100);
			}
			else if (Game.IsControlJustReleased(2, (Control)205) || Game.IsControlJustReleased(2, (Control)206))
			{
				Game.PlayerPed.Task.LookAt(API.GetOffsetFromEntityInWorldCoords(((PoolObject)Game.PlayerPed).Handle, 0f, 0.5f, 0.7f), 1100);
			}
		}
	}

	private async Coroutine ConcealTick()
	{
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player.Handle != API.PlayerId())
			{
				API.NetworkConcealPlayer(player.Handle, true, true);
			}
		}
		await Script.Wait(200);
	}

	private void AttachTasks()
	{
		base.Update += MenuTick;
		base.Update += ConcealTick;
	}

	private void DetachTasks()
	{
		base.Update -= MenuTick;
		base.Update -= ConcealTick;
	}

	private void StopConceal()
	{
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player.Handle != API.PlayerId())
			{
				API.NetworkConcealPlayer(player.Handle, false, false);
			}
		}
	}

	private void ConcealTestCommand(string[] args)
	{
		if (args.Length != 0 && args[0] == "true")
		{
			base.Update += ConcealTick;
			Chat.AddMessage("Concealing");
		}
		else
		{
			base.Update -= ConcealTick;
			Chat.AddMessage("Stopped concealing");
			StopConceal();
		}
	}

	private void ResetTemporary()
	{
		API.DeleteResourceKvp(isEditMode ? "char_editor_v8" : "char_creator_v8");
	}

	private void SaveTemporary()
	{
		string obj = (isEditMode ? "char_editor_v8" : "char_creator_v8");
		ApplyCharacterData();
		TempCharacterData obj2 = new TempCharacterData
		{
			Character = editingCharacter,
			Dad = dad,
			Mom = mom,
			HeadShape = headShape,
			SkinTone = skinTone,
			SelectedOutfit = selectedOutfit,
			SelectedGlasses = selectedGlasses,
			SelectedHat = selectedHat,
			SelectedWatch = selectedWatch
		};
		API.SetResourceKvp(obj, obj2.Json());
	}

	private Character LoadTemporary()
	{
		string resourceKvpString = API.GetResourceKvpString(isEditMode ? "char_editor_v8" : "char_creator_v8");
		if (string.IsNullOrEmpty(resourceKvpString))
		{
			return null;
		}
		TempCharacterData tempCharacterData;
		try
		{
			tempCharacterData = resourceKvpString.Unjson<TempCharacterData>();
		}
		catch (Exception exception)
		{
			Print("Unable to load temporary character data: " + resourceKvpString);
			Gtacnr.Utils.PrintException(exception);
			return null;
		}
		if (tempCharacterData == null)
		{
			return null;
		}
		dad = tempCharacterData.Dad;
		mom = tempCharacterData.Mom;
		headShape = tempCharacterData.HeadShape;
		skinTone = tempCharacterData.SkinTone;
		selectedOutfit = tempCharacterData.SelectedOutfit;
		selectedGlasses = tempCharacterData.SelectedGlasses;
		selectedHat = tempCharacterData.SelectedHat;
		selectedWatch = tempCharacterData.SelectedWatch;
		return tempCharacterData.Character;
	}

	[EventHandler("gtacnr:characters:enterCreationMode")]
	private async void EnterCreationMode()
	{
		_ = 4;
		try
		{
			BaseScript.TriggerServerEvent("gtacnr:characters:characterCreationEntered", new object[0]);
			editingCharacter = new Character();
			isEditMode = false;
			Utils.Unblur(1);
			await BaseScript.Delay(10);
			if (!API.IsScreenFadedOut())
			{
				await Utils.FadeOut();
			}
			if (Utils.IsSwitchInProgress())
			{
				await Utils.SwitchIn();
			}
			MainScript.DestroyDefaultCamera();
			API.DisplayHud(true);
			API.DisplayRadar(false);
			HideHUDScript.ToggleChat(toggle: false, showMessage: false);
			CreateMenu();
			await CreateScene();
			await BaseScript.Delay(3000);
			OpenMenu();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[EventHandler("gtacnr:characters:enterEditMode")]
	private async void EnterEditMode()
	{
		_ = 3;
		try
		{
			editingCharacter = await Gtacnr.Client.API.Characters.GetActiveCharacter();
			isEditMode = true;
			SpawnScript.HasSpawned = false;
			DeathScript.IsAlive = null;
			if (!API.IsScreenFadedOut())
			{
				await Utils.FadeOut();
			}
			API.DisplayHud(true);
			API.DisplayRadar(false);
			HideHUDScript.ToggleChat(toggle: false, showMessage: false);
			CreateMenu();
			await CreateScene();
			await BaseScript.Delay(3000);
			OpenMenu();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private async Task CreateScene()
	{
		cam1Coords = new Vector3(-798.0264f, 330.9363f, 190.601f);
		cam2Coords = new Vector3(-798.0132f, 329.8286f, 191.001f);
		cam3Coords = new Vector3(-797.8962f, 327.9835f, 191.321f);
		pedCoords = new Vector4(-797.8967f, 326.9011f, 190.701f, 0f);
		isInCreator = true;
		AttachTasks();
		API.DestroyAllCams(true);
		camera1 = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", cam1Coords.X, cam1Coords.Y, cam1Coords.Z, 0f, 0f, 180f, 30f, false, 0);
		API.PointCamAtCoord(camera1, pedCoords.X, pedCoords.Y, pedCoords.Z);
		API.SetCamActive(camera1, true);
		API.RenderScriptCams(true, false, 2000, true, true);
		Character character = LoadTemporary();
		if (editingCharacter.Appearance == null && character == null)
		{
			await CreateRandomCharacter();
		}
		else
		{
			if (character != null)
			{
				editingCharacter.Appearance = character.Appearance;
				editingCharacter.Apparel = character.Apparel;
			}
			await LoadSavedCharacter(editingCharacter);
			RecreateMenuItems();
			ApplyCharacterInfoToMenu();
		}
		AntiTeleportScript.JustTeleported();
		((Entity)Game.PlayerPed).PositionNoOffset = pedCoords.XYZ();
		((Entity)Game.PlayerPed).Heading = pedCoords.W;
		Utils.Freeze();
		await BaseScript.Delay(4000);
		camera2 = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", cam2Coords.X, cam2Coords.Y, cam2Coords.Z, 0f, 0f, 180f, 30f, false, 0);
		API.PointCamAtCoord(camera2, pedCoords.X, pedCoords.Y, pedCoords.Z);
		API.SetCamActiveWithInterp(camera1, camera2, 4000, 1, 1);
		await BaseScript.Delay(200);
		LoadingPrompt.Hide();
		await Utils.FadeIn();
		Utils.Unblur(1);
	}

	private async void ZoomCameraIn()
	{
		while (detailZoomInProgress)
		{
			await BaseScript.Delay(0);
		}
		detailZoomInProgress = true;
		camera2 = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", cam3Coords.X, cam3Coords.Y, cam3Coords.Z, 0f, 0f, 180f, 30f, false, 0);
		API.SetCamActiveWithInterp(camera2, camera1, 500, 1, 1);
		await BaseScript.Delay(500);
		detailZoomInProgress = false;
	}

	private async void ZoomCameraOut()
	{
		while (detailZoomInProgress)
		{
			await BaseScript.Delay(0);
		}
		detailZoomInProgress = true;
		API.SetCamActiveWithInterp(camera1, camera2, 500, 1, 1);
		await BaseScript.Delay(500);
		detailZoomInProgress = false;
	}

	private void DeleteScene()
	{
		isInCreator = false;
		DetachTasks();
		StopConceal();
		API.SetCamActive(camera1, false);
		API.SetCamActive(camera1, false);
		API.DestroyCam(camera1, false);
		API.DestroyCam(camera2, false);
		API.RenderScriptCams(false, false, 0, true, false);
	}

	private async Task CreateRandomCharacter()
	{
		await LoadMpPed(editingCharacter);
		Apparel.ClearFromPed(Game.PlayerPed);
		if (Clothes.CurrentApparel == null)
		{
			Clothes.CurrentApparel = Apparel.GetUnderwear();
		}
		PrefabAppearance randomPrefabFace = Utils.GetRandomPrefabFace(editingCharacter.Sex);
		Utils.ApplyAppearance(Game.PlayerPed, randomPrefabFace);
		if (isEditMode)
		{
			Clothes.CurrentApparel = new Apparel("surgery_outfit");
		}
		dad = randomPrefabFace.Heritage.Father;
		mom = randomPrefabFace.Heritage.Mother;
		headShape = randomPrefabFace.Heritage.ShapeMix;
		skinTone = randomPrefabFace.Heritage.SkinMix;
	}

	private async Task LoadSavedCharacter(Character character)
	{
		await LoadMpPed(character);
		Utils.ApplyAppearance(Game.PlayerPed, character.Appearance);
		Clothes.CurrentApparel = character?.Apparel;
		if (isEditMode)
		{
			Clothes.CurrentApparel = new Apparel("surgery_outfit");
		}
	}

	private async Task LoadMpPed(Character character)
	{
		Model model = Model.op_Implicit((character.Sex == Sex.Male) ? Utils.FreemodeMale : Utils.FreemodeFemale);
		if (((Entity)Game.PlayerPed).Model != model)
		{
			Print("Reloading MP ped");
			int attempts = 1;
			while (!(await Game.Player.ChangeModel(model)) && attempts <= 10)
			{
				Print($"Unable to load ped model '{model}' (attempt {attempts}/10)");
				attempts++;
				await BaseScript.Delay(500);
			}
		}
	}

	private void UpdateCharacterHeritage()
	{
		API.SetPedHeadBlendData(API.PlayerPedId(), mom, dad, 0, mom, dad, 0, headShape, skinTone, 0f, true);
	}

	private void CreateMenu()
	{
		menuPool = new MenuPool();
		mainMenu = new UIMenu("Cops and Robbers", "                       ~b~discord.gg/cnr");
		menuPool.Add(mainMenu);
		menuItems = new Dictionary<string, UIMenuItem>();
		menuItems["sex"] = new UIMenuListItem("Sex", new List<object> { "Male", "Female" }, 0, "Set your character's sex. Click to randomize.\n~y~Warning: ~s~if you change or select this option you will lose your current customization.");
		menuItems["save"] = new UIMenuItem("~g~Continue", "Save your character and continue.");
		menuItems["leave"] = new UIMenuItem("~r~Exit without saving", "Exit the menu ~r~without saving ~s~your character!");
		mainMenu.AddItem(menuItems["sex"]);
		heritageMenu = menuPool.AddSubMenu(mainMenu, "Heritage", "Set your character's parents.");
		featuresMenu = menuPool.AddSubMenu(mainMenu, "Features", "Set the facial features of your character.");
		appearanceMenu = menuPool.AddSubMenu(mainMenu, "Appearance", "Set the appearance of your character.");
		if (!isEditMode)
		{
			apparelMenu = menuPool.AddSubMenu(mainMenu, "Apparel", "Choose an outfit and some accessories for your character.");
		}
		mainMenu.AddItem(menuItems["save"]);
		if (isEditMode)
		{
			mainMenu.AddItem(menuItems["leave"]);
		}
		(menuItems["sex"] as UIMenuListItem).OnListChanged += OnSelectSex;
		(menuItems["sex"] as UIMenuListItem).OnListSelected += OnSelectSex;
		mainMenu.OnMenuChange += OnMenuChange;
		heritageMenu.OnMenuChange += OnMenuChange;
		featuresMenu.OnMenuChange += OnMenuChange;
		appearanceMenu.OnMenuChange += OnMenuChange;
		mainMenu.OnItemSelect += OnMainMenuItemSelect;
		mainMenu.CanUserGoBack = false;
		List<object> items = parentInfo[Sex.Female].Select((EditorParent p) => p.Name).Cast<object>().ToList();
		List<object> items2 = parentInfo[Sex.Male].Select((EditorParent p) => p.Name).Cast<object>().ToList();
		int index = parentInfo[Sex.Female].IndexOf(parentInfo[Sex.Female].FirstOrDefault((EditorParent p) => p.Id == mom));
		int index2 = parentInfo[Sex.Male].IndexOf(parentInfo[Sex.Male].FirstOrDefault((EditorParent p) => p.Id == dad));
		heritageWindow = new UIMenuHeritageWindow(index, index2);
		heritageMenu.AddWindow(heritageWindow);
		menuItems["moms"] = new UIMenuListItem("Mother", items, index);
		menuItems["dads"] = new UIMenuListItem("Father", items2, index2);
		menuItems["shapeSlider"] = new UIMenuSliderHeritageItem("Resemblance", "", divider: true);
		menuItems["skinSlider"] = new UIMenuSliderHeritageItem("Skin Tone", "", divider: true);
		heritageMenu.AddItem(menuItems["moms"]);
		heritageMenu.AddItem(menuItems["dads"]);
		heritageMenu.AddItem(menuItems["shapeSlider"]);
		heritageMenu.AddItem(menuItems["skinSlider"]);
		(menuItems["moms"] as UIMenuListItem).Index = mom;
		(menuItems["dads"] as UIMenuListItem).Index = dad;
		(menuItems["shapeSlider"] as UIMenuSliderHeritageItem).Value = (int)(headShape * 100f);
		(menuItems["skinSlider"] as UIMenuSliderHeritageItem).Value = (int)(skinTone * 100f);
		(menuItems["moms"] as UIMenuListItem).OnListChanged += OnMomChanged;
		(menuItems["dads"] as UIMenuListItem).OnListChanged += OnDadChanged;
		(menuItems["shapeSlider"] as UIMenuSliderHeritageItem).OnSliderChanged += OnShapeChanged;
		(menuItems["skinSlider"] as UIMenuSliderHeritageItem).OnSliderChanged += OnSkinChanged;
		menuItems["brow"] = new UIMenuListItem("Brow", new List<object> { "Custom" }, 0);
		menuItems["eyes"] = new UIMenuListItem("Eyes", new List<object> { "Custom" }, 0);
		menuItems["nose"] = new UIMenuListItem("Nose", new List<object> { "Custom" }, 0);
		menuItems["noseProfile"] = new UIMenuListItem("Nose Profile", new List<object> { "Custom" }, 0);
		menuItems["noseTip"] = new UIMenuListItem("Nose Tip", new List<object> { "Custom" }, 0);
		menuItems["cheekbones"] = new UIMenuListItem("Cheekbones", new List<object> { "Custom" }, 0);
		menuItems["cheeks"] = new UIMenuListItem("Cheeks", new List<object> { "Custom" }, 0);
		menuItems["lips"] = new UIMenuListItem("Lips", new List<object> { "Custom" }, 0);
		menuItems["jaw"] = new UIMenuListItem("Jaw", new List<object> { "Custom" }, 0);
		menuItems["chinProfile"] = new UIMenuListItem("Chin Profile", new List<object> { "Custom" }, 0);
		menuItems["chinShape"] = new UIMenuListItem("Chin Shape", new List<object> { "Custom" }, 0);
		menuItems["neck"] = new UIMenuListItem("Neck", new List<object> { "Custom" }, 0);
		UIMenuGridPanel panel = new UIMenuGridPanel("Up", "Down", "In", "Out");
		UIMenuHorizontalOneLineGridPanel panel2 = new UIMenuHorizontalOneLineGridPanel("Wide", "Squint");
		UIMenuGridPanel panel3 = new UIMenuGridPanel("Up", "Down", "Narrow", "Wide");
		UIMenuGridPanel panel4 = new UIMenuGridPanel("Crooked", "Curved", "Long", "Short");
		UIMenuGridPanel panel5 = new UIMenuGridPanel("Tip Up", "Tip Down", "Broken Right", "Broken Left");
		UIMenuGridPanel panel6 = new UIMenuGridPanel("Up", "Down", "In", "Out");
		UIMenuHorizontalOneLineGridPanel panel7 = new UIMenuHorizontalOneLineGridPanel("Puffed", "Gaunt");
		UIMenuHorizontalOneLineGridPanel panel8 = new UIMenuHorizontalOneLineGridPanel("Thick", "Thin");
		UIMenuGridPanel panel9 = new UIMenuGridPanel("Round", "Square", "Narrow", "Wide");
		UIMenuGridPanel panel10 = new UIMenuGridPanel("In", "Out", "Up", "Down");
		UIMenuGridPanel panel11 = new UIMenuGridPanel("Rounded", "Bum", "Pointed", "Square");
		UIMenuHorizontalOneLineGridPanel panel12 = new UIMenuHorizontalOneLineGridPanel("Thin", "Thick");
		featuresMenu.AddItem(menuItems["brow"]);
		featuresMenu.AddItem(menuItems["eyes"]);
		featuresMenu.AddItem(menuItems["nose"]);
		featuresMenu.AddItem(menuItems["noseProfile"]);
		featuresMenu.AddItem(menuItems["noseTip"]);
		featuresMenu.AddItem(menuItems["cheekbones"]);
		featuresMenu.AddItem(menuItems["cheeks"]);
		featuresMenu.AddItem(menuItems["lips"]);
		featuresMenu.AddItem(menuItems["jaw"]);
		featuresMenu.AddItem(menuItems["chinProfile"]);
		featuresMenu.AddItem(menuItems["chinShape"]);
		featuresMenu.AddItem(menuItems["neck"]);
		(menuItems["brow"] as UIMenuListItem).AddPanel(panel);
		(menuItems["eyes"] as UIMenuListItem).AddPanel(panel2);
		(menuItems["nose"] as UIMenuListItem).AddPanel(panel3);
		(menuItems["noseProfile"] as UIMenuListItem).AddPanel(panel4);
		(menuItems["noseTip"] as UIMenuListItem).AddPanel(panel5);
		(menuItems["cheekbones"] as UIMenuListItem).AddPanel(panel6);
		(menuItems["cheeks"] as UIMenuListItem).AddPanel(panel7);
		(menuItems["lips"] as UIMenuListItem).AddPanel(panel8);
		(menuItems["jaw"] as UIMenuListItem).AddPanel(panel9);
		(menuItems["chinProfile"] as UIMenuListItem).AddPanel(panel10);
		(menuItems["chinShape"] as UIMenuListItem).AddPanel(panel11);
		(menuItems["neck"] as UIMenuListItem).AddPanel(panel12);
		(menuItems["brow"] as UIMenuListItem).OnListChanged += OnBrowChanged;
		(menuItems["eyes"] as UIMenuListItem).OnListChanged += OnEyesChanged;
		(menuItems["nose"] as UIMenuListItem).OnListChanged += OnNoseChanged;
		(menuItems["noseProfile"] as UIMenuListItem).OnListChanged += OnNoseProfileChanged;
		(menuItems["noseTip"] as UIMenuListItem).OnListChanged += OnNoseTipChanged;
		(menuItems["cheekbones"] as UIMenuListItem).OnListChanged += OnCheekbonesChanged;
		(menuItems["cheeks"] as UIMenuListItem).OnListChanged += OnCheeksChanged;
		(menuItems["lips"] as UIMenuListItem).OnListChanged += OnLipsChanged;
		(menuItems["jaw"] as UIMenuListItem).OnListChanged += OnJawChanged;
		(menuItems["chinProfile"] as UIMenuListItem).OnListChanged += OnChinProfileChanged;
		(menuItems["chinShape"] as UIMenuListItem).OnListChanged += OnChinShapeChanged;
		(menuItems["neck"] as UIMenuListItem).OnListChanged += OnNeckChanged;
		RecreateMenuItems();
		SetProperMenuControls(mainMenu);
		SetProperMenuControls(heritageMenu);
		SetProperMenuControls(featuresMenu);
		SetProperMenuControls(appearanceMenu);
		SetProperMenuControls(apparelMenu);
	}

	private void OnMenuChange(UIMenu oldMenu, UIMenu newMenu, bool forward)
	{
		if (newMenu == heritageMenu || newMenu == featuresMenu || newMenu == appearanceMenu)
		{
			ZoomCameraIn();
		}
		else if (oldMenu == heritageMenu || oldMenu == featuresMenu || oldMenu == appearanceMenu)
		{
			ZoomCameraOut();
		}
	}

	private void IndexHeritageWindow()
	{
		int index = parentInfo[Sex.Female].IndexOf(parentInfo[Sex.Female].FirstOrDefault((EditorParent p) => p.Id == mom));
		int index2 = parentInfo[Sex.Male].IndexOf(parentInfo[Sex.Male].FirstOrDefault((EditorParent p) => p.Id == dad));
		(menuItems["moms"] as UIMenuListItem).Index = index;
		(menuItems["dads"] as UIMenuListItem).Index = index2;
		heritageWindow.Index(index, index2);
	}

	private void ApplyCharacterInfoToMenu()
	{
		(menuItems["sex"] as UIMenuListItem).Index = (int)editingCharacter.Sex;
		IndexHeritageWindow();
		(menuItems["shapeSlider"] as UIMenuSliderHeritageItem).Value = (int)(headShape * 100f);
		(menuItems["skinSlider"] as UIMenuSliderHeritageItem).Value = (int)(skinTone * 100f);
	}

	private void RecreateMenuItems()
	{
		if (appearanceMenu != null)
		{
			appearanceMenu.Clear();
		}
		if (apparelMenu != null)
		{
			apparelMenu.Clear();
		}
		Sex sex = editingCharacter.Sex;
		EditorConfig editorConfig = EditorConfig.Default[(int)sex];
		List<ClothingItem> list = new List<ClothingItem>();
		foreach (string hairstyle in editorConfig.Hairstyles)
		{
			ClothingItem clothingItemDefinition = Gtacnr.Data.Items.GetClothingItemDefinition(hairstyle);
			if (clothingItemDefinition != null)
			{
				list.Add(clothingItemDefinition);
			}
		}
		menuItems["hair"] = new UIMenuListItem("Hairstyle", list.Cast<object>().ToList(), 0);
		menuItems["eyebrows"] = new UIMenuListItem("Eyebrows", editorConfig.Eyebrows.Cast<object>().ToList(), editorConfig.Eyebrows.Count - 1);
		menuItems["facialHair"] = new UIMenuListItem("Facial Hair", editorConfig.FacialHairstyles.Cast<object>().ToList(), editorConfig.FacialHairstyles.Count - 1);
		menuItems["blemishes"] = new UIMenuListItem("Skin Blemishes", editorConfig.Blemishes.Cast<object>().ToList(), editorConfig.Blemishes.Count - 1);
		menuItems["aging"] = new UIMenuListItem("Skin Aging", editorConfig.Agings.Cast<object>().ToList(), editorConfig.Agings.Count - 1);
		menuItems["complexion"] = new UIMenuListItem("Skin Complexion", editorConfig.Complexions.Cast<object>().ToList(), editorConfig.Complexions.Count - 1);
		menuItems["moles"] = new UIMenuListItem("Moles & Freckles", editorConfig.Moles.Cast<object>().ToList(), editorConfig.Moles.Count - 1);
		menuItems["damage"] = new UIMenuListItem("Skin Damage", editorConfig.Damages.Cast<object>().ToList(), editorConfig.Damages.Count - 1);
		menuItems["eyeColor"] = new UIMenuListItem("Eye Color", editorConfig.EyeColors.Cast<object>().ToList(), 0);
		menuItems["makeup"] = new UIMenuListItem("Makeup", editorConfig.Makeups.Cast<object>().ToList(), editorConfig.Makeups.Count - 1);
		menuItems["lipstick"] = new UIMenuListItem("Lipstick", editorConfig.Lipsticks.Cast<object>().ToList(), editorConfig.Lipsticks.Count - 1);
		UIMenuColorPanel panel = new UIMenuColorPanel("Color", UIMenuColorPanel.ColorPanelType.Hair);
		UIMenuPercentagePanel panel2 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuColorPanel panel3 = new UIMenuColorPanel("Color", UIMenuColorPanel.ColorPanelType.Hair);
		UIMenuPercentagePanel panel4 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuColorPanel panel5 = new UIMenuColorPanel("Color", UIMenuColorPanel.ColorPanelType.Hair);
		UIMenuPercentagePanel panel6 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuPercentagePanel panel7 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuPercentagePanel panel8 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuPercentagePanel panel9 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuPercentagePanel panel10 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuPercentagePanel panel11 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuPercentagePanel panel12 = new UIMenuPercentagePanel("Opacity", "0%", "100%");
		UIMenuColorPanel panel13 = new UIMenuColorPanel("Color", UIMenuColorPanel.ColorPanelType.Makeup);
		appearanceMenu.AddItem(menuItems["hair"]);
		appearanceMenu.AddItem(menuItems["eyebrows"]);
		appearanceMenu.AddItem(menuItems["facialHair"]);
		appearanceMenu.AddItem(menuItems["blemishes"]);
		appearanceMenu.AddItem(menuItems["aging"]);
		appearanceMenu.AddItem(menuItems["complexion"]);
		appearanceMenu.AddItem(menuItems["moles"]);
		appearanceMenu.AddItem(menuItems["damage"]);
		appearanceMenu.AddItem(menuItems["eyeColor"]);
		appearanceMenu.AddItem(menuItems["makeup"]);
		appearanceMenu.AddItem(menuItems["lipstick"]);
		(menuItems["hair"] as UIMenuListItem).AddPanel(panel);
		(menuItems["eyebrows"] as UIMenuListItem).AddPanel(panel2);
		(menuItems["eyebrows"] as UIMenuListItem).AddPanel(panel3);
		(menuItems["facialHair"] as UIMenuListItem).AddPanel(panel4);
		(menuItems["facialHair"] as UIMenuListItem).AddPanel(panel5);
		(menuItems["blemishes"] as UIMenuListItem).AddPanel(panel6);
		(menuItems["aging"] as UIMenuListItem).AddPanel(panel7);
		(menuItems["complexion"] as UIMenuListItem).AddPanel(panel8);
		(menuItems["moles"] as UIMenuListItem).AddPanel(panel9);
		(menuItems["damage"] as UIMenuListItem).AddPanel(panel10);
		(menuItems["makeup"] as UIMenuListItem).AddPanel(panel11);
		(menuItems["lipstick"] as UIMenuListItem).AddPanel(panel12);
		(menuItems["lipstick"] as UIMenuListItem).AddPanel(panel13);
		(menuItems["hair"] as UIMenuListItem).OnListChanged += OnHairChanged;
		(menuItems["eyebrows"] as UIMenuListItem).OnListChanged += OnEyebrowsChanged;
		(menuItems["facialHair"] as UIMenuListItem).OnListChanged += OnFacialHairChanged;
		(menuItems["blemishes"] as UIMenuListItem).OnListChanged += OnBlemishesChanged;
		(menuItems["aging"] as UIMenuListItem).OnListChanged += OnAgingChanged;
		(menuItems["complexion"] as UIMenuListItem).OnListChanged += OnComplexionChanged;
		(menuItems["moles"] as UIMenuListItem).OnListChanged += OnMolesChanged;
		(menuItems["damage"] as UIMenuListItem).OnListChanged += OnDamageChanged;
		(menuItems["eyeColor"] as UIMenuListItem).OnListChanged += OnEyeColorChanged;
		(menuItems["makeup"] as UIMenuListItem).OnListChanged += OnMakeupChanged;
		(menuItems["lipstick"] as UIMenuListItem).OnListChanged += OnLipstickChanged;
		if (apparelMenu != null)
		{
			List<ClothingItem> list2 = new List<ClothingItem>
			{
				new ClothingItem
				{
					Name = "None",
					Id = ""
				}
			};
			foreach (string outfit in editorConfig.Outfits)
			{
				ClothingItem clothingItemDefinition2 = Gtacnr.Data.Items.GetClothingItemDefinition(outfit);
				if (clothingItemDefinition2 != null)
				{
					list2.Add(clothingItemDefinition2);
				}
			}
			menuItems["outfit"] = new UIMenuListItem("Outfit", list2.Cast<object>().ToList(), 0);
			apparelMenu.AddItem(menuItems["outfit"]);
			List<ClothingItem> list3 = new List<ClothingItem>
			{
				new ClothingItem
				{
					Name = "None",
					Id = ""
				}
			};
			foreach (string hat in editorConfig.Hats)
			{
				ClothingItem clothingItemDefinition3 = Gtacnr.Data.Items.GetClothingItemDefinition(hat);
				if (clothingItemDefinition3 != null)
				{
					list3.Add(clothingItemDefinition3);
				}
			}
			menuItems["hat"] = new UIMenuListItem("Hat", list3.Cast<object>().ToList(), 0);
			apparelMenu.AddItem(menuItems["hat"]);
			List<ClothingItem> list4 = new List<ClothingItem>
			{
				new ClothingItem
				{
					Name = "None",
					Id = ""
				}
			};
			foreach (string glass in editorConfig.Glasses)
			{
				ClothingItem clothingItemDefinition4 = Gtacnr.Data.Items.GetClothingItemDefinition(glass);
				if (clothingItemDefinition4 != null)
				{
					list4.Add(clothingItemDefinition4);
				}
			}
			menuItems["glasses"] = new UIMenuListItem("Glasses", list4.Cast<object>().ToList(), 0);
			apparelMenu.AddItem(menuItems["glasses"]);
			List<ClothingItem> list5 = new List<ClothingItem>
			{
				new ClothingItem
				{
					Name = "None",
					Id = ""
				}
			};
			foreach (string watch in editorConfig.Watches)
			{
				ClothingItem clothingItemDefinition5 = Gtacnr.Data.Items.GetClothingItemDefinition(watch);
				if (clothingItemDefinition5 != null)
				{
					list5.Add(clothingItemDefinition5);
				}
			}
			menuItems["watch"] = new UIMenuListItem("Watch", list5.Cast<object>().ToList(), 0);
			apparelMenu.AddItem(menuItems["watch"]);
			(menuItems["outfit"] as UIMenuListItem).OnListChanged += OnOutfitChanged;
			(menuItems["hat"] as UIMenuListItem).OnListChanged += OnHatChanged;
			(menuItems["glasses"] as UIMenuListItem).OnListChanged += OnGlassesChanged;
			(menuItems["watch"] as UIMenuListItem).OnListChanged += OnWatchChanged;
		}
		menuPool.RefreshIndex();
		selectedOutfit = "";
		selectedHat = "";
		selectedGlasses = "";
		selectedWatch = "";
	}

	private void OnDadChanged(UIMenuListItem sender, int newIndex)
	{
		dad = parentInfo[Sex.Male].ElementAt(newIndex).Id;
		IndexHeritageWindow();
		UpdateCharacterHeritage();
		SaveTemporary();
	}

	private void OnMomChanged(UIMenuListItem sender, int newIndex)
	{
		mom = parentInfo[Sex.Female].ElementAt(newIndex).Id;
		IndexHeritageWindow();
		UpdateCharacterHeritage();
		SaveTemporary();
	}

	private void OnShapeChanged(UIMenuSliderItem sender, int newIndex)
	{
		headShape = (float)newIndex / 100f;
		UpdateCharacterHeritage();
		SaveTemporary();
	}

	private void OnSkinChanged(UIMenuSliderItem sender, int newIndex)
	{
		skinTone = (float)newIndex / 100f;
		UpdateCharacterHeritage();
		SaveTemporary();
	}

	private static float ChangeScale(float input)
	{
		return (float)Gtacnr.Utils.ConvertRange(input, 0.0, 1.0, -1.0, 1.0);
	}

	private void OnBrowChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 6, ChangeScale(circlePosition.Y));
		API.SetPedFaceFeature(API.PlayerPedId(), 7, ChangeScale(circlePosition.X));
		SaveTemporary();
	}

	private void OnEyesChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuHorizontalOneLineGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 11, ChangeScale(circlePosition.X));
		SaveTemporary();
	}

	private void OnNoseChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 0, ChangeScale(circlePosition.X));
		API.SetPedFaceFeature(API.PlayerPedId(), 1, ChangeScale(circlePosition.Y));
		SaveTemporary();
	}

	private void OnNoseProfileChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 2, ChangeScale(circlePosition.X));
		API.SetPedFaceFeature(API.PlayerPedId(), 3, ChangeScale(circlePosition.Y));
		SaveTemporary();
	}

	private void OnNoseTipChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 4, ChangeScale(circlePosition.Y));
		API.SetPedFaceFeature(API.PlayerPedId(), 5, ChangeScale(circlePosition.X));
		SaveTemporary();
	}

	private void OnCheekbonesChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 8, ChangeScale(circlePosition.Y));
		API.SetPedFaceFeature(API.PlayerPedId(), 9, ChangeScale(circlePosition.X));
		SaveTemporary();
	}

	private void OnCheeksChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuHorizontalOneLineGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 10, ChangeScale(circlePosition.X));
		SaveTemporary();
	}

	private void OnLipsChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuHorizontalOneLineGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 12, ChangeScale(circlePosition.X));
		SaveTemporary();
	}

	private void OnJawChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 13, ChangeScale(circlePosition.X));
		API.SetPedFaceFeature(API.PlayerPedId(), 14, ChangeScale(circlePosition.Y));
		SaveTemporary();
	}

	private void OnChinProfileChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 15, ChangeScale(circlePosition.X));
		API.SetPedFaceFeature(API.PlayerPedId(), 16, ChangeScale(circlePosition.Y));
		SaveTemporary();
	}

	private void OnChinShapeChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 17, ChangeScale(circlePosition.X));
		API.SetPedFaceFeature(API.PlayerPedId(), 18, ChangeScale(circlePosition.Y));
		SaveTemporary();
	}

	private void OnNeckChanged(UIMenuListItem sender, int newIndex)
	{
		PointF circlePosition = (sender.Panels[0] as UIMenuHorizontalOneLineGridPanel).CirclePosition;
		API.SetPedFaceFeature(API.PlayerPedId(), 19, ChangeScale(circlePosition.X));
		SaveTemporary();
	}

	private void OnHairChanged(UIMenuListItem sender, int newIndex)
	{
		int currentSelection = (sender.Panels[0] as UIMenuColorPanel).CurrentSelection;
		ClothingItem item = (ClothingItem)sender.Items[newIndex];
		Clothes.CurrentApparel.Replace(item);
		API.SetPedHairColor(API.PlayerPedId(), currentSelection, 0);
		SaveTemporary();
	}

	private void OnEyebrowsChanged(UIMenuListItem sender, int newIndex)
	{
		UIMenuPercentagePanel uIMenuPercentagePanel = sender.Panels[0] as UIMenuPercentagePanel;
		UIMenuColorPanel obj = sender.Panels[1] as UIMenuColorPanel;
		float percentage = uIMenuPercentagePanel.Percentage;
		int currentSelection = obj.CurrentSelection;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 2, editorEntry.Index, percentage);
		API.SetPedHeadOverlayColor(API.PlayerPedId(), 2, 1, currentSelection, 0);
		SaveTemporary();
	}

	private void OnFacialHairChanged(UIMenuListItem sender, int newIndex)
	{
		UIMenuPercentagePanel uIMenuPercentagePanel = sender.Panels[0] as UIMenuPercentagePanel;
		UIMenuColorPanel obj = sender.Panels[1] as UIMenuColorPanel;
		float percentage = uIMenuPercentagePanel.Percentage;
		int currentSelection = obj.CurrentSelection;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 1, editorEntry.Index, percentage);
		API.SetPedHeadOverlayColor(API.PlayerPedId(), 1, 1, currentSelection, 0);
		SaveTemporary();
	}

	private void OnBlemishesChanged(UIMenuListItem sender, int newIndex)
	{
		float percentage = (sender.Panels[0] as UIMenuPercentagePanel).Percentage;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 0, editorEntry.Index, percentage);
		SaveTemporary();
	}

	private void OnAgingChanged(UIMenuListItem sender, int newIndex)
	{
		float percentage = (sender.Panels[0] as UIMenuPercentagePanel).Percentage;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 3, editorEntry.Index, percentage);
		SaveTemporary();
	}

	private void OnComplexionChanged(UIMenuListItem sender, int newIndex)
	{
		float percentage = (sender.Panels[0] as UIMenuPercentagePanel).Percentage;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 6, editorEntry.Index, percentage);
		SaveTemporary();
	}

	private void OnMolesChanged(UIMenuListItem sender, int newIndex)
	{
		float percentage = (sender.Panels[0] as UIMenuPercentagePanel).Percentage;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 9, editorEntry.Index, percentage);
		SaveTemporary();
	}

	private void OnDamageChanged(UIMenuListItem sender, int newIndex)
	{
		float percentage = (sender.Panels[0] as UIMenuPercentagePanel).Percentage;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 7, editorEntry.Index, percentage);
		SaveTemporary();
	}

	private void OnEyeColorChanged(UIMenuListItem sender, int newIndex)
	{
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedEyeColor(API.PlayerPedId(), editorEntry.Index);
		SaveTemporary();
	}

	private void OnMakeupChanged(UIMenuListItem sender, int newIndex)
	{
		float percentage = (sender.Panels[0] as UIMenuPercentagePanel).Percentage;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 4, editorEntry.Index, percentage);
		SaveTemporary();
	}

	private void OnLipstickChanged(UIMenuListItem sender, int newIndex)
	{
		UIMenuPercentagePanel uIMenuPercentagePanel = sender.Panels[0] as UIMenuPercentagePanel;
		UIMenuColorPanel obj = sender.Panels[1] as UIMenuColorPanel;
		float percentage = uIMenuPercentagePanel.Percentage;
		int currentSelection = obj.CurrentSelection;
		EditorEntry editorEntry = (EditorEntry)sender.Items[newIndex];
		API.SetPedHeadOverlay(API.PlayerPedId(), 8, editorEntry.Index, percentage);
		API.SetPedHeadOverlayColor(API.PlayerPedId(), 8, 2, currentSelection, 0);
		SaveTemporary();
	}

	private void OnOutfitChanged(UIMenuListItem sender, int newIndex)
	{
		ClothingItem clothingItem = (ClothingItem)sender.Items[newIndex];
		Clothes.CurrentApparel.Replace(clothingItem);
		selectedOutfit = clothingItem.Id;
		if (string.IsNullOrWhiteSpace(selectedOutfit))
		{
			Apparel.ClearFromPed(Game.PlayerPed);
		}
		SaveTemporary();
	}

	private void OnHatChanged(UIMenuListItem sender, int newIndex)
	{
		ClothingItem clothingItem = (ClothingItem)sender.Items[newIndex];
		Clothes.CurrentApparel.Replace(clothingItem);
		selectedHat = clothingItem.Id;
		if (string.IsNullOrWhiteSpace(selectedHat))
		{
			API.ClearPedProp(((PoolObject)Game.PlayerPed).Handle, 0);
		}
		SaveTemporary();
	}

	private void OnGlassesChanged(UIMenuListItem sender, int newIndex)
	{
		ClothingItem clothingItem = (ClothingItem)sender.Items[newIndex];
		Clothes.CurrentApparel.Replace(clothingItem);
		selectedGlasses = clothingItem.Id;
		if (string.IsNullOrWhiteSpace(selectedGlasses))
		{
			API.ClearPedProp(((PoolObject)Game.PlayerPed).Handle, 1);
		}
		SaveTemporary();
	}

	private void OnWatchChanged(UIMenuListItem sender, int newIndex)
	{
		ClothingItem clothingItem = (ClothingItem)sender.Items[newIndex];
		Clothes.CurrentApparel.Replace(clothingItem);
		selectedWatch = clothingItem.Id;
		if (string.IsNullOrWhiteSpace(selectedWatch))
		{
			API.ClearPedProp(((PoolObject)Game.PlayerPed).Handle, 6);
		}
		SaveTemporary();
	}

	private async void OnMainMenuItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
	{
		if (selectedItem == menuItems["save"])
		{
			if (isAwaitingCreation)
			{
				Screen.ShowNotification("~r~Please wait, the created character is being saved.", false);
				Debug.WriteLine("Error: the character is being created!");
				return;
			}
			CloseMenu();
			if (editingCharacter == null)
			{
				Screen.ShowNotification(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x01 - character object is null") + " Please, open a ticket on our Discord.", false);
				Debug.WriteLine("Error: the character object is null!");
				OpenMenu();
				return;
			}
			await Utils.FadeOut();
			ApplyCharacterData();
			List<string> apparelItems = Clothes.CurrentApparel.Items.ToList();
			int freeSlot = -1;
			int i;
			for (i = 0; i < Gtacnr.Client.API.Characters.MaxSlots; i++)
			{
				if (MainScript.Characters.FirstOrDefault((Character c) => c.Slot == i) == null)
				{
					freeSlot = i;
					break;
				}
			}
			if (freeSlot < 0)
			{
				await Utils.FadeIn();
				Screen.ShowNotification(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x02 - no slot available") + " Please, open a ticket on our Discord.", false);
				Debug.WriteLine("Error: there are no free slots!");
				OpenMenu();
				return;
			}
			bool success = false;
			try
			{
				isAwaitingCreation = true;
				success = (isEditMode ? (await Gtacnr.Client.API.Characters.Update(editingCharacter.Slot, editingCharacter)) : (await Gtacnr.Client.API.Characters.Create(freeSlot, editingCharacter)));
			}
			catch (Exception exception)
			{
				Print(exception);
			}
			finally
			{
				isAwaitingCreation = false;
			}
			if (!success)
			{
				await Utils.FadeIn();
				Screen.ShowNotification("~r~Unable to " + (isEditMode ? "save" : "create") + " your character. Your character has been stored in your local storage. If you've already created a character before, please re-log.", false);
				Debug.WriteLine("Error: unexpected server error while saving character!");
				Debug.WriteLine("Character json dump below:");
				Debug.WriteLine(JsonConvert.SerializeObject(editingCharacter));
				OpenMenu();
				return;
			}
			ResetTemporary();
			await MainScript.ReloadCharacters();
			if (!isEditMode)
			{
				Gtacnr.Client.API.Characters.SetActiveCharacter(freeSlot);
			}
			DeleteScene();
			Utils.Unfreeze();
			lock (AntiHealthLockScript.HealThreadLock)
			{
				AntiHealthLockScript.JustHealed();
				API.SetEntityHealth(API.PlayerPedId(), 400);
			}
			if (!isEditMode)
			{
				BaseScript.TriggerEvent("gtacnr:spawn", new object[1] { (object)default(Vector4) });
				BaseScript.TriggerServerEvent("gtacnr:characters:characterCreationCompleted", new object[5]
				{
					freeSlot,
					selectedOutfit ?? "",
					selectedHat ?? "",
					selectedGlasses ?? "",
					selectedWatch ?? ""
				});
				int tries = 0;
				while (!SpawnScript.HasSpawned && tries < 60)
				{
					await BaseScript.Delay(250);
					tries++;
				}
				Clothes.CurrentApparel = new Apparel(apparelItems);
			}
			else if (await TriggerServerEventAsync<bool>("gtacnr:endPlasticSurgery", new object[0]))
			{
				Print("^2Plastic surgery completed. Respawning at the hospital.");
				BaseScript.TriggerEvent("gtacnr:spawn", new object[2]
				{
					(object)new Vector4(352.705f, -588.3478f, 43.315f, 65f),
					true
				});
			}
			else
			{
				Print("^1A server error has occurred while processing your plastic surgery.");
			}
		}
		else
		{
			if (selectedItem != menuItems["leave"])
			{
				return;
			}
			CloseMenu();
			if (isEditMode)
			{
				if (await Utils.ShowConfirm("Do you really want to exit without saving your changes?"))
				{
					await Utils.FadeOut();
					DeleteScene();
					await BaseScript.Delay(1000);
					if (await TriggerServerEventAsync<bool>("gtacnr:cancelPlasticSurgery", new object[0]))
					{
						Print("^2Plastic surgery canceled. Respawning at the hospital.");
					}
					else
					{
						Print("^1A server error has occurred while processing your plastic surgery cancellation.");
					}
					await LoadSavedCharacter(await Gtacnr.Client.API.Characters.GetActiveCharacter());
					BaseScript.TriggerEvent("gtacnr:spawn", new object[2]
					{
						(object)new Vector4(352.705f, -588.3478f, 43.315f, 65f),
						true
					});
				}
				else
				{
					OpenMenu();
				}
			}
			else if (MainScript.Characters.Count() > 0)
			{
				if (await Utils.ShowConfirm("Do you really want to exit without saving your character?"))
				{
					BaseScript.TriggerServerEvent("gtacnr:characters:characterCreationAbandoned", new object[0]);
					await Utils.FadeOut();
					DeleteScene();
					await BaseScript.Delay(1000);
					BaseScript.TriggerEvent("gtacnr:characters:showSelectionHud", new object[1] { JsonConvert.SerializeObject(MainScript.Characters) });
				}
				else
				{
					OpenMenu();
				}
			}
			else
			{
				await Utils.ShowAlert("You must create at least one character to play.");
				OpenMenu();
			}
		}
	}

	private async void OnSelectSex(UIMenuListItem sender, int index)
	{
		editingCharacter.Sex = (Sex)index;
		await CreateRandomCharacter();
		RecreateMenuItems();
		ApplyCharacterInfoToMenu();
		SaveTemporary();
	}

	private void ApplyCharacterData()
	{
		if (editingCharacter != null)
		{
			editingCharacter.Appearance = new Appearance();
			editingCharacter.Apparel = new Apparel();
			editingCharacter.Appearance.Heritage = new Heritage
			{
				Mother = mom,
				Father = dad,
				ShapeMix = headShape,
				SkinMix = skinTone
			};
			editingCharacter.Appearance.FaceFeatures = new List<FaceFeature>();
			for (int i = 0; i < 20; i++)
			{
				editingCharacter.Appearance.FaceFeatures.Add(new FaceFeature
				{
					Index = i,
					Scale = API.GetPedFaceFeature(API.PlayerPedId(), i)
				});
			}
			editingCharacter.Appearance.ComponentVariations = new List<ComponentVariation>
			{
				new ComponentVariation
				{
					Index = 2,
					Drawable = API.GetPedDrawableVariation(API.PlayerPedId(), 2),
					Texture = API.GetPedTextureVariation(API.PlayerPedId(), 2),
					Palette = API.GetPedPaletteVariation(API.PlayerPedId(), 2)
				}
			};
			editingCharacter.Appearance.HeadOverlays = new List<HeadOverlay>();
			for (int j = 0; j < 13; j++)
			{
				int overlay = 255;
				int num = 0;
				int color = 0;
				int num2 = 0;
				float opacity = 0f;
				API.GetPedHeadOverlayData(API.PlayerPedId(), j, ref overlay, ref num, ref color, ref num2, ref opacity);
				editingCharacter.Appearance.HeadOverlays.Add(new HeadOverlay
				{
					Index = j,
					Overlay = overlay,
					Color = color,
					Opacity = opacity
				});
			}
			editingCharacter.Appearance.HairColor = API.GetPedHairColor(API.PlayerPedId());
			editingCharacter.Appearance.EyeColor = API.GetPedEyeColor(API.PlayerPedId());
			editingCharacter.Apparel = Clothes.CurrentApparel;
		}
	}

	private void OpenMenu()
	{
		mainMenu.Visible = true;
	}

	private void CloseMenu()
	{
		mainMenu.Visible = false;
	}

	private void SetProperMenuControls(UIMenu menu)
	{
		if (menu != null)
		{
			menu.ResetKey(UIMenu.MenuControls.Up);
			menu.ResetKey(UIMenu.MenuControls.Down);
			menu.ResetKey(UIMenu.MenuControls.Left);
			menu.ResetKey(UIMenu.MenuControls.Right);
			menu.ResetKey(UIMenu.MenuControls.Select);
			menu.ResetKey(UIMenu.MenuControls.Back);
			menu.SetKey(UIMenu.MenuControls.Up, (Control)188);
			menu.SetKey(UIMenu.MenuControls.Down, (Control)187);
			menu.SetKey(UIMenu.MenuControls.Left, (Control)189);
			menu.SetKey(UIMenu.MenuControls.Right, (Control)190);
			menu.SetKey(UIMenu.MenuControls.Select, (Control)201);
			menu.SetKey(UIMenu.MenuControls.Back, (Control)202);
			menu.SetKey(UIMenu.MenuControls.Up, (Control)181);
			menu.SetKey(UIMenu.MenuControls.Down, (Control)180);
			menu.AddInstructionalButton(new InstructionalButton((Control)201, LocalizationController.S(Entries.Main.BTN_SELECT)));
			menu.AddInstructionalButton(new InstructionalButton((Control)202, LocalizationController.S(Entries.Main.BTN_BACK)));
			menu.AddInstructionalButton(new InstructionalButton((Control)206, "Look Right"));
			menu.AddInstructionalButton(new InstructionalButton((Control)205, "Look Left"));
		}
	}
}
