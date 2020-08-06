using DV.ServicePenalty;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
using Newtonsoft.Json.Linq;

namespace DvMod.CustomFeeValidator
{
    [EnableReloading]
    static class Main
    {
        public static bool enabled;
        [SaveOnReload]
        public static bool loggingEnabled =
#if DEBUG
            true;
#else
            false;
#endif
        [SaveOnReload]
        public static FeeType selectedFeeType;
        public static UnityModManager.ModEntry mod;

        public enum FeeType
		{
            ignore_fees_from_train_with_last_loco,
            ignore_all_loco_fees_until_despawned,
		}
        private static List<string> feeTypeNames = Enum.GetNames(typeof(FeeType)).ToList();

        private const string SAVE_DATA_PRIMARY_KEY = "CustomFeeValidator";
        private const string SAVE_DATA_VERSION_KEY = "Version";
        private const string SAVE_DATA_FEE_TYPE_KEY = "FeeType";

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
            GUILayout.Label("Fee Validation Type:");
            try
            {
                selectedFeeType = (FeeType)Enum.Parse(typeof(FeeType), feeTypeNames.ElementAt(GUILayout.SelectionGrid(
                    feeTypeNames.IndexOf(selectedFeeType.ToString()),
                    feeTypeNames.Select(name => name.Replace('_', ' ')).ToArray(),
                    1, // # of columns
                    GUI.skin.toggle)));
            }
            catch (ArgumentException)
			{
                selectedFeeType = FeeType.ignore_fees_from_train_with_last_loco;
			}
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

            switch (selectedFeeType) {
                case FeeType.ignore_fees_from_train_with_last_loco:
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
                case FeeType.ignore_all_loco_fees_until_despawned:
                    {
                        ExistingLocoDebt locoDebt = debt as ExistingLocoDebt;
                        if (locoDebt == null)
                            return debt.GetTotalPrice();

                        TrainCar indebtedLoco = TrainCar.logicCarToTrainCar.Values.FirstOrDefault(tc => tc.ID == locoDebt.ID);
                        if (indebtedLoco != null)
                        {
                            DebugLog($"Locomotive {indebtedLoco.ID} still exists; ignoring its fees.");
                            return 0;
                        }
                        DebugLog($"Locomotive {locoDebt.ID} cannot be found, thus it must have been destroyed.");
                        return debt.GetTotalPrice();
                    }
            }

            DebugLog($"This should never happen. In case it does, the selected fee type is {selectedFeeType}.");
            return debt.GetTotalPrice();
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

        [HarmonyPatch(typeof(SaveGameManager), "Save")]
        class SaveGameManager_Save_Patch
        {
            static void Prefix(SaveGameManager __instance)
            {
                try
                {
                    JObject saveData = new JObject(
                        new JProperty(SAVE_DATA_VERSION_KEY, new JValue(mod.Version.ToString())),
                        new JProperty(SAVE_DATA_FEE_TYPE_KEY, selectedFeeType.ToString()));

                    SaveGameManager.data.SetJObject(SAVE_DATA_PRIMARY_KEY, saveData);
                }
                catch (Exception e)
                {
                    DebugLog($"Saving mod settings failed with exception:\n{e}");
                }
            }
        }

        [HarmonyPatch(typeof(SaveGameManager), "Load")]
        class SaveGameManager_Load_Patch
        {
            static void Postfix(SaveGameManager __instance)
            {
                try
                {
                    JObject saveData = SaveGameManager.data.GetJObject(SAVE_DATA_PRIMARY_KEY);

                    if (saveData == null)
                    {
                        DebugLog("Not loading save data: primary object was null.");
                        return;
                    }

                    string feeTypeName = (string)saveData[SAVE_DATA_FEE_TYPE_KEY];
                    if (feeTypeName != null)
                    {
                        try
                        {
                            selectedFeeType = (FeeType)Enum.Parse(typeof(FeeType), feeTypeName);
                        }
                        catch (ArgumentException)
                        {
                            DebugLog($"Could not parse fee type from save data: {feeTypeName}");
                            selectedFeeType = FeeType.ignore_fees_from_train_with_last_loco;
                        }
                    } else
					{
                        DebugLog("No fee type found in mod save data.");
					}
                }
                catch (Exception e)
                {
                    DebugLog($"Loading mod settings failed with exception:\n{e}");
                }
            }
        }
    }
}