// // Add this class to assembly: Assembly-CSharp.dll
using System;
using System.Reflection;

public static class HTFDirectJoin
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
			string text = System.IO.Path.Combine(GetGameRoot(), "ip.txt");
			if (System.IO.File.Exists(text))
			{
				string[] allLines = System.IO.File.ReadAllLines(text);
				foreach (string text2 in allLines)
				{
					string text3 = text2.Trim();
					if (text3.Length > 0 && !text3.StartsWith("#"))
					{
						return Normalize(text3);
					}
				}
			}
		}
		catch
		{
		}
		return null;
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

	public static void JoinDirect(string addressAndPort)
	{
		ConnectionManager instance = ConnectionManager.Instance;
		string ip;
		ushort port;
		ParseAddress(addressAndPort, out ip, out port);
		object[] parameters = new object[1] { false };
		typeof(ConnectionManager).GetMethod("SetTransport", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, parameters);
		FishNet.Transporting.Multipass multipass = (FishNet.Transporting.Multipass)typeof(ConnectionManager).GetField("_multipass", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
		FishNet.Transporting.UTP.UnityTransport unityTransport = multipass.ClientTransport as FishNet.Transporting.UTP.UnityTransport;
		unityTransport.SetClientAddress(ip);
		unityTransport.SetPort(port);
		typeof(ConnectionManager).GetField("_clientConnectionExpected", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, true);
		if (!multipass.ClientTransport.StartConnection(server: false))
		{
			typeof(ConnectionManager).GetField("_returnToMenuRequested", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, true);
		}
		MainMenuManager.CrashAnimation();
	}

	private static string GetGameRoot()
	{
		try
		{
			string location = typeof(HTFDirectJoin).Assembly.Location;
			return System.IO.Directory.GetParent(System.IO.Directory.GetParent(location).FullName).FullName;
		}
		catch
		{
			return ".";
		}
	}
}
