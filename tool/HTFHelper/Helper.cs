using System;
using System.Reflection;
using Steamworks;

namespace HTF
{
	public static class HTFFake
	{
		private const ulong IDTag = 0x1000000000000000UL;

		private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";

		private static string _cachedName;

		public static string GetLocalName()
		{
			if (_cachedName != null)
			{
				return _cachedName;
			}
			string text = "";
			try
			{
				string path = System.IO.Path.Combine(GetGameRoot(), "playername.txt");
				if (System.IO.File.Exists(path))
				{
					text = System.IO.File.ReadAllText(path).Trim();
				}
			}
			catch
			{
			}
			if (string.IsNullOrEmpty(text))
			{
				try
				{
					text = Environment.UserName;
				}
				catch
				{
					text = "Fisher";
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				text = "Fisher";
			}
			_cachedName = text;
			return _cachedName;
		}

		public static CSteamID GetLocalSteamID()
		{
			return new CSteamID(MakeID(GetLocalName()));
		}

		private static ulong MakeID(string name)
		{
			string text = name.ToLowerInvariant();
			ulong num = IDTag;
			for (int i = 0; i < 10; i++)
			{
				char value = (i < text.Length) ? text[i] : '_';
				int num2 = Alphabet.IndexOf(value);
				if (num2 < 0)
				{
					num2 = Alphabet.IndexOf('_');
				}
				num |= ((ulong)(num2 & 63)) << (i * 6);
			}
			return num;
		}

		public static string DecodeName(ulong id)
		{
			ulong num = id & 0xFFFFFFFFFFFFFFFUL;
			if (num == 0L)
			{
				return "Fisher";
			}
			char[] array = new char[10];
			for (int i = 0; i < 10; i++)
			{
				array[i] = Alphabet[(int)((num >> (i * 6)) & 63UL)];
			}
			string text = new string(array).TrimEnd('_');
			if (string.IsNullOrEmpty(text))
			{
				return "Fisher";
			}
			return text;
		}

		public static string GetPersonaName(CSteamID steamIDFriend)
		{
			return DecodeName(steamIDFriend.m_SteamID);
		}

		private static string GetGameRoot()
		{
			try
			{
				string location = typeof(HTFFake).Assembly.Location;
				return System.IO.Directory.GetParent(System.IO.Directory.GetParent(location).FullName).FullName;
			}
			catch
			{
				return ".";
			}
		}
	}

	public static class HTFDirect
	{
		public static ushort HostPort()
		{
			try
			{
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				for (int i = 0; i < commandLineArgs.Length - 1; i++)
				{
					if (commandLineArgs[i] == "--htfport")
					{
						ushort result;
						if (ushort.TryParse(commandLineArgs[i + 1], out result))
						{
							return result;
						}
					}
				}
			}
			catch
			{
			}
			return 7777;
		}

		public static string ReadTargetAddress()
		{
			try
			{
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				for (int i = 0; i < commandLineArgs.Length - 1; i++)
				{
					if (commandLineArgs[i] == "--htfjoin")
					{
						return Normalize(commandLineArgs[i + 1]);
					}
				}
			}
			catch
			{
			}
			try
			{
				string path = System.IO.Path.Combine(GetGameRoot(), "ip.txt");
				if (System.IO.File.Exists(path))
				{
					string[] allLines = System.IO.File.ReadAllLines(path);
					foreach (string text in allLines)
					{
						string text2 = text.Trim();
						if (text2.Length > 0 && !text2.StartsWith("#"))
						{
							return Normalize(text2);
						}
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private static string GetGameRoot()
		{
			try
			{
				string location = typeof(HTFDirect).Assembly.Location;
				return System.IO.Directory.GetParent(System.IO.Directory.GetParent(location).FullName).FullName;
			}
			catch
			{
				return ".";
			}
		}

		private static string Normalize(string addressAndPort)
		{
			string text = addressAndPort.Trim();
			if (text.LastIndexOf(':') <= 0)
			{
				return text + ":" + HostPort();
			}
			return text;
		}

		private static void ParseAddress(string addressAndPort, out string ip, out ushort port)
		{
			string text = addressAndPort.Trim();
			port = 7777;
			ip = text;
			int num = text.LastIndexOf(':');
			if (num > 0 && ushort.TryParse(text.Substring(num + 1), out port))
			{
				ip = text.Substring(0, num);
			}
		}

		private static object GetPrivateField(object obj, string name)
		{
			return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
		}

		private static void SetPrivateField(object obj, string name, object value)
		{
			obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
		}

		private static void InvokePrivate(object obj, string method, object[] args)
		{
			obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
		}

		private static FishNet.Transporting.Multipass.Multipass GetMultipass(ConnectionManager cm)
		{
			return (FishNet.Transporting.Multipass.Multipass)GetPrivateField(cm, "_multipass");
		}

		private static FishNet.Transporting.UTP.UnityTransport GetUtp(FishNet.Transporting.Multipass.Multipass mp)
		{
			return mp.ClientTransport as FishNet.Transporting.UTP.UnityTransport;
		}

		public static void CreateOfflineLobby(ConnectionManager inst)
		{
			InvokePrivate(inst, "SetTransport", new object[1] { false });
			GameInfo.GenerateSeed();
			FishNet.Transporting.Multipass.Multipass multipass = GetMultipass(inst);
			FishNet.Transporting.UTP.UnityTransport unityTransport = GetUtp(multipass);
			unityTransport.SetServerBindAddress("0.0.0.0", FishNet.Transporting.IPAddressType.IPv4);
			unityTransport.SetPort(HostPort());
			multipass.ClientTransport.SetClientAddress("localhost");
			multipass.ClientTransport.StartConnection(server: true);
			multipass.ClientTransport.StartConnection(server: false);
			MainMenuManager.CrashAnimation();
		}

		public static void JoinOfflineLobby(ConnectionManager inst)
		{
			string targetAddress = ReadTargetAddress();
			JoinDirectCore(inst, targetAddress ?? ("localhost:" + HostPort()));
		}

		public static void JoinDirect(string addressAndPort)
		{
			JoinDirectCore(ConnectionManager.Instance, addressAndPort);
		}

		private static void JoinDirectCore(ConnectionManager inst, string addressAndPort)
		{
			ParseAddress(addressAndPort, out var ip, out var port);
			InvokePrivate(inst, "SetTransport", new object[1] { false });
			FishNet.Transporting.Multipass.Multipass multipass = GetMultipass(inst);
			FishNet.Transporting.UTP.UnityTransport unityTransport = GetUtp(multipass);
			unityTransport.SetClientAddress(ip);
			unityTransport.SetPort(port);
			SetPrivateField(inst, "_clientConnectionExpected", true);
			if (!multipass.ClientTransport.StartConnection(server: false))
			{
				SetPrivateField(inst, "_returnToMenuRequested", true);
			}
			MainMenuManager.CrashAnimation();
		}

		// Thay thế SteamManager.CreateLobby(): nút "Multiplayer" giờ host server nghe 0.0.0.0 qua UTP
		public static void HostDirectLobby()
		{
			CreateOfflineLobby(ConnectionManager.Instance);
		}

		// Gọi sau ButtonManager.SetupUI(): cho phép gõ IP (chứa dấu . :) vào ô Lobby ID
		public static void FixLobbyInput(ButtonManager bm)
		{
			try
			{
				TMPro.TMP_InputField field = (TMPro.TMP_InputField)GetPrivateField(bm, "_lobbyIdInputField");
				if (field != null)
				{
					field.contentType = TMPro.TMP_InputField.ContentType.Standard;
					field.characterValidation = TMPro.TMP_InputField.CharacterValidation.None;
					field.characterLimit = 64;
				}
			}
			catch
			{
			}
		}

		public static void JoinByIDButton(ButtonManager bm)
		{
			TMPro.TMP_InputField tMP_InputField = (TMPro.TMP_InputField)GetPrivateField(bm, "_lobbyIdInputField");
			string text = tMP_InputField.text.Trim();
			if (text.Contains("."))
			{
				JoinDirect(text);
				return;
			}
			ulong result;
			if (ulong.TryParse(text, out result))
			{
				SteamManager.JoinLobby(result);
			}
			else
			{
				UnityEngine.MonoBehaviour.print("ID not parseable");
			}
		}

		public static void OnLobbyIDInputFieldChange(ButtonManager bm)
		{
			TMPro.TMP_InputField tMP_InputField = (TMPro.TMP_InputField)GetPrivateField(bm, "_lobbyIdInputField");
			UnityEngine.UI.Button button = (UnityEngine.UI.Button)GetPrivateField(bm, "_joinByIDButton");
			string text = tMP_InputField.text.Trim();
			ulong result;
			button.enabled = !string.IsNullOrEmpty(text) && (ulong.TryParse(text, out result) || text.Contains("."));
		}
	}
}
