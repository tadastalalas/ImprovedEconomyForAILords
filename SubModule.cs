using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using Helpers;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;


namespace ImprovedEconomyForAILords
{
    public class SubModule : MBSubModuleBase
    {
        private const string ModuleId = "ImprovedEconomyForAILords";

        private readonly UIExtender _extender = new UIExtender(ModuleId);
        private readonly Harmony _harmony = new Harmony(ModuleId);

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            _harmony.PatchAll();
            _extender.Register(typeof(SubModule).Assembly);
            _extender.Enable();
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
            ImprovedEconomyForAILordsBehavior behavior = Campaign.Current?.GetCampaignBehavior<ImprovedEconomyForAILordsBehavior>();
            if (behavior != null)
            {
                CampaignEvents.DailyTickEvent.ClearListeners(behavior);
                CampaignEvents.WeeklyTickEvent.ClearListeners(behavior);
            }

            base.OnGameEnd(game);
        }
    }

    public class ImprovedEconomyForAILordsBehavior : CampaignBehaviorBase
    {
        private static readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        private const int PROSPERITY_DIVISOR = 5;
        private const int HEARTH_DIVISOR = 5;
        private const float TRADE_SKILL_DIVISOR = 200f;
        private const float AI_BUILDING_BOOST_CHANCE = 0.3f;
        private const float RELATION_THRESHOLD_NEGATIVE = -30f;
        private const float RELATION_THRESHOLD_LOW = 29f;
        private const float RELATION_THRESHOLD_MID = 59f;
        private const float RELATION_THRESHOLD_HIGH = 99f;

        private readonly HashSet<Hero> leadersWithFiefs = new();
        private readonly HashSet<Hero> membersWithFiefs = new();
        private readonly HashSet<Hero> leadersNoFiefs = new();
        private readonly HashSet<Hero> membersNoFiefs = new();

        private int clanLeadersWithFiefsGotPaid = 0;
        private int clanMembersWithFiefsGotPaid = 0;
        private int clanLeadersWithoutFiefsGotPaid = 0;
        private int clanMembersWithoutFiefsGotPaid = 0;

        public static int playerTotalIncomeFromAllSources = 0;
        public static int playerTotalIncomeFromKingdomLeader = 0;
        public static int playerTotalIncomeFromTowns = 0;
        public static int playerTotalIncomeFromCastles = 0;
        public static int playerTotalIncomeFromVillages = 0;
        public static int playerIncomeFromArenaLeaderboard = 0;

        private int fieflessRelNoPayClans = 0;
        private int fieflessRelMinus29to29Clans = 0;
        private int fieflessRel30to59Clans = 0;
        private int fieflessRel60to99Clans = 0;
        private int fieflessRel100PlusClans = 0;

        private readonly Dictionary<Hero, (int TownSum, int CastleSum, int VillageSum, int TownPays, int CastlePays, int VillagePays)> _paymentAgg = new();

        private readonly Dictionary<string, HashSet<string>> _lordInvestmentTracker = new();

        private bool _hasValidatedInvestmentData = false;

        public readonly Dictionary<Clan, string> ClanIncomeTotal = new();
        public readonly Dictionary<Hero, string> HeroIncomeTotal = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(OnDailyTickEvent));
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, new Action(OnWeeklyTickEvent));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedEvent));
            CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(OnClanDestroyedEvent));
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

            HandleArenaLeadersForAI();
        }

        
        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
        {
        }


        private void OnNewGameCreatedEvent(CampaignGameStarter starter)
        {
            _lordInvestmentTracker.Clear();
        }

        private void OnClanDestroyedEvent(Clan destroyedClan)
        {
        }

        private void ProcessDenarsRevenueForAILords()
        {
            leadersWithFiefs.Clear();
            membersWithFiefs.Clear();
            leadersNoFiefs.Clear();
            membersNoFiefs.Clear();
            _paymentAgg.Clear();

            fieflessRelNoPayClans = 0;
            fieflessRelMinus29to29Clans = 0;
            fieflessRel30to59Clans = 0;
            fieflessRel60to99Clans = 0;
            fieflessRel100PlusClans = 0;

            playerTotalIncomeFromKingdomLeader = 0;
            playerTotalIncomeFromAllSources = 0;
            playerTotalIncomeFromTowns = 0;
            playerTotalIncomeFromCastles = 0;
            playerTotalIncomeFromVillages = 0;
            playerIncomeFromArenaLeaderboard = 0;

            clanLeadersWithFiefsGotPaid = 0;
            clanMembersWithFiefsGotPaid = 0;
            clanLeadersWithoutFiefsGotPaid = 0;
            clanMembersWithoutFiefsGotPaid = 0;

            ClanIncomeTotal.Clear();
            HeroIncomeTotal.Clear();

            _currentDenarsRevenueMultiplierFromTown = settings.DenarsRevenueMultiplierFromTown;
            _currentDenarsRevenueMultiplierFromCastle = settings.DenarsRevenueMultiplierFromCastle;
            _currentDenarsRevenueMultiplierFromVillage = settings.DenarsRevenueMultiplierFromVillage;
            _currentOtherSameClanMembersRevenueMultiplier = settings.OtherSameClanMembersRevenueMultiplier;
            _currentFieflessClanLeaderRevenueMultiplier = settings.FieflessClanLeaderRevenueMultiplier;

            Dictionary<Clan, int> clanIncomeTracker = new Dictionary<Clan, int>();
            Dictionary<Hero, int> heroIncomeTracker = new Dictionary<Hero, int>();

            foreach (Clan clan in Clan.All)
            {
                if (clan == null || clan.Leader == null || clan.Leader.IsPrisoner || clan.Leader.IsWanderer)
                    continue;
                if (clan == Hero.MainHero.Clan && !settings.EnablePlayerRevenue)
                    continue;

                if (!clanIncomeTracker.ContainsKey(clan))
                    clanIncomeTracker[clan] = 0;

                if (clan.Fiefs.Count > 0)
                {
                    ProcessClanWithFiefs(clan);
                }
                else if (clan.Kingdom != null && settings.ClansWithNoFiefsGetsAShareOfRevenue)
                {
                    ProcessFieflessClan(clan);
                }
            }

            // Populate ClanIncomeTotal from _paymentAgg
            foreach (var kvp in _paymentAgg)
            {
                Hero hero = kvp.Key;
                if (hero?.Clan == null)
                    continue;

                var (townSum, castleSum, villageSum, _, _, _) = kvp.Value;
                int totalIncome = townSum + castleSum + villageSum;

                if (clanIncomeTracker.ContainsKey(hero.Clan))
                    clanIncomeTracker[hero.Clan] += totalIncome;
            }

            // Convert to string format and populate ClanIncomeTotal
            foreach (var kvp in clanIncomeTracker)
            {
                ClanIncomeTotal[kvp.Key] = kvp.Value.ToString();
            }

            // Populate HeroIncomeTotal from _paymentAgg
            foreach (var kvp in _paymentAgg)
            {
                Hero hero = kvp.Key;
                if (hero == null)
                    continue;

                var (townSum, castleSum, villageSum, _, _, _) = kvp.Value;
                int totalIncome = townSum + castleSum + villageSum;

                heroIncomeTracker[hero] = totalIncome;
            }

            // Convert to string format and populate HeroIncomeTotal
            foreach (var kvp in heroIncomeTracker)
            {
                HeroIncomeTotal[kvp.Key] = kvp.Value.ToString();
            }

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
        }

        private void ProcessClanWithFiefs(Clan clan)
        {
            Hero clanLeader = clan.Leader;
            leadersWithFiefs.Add(clanLeader);

            foreach (var fief in clan.Fiefs)
            {
                if (fief != null)
                {
                    bool isCastle = fief.IsCastle;
                    ApplyFiefDenarsBonusForHero(clanLeader, fief, isCastle, true, true);
                    ApplyVillageDenarsBonusForHero(clanLeader, fief, true, true);
                }
            }

            if (settings.AllClanMembersGetRevenue)
            {
                var clanMembers = clan.Heroes
                    .Where(hero => hero != clanLeader && !hero.IsPrisoner && !hero.IsWanderer && IsHeroAdult(hero))
                    .ToList();

                foreach (Hero clanMember in clanMembers)
                {
                    membersWithFiefs.Add(clanMember);

                    foreach (var fief in clan.Fiefs)
                    {
                        if (fief != null)
                        {
                            bool isCastle = fief.IsCastle;

                            ApplyFiefDenarsBonusForHero(clanMember, fief, isCastle, false, true);
                            ApplyVillageDenarsBonusForHero(clanMember, fief, false, true);
                        }
                    }
                }
            }
        }

        private void ProcessFieflessClan(Clan clan)
        {
            Hero? kingdomLeader = clan.Kingdom?.Leader;

            if (kingdomLeader == null || !kingdomLeader.IsKingdomLeader)
                return;
            if (kingdomLeader.Clan == null || kingdomLeader.Clan.Fiefs.Count == 0)
                return;

            List<Hero> fieflessClanMembers = clan.Heroes
                .Where(hero => !hero.IsPrisoner && !hero.IsWanderer && IsHeroAdult(hero))
                .ToList();

            if (fieflessClanMembers.Count == 0)
                return;

            float relation = clan.Leader.GetRelation(kingdomLeader);
            float localFieflessClanMembersRevenueMultiplier = GetFieflessRevenueMultiplier(relation);

            if (localFieflessClanMembersRevenueMultiplier <= 0f)
                return;

            foreach (var fief in kingdomLeader.Clan.Fiefs)
            {
                if (fief == null)
                    continue;

                bool isCastle = fief.IsCastle;

                foreach (Hero fieflessClanMember in fieflessClanMembers)
                {
                    bool isClanLeader = fieflessClanMember == clan.Leader;

                    if (isClanLeader)
                        leadersNoFiefs.Add(fieflessClanMember);
                    else if (settings.AllClanMembersGetRevenue)
                        membersNoFiefs.Add(fieflessClanMember);
                    else
                        continue;

                    ApplyFiefDenarsBonusForHero(fieflessClanMember, fief, isCastle, isClanLeader, false, localFieflessClanMembersRevenueMultiplier);
                    ApplyVillageDenarsBonusForHero(fieflessClanMember, fief, isClanLeader, false, localFieflessClanMembersRevenueMultiplier);
                }
            }
        }

        private float GetFieflessRevenueMultiplier(float relation)
        {
            if (relation <= RELATION_THRESHOLD_NEGATIVE)
            {
                fieflessRelNoPayClans++;
                return 0f;
            }

            if (relation <= RELATION_THRESHOLD_LOW)
            {
                fieflessRelMinus29to29Clans++;
                return settings.Relation0RevenueMultiplier;
            }

            if (relation <= RELATION_THRESHOLD_MID)
            {
                fieflessRel30to59Clans++;
                return settings.Relation30RevenueMultiplier;
            }

            if (relation <= RELATION_THRESHOLD_HIGH)
            {
                fieflessRel60to99Clans++;
                return settings.Relation60RevenueMultiplier;
            }

            fieflessRel100PlusClans++;
            return settings.Relation100RevenueMultiplier;
        }

        private float _currentDenarsRevenueMultiplierFromTown;
        private float _currentDenarsRevenueMultiplierFromCastle;
        private float _currentDenarsRevenueMultiplierFromVillage;
        private float _currentOtherSameClanMembersRevenueMultiplier;
        private float _currentFieflessClanLeaderRevenueMultiplier;

        private void ApplyFiefDenarsBonusForHero(Hero hero, Town town, bool IsFiefACastle, bool IsClanLeader, bool HasFief, float fieflessMult = 1f)
        {
            if (!settings.EnableAILordsTownsRevenue && !settings.EnableAILordsCastlesRevenue)
                return;

            int basePayment = (int)((CalculateFiefDenarsPayment(town) * ConsiderLordsTradeSkill(hero, settings)));
            basePayment = (int)(basePayment * (IsFiefACastle ? _currentDenarsRevenueMultiplierFromCastle : _currentDenarsRevenueMultiplierFromTown));

            int actualPayment = ProcessPaymentForHero(hero, basePayment, IsFiefACastle, IsClanLeader, HasFief, false, fieflessMult);
            UpdatePaymentAggregation(hero, actualPayment, IsFiefACastle, isVillage: false);
        }

        private void ApplyVillageDenarsBonusForHero(Hero hero, Town fief, bool IsClanLeader, bool HasFief, float fieflessMult = 1f)
        {
            if (!settings.EnableAILordsVillagesRevenue)
                return;

            foreach (Village village in fief.Villages)
            {
                int basePayment = (int)((CalculateVillageDenarsPayment(village) * ConsiderLordsTradeSkill(hero, settings))
                    * _currentDenarsRevenueMultiplierFromVillage);

                int actualPayment = ProcessPaymentForHero(hero, basePayment, false, IsClanLeader, HasFief, true, fieflessMult);
                UpdatePaymentAggregation(hero, actualPayment, false, isVillage: true);
            }
        }

        private int ProcessPaymentForHero(Hero hero, int basePayment, bool isFiefACastle,
            bool isClanLeader, bool hasFief, bool isVillage, float fieflessMult = 1f)
        {
            int payment = basePayment;

            if (hero.Clan == Hero.MainHero.Clan)
            {
                payment = (int)(payment * settings.PlayerRevenueMultiplier);
            }

            if (isClanLeader && hasFief)
            {
                clanLeadersWithFiefsGotPaid += payment;
                if (hero == Hero.MainHero)
                    CalculateHowMuchRevenuePlayerGets(payment, hero, isFiefACastle, isClanLeader, hasFief, isVillage);
            }
            else if (!isClanLeader && hasFief)
            {
                payment = (int)(payment * _currentOtherSameClanMembersRevenueMultiplier);
                clanMembersWithFiefsGotPaid += payment;
            }
            else if (isClanLeader && !hasFief)
            {
                payment = (int)(payment * fieflessMult * _currentFieflessClanLeaderRevenueMultiplier);
                clanLeadersWithoutFiefsGotPaid += payment;
                if (hero == Hero.MainHero)
                    CalculateHowMuchRevenuePlayerGets(payment, hero, isFiefACastle, isClanLeader, hasFief, isVillage);
            }
            else if (!isClanLeader && !hasFief)
            {
                payment = (int)(payment * fieflessMult);
                clanMembersWithoutFiefsGotPaid += payment;
            }
            if (hero != Hero.MainHero)
            {
                hero.ChangeHeroGold(payment);
            }
            return payment;
        }

        private void CalculateHowMuchRevenuePlayerGets(int payment, Hero hero, bool IsFiefACastle, bool IsClanLeader, bool HasFief, bool IsVillage)
        {
            playerTotalIncomeFromAllSources += payment;

            if (!hero.IsKingdomLeader && !HasFief)
            {
                playerTotalIncomeFromKingdomLeader += payment;
                return;
            }

            if (HasFief && !IsVillage)
            {
                if (IsFiefACastle)
                    playerTotalIncomeFromCastles += payment;
                else
                    playerTotalIncomeFromTowns += payment;
            }

            if (IsVillage)
                playerTotalIncomeFromVillages += payment;
        }

        private void UpdatePaymentAggregation(Hero hero, int payment, bool isCastle, bool isVillage)
        {
            if (_paymentAgg.TryGetValue(hero, out var agg))
            {
                if (isVillage)
                    _paymentAgg[hero] = (agg.TownSum, agg.CastleSum, agg.VillageSum + payment,
                        agg.TownPays, agg.CastlePays, agg.VillagePays + 1);
                else if (isCastle)
                    _paymentAgg[hero] = (agg.TownSum, agg.CastleSum + payment, agg.VillageSum,
                        agg.TownPays, agg.CastlePays + 1, agg.VillagePays);
                else
                    _paymentAgg[hero] = (agg.TownSum + payment, agg.CastleSum, agg.VillageSum,
                        agg.TownPays + 1, agg.CastlePays, agg.VillagePays);
            }
            else
            {
                _paymentAgg[hero] = isVillage ? (0, 0, payment, 0, 0, 1) :
                                   isCastle ? (0, payment, 0, 0, 1, 0) :
                                             (payment, 0, 0, 1, 0, 0);
            }
        }

        public static int CalculateFiefDenarsPayment(Town town)
        {
            if (town == null || town.IsUnderSiege)
                return 0;

            return (int)town.Prosperity / PROSPERITY_DIVISOR;
        }

        public static int CalculateVillageDenarsPayment(Village village)
        {
            if (village == null || village.Settlement.IsRaided || village.Settlement.IsUnderRaid)
                return 0;

            return (int)village.Hearth / HEARTH_DIVISOR;
        }

        private static float ConsiderLordsTradeSkill(Hero hero, MCMSettings settings)
        {
            if (settings.ConsiderLordsTradeSkill)
            {
                int tradeSkillValue = hero.GetSkillValue(DefaultSkills.Trade);
                return 1f + (tradeSkillValue / TRADE_SKILL_DIVISOR);
            }
            return 1f;
        }

        private void HandleArenaLeadersForAI()
        {
            if (!settings.EnableArenaRevenue)
                return;

            playerIncomeFromArenaLeaderboard = 0;

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
            }

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                Hero hero = entry.Key;
                int actualRank = i + 1;

                if (hero.IsPrisoner)
                    continue;

                int reward = CalculateArenaReward(actualRank);

                if (reward > 0)
                {
                    if (hero.Clan == Hero.MainHero.Clan)
                    {
                        reward = (int)(reward * settings.PlayerRevenueMultiplier);
                    }

                    if (hero != Hero.MainHero)
                    {
                        hero.ChangeHeroGold(reward);
                    }

                    if (hero == Hero.MainHero)
                        playerIncomeFromArenaLeaderboard = reward;

                    if (settings.LoggingEnabled)
                    {
                        LogMessage($"{hero.Name} earned {reward} denars from arena rank {actualRank}");
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
                if (town?.OwnerClan?.Leader == null || town.OwnerClan.Leader == Hero.MainHero || town.OwnerClan.Leader.IsWanderer)
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

                if (MBRandom.RandomFloat > AI_BUILDING_BOOST_CHANCE)
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
                    string savedDataString = "";
                    dataStore.SyncData("AILordsInvestmentString", ref savedDataString);

                    _lordInvestmentTracker.Clear();
                    _hasValidatedInvestmentData = false;

                    if (!string.IsNullOrEmpty(savedDataString))
                    {
                        var heroEntries = savedDataString.Split('|');
                        foreach (var heroEntry in heroEntries)
                        {
                            if (string.IsNullOrEmpty(heroEntry)) continue;

                            var parts = heroEntry.Split(':');
                            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]))
                                continue;

                            string heroId = parts[0];
                            var settlements = parts[1].Split(',')
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToHashSet();

                            if (settlements.Count > 0)
                                _lordInvestmentTracker[heroId] = settlements;

                        }

                        if (settings.LoggingEnabled)
                            LogMessage($"Loaded {_lordInvestmentTracker.Count} AI lord records from string", Colors.Green);
                    }
                }
                else
                {
                    string dataToSave = "";

                    if (_lordInvestmentTracker.Count > 0)
                    {
                        var heroEntries = new List<string>();
                        foreach (var kvp in _lordInvestmentTracker)
                        {
                            if (!string.IsNullOrEmpty(kvp.Key) && kvp.Value.Count > 0)
                            {
                                bool heroExists = Hero.AllAliveHeroes.Any(h => h.StringId == kvp.Key);
                                if (!heroExists) continue;

                                var validSettlements = kvp.Value
                                    .Where(sid => Settlement.All.Any(s => s.StringId == sid))
                                    .ToList();

                                if (validSettlements.Count > 0)
                                    heroEntries.Add($"{kvp.Key}:{string.Join(",", validSettlements)}");
                            }
                        }
                        dataToSave = string.Join("|", heroEntries);
                    }

                    dataStore.SyncData("AILordsInvestmentString", ref dataToSave);

                    if (settings.LoggingEnabled)
                        LogMessage($"Saved investment data as string (length: {dataToSave.Length})", Colors.Green);
                }
            }
            catch (Exception ex)
            {
                if (settings.LoggingEnabled)
                    LogMessage($"String-based sync error: {ex.Message}", Colors.Red);

                if (dataStore.IsLoading)
                {
                    _lordInvestmentTracker.Clear();
                    _hasValidatedInvestmentData = false;
                }
            }
        }
    }
}