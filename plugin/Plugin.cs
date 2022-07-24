using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Receiver2;

namespace Receiver2BP
{
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	[BepInProcess("Receiver2.exe")]
	internal class Plugin : BaseUnityPlugin
	{
		internal const string pluginGuid = "bodle.receiver2.bp";
		internal const string pluginName = "Receiver 2 BP";
		internal const string pluginVersion = "0.1";
		
		internal static ManualLogSource Log;

		// str config values
		internal static double playerShootStr;
		internal static double explodeStr;
		internal static double fallStr;
		internal static double glassStr;
		internal static double holsterStr;
		internal static double shockStr;
		internal static double shotStr;
		internal static double shrapnelStr;
		internal static double impaledStr;
		internal static double hackTapStr;
		internal static double hackStr;
		// time config values
		internal static int playerShootLen;
		internal static int explodeLen;
		internal static int fallLen;
		internal static int glassLen;
		internal static int holsterLen;
		internal static int shockLen;
		internal static int shotLen;
		internal static int shrapnelLen;
		internal static int impaledLen;
		internal static int hackTapLen;
		internal static int hackLen;
		
		private void Awake()
		{
			Log = Logger;
			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
			// create and load config values
			CreateConfig();
			// connect to server
			BP.Connect();
			// patch the game with modified functions
			Patch();
		}

		private void CreateConfig()
		{
			List<ConfigEntry<double>> strengthList = new List<ConfigEntry<double>>();
			strengthList.Add(Config.Bind("Vibration Strength",
				"Player Shoot",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Explode",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Fall",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Glass",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Holster Discharge",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Shock",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Shot",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Shrapnel",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Impaled",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Hack Tap",
				1.0));
			strengthList.Add(Config.Bind("Vibration Strength",
				"Hap Complete",
				1.0));

			// clamp to 0 and 1
			foreach (ConfigEntry<double> str in strengthList)
			{
				str.Value = (str.Value < 0.0) ? 0.0 : (str.Value > 1.0) ? 1.0 : str.Value;
			}
			playerShootStr = strengthList[0].Value;
			explodeStr = strengthList[1].Value;
			fallStr = strengthList[2].Value;
			glassStr = strengthList[3].Value;
			holsterStr = strengthList[4].Value;
			shockStr = strengthList[5].Value;
			shotStr = strengthList[6].Value;
			shrapnelStr = strengthList[7].Value;
			impaledStr = strengthList[8].Value;
			hackTapStr = strengthList[9].Value;
			hackStr = strengthList[10].Value;

			List<ConfigEntry<int>> timeList = new List<ConfigEntry<int>>();
			timeList.Add(Config.Bind("Vibration Length",
				"Player Shoot",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Explode",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Fall",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Glass",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Holster Discharge",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Shock",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Shot",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Shrapnel",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Impaled",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Hack Tap",
				100));
			timeList.Add(Config.Bind("Vibration Length",
				"Hap Complete",
				100));
			
			// ensure greater than 0
			foreach (ConfigEntry<int> len in timeList)
			{
				len.Value = (len.Value < 0) ? 0 : len.Value;
			}
			playerShootLen = timeList[0].Value;
			explodeLen = timeList[1].Value;
			fallLen = timeList[2].Value;
			glassLen = timeList[3].Value;
			holsterLen = timeList[4].Value;
			shockLen = timeList[5].Value;
			shotLen = timeList[6].Value;
			shrapnelLen = timeList[7].Value;
			impaledLen = timeList[8].Value;
			hackTapLen = timeList[9].Value;
			hackLen = timeList[10].Value;
		}

		private void Patch()
		{
			Harmony harmony = new Harmony(pluginGuid);
			harmony.Patch(AccessTools.Method(typeof(GunScript), "FireBullet"),
				new HarmonyMethod(AccessTools.Method(typeof(Patches), "FireBullet_Patch")));
			harmony.Patch(AccessTools.Method(typeof(LocalAimHandler), "SetDead"),
				new HarmonyMethod(AccessTools.Method(typeof(Patches), "SetDead_Patch")));
			harmony.Patch(AccessTools.Method(typeof(HackingMinigame), "Tap"),
				new HarmonyMethod(AccessTools.Method(typeof(Patches), "Tap_Patch")));
			harmony.Patch(AccessTools.Method(typeof(HackingMinigame), "Win"),
				new HarmonyMethod(AccessTools.Method(typeof(Patches), "Win_Patch")));
		}
	}

	internal class Patches
	{
		internal static bool FireBullet_Patch()
		{
			BP.SendToServer(Plugin.playerShootStr, Plugin.playerShootLen, "Player Shoot");
			return true;
		}
		
		internal static bool SetDead_Patch(PlayerDeathInformation death_info)
		{
			switch(death_info.cause_of_death)
			{
				case CauseOfDeath.Explosion:
					BP.SendToServer(Plugin.explodeStr, Plugin.explodeLen, "Explosion");
					break;
				case CauseOfDeath.Fall:
					BP.SendToServer(Plugin.fallStr, Plugin.fallLen, "Fall");
					break;
				case CauseOfDeath.Glass:
					BP.SendToServer(Plugin.glassStr, Plugin.glassLen, "Glass");
					break;
				case CauseOfDeath.HolsterDischarge:
					BP.SendToServer(Plugin.holsterStr, Plugin.holsterLen, "Holster Discharge");
					break;
				case CauseOfDeath.Shocked:
					BP.SendToServer(Plugin.shockStr, Plugin.shockLen, "Shock");
					break;
				case CauseOfDeath.Shot:
					BP.SendToServer(Plugin.shotStr, Plugin.shotLen, "Shot");
					break;
				case CauseOfDeath.Shrapnel:
					BP.SendToServer(Plugin.shrapnelStr, Plugin.shrapnelLen, "Shrapnel");
					break;
				case CauseOfDeath.Impaled:
					BP.SendToServer(Plugin.impaledStr, Plugin.impaledLen, "Impaled");
					break;
			}
			return true;
		}
		
		internal static bool Tap_Patch()
		{
			BP.SendToServer(Plugin.hackTapStr, Plugin.hackTapLen, "Hack Tap");
			return true;
		}
		
		internal static bool Win_Patch()
		{
			BP.SendToServer(Plugin.hackStr, Plugin.hackLen, "Hack Complete");
			return true;
		}
	}

	// cannot connect to intiface directly from plugin as bp does not support net46
	internal static class BP
	{
		private static ClientWebSocket socket;

		public static void Connect()
		{
			if (socket != null) return;
			
			Plugin.Log.LogInfo("Connecting to server...");
			socket = new ClientWebSocket();
			socket.ConnectAsync(new Uri("ws://127.0.0.1:54321"), CancellationToken.None);
			Plugin.Log.LogInfo("Connected!");
		}

		public static void SendToServer(double strength, int time, string message)
		{
			SendToServer($"{strength},{time},{message}");
		}

		public static void SendToServer(string message)
		{
			socket?.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}
}
