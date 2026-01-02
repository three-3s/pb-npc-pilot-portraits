using HarmonyLib;
using PhantomBrigade.Mods;
using System.Diagnostics;
using UnityEngine;

// Debug.Log goes to LocalLow/Brace.../.../Player.log
// Harmony.Debug = true + FileLog.Log (and FlushBuffer) goes to desktop harmony.log.txt

namespace ModExtensions
{
    public class ModLinkCustom : ModLink
    {
        public static ModLinkCustom ins;

        public override void OnLoadStart()
        {
            ins = this;
            Debug.Log($"OnLoadStart");
        }

        public override void OnLoad(Harmony harmonyInstance)
        {
            base.OnLoad(harmonyInstance);
            Debug.Log($"OnLoad | Mod: {modID} | Index: {modIndexPreload} | Path: {modPath}");
        }
    }

    [HarmonyPatch(typeof(PhantomBrigade.GameController), MethodType.Normal), HarmonyPatch("Initialize")]
    public class InitLogic
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Debug.Log(message: $"my mod :: InitLogic :: Postfix()");
        }
    }
}
