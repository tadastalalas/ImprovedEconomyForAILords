using HarmonyLib;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ImprovedEconomyForAILords
{
    [HarmonyPatch(typeof(Village), "DailyTick")]
    class VillagesDenarsPatch
    {
        private static readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        static void Postfix(Village __instance)
        {
            if (!settings.EnableThisModification || !settings.EnableTownsDenarsIncrease)
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

            if (!settings.EnableThisModification || !settings.EnableAILordsBuildingBoost)
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

    [HarmonyPatch(typeof(DefaultClanFinanceModel), "CalculateClanIncomeInternal")]
    class ClanIncomeTooltipPatch
    {
        private static readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        static void Postfix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false)
        {
            if (!settings.EnableThisModification || !settings.EnablePlayerRevenue)
                return;

            if (clan == null || clan.Leader == null || !clan.Leader.IsHumanPlayerCharacter)
                return;

            int fromAllSources = ImprovedEconomyForAILordsBehavior.playerTotalIncomeFromAllSources;
            int fromKingdomLeader = ImprovedEconomyForAILordsBehavior.playerTotalIncomeFromKingdomLeader;
            int fromTowns = ImprovedEconomyForAILordsBehavior.playerTotalIncomeFromTowns;
            int fromCastles = ImprovedEconomyForAILordsBehavior.playerTotalIncomeFromCastles;
            int fromVillages = ImprovedEconomyForAILordsBehavior.playerTotalIncomeFromVillages;
            int fromArena = ImprovedEconomyForAILordsBehavior.playerIncomeFromArenaLeaderboard;

            if (fromKingdomLeader > 0)
                goldChange.Add(fromKingdomLeader, new TextObject("{=IEFAIL_ZZHU48}Improved Economy income from Kingdom Leader"), null);
            if (fromTowns > 0)
                goldChange.Add(fromTowns, new TextObject("{=IEFAIL_8AYbs7}Improved Economy income from Towns"), null);
            if (fromCastles > 0)
                goldChange.Add(fromCastles, new TextObject("{=IEFAIL_Nfc05L}Improved Economy income from Castles"), null);
            if (fromVillages > 0)
                goldChange.Add(fromVillages, new TextObject("{=IEFAIL_KYa6V7}Improved Economy income from Villages"), null);
            if (fromArena > 0)
                goldChange.Add(fromArena, new TextObject("{=IEFAIL_IK5Yhc}Improved Economy income from Arena"), null);
        }
    }
}