using DV.ServicePenalty;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace DvMod.CustomFeeValidator
{
    [EnableReloading]
    static class Main
    {
        public static bool enabled;
        public static bool loggingEnabled = true;
        public static UnityModManager.ModEntry mod;
        public static Preferences prefs = new Preferences();

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            mod = modEntry;
            modEntry.OnGUI = OnGui;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;

            return true;
        }

        static void OnGui(UnityModManager.ModEntry modEntry)
        {
            loggingEnabled = GUILayout.Toggle(loggingEnabled, "enable logging");
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value != enabled)
                enabled = value;
            return true;
        }

        static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.UnpatchAll(modEntry.Info.Id);
            return true;
        }

        static void DebugLog(string message)
        {
            if (loggingEnabled)
                mod.Logger.Log(message);
        }

        static List<ResourceType> nonConsumableTypes = new List<ResourceType>(
            Enumerable.Except(
                (IList<ResourceType>)Enum.GetValues(typeof(ResourceType)),
                ResourceTypes.consumableResources));

        public static float GetTotalDebtForJobPurposes(DisplayableDebt debt)
        {
            if (!enabled)
                return debt.GetTotalPrice();
            ExistingLocoDebt locoDebt = debt as ExistingLocoDebt;
            if (locoDebt == null)
                return debt.GetTotalPrice();

            var locoInTrainset = PlayerManager.LastLoco?.trainset?.cars?.Find(car => car.ID == locoDebt.ID);
            if (locoInTrainset != null)
            {
                float fees = debt.GetTotalPriceOfResources(nonConsumableTypes);
                DebugLog($"locomotive {locoInTrainset.ID} is in player's last trainset. Fees without consumables = {fees}");
                return fees;
            }
            else
            {
                DebugLog($"locomotive {debt.ID} not found in last trainset");
                return debt.GetTotalPrice();
            }
        }

        [HarmonyPatch(typeof(CareerManagerDebtController), "IsPlayerAllowedToTakeJob")]
        static class IsPlayerAllowedToTakeJobPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var insts = new List<CodeInstruction>(instructions);
                var calls = insts.FindAll(inst => inst.Calls(typeof(DisplayableDebt).GetMethod("GetTotalPrice")));
                foreach (var call in calls)
                    call.operand = typeof(Main).GetMethod("GetTotalDebtForJobPurposes");
                return insts;
            }
        }

        [HarmonyPatch(typeof(SaveGameManager), "Load")]
        static class LoadPatch
        {
            static void Postfix()
            {
                try
                {
                    prefs = SaveGameManager.data.GetJObject(mod.Info.Id).ToObject<Preferences>();
                }
                catch (Exception e)
                {
                    mod.Logger.LogException("Failed to load preferences from save", e);
                }
            }

        }

        [HarmonyPatch(typeof(SaveGameManager), "Save")]
        static class SavePatch
        {
            static bool Prefix()
            {
                try
                {
                    SaveGameManager.data.SetJObject(mod.Info.Id, JObject.FromObject(prefs));
                }
                catch (Exception e)
                {
                    mod.Logger.LogException("Failed to save preferences", e);
                }
                return true;
            }
        }
    }

    class Preferences
    {
        public bool includeLastLoco = true;
        public bool includeLastTrainset = true;
        public bool includeLocoResources = true;
    }
}