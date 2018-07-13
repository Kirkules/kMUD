using System;
using System.Collections.Generic;

namespace KirkProject0
{
	class kMUDDisplay
	{
		public SliceableQueue<string> OutputHistory { get; set; }

		public int LastLineToDisplay { get; set; }

		public ConsoleColor FG { get; set; } // foreground color
		public ConsoleColor BG { get; set; } // background color
		public ConsoleColor CC { get; set; } // command color

		// shown on second-to-bottom line
		public string kMUDMessage { get; set; }

		public bool Echoing { get; set; }

		private string theCommand;
		public string Command {
			get{ return theCommand; } 
			set {
				theCommand = value; 
				InsertPosition = value.Length;
			}
		}
		// shown on bottom line

		public int InsertPosition;
		// position in command to insert next character
		public bool Highlighted = false;

		public kMUDDisplay (int historyLimit = 2000)
		{
			OutputHistory = new SliceableQueue<string> (historyLimit);
			LastLineToDisplay = 0;
			// fill the console buffer with blank spaces to work within
			for (int i = 0; i <= Console.WindowHeight; i++) {
				Console.WriteLine ();
				OutputHistory.Enqueue (" ");
			}

			// later, color schemes may change these.
			FG = ConsoleColor.White;
			BG = ConsoleColor.Black;

			CC = ConsoleColor.DarkCyan;
			Echoing = true;
		}

		public void AddOutputLine (string text)
		{
			// break the input text up into lines of the right size
			List<string> lines = Chunker.ChunkString(text, Console.WindowWidth);
			foreach (string line in lines) {
				OutputHistory.Enqueue (line);
			}
			LastLineToDisplay = OutputHistory.Count;
			Update ();
		}

		public void AddToCommand (char c)
		{
			theCommand = theCommand.Substring (0, InsertPosition) + c.ToString () +
				theCommand.Substring (InsertPosition, theCommand.Length - InsertPosition);
			InsertPosition += 1;
			WriteCommand ();
		}

		public void BackspaceDelete ()
		{
			theCommand = theCommand.Substring (0, Math.Max(InsertPosition - 1, 0)) +
				theCommand.Substring (InsertPosition, theCommand.Length - InsertPosition);
			InsertPosition = Math.Max (InsertPosition - 1, 0);
			WriteCommand ();
		}

		public void MoveCursorLeft ()
		{
			InsertPosition = Math.Max (InsertPosition - 1, 0);
			WriteCommand ();
		}

		public void MoveCursorRight ()
		{
			InsertPosition = Math.Min (InsertPosition + 1, theCommand.Length);
			WriteCommand ();
		}

		public void SpecialWrite(string text) {
			if (text.Length == 0) {
				return;
			}
			switch (text [0]) {
			case '#':
				// if starts with 2 # symbols, it means it was a kMUD command from the user
				if (text.Length > 1 && text [1] == '#') {
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write (text.Substring (1, text.Length - 1));
					Console.ForegroundColor = FG;
				} else {  // otherwise it's a command the user sent to the MUD
					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.Write (text.Substring(1, text.Length-1));
					Console.ForegroundColor = FG;
				}
				break;
			default:
				Console.Write (text);
				break;
			}
		}

		public void WriteCommand() {
			// clear bottom line
			Console.SetCursorPosition (0, Console.WindowHeight - 1);
			for (int i = 0; i < Console.WindowWidth; i++)
				Console.Write (' ');
			Console.SetCursorPosition (0, Console.WindowHeight - 1);

			// highlight command if it was just entered, otherwise print normally
			if (Highlighted) { 
				Console.BackgroundColor = FG;
				Console.ForegroundColor = BG;
			} else {
				Console.ForegroundColor = CC;
			}
			Console.Write (theCommand);
			Console.BackgroundColor = BG;
			Console.ForegroundColor = FG;

			// move the cursor so the user can see where insertion/deletion in the command happens
			Console.SetCursorPosition (InsertPosition, Math.Min (Console.BufferHeight - 1, Console.WindowHeight - 1));
		}


		// TODO: finish this new version of Update
		// Redraw the display within the console window without overflowing the buffer, so no scrolling happens
		public void Update() {
			Console.Clear ();
			// start the cursor in a place so that there's exactly enough room to print out all of the output from the 
			int CursorRow = 0;
			Console.SetCursorPosition (0, CursorRow);

			// write output lines until the cursor is at 4th from the bottom line
			foreach (string line in OutputHistory.Slice(LastLineToDisplay - Console.WindowHeight + 4, LastLineToDisplay)) {
				SpecialWrite(line);
				CursorRow += 1;
				Console.SetCursorPosition (0, CursorRow);
			}

			Console.SetCursorPosition (0, Console.WindowHeight-4);
			// draw the kMUD messages and the command in the bottom 4 lines
			Console.Write ("\u2508\u2508\u2508kMUD (Kirk's MUD client)");
			for (int i = 0; i < Console.WindowWidth - 27; i++) {
				Console.Write ('\u2508');
			}
			CursorRow += 1;
			Console.SetCursorPosition (0, Console.WindowHeight-3);

			// write as much of the message as fits
			// TODO: make message area expand for larger messages?
			Console.Write (kMUDMessage.Substring(0, Math.Min(Console.WindowWidth, kMUDMessage.Length)));

			Console.SetCursorPosition (0, Console.WindowHeight-2);

			for (int i = 0; i < Console.WindowWidth; i++) {
				Console.Write ('\u2508');
			}

			if (Echoing) {
				WriteCommand ();
			} else {
				// put the cursor there
				Console.SetCursorPosition (0, Console.WindowHeight - 1);
			}

		}


	}
}

