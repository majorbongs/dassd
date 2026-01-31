using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CitizenFX.Core;

[AsyncMethodBuilder(typeof(CoroutineBuilder<>))]
public class Coroutine<T> : Coroutine
{
	private T m_result;

	public T Result => m_result;

	internal Coroutine()
	{
	}

	public new CoroutineAwaiter<T> GetAwaiter()
	{
		return new CoroutineAwaiter<T>(this);
	}

	public new T GetResult()
	{
		return Result;
	}

	protected override object GetResultInternal()
	{
		return m_result;
	}

	public void Complete(T value)
	{
		CompleteInternal(State.Completed, value);
	}

	public override void Complete(object value)
	{
		CompleteInternal(State.Completed, (T)value);
	}

	public override void Complete(object value, Exception ex)
	{
		CompleteInternal(State.Completed, (T)value, ex);
	}

	public void Cancel(T value)
	{
		CompleteInternal(State.Canceled, value);
	}

	public override void Cancel(object value)
	{
		CompleteInternal(State.Canceled, (T)value);
	}

	public override void Cancel(object value, Exception ex)
	{
		CompleteInternal(State.Canceled, (T)value, ex);
	}

	public void Fail(T value)
	{
		CompleteInternal(State.Failed, value);
	}

	public override void Fail(object value)
	{
		CompleteInternal(State.Failed, (T)value);
	}

	public override void Fail(object value, Exception ex)
	{
		CompleteInternal(State.Failed, (T)value, ex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CompleteInternal(State completionState, T value)
	{
		m_result = value;
		CompleteInternal(completionState);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CompleteInternal(State completionState, T value, Exception exception)
	{
		m_result = value;
		CompleteInternal(completionState, exception);
	}

	public static Coroutine<T> Completed(T completeWithValue)
	{
		Coroutine<T> coroutine = new Coroutine<T>();
		coroutine.Complete(completeWithValue);
		return coroutine;
	}
}
[AsyncMethodBuilder(typeof(CoroutineBuilder))]
public class Coroutine
{
	public enum State : uint
	{
		Idle = 0u,
		Active = 2u,
		Completed = 16777216u,
		Succesful = 16777216u,
		Failed = 50331648u,
		Canceled = 83886080u,
		MaskActive = 15u,
		MaskCompletion = 61440u
	}

	private Action<Coroutine> m_continuation;

	protected State m_state;

	public Exception Exception { get; set; }

	public bool IsCompleted => (m_state & State.Completed) != 0;

	public bool IsSuccessful => (m_state & State.MaskCompletion) == State.Completed;

	public bool IsFailed => (m_state & State.MaskCompletion) == State.Failed;

	public bool IsCanceled => (m_state & State.MaskCompletion) == State.Canceled;

	internal Coroutine()
	{
	}

	public object GetResult()
	{
		return GetResultInternal();
	}

	internal object GetResultNonThrowing()
	{
		return GetResultInternal();
	}

	protected virtual object GetResultInternal()
	{
		return null;
	}

	internal void Complete()
	{
		CompleteInternal(State.Completed);
	}

	public virtual void Complete(object value = null)
	{
		CompleteInternal(State.Completed);
	}

	public virtual void Complete(object value, Exception exception)
	{
		CompleteInternal(State.Completed, exception);
	}

	public virtual void Cancel(object value = null)
	{
		CompleteInternal(State.Canceled);
	}

	public virtual void Cancel(object value, Exception exception)
	{
		CompleteInternal(State.Canceled, exception);
	}

	public virtual void Fail(object value = null)
	{
		CompleteInternal(State.Failed);
	}

	public virtual void Fail(object value, Exception exception)
	{
		CompleteInternal(State.Failed, exception);
	}

	internal void SetException(Exception exception)
	{
		CompleteInternal(State.Failed, exception);
	}

	protected void CompleteInternal(State completionState)
	{
		if (!IsCompleted)
		{
			m_state |= completionState;
			if (Exception != null)
			{
				Debug.WriteLine(Exception.ToString());
			}
			m_continuation?.Invoke(this);
		}
	}

	protected void CompleteInternal(State completionState, Exception exception)
	{
		Exception = exception;
		CompleteInternal(completionState);
	}

	public void ContinueWith(Action<Coroutine> action)
	{
		m_continuation = (Action<Coroutine>)Delegate.Combine(m_continuation, action);
	}

	public void ContinueWith(Action action)
	{
		ContinueWith((Action<Coroutine>)delegate
		{
			action();
		});
	}

	internal void ClearContinueWith()
	{
		m_continuation = delegate
		{
		};
	}

	public CoroutineAwaiter GetAwaiter()
	{
		return new CoroutineAwaiter(this);
	}

	public static Coroutine Completed()
	{
		Coroutine coroutine = new Coroutine();
		coroutine.Complete();
		return coroutine;
	}

	public static Coroutine<T> Completed<T>(T result)
	{
		Coroutine<T> coroutine = new Coroutine<T>();
		coroutine.Complete(result);
		return coroutine;
	}

	public static Coroutine Yield()
	{
		Coroutine coroutine = new Coroutine();
		Scheduler.Schedule(coroutine.Complete);
		return coroutine;
	}

	public static Coroutine WaitUntil(TimePoint time)
	{
		Coroutine coroutine = new Coroutine();
		Scheduler.Schedule(coroutine.Complete, time);
		return coroutine;
	}

	public static Coroutine WaitUntil(TimePoint time, CancelerToken cancelerToken)
	{
		Coroutine coroutine;
		if (!cancelerToken.CancelOrThrowIfRequested())
		{
			coroutine = new Coroutine();
			Scheduler.Schedule(OnWaited, time);
			cancelerToken.OnCancel += OnCancel;
			return coroutine;
		}
		return Completed();
		void OnCancel()
		{
			Scheduler.Unschedule(OnWaited, time);
			coroutine.Complete();
		}
		void OnWaited()
		{
			coroutine.Complete();
			cancelerToken.OnCancel -= OnCancel;
		}
	}

	public static Coroutine Wait(ulong delay)
	{
		return WaitUntil(Scheduler.CurrentTime + delay);
	}

	public static Coroutine Wait(ulong delay, CancelerToken cancelerToken)
	{
		return WaitUntil(Scheduler.CurrentTime + delay, cancelerToken);
	}

	public static Coroutine Delay(ulong delay)
	{
		return WaitUntil(Scheduler.CurrentTime + delay);
	}

	public static Coroutine Delay(ulong delay, CancelerToken cancelerToken)
	{
		return WaitUntil(Scheduler.CurrentTime + delay, cancelerToken);
	}

	public static async Coroutine Schedule(Func<Coroutine> function, ulong delay, ulong iterations = ulong.MaxValue, CancelerToken cancelerToken = default(CancelerToken))
	{
		if (iterations == 0)
		{
			return;
		}
		while ((iterations == ulong.MaxValue || iterations-- != 0) && !cancelerToken.CancelOrThrowIfRequested())
		{
			await Delay(delay, cancelerToken);
			if (cancelerToken.CancelOrThrowIfRequested())
			{
				break;
			}
			await function();
		}
	}

	public static async Coroutine<T> Schedule<T>(Func<Coroutine<T>> function, ulong delay, ulong iterations = ulong.MaxValue, CancelerToken cancelerToken = default(CancelerToken))
	{
		T result = default(T);
		if (iterations != 0)
		{
			while ((iterations == ulong.MaxValue || iterations-- != 0) && !cancelerToken.CancelOrThrowIfRequested())
			{
				await Delay(delay, cancelerToken);
				if (cancelerToken.CancelOrThrowIfRequested())
				{
					return result;
				}
				result = await function();
			}
		}
		return result;
	}

	public static async Coroutine Schedule(Func<Coroutine> function, TimePoint time, CancelerToken cancelerToken = default(CancelerToken))
	{
		await WaitUntil(time, cancelerToken);
		if (!cancelerToken.CancelOrThrowIfRequested())
		{
			await function();
		}
	}

	public static async Coroutine<T> Schedule<T>(Func<Coroutine<T>> function, TimePoint time, CancelerToken cancelerToken = default(CancelerToken))
	{
		await WaitUntil(time, cancelerToken);
		return cancelerToken.CancelOrThrowIfRequested() ? default(T) : (await function());
	}

	public static Coroutine Schedule(Func<Coroutine> function)
	{
		return Schedule(function, Scheduler.CurrentTime);
	}

	public static Coroutine Schedule(Func<Coroutine> function, ulong delay, CancelerToken cancelerToken)
	{
		return Schedule(function, Scheduler.CurrentTime + delay, cancelerToken);
	}

	public static Coroutine<T> Schedule<T>(Func<Coroutine<T>> function, ulong delay, CancelerToken cancelerToken)
	{
		return Schedule(function, Scheduler.CurrentTime + delay, cancelerToken);
	}

	public static Coroutine Schedule(Action function)
	{
		return Schedule(async delegate
		{
			function();
		}, Scheduler.CurrentTime);
	}

	public static Coroutine Schedule(Action function, ulong delay, CancelerToken cancelerToken = default(CancelerToken))
	{
		return Schedule(async delegate
		{
			function();
		}, Scheduler.CurrentTime + delay, cancelerToken);
	}

	public static Coroutine Schedule(Action function, ulong delay, ulong iterations, CancelerToken cancelerToken = default(CancelerToken))
	{
		return Schedule(async delegate
		{
			function();
		}, delay, iterations, cancelerToken);
	}

	public static Coroutine Run(Action function)
	{
		if (Thread.CurrentThread == Scheduler.MainThread)
		{
			function();
			return Completed();
		}
		return RunNextFrame(function);
	}

	public static Coroutine Run(Func<Coroutine> function)
	{
		if (Thread.CurrentThread != Scheduler.MainThread)
		{
			return RunNextFrame(function);
		}
		return function();
	}

	public static Coroutine<T> Run<T>(Func<Coroutine<T>> function)
	{
		if (Thread.CurrentThread != Scheduler.MainThread)
		{
			return RunNextFrame(function);
		}
		return function();
	}

	public static Coroutine RunNextFrame(Func<Coroutine> function)
	{
		Coroutine coroutine = new Coroutine();
		Scheduler.Schedule(async delegate
		{
			await function();
			coroutine.Complete();
		});
		return coroutine;
	}

	public static Coroutine RunNextFrame(Action function)
	{
		Coroutine coroutine = new Coroutine();
		Scheduler.Schedule(delegate
		{
			function();
			coroutine.Complete();
		});
		return coroutine;
	}

	public static Coroutine<T> RunNextFrame<T>(Func<Coroutine<T>> function)
	{
		Coroutine<T> coroutine = new Coroutine<T>();
		Scheduler.Schedule(async delegate
		{
			T value = await function();
			coroutine.Complete(value);
		});
		return coroutine;
	}

	public static Coroutine WhenAny(params Coroutine[] coroutines)
	{
		Coroutine coroutine = new Coroutine();
		List<Coroutine> awaitees = new List<Coroutine>();
		foreach (Coroutine coroutine2 in coroutines)
		{
			if (!coroutine2.IsCompleted)
			{
				awaitees.Add(coroutine2);
				coroutine2.ContinueWith(Complete);
				continue;
			}
			return coroutine2;
		}
		return coroutine;
		void Complete()
		{
			for (int j = 0; j < awaitees.Count; j++)
			{
				awaitees[j].ClearContinueWith();
			}
			coroutine.Complete();
		}
	}

	public static Coroutine<T> WhenAny<T>(params Coroutine<T>[] coroutines)
	{
		Coroutine<T> coroutine = new Coroutine<T>();
		List<Coroutine<T>> awaitees = new List<Coroutine<T>>();
		foreach (Coroutine<T> coroutine2 in coroutines)
		{
			if (!coroutine2.IsCompleted)
			{
				awaitees.Add(coroutine2);
				coroutine2.ContinueWith(Complete);
				continue;
			}
			return coroutine2;
		}
		return coroutine;
		void Complete(Coroutine awaitee)
		{
			for (int j = 0; j < awaitees.Count; j++)
			{
				awaitees[j].m_continuation = null;
			}
			coroutine.Complete((awaitee as Coroutine<T>).Result);
		}
	}

	public static Coroutine WhenAll(params Coroutine[] coroutines)
	{
		Coroutine coroutine = new Coroutine();
		List<Coroutine> awaitees = new List<Coroutine>();
		List<Exception> exceptions = new List<Exception>();
		foreach (Coroutine coroutine2 in coroutines)
		{
			if (!coroutine2.IsCompleted)
			{
				awaitees.Add(coroutine2);
				coroutine2.ContinueWith(OnAwaiteeComplete);
			}
		}
		if (awaitees.Count <= 0)
		{
			return Completed();
		}
		return coroutine;
		void OnAwaiteeComplete(Coroutine awaitee)
		{
			if (awaitee.Exception != null)
			{
				exceptions.Add(awaitee.Exception);
			}
			awaitees.Remove(awaitee);
			if (awaitees.Count == 0)
			{
				if (exceptions.Count == 0)
				{
					coroutine.Complete();
				}
				else
				{
					coroutine.Fail(null, new AggregateException(exceptions.ToArray()));
				}
			}
		}
	}

	public static Coroutine WhenAll<T>(params Coroutine<T>[] coroutines)
	{
		Coroutine coroutine = new Coroutine();
		List<Coroutine> awaitees = new List<Coroutine>();
		List<Exception> exceptions = new List<Exception>();
		foreach (Coroutine coroutine2 in coroutines)
		{
			if (!coroutine2.IsCompleted)
			{
				awaitees.Add(coroutine2);
				coroutine2.ContinueWith(OnAwaiteeComplete);
			}
		}
		if (awaitees.Count <= 0)
		{
			return Completed();
		}
		return coroutine;
		void OnAwaiteeComplete(Coroutine awaitee)
		{
			if (awaitee.Exception != null)
			{
				exceptions.Add(awaitee.Exception);
			}
			awaitees.Remove(awaitee);
			if (awaitees.Count == 0)
			{
				if (exceptions.Count == 0)
				{
					coroutine.Complete((awaitee as Coroutine<T>).Result);
				}
				else
				{
					coroutine.Fail(null, new AggregateException(exceptions.ToArray()));
				}
			}
		}
	}
}
