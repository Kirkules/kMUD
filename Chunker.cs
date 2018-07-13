using System;
using System.Collections.Generic;

namespace KirkProject0
{
	// break strings (and other objects?) into chunks
	class Chunker
	{
		public static List<string> ChunkString (string str, int chunkSize)
		{
			List<string> result = new List<string> ();
			int i = 0;
			while (i + chunkSize < str.Length) {
				result.Add (str.Substring (i, chunkSize));
				i += chunkSize;
			}
			if (i < str.Length) {
				result.Add (str.Substring (i, str.Length - i));
			}
			return result;
		}
	}
}

