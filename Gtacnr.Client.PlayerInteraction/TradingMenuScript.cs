using System;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.PlayerInteraction;

public class TradingMenuScript : Script
{
	private static Menu MainMenu;

	private EditableOfferMenu MyOfferMenu;

	private OfferMenu OtherOfferMenu;

	private MenuItem confirmItem;

	private MenuItem myOfferItem;

	private MenuItem otherOfferItem;

	private bool _isConfirmed;

	private TradeOfferState _currentState;

	private int otherPlayer;

	private Vector3 tradeLocation;

	private readonly string keyboardControlStr = "INPUT_MP_TEXT_CHAT_TEAM";

	private readonly string gamepadControlStr = "INPUT_REPLAY_SCREENSHOT";

	public static TradingMenuScript Instance { get; private set; }

	public bool IsConfirmed
	{
		get
		{
			return _isConfirmed;
		}
		set
		{
			_isConfirmed = value;
			myOfferItem.Enabled = !_isConfirmed;
			otherOfferItem.Enabled = !_isConfirmed;
			confirmItem.Text = (_isConfirmed ? ("~y~" + LocalizationController.S(Entries.Player.MENU_TRADING_UNDO_CONFIRM_TEXT)) : ("~b~" + LocalizationController.S(Entries.Player.MENU_TRADING_CONFIRM_TEXT)));
			confirmItem.Description = (_isConfirmed ? LocalizationController.S(Entries.Player.MENU_TRADING_UNDO_CONFIRM_DESCRIPTION) : LocalizationController.S(Entries.Player.MENU_TRADING_CONFIRM_DESCRIPTION));
		}
	}

	private TradeOfferState CurrentState
	{
		get
		{
			return _currentState;
		}
		set
		{
			_currentState = value;
			if (_currentState == TradeOfferState.Finalized)
			{
				MainMenu.CloseMenu();
			}
		}
	}

	protected override void OnStarted()
	{
		Instance = this;
		MainMenu = new Menu(LocalizationController.S(Entries.Player.MENU_TRADING_TITLE))
		{
			MaxDistance = 15f
		};
		myOfferItem = new MenuItem(LocalizationController.S(Entries.Player.MENU_TRADING_MY_OFFER_TEXT))
		{
			Label = Utils.MENU_ARROW
		};
		MainMenu.AddMenuItem(myOfferItem);
		MyOfferMenu = new EditableOfferMenu(LocalizationController.S(Entries.Player.MENU_TRADING_MY_OFFER_TITLE), LocalizationController.S(Entries.Player.MENU_TRADING_MY_OFFER_SUBTITLE));
		MenuController.BindMenuItem(MainMenu, MyOfferMenu.MainMenu, myOfferItem);
		MyOfferMenu.OnOfferChanged = OnMenuOfferChanged;
		otherOfferItem = new MenuItem(LocalizationController.S(Entries.Player.MENU_TRADING_OTHER_OFFER_TEXT))
		{
			Label = Utils.MENU_ARROW
		};
		MainMenu.AddMenuItem(otherOfferItem);
		OtherOfferMenu = new OfferMenu(LocalizationController.S(Entries.Player.MENU_TRADING_OTHER_OFFER_TITLE), LocalizationController.S(Entries.Player.MENU_TRADING_OTHER_OFFER_SUBTITLE));
		MenuController.BindMenuItem(MainMenu, OtherOfferMenu.MainMenu, otherOfferItem);
		confirmItem = new MenuItem("~b~" + LocalizationController.S(Entries.Player.MENU_TRADING_CONFIRM_TEXT), LocalizationController.S(Entries.Player.MENU_TRADING_CONFIRM_DESCRIPTION));
		MainMenu.AddMenuItem(confirmItem);
		MainMenu.OnMenuOpen += OnMenuOpen;
		MainMenu.OnItemSelect += OnItemSelect;
		MainMenu.OnMenuClosing += OnMenuClosing;
		MainMenu.OnMenuClose += OnMenuClose;
		MenuController.AddMenu(MainMenu);
	}

	public void OpenMenu()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		CurrentState = TradeOfferState.InProgress;
		MyOfferMenu.ClearAllButtons();
		MyOfferMenu.UpdateTradeOffer(new TradeOffer());
		OtherOfferMenu.ClearAllButtons();
		OtherOfferMenu.UpdateTradeOffer(new TradeOffer());
		tradeLocation = ((Entity)Game.PlayerPed).Position;
		MainMenu.OpenMenu();
	}

	private void Cleanup()
	{
		IsConfirmed = false;
		CurrentState = TradeOfferState.InProgress;
	}

	private void OnMenuOfferChanged(TradeOffer offer)
	{
		BaseScript.TriggerServerEvent("gtacnr:trading:offerChanged", new object[1] { offer.Json() });
		CurrentState = TradeOfferState.InProgress;
	}

	private void OnMenuOpen(Menu menu)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		MainMenu.MenuCoords = tradeLocation;
	}

	private void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == confirmItem)
		{
			IsConfirmed = !IsConfirmed;
			if (IsConfirmed)
			{
				BaseScript.TriggerServerEvent("gtacnr:trading:confirm", new object[0]);
			}
			else
			{
				BaseScript.TriggerServerEvent("gtacnr:trading:undoConfirm", new object[0]);
			}
		}
	}

	private bool OnMenuClosing(Menu menu)
	{
		ShowCloseMenuConfirmation();
		return false;
		static async void ShowCloseMenuConfirmation()
		{
			if (await Utils.ShowConfirm("Do you really want to cancel this trade?", "Trading"))
			{
				MainMenu.CloseMenu();
			}
		}
	}

	private void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		if (!e.IsOpeningSubmenu)
		{
			BaseScript.TriggerServerEvent("gtacnr:trading:canceled", new object[0]);
		}
	}

	[EventHandler("gtacnr:trading:started")]
	private void OnTradingStarted(int playerId)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (playerState == null)
		{
			BaseScript.TriggerServerEvent("gtacnr:trading:canceled", new object[0]);
			return;
		}
		otherPlayer = playerId;
		MainMenu.MenuSubtitle = $"{playerState.ColorTextCode}{playerState.Name} ({playerState.Id})";
		OpenMenu();
	}

	[EventHandler("gtacnr:trading:offered")]
	private async void OnOffered(int playerId)
	{
		PlayerState playerState = LatentPlayers.Get(playerId);
		string text = (Utils.IsUsingKeyboard() ? LocalizationController.S(Entries.Businesses.STP_PRESS, "~" + keyboardControlStr + "~") : LocalizationController.S(Entries.Businesses.STP_HOLD, "~" + gamepadControlStr + "~"));
		await InteractiveNotificationsScript.Show(LocalizationController.S(Entries.Player.TRADING_GOT_OFFER, playerState.ColorNameAndId, text), InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0));
		bool OnAccepted()
		{
			Utils.PlaySelectSound();
			Utils.DisplayHelpText();
			BaseScript.TriggerServerEvent("gtacnr:trading:acceptedOffer", new object[1] { playerId });
			return true;
		}
	}

	[EventHandler("gtacnr:trading:stateChanged")]
	private void OnStateChanged(byte stateByte)
	{
		CurrentState = (TradeOfferState)stateByte;
		confirmItem.Enabled = true;
	}

	[EventHandler("gtacnr:trading:offerChanged")]
	private void OnTradeOfferChanged(string jData)
	{
		TradeOffer newTradeOffer = jData.Unjson<TradeOffer>();
		OtherOfferMenu.UpdateTradeOffer(newTradeOffer);
		CurrentState = TradeOfferState.InProgress;
		IsConfirmed = false;
		PlayerState playerState = LatentPlayers.Get(otherPlayer);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_CHANGED_OFFER, playerState.ColorNameAndId));
	}

	[EventHandler("gtacnr:trading:otherConfirmed")]
	private void OnOtherConfirmed()
	{
		PlayerState playerState = LatentPlayers.Get(otherPlayer);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_OTHER_CONFIRMED, playerState.ColorNameAndId));
	}

	[EventHandler("gtacnr:trading:otherPlayerUnconfirmed")]
	private void OnOtherPlayerUnconfirmed(TradeOffer offer)
	{
		PlayerState playerState = LatentPlayers.Get(otherPlayer);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_OTHER_UNCONFIRMED, playerState.ColorNameAndId));
	}

	[EventHandler("gtacnr:trading:canceled")]
	private void OnCanceled()
	{
		MenuController.CloseAllMenus();
		Cleanup();
	}

	[EventHandler("gtacnr:trading:finished")]
	private void OnFinished(byte tradeResponseByte)
	{
		TradeResponse tradeResponse = (TradeResponse)tradeResponseByte;
		switch (tradeResponse)
		{
		case TradeResponse.Success:
			Utils.DisplayHelpText("~g~" + LocalizationController.S(Entries.Player.TRADING_RESPONSE_SUCCESS));
			break;
		case TradeResponse.InsufficientAmount:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_AMOUNT));
			break;
		case TradeResponse.InvalidItem:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_ITEM));
			break;
		case TradeResponse.ItemLimitReached:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_ITEM_LIMIT));
			break;
		case TradeResponse.ItemNotTransferable:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_NOT_TRANSFERABLE));
			break;
		case TradeResponse.JobNotAllowed:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_JOB));
			break;
		case TradeResponse.NoSpaceLeft:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_NO_SPACE));
			break;
		case TradeResponse.TransferLimit:
		{
			string text = 50000000.ToCurrencyString() ?? "";
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_TRANSFER_LIMIT, text));
			break;
		}
		case TradeResponse.Transaction:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_RESPONSE_TRANSACTION));
			break;
		default:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xE9-{(int)tradeResponse}"));
			break;
		}
		Cleanup();
	}

	public void SendTradeOffer(Player player)
	{
		PlayerState playerState = LatentPlayers.Get(player);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TRADING_OFFERED, playerState.ColorNameAndId));
		BaseScript.TriggerServerEvent("gtacnr:trading:offer", new object[1] { player.ServerId });
	}
}
