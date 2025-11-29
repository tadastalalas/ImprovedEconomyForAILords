using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace ImprovedEconomyForAILords
{
    internal class MCMSettings : AttributeGlobalSettings<MCMSettings>
    {
        public override string Id => "ImprovedEconomyForAILordsSettings";
        public override string DisplayName => new TextObject("{=IEFAIL_WO2gdm}Improved Economy For AI Lords").ToString();
        public override string FolderName => "ImprovedEconomyForAILords";
        public override string FormatType => "json2";

        // Main settings group
        [SettingPropertyBool("{=IEFAIL_3IXOnv}Enable This Modification", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_dn5p7T}Enable this modification. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public bool EnableThisModification { get; set; } = true;

        [SettingPropertyBool("{=IEFAIL_4owJ8r}Enable Player Revenue", Order = 1, RequireRestart = false, HintText = "{=IEFAIL_KWLmkq}Enable revenue for Player Lords. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public bool EnablePlayerRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("{=IEFAIL_gBadtv}Player Revenue Multiplier", 0.1f, 1.0f, "0.00", Order = 2, RequireRestart = false, HintText = "{=IEFAIL_zOjwvh}Multiplier for player clan revenue. Lower values reduce player income. [Default: 0.30]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public float PlayerRevenueMultiplier { get; set; } = 0.30f;

        [SettingPropertyBool("{=IEFAIL_K3lo31}Enable AI Lords' Towns Revenue", Order = 3, RequireRestart = false, HintText = "{=IEFAIL_uO5Akt}Enable towns revenue for AI Lords. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public bool EnableAILordsTownsRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("{=IEFAIL_qNOLon}Town Denar Revenue Multiplier", 0f, 10.0f, "0.00", Order = 4, RequireRestart = false, HintText = "{=IEFAIL_hxX9ri}Multiplier for town prosperity income. If set to the default value of 1.00, AI Lords’ income will match the description on the Nexus page. [Default: 1.00]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public float DenarsRevenueMultiplierFromTown { get; set; } = 1.00f;

        [SettingPropertyBool("{=IEFAIL_5mR3zp}Enable AI Lords' Castles Revenue", Order = 5, RequireRestart = false, HintText = "{=IEFAIL_lACQz8}Enable castles revenue for AI Lords. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public bool EnableAILordsCastlesRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("{=IEFAIL_Be1Tts}Castle Denar Revenue Multiplier", 0f, 10.0f, "0.00", Order = 6, RequireRestart = false, HintText = "{=IEFAIL_xE4L22}Multiplier for castle prosperity income. [Default: 1.00]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public float DenarsRevenueMultiplierFromCastle { get; set; } = 1.00f;

        [SettingPropertyBool("{=IEFAIL_fBmPe5}Enable AI Lords' Villages Revenue", Order = 7, RequireRestart = false, HintText = "{=IEFAIL_8wrURV}Enable villages revenue for AI Lords. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public bool EnableAILordsVillagesRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("{=IEFAIL_FOjwIB}Village Denar Revenue Multiplier", 0f, 10.0f, "0.00", Order = 8, RequireRestart = false, HintText = "{=IEFAIL_ydjRvC}Multiplier for village hearth income. If set to the default value of 1.00, AI Lords’ income will match the description on the Nexus page. [Default: 1.00]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public float DenarsRevenueMultiplierFromVillage { get; set; } = 1.00f;

        [SettingPropertyBool("{=IEFAIL_BoPM3k}Consider Lords' Trade Skill", Order = 9, RequireRestart = false, HintText = "{=IEFAIL_OAoKcv}Enable this to consider Lords' trade skill when calculating revenue. For example Trade skill of 75 will increase revenue by 37%. [Default: false]")]
        [SettingPropertyGroup("{=IEFAIL_BTTEQa}Main Settings", GroupOrder = 0)]
        public bool ConsiderLordsTradeSkill { get; set; } = false;


        // Same-clan member revenue settings
        [SettingPropertyBool("{=IEFAIL_wwfNzO}Other Clan Members Get Revenue", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_T1H7z0}When this option is enabled all other clan members (not even clan leader) will get revenue. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_zVBpU2}Other Members Of The Same Clan Settings", GroupOrder = 1)]
        public bool AllClanMembersGetRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("{=IEFAIL_8urE1P}Other Clan Members Revenue Percentage", 0f, 1.0f, "0.00", Order = 1, RequireRestart = false, HintText = "{=IEFAIL_4703gl}All other clan members' revenue percentage. Default is 20% of what clan leader gets. [Default: 0.20]")]
        [SettingPropertyGroup("{=IEFAIL_zVBpU2}Other Members Of The Same Clan Settings", GroupOrder = 1)]
        public float OtherSameClanMembersRevenueMultiplier { get; set; } = 0.20f;


        // Fiefless (no fiefs) clan settings
        [SettingPropertyBool("{=IEFAIL_LCRAH6}Clans With No Fiefs Gets A Share Of Revenue", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_1AyhZy}Other clans with no fiefs will earn revenue based on the relationship with the kingdom leader and fiefless clan leader. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_KPSPjv}Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public bool ClansWithNoFiefsGetsAShareOfRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("{=IEFAIL_c9l9FN}0 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 1, RequireRestart = false, HintText = "{=IEFAIL_JoAUXL}Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 0 with the kingdom leader. [Default: 0.05]")]
        [SettingPropertyGroup("{=IEFAIL_KPSPjv}Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation0RevenueMultiplier { get; set; } = 0.05f;

        [SettingPropertyFloatingInteger("{=IEFAIL_VUVXkB}30 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 2, RequireRestart = false, HintText = "{=IEFAIL_6osLoj}Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 30 with the kingdom leader. [Default: 0.10]")]
        [SettingPropertyGroup("{=IEFAIL_KPSPjv}Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation30RevenueMultiplier { get; set; } = 0.10f;

        [SettingPropertyFloatingInteger("{=IEFAIL_9yKPTN}60 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 3, RequireRestart = false, HintText = "{=IEFAIL_q47Ow6}Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 60 with the kingdom leader. [Default: 0.15]")]
        [SettingPropertyGroup("{=IEFAIL_KPSPjv}Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation60RevenueMultiplier { get; set; } = 0.15f;

        [SettingPropertyFloatingInteger("{=IEFAIL_WbnLkQ}100 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 4, RequireRestart = false, HintText = "{=IEFAIL_iV7jf9}Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 100 with the kingdom leader. [Default: 0.20]")]
        [SettingPropertyGroup("{=IEFAIL_KPSPjv}Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation100RevenueMultiplier { get; set; } = 0.20f;

        [SettingPropertyFloatingInteger("{=IEFAIL_VTwptk}How Much More Percentage Clan Leader Will Get", 1.0f, 10.0f, "0.00", Order = 5, RequireRestart = false, HintText = "{=IEFAIL_pGAU00}Fiefless Clan leader revenue percentage added after above relationship percentages are calculated. In default setting fiefless clan leader will earn 50% more than other his clan members. [Default: 1.50]")]
        [SettingPropertyGroup("{=IEFAIL_KPSPjv}Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float FieflessClanLeaderRevenueMultiplier { get; set; } = 1.50f;


        // AI Lords' caravans settings
        [SettingPropertyBool("{=IEFAIL_rgVmYA}Enable AI Lords' Caravans", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_8ol5a2}Enable spawning/handling caravans of AI Lords. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_fyyCjf}Caravans Settings", GroupOrder = 3)]
        public bool EnableAILordsCaravans { get; set; } = true;
        /*
        [SettingPropertyInteger("{=IEFAIL_USwg4A}Caravans Troops Amount", 10, 100, Order = 1, RequireRestart = false, HintText = "{=IEFAIL_kZKaW2}Amount of troops caravans will have when created. [Default: 30]")]
        [SettingPropertyGroup("{=IEFAIL_fyyCjf}Caravans Settings", GroupOrder = 3)]
        public int CaravansTroopsAmount { get; set; } = 30;
        */
        [SettingPropertyInteger("{=IEFAIL_DYTASs}Caravans Denars Amount", 1000, 100000, Order = 1, RequireRestart = false, HintText = "{=IEFAIL_1Ek9CH}Amount of denars caravans will have when created. [Default: 8520]")]
        [SettingPropertyGroup("{=IEFAIL_fyyCjf}Caravans Settings", GroupOrder = 3)]
        public int CaravansDenarsAmount { get; set; } = 8520;

        [SettingPropertyBool("{=IEFAIL_Kiyniz}Enable AI Lords' Trade Experience", Order = 2, RequireRestart = false, HintText = "{=IEFAIL_ZcELyJ}Enable trade skill experience for AI lords with caravans. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_fyyCjf}Caravans Settings", GroupOrder = 4)]
        public bool EnableAILordsTradeExperience { get; set; } = true;

        [SettingPropertyInteger("{=IEFAIL_NKMFnq}Trade XP Per Caravan Daily", 1, 100, Order = 3, RequireRestart = false, HintText = "{=IEFAIL_6wOF90}Base amount of trade experience per caravan that AI lords receive daily. [Default: 15]")]
        [SettingPropertyGroup("{=IEFAIL_fyyCjf}Caravans Settings", GroupOrder = 4)]
        public int TradeExperiencePerCaravanDaily { get; set; } = 15;

        [SettingPropertyBool("{=IEFAIL_1Kn8ZV}Consider Focus Points For Trade XP", Order = 4, RequireRestart = false, HintText = "{=IEFAIL_lUfWn9}When enabled, trade experience from caravans will be affected by focus points and attributes. [Default: false]")]
        [SettingPropertyGroup("{=IEFAIL_fyyCjf}Caravans Settings", GroupOrder = 4)]
        public bool ConsiderFocusFactorsForTradeXp { get; set; } = false;


        // Arena revenue settings
        [SettingPropertyBool("{=IEFAIL_h8Whk3}Enable AI Lords' Arena Revenue", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_yWOIIP}Enable arena tournament leaderboard revenue for AI Lords. Every week 12 (by default) most distinguished heroes will get monetary rewards based on their rankings and adjusted number below. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_IijmV5}Arena Revenue Settings", GroupOrder = 4)]
        public bool EnableAILordsArenaRevenue { get; set; } = true;

        [SettingPropertyBool("{=IEFAIL_n5p2Xp}Enable Player Arena Revenue", Order = 1, RequireRestart = false, HintText = "{=IEFAIL_v6mbdQ}Enable arena tournament leaderboard revenue for Player. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_IijmV5}Arena Revenue Settings", GroupOrder = 4)]
        public bool EnablePlayerArenaRevenue { get; set; } = true;

        [SettingPropertyInteger("{=IEFAIL_LWsF6y}Arena Base Reward", 100, 100000, Order = 2, RequireRestart = false, HintText = "{=IEFAIL_v7Fo3j}Base reward for arena top leaderboard members. First place gets 2x this amount, 10th place gets 0.5x. [Default: 12460]")]
        [SettingPropertyGroup("{=IEFAIL_IijmV5}Arena Revenue Settings", GroupOrder = 4)]
        public int ArenaBaseReward { get; set; } = 12460;

        [SettingPropertyInteger("{=IEFAIL_AMlJaO}Arena Leaderboard Count", 3, 30, Order = 3, RequireRestart = false, HintText = "{=IEFAIL_ePuYiy}Number of arena leaderboard members to reward (from 3 to 30). [Default: 12]")]
        [SettingPropertyGroup("{=IEFAIL_IijmV5}Arena Revenue Settings", GroupOrder = 4)]
        public int ArenaLeaderboardCount { get; set; } = 12;


        // Towns settings
        [SettingPropertyBool("{=IEFAIL_V4JjrL}Enable Towns Denars Increase", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_uEtPPL}Enable towns denars increase. Towns denars will be increased based on village's hearth. Formula: Vanilla amount (1000 denars) + hearth level (for example 243) = 1243 daily village denars. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_udRISV}Towns Settings", GroupOrder = 5)]
        public bool EnableTownsDenarsIncrease { get; set; } = true;


        // Building boost settings
        [SettingPropertyBool("{=IEFAIL_G84W1z}Enable AI Lords' Building Boost", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_odBTq0}Enable automatic building boost for AI lords' settlements. [Default: true]")]
        [SettingPropertyGroup("{=IEFAIL_RwvFU8}Building Boost Settings", GroupOrder = 6)]
        public bool EnableAILordsBuildingBoost { get; set; } = true;

        [SettingPropertyFloatingInteger("{=IEFAIL_KhxdZq}AI Lords Building Boost Multiplier", 1.0f, 10.0f, "0.00", Order = 1, RequireRestart = false, HintText = "{=IEFAIL_EITu0G}Multiplier for AI lords' building construction speed. Higher values mean faster building construction. [Default: 1.50]")]
        [SettingPropertyGroup("{=IEFAIL_RwvFU8}Building Boost Settings", GroupOrder = 6)]
        public float AILordsBuildingBoostMultiplier { get; set; } = 1.50f;

        [SettingPropertyFloatingInteger("{=IEFAIL_rjeGzF}Denars In Percent AI Clan Lord Will Invest For Buildings Boost", 0.0f, 100.0f, "0.0", Order = 2, RequireRestart = false, HintText = "{=IEFAIL_f775tL}Percentage of AI clan lord's gold they will invest in building boosts (0.0 = none, 100.0 = all gold). [Default: 10.0]")]
        [SettingPropertyGroup("{=IEFAIL_RwvFU8}Building Boost Settings", GroupOrder = 6)]
        public float AILordsInvestmentPercentage { get; set; } = 10.0f;


        // Player-relevant logging settings
        [SettingPropertyBool("{=IEFAIL_6UGGcL}Enable Player-Relevant Logging", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_Ju7EwW}Show logs relevant to the player's kingdom. [Default: false]")]
        [SettingPropertyGroup("{=IEFAIL_cB54lB}Player Logging", GroupOrder = 7)]
        public bool EnablePlayerRelevantLogging { get; set; } = false;


        // Debug / technical settings
        [SettingPropertyBool("{=IEFAIL_BM0QvR}Logging For Debugging", Order = 0, RequireRestart = false, HintText = "{=IEFAIL_qAsIl8}Logging for debugging. [Default: disabled]")]
        [SettingPropertyGroup("{=IEFAIL_ADa3ys}Technical Settings", GroupOrder = 8)]
        public bool LoggingEnabled { get; set; } = false;
    }
}