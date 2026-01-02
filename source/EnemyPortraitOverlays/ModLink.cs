using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Mods;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

// INTRODUCTION / USAGE NOTES:
//  - The project's reference to 'System' must point to the Phantom Brigade one, not Microsoft.
//    It was necessary to add
//    C:\Program Files(x86)\Steam\steamapps\common\Phantom Brigade\PhantomBrigade_Data\Managed\
//    to the project's Reference Paths, which unfortunately isn't stored in the .csproj.
//  - Debug.Log goes to LocalLow/Brace.../.../Player.log
//  - Harmony.Debug = true + FileLog.Log (and FlushBuffer) goes to desktop harmony.log.txt
//  - Ask a chatbot:
//     - How to use eg dnSpy to decompile & search the Phantom Brigade C# module assemblies.
//     - General info about the 'Entitas' Entity Component System.
//     - Explain what the HarmonyPatch things are.
//  - Note that modding this game via C# has some significant overlap with some other heavily
//    modded games such as RimWorld (another Unity game (+HarmonyLib)).
//  - Other basic getting-started info:
//     - https://github.com/BraceYourselfGames/PB_ModSDK/wiki/Mod-system-overview#libraries
//     - https://wiki.braceyourselfgames.com/en/PhantomBrigade/Modding/ModSystem
//
//  - Image notes:
//     - Use the Mods\my_mod_name\Textures\UI\PilotPortraits\*.png location, in line with
//       the 'Sample: 2D Portraits' mod. 256x256 resolution. Use RGB format without alpha,
//       since even having an alpha channel (even fully opaque) seems to cause the game to
//       try to render the overlay with a sort of forced transparency applied to the middle.
//     - Comms chatter images must be overridden separately. See
//       Phantom Brigade\PhantomBrigade_Data\StreamingAssets\UI\CombatComms
//       (e.g., put replacements in Mods\my_mod_name\Textures\UI\CombatComms\*.png).

namespace ModExtensions
{
    //==================================================================================================
    // (Having a class derived from ModLink might (?) be necessary, but the overrides are probably just
    //  leftover 'hello world' stuff at this point.)
    public class ModLinkCustom : ModLink
    {
        public static ModLinkCustom ins;

        public override void OnLoadStart()
        {
            ins = this;
            //Debug.Log($"OnLoadStart");
        }

        public override void OnLoad(Harmony harmonyInstance)
        {
            base.OnLoad(harmonyInstance);
            //Debug.Log($"OnLoad | Mod: {modID} | Index: {modIndexPreload} | Path: {modPath}");
        }
    }//class

    //+================================================================================================+
    //||                                                                                              ||
    //+================================================================================================+
    public class Patches
    {
        public static readonly string ally_overlay_variant_str = "ts33.ally_overlay";
        public static readonly string enemy_overlay_variant_str = "ts33.enemy_overlay";

        //-------------------------------------------------------------------------------------------
        // "Dear Harmony, please call into this InitLogic class whenever GameController.Initialize() runs"
        [HarmonyPatch(typeof(GameController), MethodType.Normal), HarmonyPatch("Initialize")]
        public class InitLogic
        {
            // "Dear Harmony, please call this Postfix() function after that GameController.Initialize() runs"
            [HarmonyPostfix]
            public static void Postfix()
            {
                //Debug.Log(message: $"my mod :: InitLogic :: Postfix()");

                DataContainerSettingsPilot settings_pilot = DataLinker<DataContainerSettingsPilot>.data;
                DataBlockOverlayVariant ally = new DataBlockOverlayVariant();  // (colorize as: green)
                DataBlockOverlayVariant enemy = new DataBlockOverlayVariant(); // (colorize as: red)
                ally.filterColor.r = 0.3f;
                ally.filterColor.g = 1.0f;
                ally.filterColor.b = 0.4f;
                enemy.filterColor.r = 1.0f;
                enemy.filterColor.g = 0.3f;
                enemy.filterColor.b = 0.3f;
                settings_pilot.overlayVariants.Add(ally_overlay_variant_str, ally);
                settings_pilot.overlayVariants.Add(enemy_overlay_variant_str, enemy);

                //DataBlockOverlayVariant test = new DataBlockOverlayVariant();
                //test.filterInputs.x = 0.2f; // something to do with bloom? also: 1,1,1=white just colorizes the image as white (ie, it's not a color-multiply, it's a desaturate-then-multiply, ie "colorize")
                //test.filterInputs.x = 0.4f;
                //test.filterInputs.x = 0.6f;
                //test.filterInputs.x = 0.8f;
                //settings_pilot.overlayVariants.Add("ts33.test", test);
            }
        }//func

        //-------------------------------------------------------------------------------------------
        // This function does get called for generating random enemy pilots, but seems to not get
        // called for allied pilots. (Possibly those are pre-generated, or maybe that was specific
        // to the tutorial and any allied pilots would get randomly-generated outside tutorial --
        // if that's even a thing that happens?)
        //
        // "Dear Harmony, please call into this PortraitRandomizer class whenever DataContainerSettingsPilot.RandomizePilotAppearance() runs"
        [HarmonyPatch(typeof(DataContainerSettingsPilot), MethodType.Normal), HarmonyPatch("RandomizePilotAppearance")]
        public class PortraitRandomizer
        {
            static bool log_dump = false;

            // "Dear Harmony, please call this RandomizePilotAppearance() function after that DataContainerSettingsPilot.RandomizePilotAppearance() runs"
            [HarmonyPostfix]
            public static void RandomizePilotAppearance(DataBlockPilotAppearance data, bool friendly, string modelKeyOverride = null)
            {
                // Possible improvements:
                //  - Exclude the built-in pilot_overlay01..08.
                //  - Avoid duplicates with any other units in the combat.
                //  - Avoid duplicates with any player pilots (including not in combat).
                //  - Further avoid duplicates by cycling thru portraits in a random order, rather than risk eg
                //    using the same portrait two combats in a row, or if one unit dies and reinforcements arrive.
                //  - Invent some scheme to mark portraits as only for hostiles, or only for allies, or only
                //    for player's pilots, or only for bosses, etc.
                //    (Tanks vs mech? Do the cruise missles still exist / have a pilot portrait? Bosses?)

                bool had_portrait = (!data.portrait.IsNullOrEmpty()); // (not sure this will ever be relevant; maybe for other mods)
                if (!had_portrait)
                {
                    List<string> portraits = TextureManager.GetExposedTextureKeys(TextureGroupKeys.PilotPortraits);

                    if (log_dump)
                    {
                        log_dump = false;
                        foreach (string s in portraits)
                        {
                            // The below log cmd lists entries like f-001, f-002, etc.
                            // Seems to reflect my Mods\TS33_portraits\Textures\UI\PilotPortraits\*.png file naming (minus the .png).
                            // They're all in alphabetical order, and include pilot_overlay_01 .. 08, which are presumably the built-in markup-overlays.
                            Debug.Log($"todo.portrait: {s}");
                        }
                        DataContainerSettingsPilot settings_pilot = DataLinker<DataContainerSettingsPilot>.data;
                        foreach (KeyValuePair<string, DataBlockOverlayVariant> kv in settings_pilot.overlayVariants)
                        {
                            // existing: 01_default, 02_hue_00, 02_hue_05, 02_hue_40, 02_hue_55
                            // possibly:    nil          red       orange     green     blue
                            // (ie maybe we didn't need to create ally vs enemy overlays, but whatever; at least we're robust against change)
                            Debug.Log($"todo.overlayVariant: {kv.Key}");
                        }
                    }

                    // ('friendly' seems to be based on CombatUIUtility.IsFactionFriendly(pilot.faction.s))
                    data.portrait = portraits.GetRandomEntry();
                    data.portraitVariant = (friendly) ? ally_overlay_variant_str : enemy_overlay_variant_str;
                }
            }//func
        }//class PortraitRandomizer
    }//class Patches
}//namespace
