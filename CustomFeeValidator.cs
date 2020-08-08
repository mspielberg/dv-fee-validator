using DV.ServicePenalty;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;

namespace DvMod.CustomFeeValidator
{
    [EnableReloading]
    static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry mod;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try { settings = Settings.Load<Settings>(modEntry); } catch { }
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            mod = modEntry;
            modEntry.OnGUI = OnGui;
            modEntry.OnSaveGUI = OnSaveGui;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;

            return true;
        }

        static void OnGui(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        static void OnSaveGui(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
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
            if (settings.isLoggingEnabled)
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

            switch (settings.selectedValidationType) {
                case FeeValidationType.LastLoco:
                    return GetTotalDebtForFeeValidationTypeLastLoco(debt);
                case FeeValidationType.ExistingLocos:
                    return GetTotalDebtForFeeValidationTypeExistingLocos(debt);
            }

            DebugLog($"This should never happen. In case it does, the selected fee type is {settings.selectedValidationType}.");
            return debt.GetTotalPrice();
        }

        private static float GetTotalDebtForFeeValidationTypeLastLoco(DisplayableDebt debt)
        {
            ExistingLocoDebt locoDebt = debt as ExistingLocoDebt;
            if (locoDebt == null)
                return debt.GetTotalPrice();

            var locoInTrainset = PlayerManager.LastLoco?.trainset?.cars?.Find(car => car.ID == locoDebt.ID);
            if (locoInTrainset != null)
            {
                float fees = debt.GetTotalPriceOfResources(nonConsumableTypes);
                DebugLog($"Locomotive {locoInTrainset.ID} is in player's last trainset. Fees without consumables = {fees}.");
                return fees;
            }
            DebugLog($"Locomotive {debt.ID} not found in last trainset.");
            return debt.GetTotalPrice();
        }

        private static float GetTotalDebtForFeeValidationTypeExistingLocos(DisplayableDebt debt)
        {
            ExistingLocoDebt locoDebt = debt as ExistingLocoDebt;
            if (locoDebt == null)
                return debt.GetTotalPrice();

            DebugLog($"Locomotive {locoDebt.ID} still exists; ignoring its fees.");
            return 0;
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
    }
}
