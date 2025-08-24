using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;


namespace ImprovedEconomyForAILords
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("ImprovedEconomyForAILordsPatch");
            harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (Campaign.Current is Campaign campaign && campaign.GameMode == CampaignGameMode.Campaign)
            {
                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
                campaignGameStarter.AddBehavior(new ImprovedEconomyForAILordsBehavior());
            }
        }

        public override void OnGameEnd(Game game)
        {
            var dailyTickEventField = typeof(CampaignEvents).GetField("DailyTickEvent", BindingFlags.Static | BindingFlags.NonPublic);
            var weeklyTickEventField = typeof(CampaignEvents).GetField("WeeklyTickEvent", BindingFlags.Static | BindingFlags.NonPublic);

            if (dailyTickEventField?.GetValue(null) is MulticastDelegate dailyDelegate && dailyDelegate.GetInvocationList().Length > 0)
                CampaignEvents.DailyTickEvent.ClearListeners(this);

            if (weeklyTickEventField?.GetValue(null) is MulticastDelegate weeklyDelegate && weeklyDelegate.GetInvocationList().Length > 0)
                CampaignEvents.WeeklyTickEvent.ClearListeners(this);

            base.OnGameEnd(game);
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }
    }

    public class ImprovedEconomyForAILordsBehavior : CampaignBehaviorBase
    {
        private static readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        private readonly HashSet<Hero> leadersWithFiefs = new();
        private readonly HashSet<Hero> membersWithFiefs = new();
        private readonly HashSet<Hero> leadersNoFiefs = new();
        private readonly HashSet<Hero> membersNoFiefs = new();

        int clanLeadersWithFiefsGotPaid = 0;
        int clanMembersWithFiefsGotPaid = 0;
        int clanLeadersWithoutFiefsGotPaid = 0;
        int clanMembersWithoutFiefsGotPaid = 0;

        int fieflessRelNoPayClans = 0;
        int fieflessRelMinus29to29Clans = 0;
        int fieflessRel30to59Clans = 0;
        int fieflessRel60to99Clans = 0;
        int fieflessRel100PlusClans = 0;
        readonly HashSet<Clan> _countedFieflessClans = new();

        private readonly Dictionary<Hero, (int TownSum, int CastleSum, int VillageSum, int TownPays, int CastlePays, int VillagePays)> _paymentAgg = new();

        private readonly Dictionary<string, HashSet<string>> _lordInvestmentTracker = new();

        private bool _hasValidatedInvestmentData = false;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(OnDailyTickEvent));
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, new Action(OnWeeklyTickEvent));
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(OnMobilePartyDestroyed));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedEvent));
        }

        private void OnDailyTickEvent()
        {
            if (!settings.EnableThisModification)
                return;

            if (!_hasValidatedInvestmentData)
            {
                ValidateLoadedInvestmentData();
                _hasValidatedInvestmentData = true;
            }

            if (settings.EnableAILordsTownsRevenue || settings.EnableAILordsCastlesRevenue || settings.EnableAILordsVillagesRevenue)
                ProcessDenarsRevenueForAILords();

            if (settings.EnableAILordsTradeExperience)
                HandleTradeExperienceForAI();

            if (settings.EnableAILordsBuildingBoost)
            {
                HandleAIBuildingBoosts();
                CleanupCompletedBuildingInvestments();
            }
        }

        private void OnWeeklyTickEvent()
        {
            if (!settings.EnableThisModification)
                return;

            HandleCaravansForAI();
            HandleArenaLeadersForAI();
        }

        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
        {
            if (!settings.EnableAILordsCaravans)
                return;
            if (mobileParty?.PartyComponent?.Leader == null)
                return;

            Hero destroyedPartyLeader = mobileParty.PartyComponent.Leader;

            if (!destroyedPartyLeader.IsClanLeader)
                return;

            int ownedCaravansCount = destroyedPartyLeader.OwnedCaravans.Count;

            if (ownedCaravansCount > 0)
                RemoveExcessCaravansForHero(destroyedPartyLeader, ownedCaravansCount, 0);
        }

        private void OnNewGameCreatedEvent(CampaignGameStarter starter)
        {
            _lordInvestmentTracker.Clear();
        }

        private void ProcessDenarsRevenueForAILords()
        {
            leadersWithFiefs.Clear();
            membersWithFiefs.Clear();
            leadersNoFiefs.Clear();
            membersNoFiefs.Clear();

            _paymentAgg.Clear();
            _countedFieflessClans.Clear();
            fieflessRelNoPayClans = 0;
            fieflessRelMinus29to29Clans = 0;
            fieflessRel30to59Clans = 0;
            fieflessRel60to99Clans = 0;
            fieflessRel100PlusClans = 0;

            ProcessFiefs(Town.AllTowns, false);
            ProcessFiefs(Town.AllCastles, true);

            if (settings.AllClanMembersGetRevenue)
            {
                LogMessage($"Total clan leaders with fiefs: {leadersWithFiefs.Count}, total clan members with fiefs: {membersWithFiefs.Count}");
                LogMessage($"Total clan leaders without fiefs: {leadersNoFiefs.Count}, total clan members without fiefs: {membersNoFiefs.Count}");
                LogMessage($"Clan leaders with fiefs earned: {clanLeadersWithFiefsGotPaid}, their clan members: {clanMembersWithFiefsGotPaid}");
                LogMessage($"Clan leaders without fiefs earned: {clanLeadersWithoutFiefsGotPaid}, their clan members: {clanMembersWithoutFiefsGotPaid}");

                LogMessage($"Fiefless clans' leaders relationship with their kingdom lords:\n" +
                    $"[Relationship -100..-30]: {fieflessRelNoPayClans} clan(s)      " +
                    $"[Relationship -29..29]: {fieflessRelMinus29to29Clans} clan(s)\n" +
                    $"[Relationship 30..59]: {fieflessRel30to59Clans} clan(s)      " +
                    $"[Relationship 60..99]: {fieflessRel60to99Clans} clan(s)\n" +
                    $"[Relationship 100]: {fieflessRel100PlusClans} clan(s)", Colors.Green);
            }

            foreach (var kvp in _paymentAgg
                .Where(k => k.Key.IsClanLeader && k.Key.Clan != null && k.Key.Clan.Fiefs != null && k.Key.Clan.Fiefs.Count > 0)
                .OrderByDescending(k => k.Value.TownSum + k.Value.CastleSum + k.Value.VillageSum)
                .ThenBy(k => k.Key.Name.ToString()))
            {
                var hero = kvp.Key;
                var (townSum, castleSum, villageSum, townCount, castleCount, villageCount) = kvp.Value;
                int total = townSum + castleSum + villageSum;
                LogMessage($"{hero.Name} earned {total} (Towns ({townCount}) - {townSum}, Castles ({castleCount}) - {castleSum}, Villages ({villageCount}) - {villageSum})");
            }

            LogPlayerKingdomSummary();

            clanLeadersWithFiefsGotPaid = 0;
            clanMembersWithFiefsGotPaid = 0;
            clanLeadersWithoutFiefsGotPaid = 0;
            clanMembersWithoutFiefsGotPaid = 0;
        }

        private void ProcessFiefs(IEnumerable<Town> fiefs, bool isFiefACastle)
        {
            foreach (Town fief in fiefs)
            {
                Clan? ownerClan = fief.OwnerClan;
                Hero? clanLeader = fief.OwnerClan?.Leader;

                if (clanLeader == null || clanLeader.IsPrisoner)
                    continue;

                leadersWithFiefs.Add(clanLeader);

                if (clanLeader != Hero.MainHero)
                {
                    ApplyFiefDenarsBonusForHero(clanLeader, fief, isFiefACastle, true, true);
                    ApplyVillageDenarsBonusForHero(clanLeader, fief, true, true);
                }

                if (clanLeader.IsKingdomLeader)
                {
                    var clans = Clan.All.Where(clan => clan.Kingdom == clanLeader.Clan.Kingdom && clan.Fiefs.Count <= 0 && clan != Hero.MainHero.Clan);

                    foreach (Clan clan in clans)
                    {
                        List<Hero> fieflessClanMembers = clan?.Heroes.Where(hero => !hero.IsPrisoner && hero != Hero.MainHero && IsHeroAdult(hero)).ToList() ?? new List<Hero>();

                        if (fieflessClanMembers.Count <= 0)
                            continue;

                        float relation = clan.Leader.GetRelation(clanLeader);
                        
                        if (relation <= -30f)
                        {
                            fieflessClanMembersRevenueMultiplier = 0f;
                            fieflessRelNoPayClans++;
                        }
                        else
                        {
                            if (relation <= 29f)
                            {
                                fieflessClanMembersRevenueMultiplier = settings.Relation0RevenueMultiplier;
                                fieflessRelMinus29to29Clans++;
                            }
                            else
                            {
                                if (relation <= 59f)
                                {
                                    fieflessClanMembersRevenueMultiplier = settings.Relation30RevenueMultiplier;
                                    fieflessRel30to59Clans++;
                                }
                                else
                                {
                                    if (relation <= 99f)
                                    {
                                        fieflessClanMembersRevenueMultiplier = settings.Relation60RevenueMultiplier;
                                        fieflessRel60to99Clans++;
                                    }
                                    else
                                    {
                                        fieflessClanMembersRevenueMultiplier = settings.Relation100RevenueMultiplier;
                                        fieflessRel100PlusClans++;
                                    }
                                }
                            }
                        }

                        if (fieflessClanMembersRevenueMultiplier <= 0f)
                            continue;

                        foreach (Hero fieflessClanMember in fieflessClanMembers)
                        {
                            if (fieflessClanMember != clan.Leader && settings.AllClanMembersGetRevenue)
                            {
                                ApplyFiefDenarsBonusForHero(fieflessClanMember, fief, isFiefACastle, false, false);
                                ApplyVillageDenarsBonusForHero(fieflessClanMember, fief, false, false);

                                membersNoFiefs.Add(fieflessClanMember);
                            }
                            else
                            {
                                ApplyFiefDenarsBonusForHero(fieflessClanMember, fief, isFiefACastle, true, false);
                                ApplyVillageDenarsBonusForHero(fieflessClanMember, fief, true, false);

                                leadersNoFiefs.Add(fieflessClanMember);
                            }
                        }
                    }
                }

                if (!settings.AllClanMembersGetRevenue)
                    continue;

                List<Hero> clanMembers = ownerClan?.Heroes.Where(hero => hero != clanLeader && !hero.IsPrisoner && hero != Hero.MainHero && IsHeroAdult(hero)).ToList() ?? new List<Hero>();

                foreach (Hero clanMember in clanMembers)
                {
                    membersWithFiefs.Add(clanMember);

                    ApplyFiefDenarsBonusForHero(clanMember, fief, isFiefACastle, false, true);
                    ApplyVillageDenarsBonusForHero(clanMember, fief, false, true);
                }
            }
        }

        float denarsRevenueMultiplierFromTown = settings.DenarsRevenueMultiplierFromTown;
        float denarsRevenueMultiplierFromCastle = settings.DenarsRevenueMultiplierFromCastle;
        float denarsRevenueMultiplierFromVillage = settings.DenarsRevenueMultiplierFromVillage;
        float otherSameClanMembersRevenueMultiplier = settings.OtherSameClanMembersRevenueMultiplier;
        float fieflessClanLeaderRevenueMultiplier = settings.FieflessClanLeaderRevenueMultiplier;
        float fieflessClanMembersRevenueMultiplier = 0f;

        private void ApplyFiefDenarsBonusForHero(Hero hero, Town town, bool IsFiefACastle, bool IsClanLeader, bool HasFief)
        {
            if (settings.EnableAILordsTownsRevenue || settings.EnableAILordsCastlesRevenue)
            {
                int payment = (int)((CalculateFiefDenarsPayment(town) * ConsiderLordsTradeSkill(hero)));

                payment = (int)(payment * (IsFiefACastle ? denarsRevenueMultiplierFromCastle : denarsRevenueMultiplierFromTown));

                if (IsClanLeader && HasFief)
                {
                    clanLeadersWithFiefsGotPaid += payment;
                }
                else if (!IsClanLeader && HasFief)
                {
                    payment = (int)(payment * otherSameClanMembersRevenueMultiplier);
                    clanMembersWithFiefsGotPaid += payment;
                }
                else if (IsClanLeader && !HasFief)
                {
                    payment = (int)(payment * fieflessClanLeaderRevenueMultiplier);
                    clanLeadersWithoutFiefsGotPaid += payment;
                }
                else if (!IsClanLeader && !HasFief)
                {
                    payment = (int)(payment * fieflessClanMembersRevenueMultiplier);
                    clanMembersWithoutFiefsGotPaid += payment;
                }

                hero.ChangeHeroGold(payment);

                if (_paymentAgg.TryGetValue(hero, out var agg))
                {
                    if (IsFiefACastle)
                        _paymentAgg[hero] = (agg.TownSum, agg.CastleSum + payment, agg.VillageSum, agg.TownPays, agg.CastlePays + 1, agg.VillagePays);
                    else
                        _paymentAgg[hero] = (agg.TownSum + payment, agg.CastleSum, agg.VillageSum, agg.TownPays + 1, agg.CastlePays, agg.VillagePays);
                }
                else
                {
                    if (IsFiefACastle)
                        _paymentAgg[hero] = (0, payment, 0, 0, 1, 0);
                    else
                        _paymentAgg[hero] = (payment, 0, 0, 1, 0, 0);
                }
            }
        }

        private void ApplyVillageDenarsBonusForHero(Hero hero, Town fief, bool IsClanLeader, bool HasFief)
        {
            if (settings.EnableAILordsVillagesRevenue)
            {
                foreach (Village village in fief.Villages)
                {
                    int payment = (int)((CalculateVillageDenarsPayment(village) * ConsiderLordsTradeSkill(hero)) * denarsRevenueMultiplierFromVillage);

                    if (IsClanLeader && HasFief)
                    {
                        clanLeadersWithFiefsGotPaid += payment;
                    }
                    else if (!IsClanLeader && HasFief)
                    {
                        payment = (int)(payment * otherSameClanMembersRevenueMultiplier);
                        clanMembersWithFiefsGotPaid += payment;
                    }
                    else if (IsClanLeader && !HasFief)
                    {
                        payment = (int)(payment * fieflessClanLeaderRevenueMultiplier);
                        clanLeadersWithoutFiefsGotPaid += payment;
                    }
                    else if (!IsClanLeader && !HasFief)
                    {
                        clanMembersWithoutFiefsGotPaid += payment;
                    }

                    hero.ChangeHeroGold(payment);

                    if (_paymentAgg.TryGetValue(hero, out var aggV))
                        _paymentAgg[hero] = (aggV.TownSum, aggV.CastleSum, aggV.VillageSum + payment, aggV.TownPays, aggV.CastlePays, aggV.VillagePays + 1);
                    else
                        _paymentAgg[hero] = (0, 0, payment, 0, 0, 1);
                }
            }
        }

        private static int CalculateFiefDenarsPayment(Town town)
        {
            if (town == null || town.IsUnderSiege)
                return 0;

            return (int)town.Prosperity / 5;
        }

        private static int CalculateVillageDenarsPayment(Village village)
        {
            if (village == null || village.Settlement.IsRaided || village.Settlement.IsUnderRaid)
                return 0;

            return (int)village.Hearth / 5;
        }

        private float ConsiderLordsTradeSkill(Hero hero)
        {
            if (settings.ConsiderLordsTradeSkill)
            {
                int tradeSkillValue = hero.GetSkillValue(DefaultSkills.Trade);
                return 1f + (tradeSkillValue / 200f);
            }
            return 1f;
        }

        private void HandleTradeExperienceForAI()
        {
            int baseXpPerCaravan = settings.TradeExperiencePerCaravanDaily;
            bool considerFocusFactors = settings.ConsiderFocusFactorsForTradeXp;

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero == Hero.MainHero || !hero.IsClanLeader || hero.Clan == null || hero.IsPrisoner)
                    continue;

                int ownedCaravansCount = hero.OwnedCaravans.Count;

                if (ownedCaravansCount <= 0)
                    continue;

                int totalXp = baseXpPerCaravan * ownedCaravansCount;

                hero.HeroDeveloper.AddSkillXp(DefaultSkills.Trade, totalXp, considerFocusFactors, true);

                if (settings.LoggingEnabled)
                {
                    LogMessage($"{hero.Name} gained {totalXp} trade experience from {ownedCaravansCount} caravan(s)");
                }
            }

            LogPlayerKingdomTradeXP();
        }

        private void HandleCaravansForAI()
        {
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero == Hero.MainHero)
                    continue;
                if (!CanHeroHaveACaravan(hero))
                    continue;

                int ownedCaravansCount = hero.OwnedCaravans.Count;
                int maxCaravansPossible = settings.EnableAILordsCaravans ? HowManyCaravansAIHeroCanHave(hero, ownedCaravansCount) : 0;

                if (ownedCaravansCount > maxCaravansPossible)
                    RemoveExcessCaravansForHero(hero, ownedCaravansCount, maxCaravansPossible);
                else if (settings.EnableAILordsCaravans && ownedCaravansCount < maxCaravansPossible)
                    TryCreateCaravanForHero(hero);
            }
        }

        private static bool CanHeroHaveACaravan(Hero hero)
        {
            return hero.IsClanLeader && hero.Clan != null && hero.Clan.Kingdom != null && !hero.Clan.IsClanTypeMercenary && !hero.Clan.IsBanditFaction && !hero.IsPrisoner;
        }

        private static void RemoveExcessCaravansForHero(Hero hero, int ownedCaravansCount, int maxCaravansPossible)
        {
            int caravansToRemoveCount = ownedCaravansCount - maxCaravansPossible;
            List<CaravanPartyComponent> caravansToRemove = hero.OwnedCaravans.Take(caravansToRemoveCount).ToList();

            foreach (var caravan in caravansToRemove)
            {
                if (caravan == null || caravan.MobileParty == null || caravan.MobileParty.IsActive == false)
                    continue;

                if (caravan.MobileParty.CurrentSettlement != null && caravan.MobileParty.CurrentSettlement.IsUnderSiege)
                    continue;

                try
                {
                    DestroyPartyAction.Apply(null, caravan.MobileParty);
                }
                catch (System.Exception ex)
                {
                    DebugLogMessage($"Failed to remove caravan for {hero?.Name}: {ex.Message}");
                }
            }
        }

        private static int HowManyCaravansAIHeroCanHave(Hero hero, int currentCaravansCount)
        {
            float heroClanInfluence = hero.Clan.Influence;

            if (heroClanInfluence <= 350f)
                return 0;
            else if (heroClanInfluence <= 1050f)
                return 1;
            else
                return 2;
        }

        private void TryCreateCaravanForHero(Hero hero)
        {
            if (hero == null)
            {
                DebugLogMessage("Hero was passed as null value to TryCreateCaravanForHero(Hero hero) method. Caravan was not created.");
                return;
            }
            
            Settlement? selectedSettlement = hero.BornSettlement ?? hero.HomeSettlement;
            int caravansTroopsAmount = settings.CaravansTroopsAmount;
            int caravansDenarsAmount = settings.CaravansDenarsAmount;

            if (selectedSettlement == null || selectedSettlement.Culture != hero.Culture)
                selectedSettlement = Enumerable.FirstOrDefault(Enumerable.OrderBy(Enumerable.Where(Settlement.All, settlement => settlement.IsTown && settlement.Culture == hero.Culture), _ => MBRandom.RandomFloat));

            if (selectedSettlement == null)
                selectedSettlement = Enumerable.FirstOrDefault(Enumerable.OrderBy(Enumerable.Where(Settlement.All, settlement => settlement.IsTown), (Settlement _) => MBRandom.RandomFloat));

            if (selectedSettlement == null)
                selectedSettlement = Enumerable.FirstOrDefault(Enumerable.OrderBy(Settlement.All, (Settlement _) => MBRandom.RandomFloat));

            if (selectedSettlement == null)
            {
                DebugLogMessage("Unable to select a proper settlement to create an AI hero caravan.");
            }
            else
            {
                MobileParty caravanParty = CaravanPartyComponent.CreateCaravanParty(hero, selectedSettlement, false, null, null, caravansTroopsAmount, true);
                caravanParty.PartyTradeGold = caravansDenarsAmount;
                caravanParty.InitializePartyTrade(caravansDenarsAmount);
            }
        }

        private void HandleArenaLeadersForAI()
        {
            if (!settings.EnableAILordsArenaRevenue)
                return;

            var tournamentManager = Campaign.Current.TournamentManager;
            if (tournamentManager == null)
                return;

            int leaderboardCount = Math.Max(3, Math.Min(30, settings.ArenaLeaderboardCount));
            List<KeyValuePair<Hero, int>> leaderboard = tournamentManager.GetLeaderboard().Take(leaderboardCount).ToList();

            LogPlayerKingdomArenaLeaders(leaderboard);

            if (settings.LoggingEnabled)
            {
                LogMessage($"Arena Leaderboard Top {leaderboardCount}:", Colors.Green);
                int position = 1;
                foreach (var entry in leaderboard)
                {
                    LogMessage($"Rank {position}: {entry.Key.Name} - {entry.Value} points", Colors.Green);
                    position++;
                }

                int rewardedCount = 0;

                for (int i = 0; i < leaderboard.Count; i++)
                {
                    var entry = leaderboard[i];
                    Hero hero = entry.Key;
                    int actualRank = i + 1;

                    if (hero == Hero.MainHero || hero.IsPrisoner)
                        continue;

                    int reward = CalculateArenaReward(actualRank);

                    if (reward > 0)
                    {
                        rewardedCount++;
                        hero.ChangeHeroGold(reward);
                        if (settings.LoggingEnabled)
                        {
                            LogMessage($"{hero.Name} earned {reward} denars from arena rank {actualRank}");
                        }
                    }
                }
            }
        }

        private int CalculateArenaReward(int rank)
        {
            float baseReward = settings.ArenaBaseReward;

            if (rank == 1) return (int)(baseReward * 2.0f);
            if (rank == 2) return (int)(baseReward * 1.5f);
            if (rank == 3) return (int)(baseReward * 1.25f);
            if (rank == 4) return (int)(baseReward * 1.0f);
            if (rank == 5) return (int)(baseReward * 0.85f);
            if (rank == 6) return (int)(baseReward * 0.75f);
            if (rank == 7) return (int)(baseReward * 0.7f);
            if (rank == 8) return (int)(baseReward * 0.65f);
            if (rank == 9) return (int)(baseReward * 0.6f);
            if (rank == 10) return (int)(baseReward * 0.55f);
            if (rank == 11) return (int)(baseReward * 0.5f);
            if (rank == 12) return (int)(baseReward * 0.4f);
            if (rank >= 13 && rank <= 18) return (int)(baseReward * 0.3f);
            if (rank >= 19 && rank <= 24) return (int)(baseReward * 0.2f);
            if (rank >= 25 && rank <= 30) return (int)(baseReward * 0.1f);

            return 0;
        }

        private static bool IsHeroAdult(Hero hero)
        {
            return hero.Age >= Campaign.Current.Models.AgeModel.HeroComesOfAge;
        }

        private bool IsHeroInPlayerKingdom(Hero hero)
        {
            if (hero == null || hero.Clan == null || Hero.MainHero?.Clan?.Kingdom == null)
                return false;

            return hero.Clan.Kingdom == Hero.MainHero.Clan.Kingdom;
        }

        private bool HasLordInvestedInSettlement(string heroId, string settlementId)
        {
            if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(settlementId))
                return false;

            return _lordInvestmentTracker.ContainsKey(heroId) &&
                   _lordInvestmentTracker[heroId].Contains(settlementId);
        }

        private void RecordLordInvestment(string heroId, string settlementId)
        {
            if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(settlementId))
                return;

            if (!_lordInvestmentTracker.ContainsKey(heroId))
            {
                _lordInvestmentTracker[heroId] = new HashSet<string>();
            }
            _lordInvestmentTracker[heroId].Add(settlementId);
        }

        private void ClearLordInvestment(string heroId, string settlementId)
        {
            if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(settlementId))
                return;

            if (_lordInvestmentTracker.ContainsKey(heroId))
            {
                _lordInvestmentTracker[heroId].Remove(settlementId);

                if (_lordInvestmentTracker[heroId].Count == 0)
                {
                    _lordInvestmentTracker.Remove(heroId);
                }
            }
        }

        private void CleanupCompletedBuildingInvestments()
        {
            foreach (Town town in Town.AllTowns.Concat(Town.AllCastles))
            {
                if (town?.OwnerClan?.Leader == null)
                    continue;

                if (town.BuildingsInProgress.IsEmpty())
                {
                    string heroId = town.OwnerClan.Leader.StringId;
                    string settlementId = town.Settlement.StringId;
                    ClearLordInvestment(heroId, settlementId);
                }
            }
        }

        private void HandleAIBuildingBoosts()
        {
            foreach (Town town in Town.AllTowns.Concat(Town.AllCastles))
            {
                if (town?.OwnerClan?.Leader == null || town.OwnerClan.Leader == Hero.MainHero)
                    continue;

                if (town.IsUnderSiege || town.OwnerClan.Leader.IsPrisoner)
                    continue;

                if (town.BuildingsInProgress.IsEmpty())
                    continue;

                Hero aiLord = town.OwnerClan.Leader;
                string heroId = aiLord.StringId;
                string settlementId = town.Settlement.StringId;

                if (HasLordInvestedInSettlement(heroId, settlementId))
                {
                    if (settings.LoggingEnabled)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"{aiLord.Name} has already invested in {town.Name} - skipping",
                            Colors.Gray));
                    }
                    continue;
                }

                if (town.BoostBuildingProcess > 0)
                    continue;

                if (MBRandom.RandomFloat > 0.3f)
                    continue;

                int optimalInvestment = CalculateOptimalBuildingInvestment(town, aiLord);

                if (optimalInvestment <= 0)
                {
                    if (settings.LoggingEnabled)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"{aiLord.Name} cannot afford meaningful investment in {town.Name}",
                            Colors.Green));
                    }
                    continue;
                }

                int originalGold = aiLord.Gold;
                float investmentPercent = originalGold > 0 ? (float)optimalInvestment / originalGold * 100 : 0f;

                town.BoostBuildingProcess = optimalInvestment;
                aiLord.ChangeHeroGold(-optimalInvestment);

                RecordLordInvestment(heroId, settlementId);

                if (settings.LoggingEnabled)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{aiLord.Name} invested {optimalInvestment} denars ({investmentPercent:F1}% of wealth) in {town.Name}",
                        Colors.Green));
                }
            }
        }

        private int CalculateOptimalBuildingInvestment(Town town, Hero aiLord)
        {
            if (town == null || aiLord == null)
                return 0;

            int baseBoostCost = town.IsCastle ?
                Campaign.Current.Models.BuildingConstructionModel.CastleBoostCost :
                Campaign.Current.Models.BuildingConstructionModel.TownBoostCost;

            int maxAffordableInvestment = (int)(aiLord.Gold * (settings.AILordsInvestmentPercentage / 100.0f));

            if (maxAffordableInvestment < baseBoostCost)
                return 0;

            int optimalInvestment;

            if (aiLord.Gold >= 100000)
            {
                optimalInvestment = Math.Min(maxAffordableInvestment, baseBoostCost * 5);
            }
            else if (aiLord.Gold >= 50000)
            {
                optimalInvestment = Math.Min(maxAffordableInvestment, baseBoostCost * 3);
            }
            else if (aiLord.Gold >= 20000)
            {
                optimalInvestment = Math.Min(maxAffordableInvestment, baseBoostCost * 2);
            }
            else
            {
                optimalInvestment = baseBoostCost;
            }

            optimalInvestment = Math.Max(optimalInvestment, baseBoostCost);

            if (settings.LoggingEnabled)
            {
                float multiplier = (float)optimalInvestment / baseBoostCost;
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Investment calculation for {aiLord.Name} in {town.Name}: " +
                    $"Base cost: {baseBoostCost}, Investment: {optimalInvestment} ({multiplier:F1}x), " +
                    $"Lord gold: {aiLord.Gold}, Max affordable: {maxAffordableInvestment}",
                    Colors.Cyan));
            }

            return optimalInvestment;
        }

        private void ValidateLoadedInvestmentData()
        {
            if (_lordInvestmentTracker.Count == 0)
                return;

            var keysToRemove = new List<string>();
            var updatedEntries = 0;

            foreach (var heroId in _lordInvestmentTracker.Keys.ToList())
            {
                var hero = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == heroId);
                if (hero == null)
                {
                    keysToRemove.Add(heroId);
                    continue;
                }

                var validSettlements = new HashSet<string>();
                foreach (var settlementId in _lordInvestmentTracker[heroId])
                {
                    var settlement = Settlement.All.FirstOrDefault(s => s.StringId == settlementId);
                    if (settlement != null)
                    {
                        validSettlements.Add(settlementId);
                    }
                }

                if (validSettlements.Count != _lordInvestmentTracker[heroId].Count)
                {
                    _lordInvestmentTracker[heroId] = validSettlements;
                    updatedEntries++;
                }

                if (validSettlements.Count == 0)
                {
                    keysToRemove.Add(heroId);
                }
            }

            foreach (var keyToRemove in keysToRemove)
            {
                _lordInvestmentTracker.Remove(keyToRemove);
            }
        }

        private void LogPlayerKingdomSummary()
        {
            if (!settings.EnablePlayerRelevantLogging || Hero.MainHero?.Clan?.Kingdom == null)
                return;

            var kingdomName = Hero.MainHero.Clan.Kingdom.Name.ToString();
            LogPlayerRelevantInfo($"--- {kingdomName} Kingdom Summary ---", Colors.White);

            var kingdomLords = _paymentAgg
                .Where(k => IsHeroInPlayerKingdom(k.Key) && k.Key.IsClanLeader)
                .OrderByDescending(k => k.Value.TownSum + k.Value.CastleSum + k.Value.VillageSum)
                .ToList();

            if (kingdomLords.Any())
            {
                foreach (var kvp in kingdomLords)
                {
                    var hero = kvp.Key;
                    var (townSum, castleSum, villageSum, townCount, castleCount, villageCount) = kvp.Value;

                    int total = townSum + castleSum + villageSum;
                    LogPlayerRelevantInfo($"{hero.Name.ToString()} earned {total} denars (Towns: {townSum}, Castles: {castleSum}, Villages: {villageSum})");
                }

                int totalIncome = kingdomLords.Sum(k => k.Value.TownSum + k.Value.CastleSum + k.Value.VillageSum);
                int averageIncome = kingdomLords.Count > 0 ? totalIncome / kingdomLords.Count : 0;
                int richestLordIncome = kingdomLords.Count > 0 ? kingdomLords.Max(k => k.Value.TownSum + k.Value.CastleSum + k.Value.VillageSum) : 0;

                string richestLordName = "None";
                if (kingdomLords.Any())
                {
                    var richestLord = kingdomLords.First().Key;
                    if (richestLord != null)
                        richestLordName = richestLord.Name.ToString();
                }

                LogPlayerRelevantInfo($"Total kingdom clans lords income: {totalIncome} denars", Colors.Magenta);
                LogPlayerRelevantInfo($"Average income per lord: {averageIncome} denars", Colors.Magenta);
                LogPlayerRelevantInfo($"Highest earning lord: {richestLordName} with {richestLordIncome} denars", new Color(255, 128, 0));
            }
            else
            {
                LogPlayerRelevantInfo($"No lords with income found in {kingdomName}");
            }
        }

        private void LogPlayerKingdomArenaLeaders(List<KeyValuePair<Hero, int>> leaderboard)
        {
            if (!settings.EnablePlayerRelevantLogging || Hero.MainHero?.Clan?.Kingdom == null)
                return;

            var kingdomName = Hero.MainHero.Clan.Kingdom.Name.ToString();
            var kingdomEntries = leaderboard
                .Select((entry, index) => new { Entry = entry, OriginalRank = index + 1 })
                .Where(x => IsHeroInPlayerKingdom(x.Entry.Key))
                .ToList();

            if (kingdomEntries.Any())
            {
                LogPlayerRelevantInfo($"--- {kingdomName} Arena Champions ---", Colors.White);
                foreach (var entry in kingdomEntries)
                {
                    LogPlayerRelevantInfo($"Rank {entry.OriginalRank}: {entry.Entry.Key.Name.ToString()} - {entry.Entry.Value} points", Colors.Red);
                }
            }
        }

        private void LogPlayerKingdomTradeXP()
        {
            if (!settings.EnablePlayerRelevantLogging || Hero.MainHero?.Clan?.Kingdom == null)
                return;

            var kingdomName = Hero.MainHero.Clan.Kingdom.Name.ToString();
            var tradesmenLords = Hero.AllAliveHeroes
                .Where(h => h != Hero.MainHero && IsHeroInPlayerKingdom(h) && h.IsClanLeader && h.OwnedCaravans.Count > 0)
                .ToList();

            if (tradesmenLords.Any())
            {
                LogPlayerRelevantInfo($"--- {kingdomName} Trading Lords ---", Colors.White);
                foreach (var lord in tradesmenLords)
                {
                    int caravanCount = lord.OwnedCaravans.Count;
                    int tradeSkill = lord.GetSkillValue(DefaultSkills.Trade);
                    LogPlayerRelevantInfo($"{lord.Name.ToString()} - Trade skill: {tradeSkill}, Caravans: {caravanCount}");
                }
            }
        }

        private void LogPlayerRelevantInfo(string message)
        {
            LogPlayerRelevantInfo(message, Colors.Yellow);
        }

        private void LogPlayerRelevantInfo(string message, Color color)
        {
            if (settings.EnablePlayerRelevantLogging && Hero.MainHero?.Clan?.Kingdom != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(message, color));
            }
        }

        private static void DebugLogMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
        }

        private void LogMessage(string message)
        {
            LogMessage(message, Colors.Yellow);
        }

        private void LogMessage(string message, Color color)
        {
            if (settings.LoggingEnabled)
            {
                InformationManager.DisplayMessage(new InformationMessage(message, color));
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                if (dataStore.IsLoading)
                {
                    // Load as simple string
                    string savedDataString = "";
                    dataStore.SyncData("AILordsInvestmentString", ref savedDataString);

                    _lordInvestmentTracker.Clear();
                    _hasValidatedInvestmentData = false;

                    if (!string.IsNullOrEmpty(savedDataString))
                    {
                        // Parse the string: "heroId1:settlement1,settlement2|heroId2:settlement3,settlement4"
                        var heroEntries = savedDataString.Split('|');
                        foreach (var heroEntry in heroEntries)
                        {
                            if (string.IsNullOrEmpty(heroEntry)) continue;

                            var parts = heroEntry.Split(':');
                            if (parts.Length == 2)
                            {
                                string heroId = parts[0];
                                var settlements = parts[1].Split(',');

                                if (!string.IsNullOrEmpty(heroId))
                                {
                                    _lordInvestmentTracker[heroId] = new HashSet<string>(settlements.Where(s => !string.IsNullOrEmpty(s)));
                                }
                            }
                        }

                        if (settings.LoggingEnabled)
                        {
                            LogMessage($"Loaded {_lordInvestmentTracker.Count} AI lord records from string", Colors.Green);
                        }
                    }
                }
                else
                {
                    // Save as simple string
                    string dataToSave = "";

                    if (_lordInvestmentTracker.Count > 0)
                    {
                        var heroEntries = new List<string>();
                        foreach (var kvp in _lordInvestmentTracker)
                        {
                            if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value.Count > 0)
                            {
                                string settlements = string.Join(",", kvp.Value);
                                heroEntries.Add($"{kvp.Key}:{settlements}");
                            }
                        }
                        dataToSave = string.Join("|", heroEntries);
                    }

                    dataStore.SyncData("AILordsInvestmentString", ref dataToSave);

                    if (settings.LoggingEnabled)
                    {
                        LogMessage($"Saved investment data as string (length: {dataToSave.Length})", Colors.Green);
                    }
                }
            }
            catch (Exception ex)
            {
                if (settings.LoggingEnabled)
                {
                    LogMessage($"String-based sync error: {ex.Message}", Colors.Red);
                }

                if (dataStore.IsLoading)
                {
                    _lordInvestmentTracker.Clear();
                    _hasValidatedInvestmentData = false;
                }
            }
        }
    }
}