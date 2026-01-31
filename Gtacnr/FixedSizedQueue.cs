using System.Collections.Generic;

namespace Gtacnr;

public class FixedSizedQueue<T>
{
	private readonly Queue<T> queue = new Queue<T>();

	public int Size { get; private set; }

	public FixedSizedQueue(int size)
	{
		Size = size;
	}

	public void Enqueue(T obj)
	{
		queue.Enqueue(obj);
		while (queue.Count > Size)
		{
			queue.Dequeue();
		}
	}

	public IReadOnlyCollection<T> AsCollection()
	{
		return queue;
	}

	public IEnumerable<T> AsEnumerable()
	{
		return queue;
	}
}
