using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client;

public abstract class Script : BaseScript
{
	private Random random = new Random();

	private readonly List<CoroutineRepeat> scheduledTasks = new List<CoroutineRepeat>();

	protected event EventHandler? Started;

	protected event EventHandler? Stopping;

	protected event Func<Coroutine> Update
	{
		add
		{
			RegisterUpdate(value);
		}
		remove
		{
			UnregisterUpdate(value);
		}
	}

	public Script()
	{
		ResourceEvents.CurrentResourceStart += OnCurrentResourceStart;
		ResourceEvents.CurrentResourceStop += OnCurrentResourceStop;
		MethodInfo[] methods = ((object)this).GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			object[] customAttributes = methodInfo.GetCustomAttributes(inherit: false);
			foreach (object obj in customAttributes)
			{
				try
				{
					if (obj is UpdateAttribute updateAttribute)
					{
						RegisterUpdate((Func<Coroutine>)methodInfo.CreateDelegate(typeof(Func<Coroutine>), this), updateAttribute.StopOnException);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(string.Format("Registering {0} {1}.{2} failed with exception: {3}", obj.ToString().Replace("Attribute", ""), methodInfo.DeclaringType.FullName, methodInfo.Name, ex));
				}
			}
		}
	}

	private void OnCurrentResourceStop(object sender, EventArgs e)
	{
		try
		{
			OnStopping();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private void OnCurrentResourceStart(object sender, EventArgs e)
	{
		try
		{
			OnStarted();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	protected virtual void OnStarted()
	{
	}

	protected virtual void OnStopping()
	{
	}

	protected void RegisterUpdate(Func<Coroutine> coroutine, bool stopOnException = false)
	{
		lock (scheduledTasks)
		{
			CoroutineRepeat coroutineRepeat = new CoroutineRepeat(coroutine, stopOnException);
			scheduledTasks.Add(coroutineRepeat);
			coroutineRepeat.Schedule();
		}
	}

	protected void UnregisterUpdate(Func<Coroutine> coroutine)
	{
		lock (scheduledTasks)
		{
			for (int i = 0; i < scheduledTasks.Count; i++)
			{
				CoroutineRepeat coroutineRepeat = scheduledTasks[i];
				if (coroutineRepeat.m_coroutine.Method.Equals(coroutine.Method) && coroutineRepeat.m_coroutine.Target.Equals(coroutine.Target))
				{
					coroutineRepeat.Stop();
					scheduledTasks.RemoveAt(i);
					break;
				}
			}
		}
	}

	protected static Coroutine Wait(int delay)
	{
		if (delay <= 0)
		{
			return Coroutine.Yield();
		}
		return Coroutine.Wait((ulong)delay);
	}

	protected static Coroutine Yield()
	{
		return Coroutine.Yield();
	}

	protected void Print(string logString)
	{
		Debug.WriteLine(logString);
	}

	protected void Print(Exception exception)
	{
		Debug.WriteLine("^1An exception has occurred (" + exception.GetType().Name + "): " + exception.Message + "\n^3" + exception.StackTrace.Replace("\n", "\n^3") + "^0\n\n                       ^4> Attach a screenshot of this message if you want to report this problem <\n\n");
	}

	private async Task<T?> TriggerEventAsyncInternal<T>(string eventName, int timeout, bool server, params object[] args)
	{
		string responseEventName = eventName + ":response";
		Action<int, T> eventHandler = null;
		bool hasResult = false;
		T result = default(T);
		int token = random.Next(int.MaxValue);
		eventHandler = delegate(int retToken, T tResult)
		{
			if (retToken == token)
			{
				EventHandlerDictionary eventHandlers2 = ((BaseScript)this).EventHandlers;
				string text2 = responseEventName;
				eventHandlers2[text2] -= (Delegate)eventHandler;
				result = tResult;
				hasResult = true;
			}
		};
		EventHandlerDictionary eventHandlers = ((BaseScript)this).EventHandlers;
		string text = responseEventName;
		eventHandlers[text] += (Delegate)eventHandler;
		List<object> list = args.ToList();
		list.Insert(0, token);
		if (server)
		{
			BaseScript.TriggerServerEvent(eventName, list.ToArray());
		}
		else
		{
			BaseScript.TriggerEvent(eventName, list.ToArray());
		}
		DateTime t = DateTime.UtcNow;
		while (!hasResult && !Gtacnr.Utils.CheckTimePassed(t, timeout))
		{
			await BaseScript.Delay(1);
		}
		return result;
	}

	protected async Task<T?> TriggerEventAsync<T>(string eventName, params object[] args)
	{
		return await TriggerEventAsyncInternal<T>(eventName, 1000, server: false, args);
	}

	protected async Task<T?> TriggerServerEventAsync<T>(string eventName, params object[] args)
	{
		return await TriggerEventAsyncInternal<T>(eventName, 20000, server: true, args);
	}

	protected async Task<ResponseCode> TriggerEventAsync(string eventName, params object[] args)
	{
		return (ResponseCode)(await TriggerEventAsync<int>(eventName, args));
	}

	protected async Task<ResponseCode> TriggerServerEventAsync(string eventName, params object[] args)
	{
		return (ResponseCode)(await TriggerServerEventAsync<int>(eventName, args));
	}
}
