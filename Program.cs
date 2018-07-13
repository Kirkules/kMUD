using System;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace KirkProject0
{
	class kMUDClient
	{
		// Ongoing environment status variables
		private static string Host { get; set; }
		private static int Port { get; set; }
		private static TcpClient Client { get; set; }
		private static kMUDDisplay Display { get; set; }
		private static char CommandSplitter { get; set; }
		private static NetworkStream netStream { get; set; }
		private static ConsoleKeyInfo KeyPressed { get; set; }
		private static bool repeatCommand { get; set; }
		private static Dictionary<string, Action> CommandFunctions;

		// Command History variables
		private static int CurrentCommand { get; set; } // position in command history
		private static List<string> CommandHistory { get; set; } // list of commands typed by the user

		// Alias variables
		private static Dictionary<string, Tuple<int, string>> Aliases;
		
		public static void Main (string[] args)
		{
			// Initialize the display
			Display = new kMUDDisplay ();
			Display.Command = "";

			// Initialize Command stuff
			CommandHistory = new List<string> ();
			CommandHistory.Add ("");
			CommandSplitter = ';'; // default, obvious splitter
			Aliases = new Dictionary<string, Tuple<int, string>>();

			// put functions for each command into the dict
			CommandFunctions = new Dictionary<string, Action> ();
			CommandFunctions.Add ("#commands", ShowCommands);
			CommandFunctions.Add ("#command", ShowCommands);
			CommandFunctions.Add ("#quit", WrapUp);
			CommandFunctions.Add ("#exit", WrapUp);
			CommandFunctions.Add ("#alias", AliasCommand);
			CommandFunctions.Add ("#unalias", UnaliasCommand);
			CommandFunctions.Add ("not a command", () => {Display.kMUDMessage = "That is not a kMUD command...";});

			// must give a Host and Port connect to!
			if (args.Length < 2) {
				Console.WriteLine ("Please enter both a hostname and a port.");
				WrapUp ();
			}

			// check that host and port are host-like and port-like
			Host = args [0];
			uint tempPort;
			if (!UInt32.TryParse (args [1], out tempPort)) {
				Console.WriteLine ("Port must be a nonnegative integer...");
				WrapUp ();
			} else {
				Port = (int)tempPort;
			}



			// Try to connect
			ConnectToMUD ();
			if (!Client.Connected) {
				Console.WriteLine ("Connection was unsuccessful. Please try again later.");
				WrapUp ();
			}
			netStream = Client.GetStream ();




			// Startup the MUD interaction
			Display.Update ();
			byte[] incomingData = new byte[1048576];
			byte[] telnet_overflow = new byte[4]; // in case telnet command is on next line
			int bytesRead = 0;
			bool tooMuchData = false;
			StringBuilder incomingLinesBuilder = new StringBuilder ();
			List<string> incomingLines;
			while (Client.Connected) {

				// Gather input from MUD
				// Initial code structure for this part taken from the csharp msdn.microsoft.com examples for the NetworkStream class
				while (netStream.DataAvailable) {
					try{
						bytesRead = netStream.Read(incomingData, 0, incomingData.Length);
					} catch (Exception e) {
						Display.kMUDMessage = "Network Stream exception: " + e.Message;
					}
					// ignore bytes that are not in ascii range 35 to 127
					int b = 0;
					// scan for telnet stuff
					while (b < incomingData.Length){
						if (incomingData [b] == 255) { // IAC (telnet interpret as command) byte
							// if next byte is 255, this is just a data byte 255. Otherwise, do telnet handling
							if (incomingData.Length > b + 1 && incomingData [b + 1] != 255) {								
								byte[] telnetCommunication = new byte[2];
								telnetCommunication [0] = incomingData [b + 1];
								if (incomingData.Length > b + 1) {
									telnetCommunication [1] = incomingData [b + 2];
								} else {
									// TELNET stuff is split over the end of this original buffer, so read more to get the rest
									try{
										bytesRead = netStream.Read(telnet_overflow, 0, 4); // 
									} catch (Exception e) {
										Display.kMUDMessage = "Network Stream exception: " + e.Message;
									}
									break; // telnet stuff broken over two incomingdata chunks... 
											// means MUD is being impolite and sending too much stuff, so just
											// make this fail.
								}
								HandleTelnetCommunication (telnetCommunication);
								incomingData [b] = incomingData [b + 1] = incomingData [b + 2] = 32; // ascii for space
								b += 3;
							}
						} else {
							b += 1;
						}

					}

					incomingLinesBuilder.AppendFormat("{0}", Encoding.ASCII.GetString(incomingData, 0, bytesRead));
					if (tooMuchData) {
						break;
					}
				}


				// split input up into lines if there is new stuff from the MUD.
				if (bytesRead > 0) {
					incomingLines = new List<string> (incomingLinesBuilder.ToString ().Split ('\n'));
					foreach (string line in incomingLines) {
						Display.AddOutputLine (line);
					}
					bytesRead = 0;
					incomingData = new byte[4096];
					incomingLines = new List<string> ();
					incomingLinesBuilder.Clear ();
					tooMuchData = false;
				}

				// Deal with user input
				// The KeyPressed true parameter means don't automatically print typed char to console
				if (Console.KeyAvailable) {
					KeyPressed = Console.ReadKey (true);
					HandleKeyPress ();
				}
			}
			Client.Close ();
			netStream.Close ();
			Display.kMUDMessage = "Connection to " + Host.ToString () + " lost...";
			Display.Update ();
		}


		private static void ConnectToMUD (uint secondsToConnect = 5)
		{
			Stopwatch connectTimer = new Stopwatch ();
			Console.WriteLine ("Trying to connect to " + Host + " on port " + Port + ". Please wait...");
			connectTimer.Start ();

			Client = new TcpClient ();
			IAsyncResult connectionResult = Client.BeginConnect (Host, (int)Port, (res) => {
			}, "connected.");

			connectionResult.AsyncWaitHandle.WaitOne (((int)secondsToConnect) * 1000);
			connectTimer.Stop ();
			if (connectionResult == null || connectionResult.IsCompleted) {
				Display.kMUDMessage = "Connected to " + Host + " on " + Port.ToString () + " in " +
				(connectTimer.ElapsedMilliseconds / 1000.0).ToString () + " seconds.";
			} else {
				Console.WriteLine ("Took longer than " + secondsToConnect.ToString () + " seconds to connect.");
				WrapUp ();
			}
		}

		private static void WrapUp () {
			Client.Close ();
			Environment.Exit (0);
		}


		private static void HandleKeyPress ()
		{
			if (KeyPressed.Key == ConsoleKey.RightArrow || KeyPressed.Key == ConsoleKey.LeftArrow) {
				Display.Highlighted = false;
			} else if (KeyPressed.Key != ConsoleKey.Enter && Display.Highlighted == true) {
				Display.Highlighted = false;
				Display.Command = "";
			}

			switch (KeyPressed.Key) {
			case ConsoleKey.Enter:
				// no command = no behavior
				if (Display.Command.Length == 0) {
					try{
						if (netStream.CanWrite) {
							byte[] toSend = ASCIIEncoding.ASCII.GetBytes ("\r\n");
							Client.Client.Send (toSend);
						} else {
							Display.kMUDMessage = "Can't send data to the MUD...?";
						}
					} catch (Exception e) {
						Display.kMUDMessage = "Got an error...: " + e.Message;
					}
					break;
				}

				// If command starts with "#", process it
				if (Display.Command [0] == '#') {
					string firstWord = Display.Command.Trim ().Split (' ') [0];
					if (CommandFunctions.ContainsKey (firstWord)) {
						CommandFunctions [firstWord] ();
					}
					// HandleCommand (Display.Command);
				}
				// otherwise, check if it's an alias, then send it to the MUD and let them deal with it.
				else {
					try {
						if (netStream.CanWrite) {
							// check if there's an alias first
							string[] wordsInCommand = Display.Command.Split (' ');
							if (Aliases.ContainsKey(wordsInCommand[0])){
								// it's an aliased command, so process it first
								string actualCommand = "";
								for (int i = 1; i < wordsInCommand.Length; i++) {
									actualCommand += wordsInCommand [i] + " ";
								}
								Display.Command = ConvertAlias (wordsInCommand [0], actualCommand);

							}

							// split command by semicolons
							// TODO make this work in conjunction with aliases
							if (CommandSplitter != '\0') { // signifies no splitting desired
								List<string> splitCommands = new List<string> (Display.Command.Split (CommandSplitter));
								foreach (string command in splitCommands) {
									byte[] toSend = ASCIIEncoding.ASCII.GetBytes (command + "\r\n");
									Client.Client.Send (toSend);
								}
							} else {
								byte[] toSend = ASCIIEncoding.ASCII.GetBytes (Display.Command + "\r\n");
								Client.Client.Send (toSend);
							}
						} else {
							Display.kMUDMessage = "Can't send data to the MUD...?";
						}
					} catch (Exception e){
						Display.kMUDMessage = "Got an error...: " + e.Message;
					}
				}
				// record command in command history
				if (Display.Echoing) {
					CommandHistory.Add (Display.Command);
				}
				CurrentCommand = CommandHistory.Count;

				// command was executed, so record it with a '#' prepended
				if (Display.Echoing) {
					Display.AddOutputLine ("#" + Display.Command);
					// show the command Highlighted, so it's clear you can hit enter and do the same command again,
					// but if you type anything else or hit delete it will overwrite it.
					Display.Highlighted = true;

					// update the display to show this.
					repeatCommand = true;
				} else {
					Display.Highlighted = false;
					repeatCommand = false;
					Display.Command = "";
				}


				Display.Update ();
				break;
			
			case ConsoleKey.Backspace:
				// ALT+Backspace deletes a whole word, not just a character
				Display.BackspaceDelete ();
				Display.Update ();
				break;

			case ConsoleKey.Escape:
				// somehow undo or cancel something, maybe?
				Display.Highlighted = false;
				Display.Command = "";
				Display.Update ();
				break;

			case ConsoleKey.UpArrow:
				// if there is no more command history, do nothing
				if (CurrentCommand <= 0) {
					break;
				}
				// If user started typing a command, save it. If not, save a blank space (if there wasn't one)
				// so the user can return to a blank slate by pressing down enough.
				if (CurrentCommand == CommandHistory.Count && CommandHistory [CommandHistory.Count - 1] != Display.Command) { 
					CommandHistory.Add (Display.Command);
				}
				// scroll back one command in the history, if possible
				CurrentCommand = Math.Max (CurrentCommand - 1, 0);
				Display.Command = CommandHistory [CurrentCommand];
				Display.Highlighted = true;
				Display.Update ();
				break;

			case ConsoleKey.DownArrow:
				// scroll forward one command in the history, if possible. Don't overwrite history.
				if (CurrentCommand >= CommandHistory.Count || CommandHistory.Count == 0) {
					break;
				}
				CurrentCommand = Math.Min (CurrentCommand + 1, CommandHistory.Count - 1);
				Display.Command = CommandHistory [CurrentCommand];
				Display.Highlighted = true;

				Display.Update ();
				break;

			case ConsoleKey.RightArrow:
				// move the cursor to the right 
				Display.MoveCursorRight ();
				Display.Update ();
				break;

			case ConsoleKey.LeftArrow:
				// move cursor to the left
				Display.MoveCursorLeft ();
				Display.Update ();
				break;

			default:
				// Display.Command += KeyPressed.KeyChar;
				Display.AddToCommand (KeyPressed.KeyChar);
				Display.Update ();
				break;
			}
		}

		private static void ShowCommands(){
			Display.AddOutputLine ("kMUD commands (add a # symbol in front of these):\n" +
				"alias\t\texit\t\tcommands\t\tcommand\t\t\n" +
				"unalias\t\t");
		}

		private static void AliasCommand(){
			List<string> commandWords = new List<string> (Display.Command.Trim().Split (' '));
			Display.kMUDMessage = Display.Command;
			if (commandWords.Count == 1) {
				// if the user just typed "#alias", show the list of aliases
				// ShowAliases ();
			} else if (commandWords.Count == 2) {
				// if the user typed "#alias somealiasname" and nothing else, show the definition of that alias.
				if (Aliases.ContainsKey (commandWords [1])) {
					Display.AddOutputLine ("\t" + commandWords [1] + ": " + Aliases [commandWords [1]]);
					Display.kMUDMessage = "To see a list of defined aliases, type #alias. To delete an alias, type #unalias alias.";
				}
			} else {
				// user tried to define an alias
				string aliasText = "";
				for (int i = 2; i < commandWords.Count; i++) {
					aliasText += commandWords[i] + " ";
				}

				AddAlias (commandWords[1], aliasText);
			}
		}

		private static void UnaliasCommand(){
			List<string> commandWords = new List<string> (Display.Command.Trim().Split (' '));
			// remove an alias from the alias list, if it's there
			if (commandWords.Count > 1 && Aliases.ContainsKey (commandWords [1])) {
				Aliases.Remove (commandWords [1]);
				Display.kMUDMessage = "Removed alias " + commandWords [1] + ".";
			} else {
				Display.kMUDMessage = "There is no alias called " + commandWords [1] + ".";
			}
		}


		private static void AddAlias(string alias, string text){
			// replace "%#" things with "{#}" things, so we can just use csharp string.format
			int aliasPos = 0;
			string newAliasText = "";
			int largestNumberSeen = -1;
			while (aliasPos < text.Length) {
				// if we find a % sign, see if it is followed by some number
				if (text [aliasPos] == '%') {

					int theNumber = -1;
					int numDigits = 0;
					while (aliasPos + numDigits + 1 < text.Length &&
					       int.TryParse (text.Substring (aliasPos + 1, numDigits+1), out theNumber)) {
						// just keep parsing longer substrings following the "%" until it doesn't yield a number
						numDigits += 1;
					}
					if (numDigits > 0) {
						newAliasText += "{" + String.Format ("{0}", theNumber) + "}";
						largestNumberSeen = Math.Max (largestNumberSeen, theNumber);
					} else {
						newAliasText += "%";
					}
					aliasPos += numDigits + 1; // skip the % and the digits of the number
				} else {
					// not a % sign, just copy over the character
					newAliasText += text[aliasPos];
					aliasPos += 1;
				}
			}

			if (Aliases.ContainsKey (alias)) {
				Display.kMUDMessage = "Overwriting alias " + alias + ".";
				Aliases [alias] = Tuple.Create (largestNumberSeen, newAliasText);
			} else {
				Aliases.Add (alias, Tuple.Create (largestNumberSeen, newAliasText));
			}
		}
										

		private static void ShowAliases(){
			// show the aliases that have been made
			if (Aliases.Count == 0) {
				Display.kMUDMessage = "You have no aliases defined right now.";
			} else {
				Display.AddOutputLine ("Currently defined aliases:");
				foreach (string alias in Aliases.Keys) {
					Display.AddOutputLine ("\t" + alias + ": " + Aliases [alias]);
				}
			}
		}

		private static string ConvertAlias(string alias, string commandText) {
			// user input to the %1, %2, %3... parts of the alias
			List<string> commandPieces = new List<string>(commandText.Split (' '));

			// fill out commandPieces with numbers if it's not long enough to format
			// number of formatted items in the alias is equal to 
			for (int i = Math.Max(0, commandPieces.Count-1); i <= Aliases [alias].Item1; i++) {
				commandPieces.Add (i.ToString());
			}

			string actualCommand = String.Format (Aliases[alias].Item2, commandPieces.ToArray ());

			return actualCommand;
		}
			
		private static void HandleTelnetCommunication(byte[] data){
			// this doesn't make sense by the TELNET specification, but it's what the MUD sends at password entry...
			if (data [0] == (byte)TELNET.Negotiation.WONT && data [1] == (byte)TELNET.Options.Echo) {
				// server asked us to turn on echo
				Display.Echoing = true; // happy to oblige!
			} else if (data [0] == (byte)TELNET.Negotiation.WILL && data [1] == (byte)TELNET.Options.Echo) {
				// server demands we turn off echo
				Display.Echoing = false; // happy to oblige!
			} else {
				// we don't do other telnet business yet!
			}
		}
	}

}
