using System;
using System.Collections.Generic;
using System.Linq;

namespace KirkProject0
{
	// A queue that enforces a limit on the number of elements it contains, and that provides easy slicing.
	// If Limit < 0, it will not enforce any limit.
	class SliceableQueue<T>
	{
		public int Limit { get; set; }
		public Queue<T> theQueue = new Queue<T> ();
		public int Count { get { return theQueue.Count; } set { } }
		public SliceableQueue (int limit = -1)
		{
			Limit = limit;
		}

		public void Enqueue (T obj)
		{
			theQueue.Enqueue (obj);
			if (Limit > 0 && theQueue.Count > Limit) {
				theQueue.Dequeue ();
			}
		}

		// endpoint-inclusive slice of the history
		public List<T> Slice (int start, int end)
		{
			end = Math.Min (end, theQueue.Count);
			start = Math.Max (0, start);
			return theQueue.Skip (start).Take (end - start).ToList (); // if end < start, let Take throw an exception for us
		}
	}
}

