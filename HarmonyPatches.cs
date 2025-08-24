using HarmonyLib;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ImprovedEconomyForAILords
{
    [HarmonyPatch(typeof(Village), "DailyTick")]
    class VillagesDenarsPatch
    {
        private static readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        static void Postfix(Village __instance)
        {
            if (!settings.EnableTownsDenarsIncrease)
                return;

            if (__instance != null && __instance.Settlement != null)
            {
                float hearth = __instance.Hearth;
                int additionalDenars = (int)(hearth);

                __instance.ChangeGold(additionalDenars);
            }
        }
    }

    [HarmonyPatch(typeof(DefaultBuildingConstructionModel), "GetBoostAmount")]
    class AILordsBuildingBoostPatch
    {
        private static readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        static void Postfix(Town town, ref int __result)
        {
            if (settings.LoggingEnabled)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"GetBoostAmount called for {town?.Name} - Result: {__result}", Colors.Yellow));
            }

            if (!settings.EnableAILordsBuildingBoost)
                return;

            if (town?.OwnerClan?.Leader == null || town.OwnerClan.Leader == Hero.MainHero)
                return;

            int originalResult = __result;
            __result = (int)(__result * settings.AILordsBuildingBoostMultiplier);

            if (settings.LoggingEnabled)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"AI enhanced boost for {town.Name}: {__result} (was: {originalResult})",
                    Colors.Cyan));
            }
        }
    }
}