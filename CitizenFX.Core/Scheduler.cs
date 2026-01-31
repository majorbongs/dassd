using System;
using System.Collections.Generic;
using System.Threading;

namespace CitizenFX.Core;

public class Scheduler
{
	private static readonly LinkedList<Tuple<ulong, Action>> s_queue = new LinkedList<Tuple<ulong, Action>>();

	private static List<Action> s_nextFrame = new List<Action>();

	private static List<Action> s_nextFrameProcessing = new List<Action>();

	public static TimePoint CurrentTime { get; internal set; }

	internal static Thread MainThread { get; private set; }

	internal static void Initialize()
	{
		MainThread = Thread.CurrentThread;
	}

	public static void Schedule(Action coroutine)
	{
		if (coroutine != null)
		{
			lock (s_nextFrame)
			{
				s_nextFrame.Add(coroutine);
				return;
			}
		}
		throw new ArgumentNullException("coroutine");
	}

	public static void Schedule(Action coroutine, ulong delay)
	{
		Schedule(coroutine, CurrentTime + delay);
	}

	public static void Schedule(Action coroutine, TimePoint time)
	{
		if ((ulong)time <= (ulong)CurrentTime)
		{
			Schedule(coroutine);
			return;
		}
		if (coroutine != null)
		{
			lock (s_queue)
			{
				for (LinkedListNode<Tuple<ulong, Action>> linkedListNode = s_queue.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
				{
					if ((ulong)time < linkedListNode.Value.Item1)
					{
						s_queue.AddBefore(linkedListNode, new Tuple<ulong, Action>(time, coroutine));
						return;
					}
				}
				s_queue.AddLast(new Tuple<ulong, Action>(time, coroutine));
				return;
			}
		}
		throw new ArgumentNullException("coroutine");
	}

	internal static void Unschedule(Action coroutine, TimePoint time)
	{
		if ((ulong)time > (ulong)CurrentTime)
		{
			lock (s_queue)
			{
				LinkedListNode<Tuple<ulong, Action>> linkedListNode = s_queue.First;
				while (linkedListNode != null && linkedListNode.Value.Item1 <= (ulong)time)
				{
					if ((Delegate)linkedListNode.Value.Item2 == (Delegate)coroutine)
					{
						s_queue.Remove(linkedListNode);
						break;
					}
					linkedListNode = linkedListNode.Next;
				}
				return;
			}
		}
		throw new ArgumentNullException("coroutine");
	}

	internal static void Update()
	{
		ulong num = CurrentTime;
		s_nextFrameProcessing = Interlocked.Exchange(ref s_nextFrame, s_nextFrameProcessing);
		for (int i = 0; i < s_nextFrameProcessing.Count; i++)
		{
			try
			{
				s_nextFrameProcessing[i]();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
		}
		s_nextFrameProcessing.Clear();
		lock (s_queue)
		{
			LinkedListNode<Tuple<ulong, Action>> linkedListNode = s_queue.First;
			while (linkedListNode != null)
			{
				LinkedListNode<Tuple<ulong, Action>> linkedListNode2 = linkedListNode;
				linkedListNode = linkedListNode2.Next;
				if (linkedListNode2.Value.Item1 > num)
				{
					break;
				}
				try
				{
					linkedListNode2.Value.Item2();
				}
				catch (Exception ex2)
				{
					Debug.WriteLine(ex2.ToString());
				}
				finally
				{
					s_queue.Remove(linkedListNode2);
				}
			}
		}
	}

	internal static ulong NextTaskTime()
	{
		if (s_nextFrame.Count != 0)
		{
			return CurrentTime;
		}
		if (s_queue.Count != 0)
		{
			return s_queue.First.Value.Item1;
		}
		return ulong.MaxValue;
	}
}
