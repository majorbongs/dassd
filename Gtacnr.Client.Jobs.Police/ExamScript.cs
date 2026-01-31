using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Police;

public class ExamScript : Script
{
	private static readonly PoliceExam exam = Gtacnr.Utils.LoadJson<PoliceExam>("data/police/exam.json");

	private static ExamScript instance;

	private PoliceExamState examState;

	private Menu examMenu;

	private int questionCounter;

	private int secondsLeft;

	private bool inExam;

	public ExamScript()
	{
		instance = this;
		exam.InitQuestionsIndex();
		examMenu = new Menu(LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_TITLE), "");
		examMenu.OnItemSelect += OnMenuItemSelect;
		examMenu.OnCheckboxChange += OnMenuCheckboxChange;
		examMenu.OnMenuClosing += OnMenuClosing;
		MenuController.AddMenu(examMenu);
	}

	public static async void StartExam()
	{
		if (instance.inExam)
		{
			MenuController.CloseAllMenus();
			instance.examMenu.OpenMenu();
		}
		else if (await Utils.ShowConfirm(LocalizationController.S(Entries.Jobs.POLICE_EXAM_CONFIRM_MESSAGE, exam.Questions.Count, exam.PassingScore, exam.TimeMinutes), LocalizationController.S(Entries.Jobs.POLICE_EXAM_CONFIRM_TITLE), TimeSpan.FromSeconds(10.0)) && await instance.RequestStart())
		{
			instance.Setup();
			instance.NextQuestion(firstQuestion: true);
		}
	}

	private async Task<bool> RequestStart()
	{
		await BaseScript.Delay(500);
		int num = await TriggerServerEventAsync<int>("gtacnr:police:startExam", new object[0]);
		switch (num)
		{
		case 3:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_EXAM_ERROR_ALREADY_STARTED));
			break;
		case 4:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_EXAM_ERROR_COOLDOWN));
			break;
		default:
			Utils.DisplayErrorMessage(83, num);
			break;
		case 1:
			break;
		}
		return num == 1;
	}

	private void Setup()
	{
		if (!inExam)
		{
			instance.questionCounter = 0;
			instance.secondsLeft = exam.TimeMinutes * 60;
			instance.Update += instance.TickTimer;
			examState = new PoliceExamState();
			RefreshTimer();
			inExam = true;
			exam.ShuffleQuestions();
			EventHandlerDictionary eventHandlers = ((BaseScript)this).EventHandlers;
			eventHandlers["gtacnr:police:examResponse"] = eventHandlers["gtacnr:police:examResponse"] + (Delegate)new Action<string>(ExamResponseHandler);
		}
	}

	private void CleanUp()
	{
		inExam = false;
		instance.Update -= instance.TickTimer;
		examState = null;
		MenuController.CloseAllMenus();
		EventHandlerDictionary eventHandlers = ((BaseScript)this).EventHandlers;
		eventHandlers["gtacnr:police:examResponse"] = eventHandlers["gtacnr:police:examResponse"] - (Delegate)new Action<string>(ExamResponseHandler);
	}

	private void Submit()
	{
		Terminate(1);
	}

	private async void Terminate(int reason)
	{
		examState.TerminationReason = reason;
		int num = await TriggerServerEventAsync<int>("gtacnr:police:endExam", new object[1] { examState.Json() });
		if (num != 1)
		{
			Utils.DisplayErrorMessage(84, num);
		}
		CleanUp();
	}

	private void ExamResponseHandler(string jData)
	{
		PoliceExamResponse policeExamResponse = jData.Unjson<PoliceExamResponse>();
		Utils.DisplayHelpText(LocalizationController.S(policeExamResponse.Passed ? Entries.Jobs.POLICE_EXAM_RESPONSE_PASSED : Entries.Jobs.POLICE_EXAM_RESPONSE_FAILED, policeExamResponse.FinalScore, policeExamResponse.MaxScore, policeExamResponse.PassingScore));
	}

	private void NextQuestion(bool firstQuestion = false)
	{
		if (!firstQuestion)
		{
			questionCounter++;
		}
		examMenu.ClearMenuItems();
		if (questionCounter < exam.Questions.Count)
		{
			PoliceExamQuestion policeExamQuestion = exam.Questions[questionCounter];
			bool flag = policeExamQuestion.Options.Where((PoliceExamOption o) => o.IsCorrect).Count() >= 2;
			int num = policeExamQuestion.Options.Sum((PoliceExamOption o) => o.IsCorrect ? o.Points : 0);
			string question = policeExamQuestion.Question;
			MenuItem menuItem = new MenuItem(LocalizationController.S(Entries.Jobs.POLICE_EXAM_QUESTION_MENU_TITLE), LocalizationController.S(question))
			{
				PlaySelectSound = false
			};
			examMenu.MenuSubtitle = LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_SUBTITLE, questionCounter + 1, exam.Questions.Count, num);
			if (flag)
			{
				menuItem.Description = menuItem.Description + " " + LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_SELECT_ALL);
			}
			examMenu.AddMenuItem(menuItem);
			int num2 = 0;
			foreach (PoliceExamOption option2 in policeExamQuestion.Options)
			{
				char c = (char)(65 + num2);
				string option = option2.Option;
				MenuCheckboxItem item = new MenuCheckboxItem(LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_OPTION, c), LocalizationController.S(option))
				{
					ItemData = Tuple.Create(policeExamQuestion, option2)
				};
				examMenu.AddMenuItem(item);
				num2++;
			}
			MenuItem menuItem2 = ((questionCounter >= exam.Questions.Count - 1) ? new MenuItem(LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_SUBMIT_BUTTON_TEXT), LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_SUBMIT_BUTTON_DESCRIPTION)) : new MenuItem(LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_NEXT_BUTTON_TEXT), LocalizationController.S(Entries.Jobs.POLICE_EXAM_MENU_NEXT_BUTTON_DESCRIPTION)));
			menuItem2.ItemData = "next";
			examMenu.AddMenuItem(menuItem2);
			if (!examMenu.Visible)
			{
				examMenu.OpenMenu();
			}
		}
		else
		{
			Submit();
		}
	}

	private void StoreAnswers()
	{
		foreach (MenuItem menuItem in examMenu.GetMenuItems())
		{
			if (!(menuItem.ItemData is Tuple<PoliceExamQuestion, PoliceExamOption> { Item1: var item, Item2: var item2 }))
			{
				continue;
			}
			int value = item.OriginalIndex.Value;
			if (!examState.Answers.ContainsKey(value))
			{
				examState.Answers[item.OriginalIndex.Value] = new List<int>();
			}
			if ((menuItem as MenuCheckboxItem).Checked)
			{
				examState.Answers[value].Add(item2.OriginalIndex.Value);
				if (item2.TerminateExamIfSelected)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_EXAM_EXTREMELY_WRONG_ANSWER));
					Terminate(4);
					break;
				}
			}
		}
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem.ItemData is string text && text == "next")
		{
			StoreAnswers();
			NextQuestion();
		}
	}

	private void OnMenuCheckboxChange(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool newCheckedState)
	{
		if (!(menuItem.ItemData is Tuple<PoliceExamQuestion, PoliceExamOption> { Item1: var item } tuple))
		{
			return;
		}
		_ = tuple.Item2;
		bool flag = item.Options.Where((PoliceExamOption o) => o.IsCorrect).Count() >= 2;
		if (!newCheckedState || flag)
		{
			return;
		}
		foreach (MenuItem menuItem2 in menu.GetMenuItems())
		{
			if (menuItem2 is MenuCheckboxItem menuCheckboxItem && menuItem2 != menuItem)
			{
				menuCheckboxItem.Checked = false;
			}
		}
	}

	private bool OnMenuClosing(Menu menu)
	{
		Confirm();
		return false;
		async void Confirm()
		{
			if (await Utils.ShowConfirm(LocalizationController.S(Entries.Jobs.POLICE_EXAM_TERMINATE_MESSAGE), LocalizationController.S(Entries.Jobs.POLICE_EXAM_TERMINATE_TITLE)))
			{
				MenuController.CloseAllMenus();
				Terminate(3);
			}
		}
	}

	private async Coroutine TickTimer()
	{
		await BaseScript.Delay(1000);
		secondsLeft--;
		RefreshTimer();
		if (secondsLeft == 0)
		{
			Terminate(2);
		}
	}

	private void RefreshTimer()
	{
		examMenu.CounterPreText = ((secondsLeft < 60) ? "~r~" : ((secondsLeft < 120) ? "~o~" : "")) + Gtacnr.Utils.SecondsToMinutesAndSeconds(secondsLeft);
	}

	[EventHandler("gtacnr:respawned")]
	private void OnRespawned()
	{
		if (inExam && !examMenu.Visible)
		{
			examMenu.OpenMenu();
		}
	}
}
