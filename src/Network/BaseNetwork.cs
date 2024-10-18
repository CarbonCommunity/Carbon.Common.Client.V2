using System;
using System.Collections.Concurrent;
using Carbon.Client.SDK;

namespace Carbon.Client;

public abstract class BaseNetwork
{
	public static ArrayPool<byte> BufferPool = new(8388608);

	public class ArrayPool<T>
	{
		private int count;
		private ConcurrentQueue<T[]>[] buffer;

		public ArrayPool(int maxSize)
		{
			count = SizeToIndex(maxSize) + 1;
			buffer = new ConcurrentQueue<T[]>[count];

			for (int index = 0; index < count; ++index)
			{
				buffer[index] = new ConcurrentQueue<T[]>();
			}
		}

		public ConcurrentQueue<T[]>[] GetBuffer() => buffer;

		public T[] Rent(int minSize)
		{
			var index = SizeToIndex(minSize);

			if (!buffer[index].TryDequeue(out var result))
			{
				result = new T[IndexToSize(index)];
			}

			return result;
		}

		public void Return(T[] array) => buffer[SizeToIndex(array.Length)].Enqueue(array);

		public int SizeToIndex(int size)
		{
			var index = 0;

			while ((size >>= 1) != 0)
			{
				++index;
			}

			return index;
		}

		public int IndexToSize(int index) => 1 << index;
	}

	public abstract void OnNetwork();
}
