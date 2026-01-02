using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Functions;
using PhantomBrigade.Mods;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// Debug.Log goes to LocalLow/Brace.../.../Player.log
// Harmony.Debug = true + FileLog.Log (and FlushBuffer) goes to desktop harmony.log.txt

namespace ModExtensions
{
    //==================================================================================================
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

    //==================================================================================================
    [HarmonyPatch(typeof(GameController), MethodType.Normal), HarmonyPatch("Initialize")]
    public class InitLogic
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Debug.Log(message: $"my mod :: InitLogic :: Postfix()");
        }
    }

    //==================================================================================================
    [HarmonyPatch(typeof(DataContainerSettingsPilot), MethodType.Normal), HarmonyPatch("RandomizePilotAppearance")]
    public class PortraitRandomizer
    {
        static bool done_yet = false;

        [HarmonyPostfix]
        public static void RandomizePilotAppearance(DataBlockPilotAppearance data, bool friendly, string modelKeyOverride = null)
        {
            if (!done_yet)
            {
                done_yet = true;
                List<string> portraits = TextureManager.GetExposedTextureKeys(TextureGroupKeys.PilotPortraits);
                foreach (string s in portraits)
                {
                    // The below log cmd lists entries like f-001, f-002, etc.
                    // Seems to reflect my Mods\TS33_portraits\Textures\UI\PilotPortraits\*.png file naming (minus the .png).
                    // They're all in alphabetical order, and include pilot_overlay_01 .. 08, which are presumably the built-in markup-overlays.
                    Debug.Log($"todo.portrait: {s}");
                }
            }
            //data.portrait = ;
            //data.portraitVariant = ;
        }
    }
}
