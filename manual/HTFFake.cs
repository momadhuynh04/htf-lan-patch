// Add this class to assembly: com.rlabrecque.steamworks.net.dll
using System;

namespace Steamworks
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

		public static ulong MakeID(string name)
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
			string text = new string(array).TrimEnd(new char[1] { '_' });
			if (string.IsNullOrEmpty(text))
			{
				return "Fisher";
			}
			return text;
		}

		public static string GetPersonaName(CSteamID steamIDFriend)
		{
			try
			{
				InteropHelp.TestIfAvailableClient();
				return InteropHelp.PtrToStringUTF8(NativeMethods.ISteamFriends_GetFriendPersonaName(CSteamAPIContext.GetSteamFriends(), steamIDFriend));
			}
			catch
			{
				return DecodeName(steamIDFriend.m_SteamID);
			}
		}

		private static string GetGameRoot()
		{
			string location = typeof(HTFFake).Assembly.Location;
			return System.IO.Directory.GetParent(System.IO.Directory.GetParent(location).FullName).FullName;
		}
	}
}
