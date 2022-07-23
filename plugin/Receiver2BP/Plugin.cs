using System;
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
        
        // str
        internal static ConfigEntry<double> playerShootStr;
        internal static ConfigEntry<double> explodeStr;
        internal static ConfigEntry<double> fallStr;
        internal static ConfigEntry<double> glassStr;
        internal static ConfigEntry<double> holsterStr;
        internal static ConfigEntry<double> shockStr;
        internal static ConfigEntry<double> shotStr;
        internal static ConfigEntry<double> shrapnelStr;
        internal static ConfigEntry<double> impaledStr;
        internal static ConfigEntry<double> hackTapStr;
        internal static ConfigEntry<double> hackStr;
        // time
        internal static ConfigEntry<int> playerShootLen;
        internal static ConfigEntry<int> explodeLen;
        internal static ConfigEntry<int> fallLen;
        internal static ConfigEntry<int> glassLen;
        internal static ConfigEntry<int> holsterLen;
        internal static ConfigEntry<int> shockLen;
        internal static ConfigEntry<int> shotLen;
        internal static ConfigEntry<int> shrapnelLen;
        internal static ConfigEntry<int> impaledLen;
        internal static ConfigEntry<int> hackTapLen;
        internal static ConfigEntry<int> hackLen;
        
        private void Awake()
        {
            Log = Logger;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            CreateConfig();
            BP.Connect();
            Patch();
        }

        private void CreateConfig()
        {
            playerShootStr = Config.Bind("Vibration Strength",
                "Player Shoot",
                1.0);
            explodeStr = Config.Bind("Vibration Strength",
                "Explode",
                1.0);
            fallStr = Config.Bind("Vibration Strength",
                "Fall",
                1.0);
            glassStr = Config.Bind("Vibration Strength",
                "Glass",
                1.0);
            holsterStr = Config.Bind("Vibration Strength",
                "Holster Discharge",
                1.0);
            shockStr = Config.Bind("Vibration Strength",
                "Shock",
                1.0);
            shotStr = Config.Bind("Vibration Strength",
                "Shot",
                1.0);
            shrapnelStr = Config.Bind("Vibration Strength",
                "Shrapnel",
                1.0);
            impaledStr = Config.Bind("Vibration Strength",
                "Impaled",
                1.0);
            hackTapStr = Config.Bind("Vibration Strength",
                "Hack Tap",
                1.0);
            hackStr = Config.Bind("Vibration Strength",
                "Hap Complete",
                1.0);
            
            playerShootLen = Config.Bind("Vibration Length",
                "Player Shoot",
                100);
            explodeLen = Config.Bind("Vibration Length",
                "Explode",
                100);
            fallLen = Config.Bind("Vibration Length",
                "Fall",
                100);
            glassLen = Config.Bind("Vibration Length",
                "Glass",
                100);
            holsterLen = Config.Bind("Vibration Length",
                "Holster Discharge",
                100);
            shockLen = Config.Bind("Vibration Length",
                "Shock",
                100);
            shotLen = Config.Bind("Vibration Length",
                "Shot",
                100);
            shrapnelLen = Config.Bind("Vibration Length",
                "Shrapnel",
                100);
            impaledLen = Config.Bind("Vibration Length",
                "Impaled",
                100);
            hackTapLen = Config.Bind("Vibration Length",
                "Hack Tap",
                100);
            hackLen = Config.Bind("Vibration Length",
                "Hap Complete",
                100);
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
            BP.SendToServer(Plugin.playerShootStr.Value, Plugin.playerShootLen.Value, "Player Shoot");
            return true;
        }
        
        internal static bool SetDead_Patch(PlayerDeathInformation death_info)
        {
            switch(death_info.cause_of_death)
            {
                case CauseOfDeath.Explosion:
                    BP.SendToServer(Plugin.explodeStr.Value, Plugin.explodeLen.Value, "Explosion");
                    break;
                case CauseOfDeath.Fall:
                    BP.SendToServer(Plugin.fallStr.Value, Plugin.fallLen.Value, "Fall");
                    break;
                case CauseOfDeath.Glass:
                    BP.SendToServer(Plugin.glassStr.Value, Plugin.glassLen.Value, "Glass");
                    break;
                case CauseOfDeath.HolsterDischarge:
                    BP.SendToServer(Plugin.holsterStr.Value, Plugin.holsterLen.Value, "Holster Discharge");
                    break;
                case CauseOfDeath.Shocked:
                    BP.SendToServer(Plugin.shockStr.Value, Plugin.shockLen.Value, "Shock");
                    break;
                case CauseOfDeath.Shot:
                    BP.SendToServer(Plugin.shotStr.Value, Plugin.shotLen.Value, "Shot");
                    break;
                case CauseOfDeath.Shrapnel:
                    BP.SendToServer(Plugin.shrapnelStr.Value, Plugin.shrapnelLen.Value, "Shrapnel");
                    break;
                case CauseOfDeath.Impaled:
                    BP.SendToServer(Plugin.impaledStr.Value, Plugin.impaledLen.Value, "Impaled");
                    break;
            }
            return true;
        }
        
        internal static bool Tap_Patch()
        {
            BP.SendToServer(Plugin.hackTapStr.Value, Plugin.hackTapLen.Value, "Hack Tap");
            return true;
        }
        
        internal static bool Win_Patch()
        {
            BP.SendToServer(Plugin.hackStr.Value, Plugin.hackLen.Value, "Hack Complete");
            return true;
        }
    }

    // cannot connect to intiface directly from plugin as bp does not support net46
    internal static class BP
    {
        private static ClientWebSocket _socket;

        public static void Connect()
        {
            if (_socket != null) return;
            
            Plugin.Log.LogInfo("Connecting to server...");
            _socket = new ClientWebSocket();
            _socket.ConnectAsync(new Uri("ws://127.0.0.1:54321"), CancellationToken.None);
            Plugin.Log.LogInfo("Connected!");
        }

        public static void SendToServer(double strength, int time, string message)
        {
            SendToServer($"{strength},{time},{message}");
        }

        public static void SendToServer(string message)
        {
            _socket?.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
