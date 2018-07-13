using System;

namespace KirkProject0
{
	class TELNET
	{
		// Negotiation values
		public class Negotiation {
			public const byte SE = 240;
			public const byte NOP = 241;
			public const byte Break = 243;
			public const byte GoAhead = 249;
			public const byte WILL = 251;
			public const byte WONT = 252;
			public const byte DO = 253;
			public const byte DONT = 254;
			public const byte IAC = 255;

			public static bool isNegotiation(byte b){
				if (b == SE || b == NOP || b == Break || b == GoAhead || b == WILL || 
					b == WONT || b == DO || b == DONT || b == IAC) {
					return true;
				}
				return false;
			}
		};
		// Options
		public class Options{ 
			public const byte BinaryTransmission = 0;
			public const byte Echo = 1;
			public const byte Reconnection = 2;
			public const byte SuppressGoAhead = 3;
			public const byte ApproxMessageSize = 4;
			public const byte Status = 5;
			public const byte TimingMark = 6;
			public const byte RemoteControlledTransAndEcho = 7;
			public const byte OutputLineWidth = 8;
			public const byte OutputPageSize = 9;
			public const byte OutputCarriageReturnDisposition = 10;
			public const byte OutputHorizontalTabStops = 11;
			public const byte OutputHorizontalTabDisposition = 12;
			public const byte OutputFormFeedDisposition = 13;
			public const byte OutputVerticalTabStops = 14;
			public const byte OutputVerticalTabDisposition = 15;
			public const byte OutputLinefeedDisposition = 16;
			public const byte ExtendedASCII = 17;
			public const byte Logout = 18;
			public const byte ByteMacro = 19;
			public const byte DataEntryTerminal = 20;
			public const byte SUPDUP = 21;
			public const byte SUPDUPOutput = 22;
			public const byte SendLocation = 23;
			public const byte TerminalType = 24;
			public const byte EndOfRecord = 25;
			public const byte TACACSUserIdentification = 26;
			public const byte OutputMarking = 27;
			public const byte TerminalLocationNumber = 28;
			public const byte Telnet3270Regime = 29;
			public const byte X3PAD = 30;
			public const byte NegotiateAboutWindowSize = 31;
			public const byte TerminalSpeed = 32;
			public const byte RemoteFlowControl = 33;
			public const byte Linemode = 34;
			public const byte XDisplayLocation = 35;
			public const byte EnvironmentOption = 36;
			public const byte AuthenticationOption = 37;
			public const byte EncryptionOption = 38;
			public const byte NewEnvironmentOption = 39;
			public const byte TN3270E = 40;
			public const byte XAUTH = 41;
			public const byte CHARSET = 42;
			public const byte TelnetRemoteSerialPort = 43;
			public const byte ComPortControlOption = 44;
			public const byte TelnetSuppressLocalEcho = 45;
			public const byte TelnetStartTLS = 46;
			public const byte KERMIT = 47;
			public const byte SENDURL = 48;
			public const byte FORWARD_X = 49;
			public const byte TeloptPragmaLogon = 138;
			public const byte TeloptSSPILogon = 139;
			public const byte TeloptPragmaHeartbeat = 140;
			public const byte ExtendedOptionsList = 255;

			public static bool isOption(byte b){
				return ((0 <= b && b <= 49) || (138 <= b && b <= 140) || b == 255);
			}
		};

	}
}

