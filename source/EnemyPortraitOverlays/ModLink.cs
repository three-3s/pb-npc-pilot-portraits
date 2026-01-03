using Entitas;
using HarmonyLib;
using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Mods;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

// INTRODUCTION / USAGE NOTES:
//  - The project's reference to 'System' must point to the Phantom Brigade one, not Microsoft.
//    It was necessary to add
//    C:\Program Files(x86)\Steam\steamapps\common\Phantom Brigade\PhantomBrigade_Data\Managed\
//    to the project's Reference Paths, which unfortunately isn't stored in the .csproj.
//  - Debug.Log goes to LocalLow/Brace.../.../Player.log
//  - Harmony.Debug = true + FileLog.Log (and FlushBuffer) goes to desktop harmony.log.txt
//  - You may want to read more about (or ask a chatbot about):
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
#if false
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
#endif
    }//class

    //+================================================================================================+
    //||                                                                                              ||
    //+================================================================================================+
    public class Patches
    {
        private static IGroup<PersistentEntity> groupPilots;
        private static List<PersistentEntity> pilotsBuffer = new List<PersistentEntity>();
        static readonly Regex regex_built_in_pilot_overlays = new Regex("^pilot_overlay_[0-9][0-9]$");
        static List<string> avail_portraits = new List<string>(); // (scratch space)

        //-------------------------------------------------------------------------------------------
        // "Dear Harmony, please call into this InitLogic class whenever GameController.Initialize() runs"
        [HarmonyPatch(typeof(GameController), MethodType.Normal), HarmonyPatch("Initialize")]
        public class InitLogic
        {
            // "Dear Harmony, please call this AfterInitialize() function AFTER that GameController.Initialize() runs"
            [HarmonyPostfix]
            public static void AfterInitialize()
            {
                //Debug.Log(message: $"my mod :: InitLogic :: Postfix()");
                if (groupPilots == null)
                {
                    groupPilots = Contexts.sharedInstance.persistent.GetGroup(PersistentMatcher.AllOf(PersistentMatcher.PilotTag).NoneOf(PersistentMatcher.Destroyed));
                }
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
            // "Dear Harmony, please call this AfterRandomizePilotAppearance() function AFTER that DataContainerSettingsPilot.RandomizePilotAppearance() runs"
            [HarmonyPostfix]
            public static void AfterRandomizePilotAppearance(DataBlockPilotAppearance data, bool friendly, string modelKeyOverride = null)
            {
                // Possible improvements (probably not worth the effort):
                //  - Reduce duplicates across combats by cycling thru portraits in a random order, to help avoid
                //    using the same portrait two combats in a row, or if one unit dies and reinforcements arrive.
                //    (Push encountered images onto a static queue, and try to take the next non-duplicate from the
                //    queue when we run out? Ideally maybe any KIA portraits would also be on cooldown or lockout.)
                //  - Invent some scheme to mark portraits as only for hostiles, or only for allies, or only
                //    for player's pilots, or only for bosses, etc. Tanks vs mech? Do the cruise missles still
                //    exist / have a pilot portrait? Bosses? E.g., name them starting e=enemy, f=friendly, a=all,
                //    p=player-only...
                //    (could be useful if someone wanted consistent uniforms etc, I suppose).
                //  - In a perfect world, maybe there would be support for multiple frames for each pilot's
                //    portrait, for idle animatinos & reactions to events/circumstances.
                //  - In a perfect world, I might experiment with partial-colorization 'overlay variants',
                //    e.g., red tint for enemy, green tint for ally. But the built-in overlay-variants support is
                //    strictly single-hue recolorization, which is a bit too much.

                bool had_portrait = (!data.portrait.IsNullOrEmpty());
                if (had_portrait)
                {
                    // no action. (not sure this will ever be relevant. maybe if some other mod assigns overlays.)
                }
                else {
                    List<string> all_portrait_textures = TextureManager.GetExposedTextureKeys(TextureGroupKeys.PilotPortraits);
                    avail_portraits.Clear();
                    avail_portraits.AddRange(all_portrait_textures);

#if false
                    ../static bool log_dump = false;
                    if (log_dump)
                    {
                        log_dump = false;
                        foreach (string s in avail_portraits)
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
#endif

                    // Exclude the built-in (non-portrait) pilot_overlay_01..08.
                    avail_portraits.RemoveAll(regex_built_in_pilot_overlays.IsMatch);

                    List<PersistentEntity> all_pilots = groupPilots.GetEntities(pilotsBuffer);
                    foreach (PersistentEntity pilot in all_pilots)
                    {
                        if (pilot.hasPilotAppearance)
                        {
                            avail_portraits.Remove(pilot.pilotAppearance.data.portrait);
                        }
                    }

                    // Assign a random portrait from the remaining (non-duplicate) portraits
                    if (avail_portraits.Count > 0)
                    {
                        data.portrait = avail_portraits.GetRandomEntry();
                    }
                }
            }//func
        }//class PortraitRandomizer
    }//class Patches
}//namespace
