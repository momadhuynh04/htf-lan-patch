using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Patcher
{
	const string HelperAsmName = "HTFHelper";
	static ModuleDefinition helper;
	static string extraRefDir;

	static int Main(string[] args)
	{
		string managedDir = args[0];
		string outDir = args[1];
		extraRefDir = args.Length > 2 && args[2].Length > 0 ? args[2] : null;
		string swIn = Path.Combine(managedDir, "com.rlabrecque.steamworks.net.dll");
		string acIn = Path.Combine(managedDir, "Assembly-CSharp.dll");
		string hIn = Path.Combine(managedDir, "Heathen.Steamworks.dll");

		helper = ModuleDefinition.ReadModule(Path.Combine(outDir, HelperAsmName + ".dll"));

		var swOut = Patch(swIn, outDir, PatchSteamworks, "steamworks.patched.dll");
		var acOut = Patch(acIn, outDir, PatchAssemblyCSharp, "assembly-csharp.patched.dll");
		var hOut = Patch(hIn, outDir, PatchHeathen, "heathen.patched.dll");

		Console.WriteLine("OK " + swOut);
		Console.WriteLine("OK " + acOut);
		Console.WriteLine("OK " + hOut);
		return 0;
	}

	static void PatchHeathen(ModuleDefinition mod)
	{
		int quits = 0;
		var allTypes = new List<TypeDefinition>();
		void Collect(TypeDefinition t)
		{
			allTypes.Add(t);
			foreach (var n in t.NestedTypes) Collect(n);
		}
		foreach (var t in mod.Types) Collect(t);

		foreach (var type in allTypes)
		{
			foreach (var m in type.Methods)
			{
				if (!m.HasBody) continue;
				bool changed = false;
				var il = m.Body.GetILProcessor();
				var snapshot = m.Body.Instructions.ToList();
				foreach (var ins in snapshot)
				{
					if ((ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) && ins.Operand is MethodReference mr
						&& mr.Name == "Quit" && mr.DeclaringType != null && mr.DeclaringType.FullName == "UnityEngine.Application")
					{
						il.Replace(ins, il.Create(OpCodes.Nop));
						quits++;
						changed = true;
					}
				}
				if (changed) { /* no macro simplification needed */ }
			}
		}
		Console.WriteLine("heathen: nopped " + quits + " Application.Quit calls");
	}

	static string Patch(string inputPath, string outDir, Action<ModuleDefinition> patcher, string outName)
	{
		var resolver = new DefaultAssemblyResolver();
		resolver.AddSearchDirectory(Path.GetDirectoryName(inputPath));
		resolver.AddSearchDirectory(outDir);
		if (extraRefDir != null) resolver.AddSearchDirectory(extraRefDir);
		var rp = new ReaderParameters { AssemblyResolver = resolver };
		using (var module = ModuleDefinition.ReadModule(inputPath, rp))
		{
			patcher(module);
			string outPath = Path.Combine(outDir, outName);
			module.Write(outPath);
			return outPath;
		}
	}

	static MethodReference HMethod(ModuleDefinition target, string typeName, string methodName)
	{
		var t = helper.GetType(typeName) ?? throw new Exception("helper type missing: " + typeName);
		var m = t.Methods.FirstOrDefault(x => x.Name == methodName) ?? throw new Exception("helper method missing: " + methodName);
		return target.ImportReference(m);
	}

	static void ReplaceBody(MethodDefinition m, Action<ILProcessor> emit)
	{
		m.Body = new MethodBody(m);
		m.Body.InitLocals = false;
		var il = m.Body.GetILProcessor();
		emit(il);
		il.Emit(OpCodes.Ret);
	}

	static MethodDefinition Find(ModuleDefinition mod, string typeFullName, string methodName)
	{
		var t = mod.GetType(typeFullName) ?? throw new Exception("type not found: " + typeFullName);
		return t.Methods.FirstOrDefault(x => x.Name == methodName && !x.IsGetter && !x.IsSetter)
			?? throw new Exception("method not found: " + typeFullName + "::" + methodName);
	}

	static void CallHelper(ILProcessor il, MethodReference helperMethod)
	{
		il.Emit(OpCodes.Call, helperMethod);
	}

	static void PatchSteamworks(ModuleDefinition mod)
	{
		var fakeGetId = HMethod(mod, "HTF.HTFFake", "GetLocalSteamID");
		var fakeGetName = HMethod(mod, "HTF.HTFFake", "GetLocalName");
		var fakePersona = HMethod(mod, "HTF.HTFFake", "GetPersonaName");

		int count = 0;

		// SteamUser.GetSteamID() -> fake id
		ReplaceBody(Find(mod, "Steamworks.SteamUser", "GetSteamID"), il => CallHelper(il, fakeGetId)); count++;

		// SteamFriends.GetPersonaName() -> local name
		ReplaceBody(Find(mod, "Steamworks.SteamFriends", "GetPersonaName"), il => CallHelper(il, fakeGetName)); count++;

		// SteamFriends.GetFriendPersonaName(CSteamID) -> decode from id
		ReplaceBody(Find(mod, "Steamworks.SteamFriends", "GetFriendPersonaName"), il =>
		{
			il.Emit(OpCodes.Ldarg_0);
			CallHelper(il, fakePersona);
		}); count++;

		// SteamUtils.GetSteamUILanguage() -> ""
		ReplaceBody(Find(mod, "Steamworks.SteamUtils", "GetSteamUILanguage"), il => il.Emit(OpCodes.Ldstr, "")); count++;

		// SteamUtils.ShowGamepadTextInput(...) -> false
		ReplaceBody(Find(mod, "Steamworks.SteamUtils", "ShowGamepadTextInput"), il => il.Emit(OpCodes.Ldc_I4_0)); count++;

		// SteamUtils.GetEnteredGamepadTextLength() -> 0
		ReplaceBody(Find(mod, "Steamworks.SteamUtils", "GetEnteredGamepadTextLength"), il => il.Emit(OpCodes.Ldc_I4_0)); count++;

		// SteamUtils.GetEnteredGamepadTextInput(out string, uint) -> pchText=null, false
		ReplaceBody(Find(mod, "Steamworks.SteamUtils", "GetEnteredGamepadTextInput"), il =>
		{
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
			il.Emit(OpCodes.Ldc_I4_0);
		}); count++;

		// SteamUtils.GetAppID() -> AppId_t(4001890)
		{
			var m = Find(mod, "Steamworks.SteamUtils", "GetAppID");
			var appIdType = mod.GetType("Steamworks.AppId_t");
			var ctor = appIdType.Methods.First(x => x.IsConstructor && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.MetadataType == MetadataType.UInt32);
			var ctorRef = mod.ImportReference(ctor);
			ReplaceBody(m, il =>
			{
				il.Emit(OpCodes.Ldc_I4, 4001890);
				il.Emit(OpCodes.Newobj, ctorRef);
			});
			count++;
		}

		// SteamAPI.Shutdown() -> nothing
		ReplaceBody(Find(mod, "Steamworks.SteamAPI", "Shutdown"), il => { }); count++;

		// SteamAPI.RestartAppIfNecessary(AppId_t) -> always false (kills "relaunch via Steam")
		ReplaceBody(Find(mod, "Steamworks.SteamAPI", "RestartAppIfNecessary"), il => il.Emit(OpCodes.Ldc_I4_0)); count++;

		// overlays -> no-op
		ReplaceBody(Find(mod, "Steamworks.SteamFriends", "ActivateGameOverlay"), il => { }); count++;
		ReplaceBody(Find(mod, "Steamworks.SteamFriends", "ActivateGameOverlayInviteDialog"), il => { }); count++;
		ReplaceBody(Find(mod, "Steamworks.SteamFriends", "ActivateGameOverlayToStore"), il => { }); count++;

		// achievements neutralized
		ReplaceBody(Find(mod, "Steamworks.SteamUserStats", "GetAchievement"), il =>
		{
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stind_I1);
			il.Emit(OpCodes.Ldc_I4_1);
		}); count++;

		ReplaceBody(Find(mod, "Steamworks.SteamUserStats", "SetAchievement"), il => il.Emit(OpCodes.Ldc_I4_1)); count++;
		ReplaceBody(Find(mod, "Steamworks.SteamUserStats", "StoreStats"), il => il.Emit(OpCodes.Ldc_I4_1)); count++;
		ReplaceBody(Find(mod, "Steamworks.SteamUserStats", "GetNumAchievements"), il => il.Emit(OpCodes.Ldc_I4_0)); count++;
		ReplaceBody(Find(mod, "Steamworks.SteamUserStats", "GetAchievementName"), il => il.Emit(OpCodes.Ldnull)); count++;
		ReplaceBody(Find(mod, "Steamworks.SteamUserStats", "ResetAllStats"), il => il.Emit(OpCodes.Ldc_I4_1)); count++;

		Console.WriteLine("steamworks: patched " + count + " methods");
	}

	static void PatchAssemblyCSharp(ModuleDefinition mod)
	{
		int count = 0;

		var createLobby = HMethod(mod, "HTF.HTFDirect", "CreateOfflineLobby");
		var joinOffline = HMethod(mod, "HTF.HTFDirect", "JoinOfflineLobby");
		var joinById = HMethod(mod, "HTF.HTFDirect", "JoinByIDButton");
		var lobbyFieldChange = HMethod(mod, "HTF.HTFDirect", "OnLobbyIDInputFieldChange");
		var hostDirect = HMethod(mod, "HTF.HTFDirect", "HostDirectLobby");

		// Nút "Multiplayer" (SteamManager.CreateLobby) -> host server nghe 0.0.0.0 qua UTP
		ReplaceBody(Find(mod, "SteamManager", "CreateLobby"), il =>
		{
			CallHelper(il, hostDirect);
		}); count++;

		// Cuối ButtonManager.SetupUI(): nới lỏng ô nhập Lobby ID để nhận IP
		{
			var setup = Find(mod, "ButtonManager", "SetupUI");
			var fixInput = HMethod(mod, "HTF.HTFDirect", "FixLobbyInput");
			var il = setup.Body.GetILProcessor();
			foreach (var ret in setup.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToList())
			{
				il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
				il.InsertBefore(ret, il.Create(OpCodes.Call, fixInput));
			}
			count++;
		}

		ReplaceBody(Find(mod, "ConnectionManager", "CreateOfflineLobby"), il =>
		{
			il.Emit(OpCodes.Ldarg_0);
			CallHelper(il, createLobby);
		}); count++;

		ReplaceBody(Find(mod, "ConnectionManager", "JoinOfflineLobby"), il =>
		{
			il.Emit(OpCodes.Ldarg_0);
			CallHelper(il, joinOffline);
		}); count++;

		ReplaceBody(Find(mod, "ButtonManager", "JoinByIDButton"), il =>
		{
			il.Emit(OpCodes.Ldarg_0);
			CallHelper(il, joinById);
		}); count++;

		ReplaceBody(Find(mod, "ButtonManager", "OnLobbyIDInputFieldChange"), il =>
		{
			il.Emit(OpCodes.Ldarg_0);
			CallHelper(il, lobbyFieldChange);
		}); count++;

		Console.WriteLine("assembly-csharp: patched " + count + " methods");
	}
}
