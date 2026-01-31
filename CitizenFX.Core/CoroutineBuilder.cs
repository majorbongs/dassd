using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CitizenFX.Core;

public sealed class CoroutineBuilder
{
	public Coroutine Task { get; } = new Coroutine();

	public static CoroutineBuilder Create()
	{
		return new CoroutineBuilder();
	}

	public void SetResult()
	{
		Task.Complete();
	}

	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		stateMachine.MoveNext();
	}

	public void SetException(Exception exception)
	{
		Task.SetException(exception);
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		TStateMachine sMachine = stateMachine;
		awaiter.OnCompleted(delegate
		{
			if (Thread.CurrentThread == Scheduler.MainThread)
			{
				sMachine.MoveNext();
			}
			else
			{
				Scheduler.Schedule(((IAsyncStateMachine)sMachine).MoveNext);
			}
		});
	}

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		TStateMachine sMachine = stateMachine;
		awaiter.UnsafeOnCompleted(delegate
		{
			if (Thread.CurrentThread == Scheduler.MainThread)
			{
				sMachine.MoveNext();
			}
			else
			{
				Scheduler.Schedule(((IAsyncStateMachine)sMachine).MoveNext);
			}
		});
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		Debug.WriteLine("SetStateMachine");
	}
}
public sealed class CoroutineBuilder<T>
{
	public Coroutine<T> Task { get; } = new Coroutine<T>();

	public static CoroutineBuilder<T> Create()
	{
		return new CoroutineBuilder<T>();
	}

	public void SetResult(T value)
	{
		Task.Complete(value);
	}

	public void SetException(Exception exception)
	{
		Task.SetException(exception);
	}

	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
	{
		stateMachine.MoveNext();
	}

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		TStateMachine sMachine = stateMachine;
		awaiter.OnCompleted(delegate
		{
			if (Thread.CurrentThread == Scheduler.MainThread)
			{
				sMachine.MoveNext();
			}
			else
			{
				Scheduler.Schedule(((IAsyncStateMachine)sMachine).MoveNext);
			}
		});
	}

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
	{
		TStateMachine sMachine = stateMachine;
		awaiter.UnsafeOnCompleted(delegate
		{
			if (Thread.CurrentThread == Scheduler.MainThread)
			{
				sMachine.MoveNext();
			}
			else
			{
				Scheduler.Schedule(((IAsyncStateMachine)sMachine).MoveNext);
			}
		});
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine)
	{
		Debug.WriteLine("SetStateMachine");
	}
}
