using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace AllUpgradesMod
{
    [BepInPlugin("com.pmadd.unlimitedupgrades", "Unlimited Upgrades", "1.0.2")]
    public class UnlimitedUpgradesPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var harmony = new Harmony("com.pmadd.unlimitedupgrades");
            harmony.PatchAll();
            Logger.LogInfo("Unlimited Upgrades loaded successfully!");
        }
    }

    [HarmonyPatch(typeof(Faction), "ApplyTraitsTo")]
    public static class Faction_ApplyTraitsTo_Patch
    {
        public static void Prefix(Faction __instance, Unit u)
        {
            if (u != null && u.modifiers != null)
            {
                // Increase the max allowed modifiers from the game's default limit of 10
                u.modifiers.statMaxSlotted = 100;
            }
        }
    }

    [HarmonyPatch(typeof(CUIFleetConfigPanel), "PopulatePanelWithFaction")]
    public static class CUIFleetConfigPanel_PopulatePanelWithFaction_Patch
    {
        public static Dictionary<string, int> ScrollOffsets = new Dictionary<string, int>();

        public static bool Prefix(CUIFleetConfigPanel __instance, Faction f)
        {
            if (f == null) return true;

            __instance.numUnitsLabel.enabled = false;
            if ((bool)__instance.unitBlueprint)
            {
                __instance.wireframeTexture.mainTexture = __instance.unitBlueprint.hudOutlineTexture;
                __instance.descriptionLabel.text = __instance.unitBlueprint.description;
                int num = 0;
                for (int i = 0; i < f.currentFleet.Count; i++)
                {
                    if (f.currentFleet[i].GetComponent<Unit>().blueprint == __instance.unitBlueprint)
                    {
                        num++;
                    }
                }
                if (num > 1)
                {
                    __instance.headerLabel.text = $"{num.ToString()}x {__instance.unitBlueprint.displayName}S";
                }
                else
                {
                    __instance.headerLabel.text = $"1x {__instance.unitBlueprint.displayName}";
                }
            }

            Faction.UnitConfig configByID = f.GetConfigByID(__instance.unitConfigTag);
            if (configByID != null)
            {
                if (!ScrollOffsets.TryGetValue(__instance.unitConfigTag, out int offset))
                {
                    offset = 0;
                    ScrollOffsets[__instance.unitConfigTag] = 0;
                }

                // Total slots = equipped upgrades count + 1 (for the empty slot to append new ones)
                int totalSlots = configByID.unitModifiers.Count + 1;
                int maxOffset = Mathf.Max(0, totalSlots - __instance.modifierButtons.Count);
                if (offset > maxOffset)
                {
                    offset = maxOffset;
                    ScrollOffsets[__instance.unitConfigTag] = offset;
                }

                for (int j = 0; j < __instance.modifierButtons.Count; j++)
                {
                    int virtualIndex = offset + j;
                    __instance.modifierButtons[j].index = virtualIndex;

                    UnitModifier modifier = null;
                    if (virtualIndex < configByID.unitModifiers.Count)
                    {
                        modifier = configByID.unitModifiers[virtualIndex];
                    }

                    __instance.modifierButtons[j].SetModifier(modifier);
                }

                for (int k = 0; k < configByID.weaponAssignments.Count; k++)
                {
                    if (k < __instance.weaponButtons.Count)
                    {
                        __instance.weaponButtons[k].SetBlueprint(configByID.weaponAssignments[k].weaponBlueprint);
                    }
                }
            }
            else
            {
                Debug.LogError("Null unit config!");
            }

            return false; // Skip original execution
        }
    }

    [HarmonyPatch(typeof(CUIFleetConfigPanel), "Update")]
    public static class CUIFleetConfigPanel_Update_Patch
    {
        public static void Postfix(CUIFleetConfigPanel __instance)
        {
            if (Controls.menuMouseControl)
            {
                UICamera.selectedObject = null;
            }

            // Scroll with mouse wheel when hovering slots
            GameObject hovered = UICamera.hoveredObject;
            if (hovered != null)
            {
                var modifierBtn = hovered.GetComponent<CUIFleetConfigPanelModifierButton>();
                if (modifierBtn != null && modifierBtn.configPanel == __instance)
                {
                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    if (scroll != 0f)
                    {
                        Scroll(__instance, scroll > 0f ? -1 : 1);
                    }
                }
            }

            // Auto-scroll when keyboard or controller navigates to the edge slots
            GameObject selected = UICamera.selectedObject;
            if (selected != null)
            {
                var selectedBtn = selected.GetComponent<CUIFleetConfigPanelModifierButton>();
                if (selectedBtn != null && selectedBtn.configPanel == __instance)
                {
                    int btnIndex = __instance.modifierButtons.IndexOf(selectedBtn);
                    
                    if (btnIndex == __instance.modifierButtons.Count - 1)
                    {
                        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Controls.player.GetButtonDown("Menu Right"))
                        {
                            Scroll(__instance, 1);
                        }
                    }
                    else if (btnIndex == 0)
                    {
                        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Controls.player.GetButtonDown("Menu Left"))
                        {
                            Scroll(__instance, -1);
                        }
                    }
                }
            }
        }

        private static void Scroll(CUIFleetConfigPanel panel, int direction)
        {
            var config = Campaign.playerFaction.GetConfigByID(panel.unitConfigTag);
            if (config != null)
            {
                CUIFleetConfigPanel_PopulatePanelWithFaction_Patch.ScrollOffsets.TryGetValue(panel.unitConfigTag, out int offset);
                int totalSlots = config.unitModifiers.Count + 1;
                int maxOffset = Mathf.Max(0, totalSlots - panel.modifierButtons.Count);
                int newOffset = Mathf.Clamp(offset + direction, 0, maxOffset);
                if (newOffset != offset)
                {
                    CUIFleetConfigPanel_PopulatePanelWithFaction_Patch.ScrollOffsets[panel.unitConfigTag] = newOffset;
                    panel.Populate();
                    AudioMenu.playToggle = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(CUIFleetConfigModifierPanel), "OnModifierButtonClicked")]
    public static class CUIFleetConfigModifierPanel_OnModifierButtonClicked_Patch
    {
        public static bool Prefix(CUIFleetConfigModifierPanel __instance, CUIModifierButton button)
        {
            if (button.isLocked)
            {
                if (Campaign.playerFaction.CanPurchaseModifier(button.prefab))
                {
                    var purchaseMethod = typeof(CUIFleetConfigModifierPanel).GetMethod("Purchase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    typeof(CUIFleetConfigModifierPanel).GetField("_nextPurchasedModifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, button.prefab);
                    typeof(CUIFleetConfigModifierPanel).GetField("_nextButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, button);
                    purchaseMethod.Invoke(__instance, null);
                }
                else if (Campaign.playerFaction.upgradeTokens < button.prefab.upgradeTokenCost)
                {
                    CUIFavorWidget.lastFlashTime = Time.time;
                    Messenger<string, Color>.Broadcast("RequestToast", $"THIS UPGRADE REQUIRES {button.prefab.upgradeTokenCost} FAVOR", CockpitToast.notificationColor);
                    AudioCockpit.PlayNegatory();
                }
                return false;
            }

            if ((button.prefab.allowedUnitTypes & CUIFleetConfigModifierPanel.nextBlueprint.prefab.unitType) == 0)
            {
                Messenger<string, Color>.Broadcast("RequestToast", "THIS UPGRADE CANNOT BE EQUIPPED ON THIS CLASS OF WARSHIP", CockpitToast.notificationColor);
                AudioCockpit.PlayNegatory();
                return false;
            }

            var config = Campaign.playerFaction.GetConfigByID(CUIFleetConfigModifierPanel.nextTag);
            if (config != null)
            {
                bool isEquipped = config.unitModifiers.Contains(button.prefab);
                if (isEquipped)
                {
                    config.unitModifiers.Remove(button.prefab);
                    AudioMenu.playCancel = true;
                }
                else
                {
                    int idx = CUIFleetConfigModifierPanel.nextModifierIndex;
                    if (idx >= 0 && idx < config.unitModifiers.Count && config.unitModifiers[idx] == null)
                    {
                        config.unitModifiers[idx] = button.prefab;
                    }
                    else
                    {
                        config.unitModifiers.Add(button.prefab);
                    }
                    AudioMenu.playUpgradeEquipped = true;
                }

                Campaign.SavePlayerFaction();
                __instance.Populate();
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(CUIFleetConfigModifierPanel), "OnConfirmPurchaseClicked")]
    public static class CUIFleetConfigModifierPanel_OnConfirmPurchaseClicked_Patch
    {
        public static bool Prefix(CUIFleetConfigModifierPanel __instance)
        {
            var nextPurchasedModifier = (UnitModifier)typeof(CUIFleetConfigModifierPanel).GetField("_nextPurchasedModifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
            var nextButton = (CUIModifierButton)typeof(CUIFleetConfigModifierPanel).GetField("_nextButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);

            Campaign.playerFaction.PurchaseModifier(nextPurchasedModifier);

            if ((nextPurchasedModifier.allowedUnitTypes & CUIFleetConfigModifierPanel.nextBlueprint.prefab.unitType) != 0)
            {
                AudioMenu.playUpgradeEquipped = true;
                var config = Campaign.playerFaction.GetConfigByID(CUIFleetConfigModifierPanel.nextTag);
                if (config != null)
                {
                    int idx = CUIFleetConfigModifierPanel.nextModifierIndex;
                    if (idx >= 0 && idx < config.unitModifiers.Count && config.unitModifiers[idx] == null)
                    {
                        config.unitModifiers[idx] = nextPurchasedModifier;
                    }
                    else
                    {
                        config.unitModifiers.Add(nextPurchasedModifier);
                    }
                }
            }
            else
            {
                AudioMenu.playUpgradePurchased = true;
            }

            Campaign.SavePlayerFaction();

            Game.Instance.menuSubstate = MenuSubstate.FleetConfig;
            CUIFleetConfigMenu.Instance.state = CUIFleetConfigMenu.State.SelectModifier;
            __instance.Populate();

            if (nextButton != null)
            {
                UICamera.selectedObject = nextButton.gameObject;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(UnitModifier), "AddNewInstanceTo")]
    public static class UnitModifier_AddNewInstanceTo_Patch
    {
        public static bool Prefix(UnitModifier prefab, Unit u, ref UnitModifier __result)
        {
            if (prefab == null || u == null) return true;

            // Block adding modifiers that are not allowed on this unit type
            if ((prefab.allowedUnitTypes & u.unitType) == 0)
            {
                Debug.LogWarning($"Blocking incompatible modifier {prefab.name} from being added to unit {u.name} (type: {u.unitType})");
                __result = null;
                return false; // Skip execution
            }
            return true;
        }
    }
}
