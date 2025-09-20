using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace ImprovedEconomyForAILords
{
    internal class MCMSettings : AttributeGlobalSettings<MCMSettings>
    {
        public override string Id => "ImprovedEconomyForAILordsSettings";
        public override string DisplayName => new TextObject("Improved Economy For AI Lords").ToString();
        public override string FolderName => "ImprovedEconomyForAILords";
        public override string FormatType => "json2";

        // Main settings group
        [SettingPropertyBool("Enable This Modification", Order = 0, RequireRestart = false, HintText = "Enable this modification. [Default: true]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public bool EnableThisModification { get; set; } = true;

        [SettingPropertyBool("Enable AI Lords' Towns Revenue", Order = 1, RequireRestart = false, HintText = "Enable towns revenue for AI Lords. [Default: true]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public bool EnableAILordsTownsRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("Town Denar Revenue Multiplier", 0f, 10.0f, "0.00", Order = 2, RequireRestart = false, HintText = "Multiplier for town prosperity income. If set to the default value of 1.00, AI Lords’ income will match the description on the Nexus page. [Default: 1.00]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public float DenarsRevenueMultiplierFromTown { get; set; } = 1.00f;

        [SettingPropertyBool("Enable AI Lords' Castles Revenue", Order = 3, RequireRestart = false, HintText = "Enable castles revenue for AI Lords. [Default: true]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public bool EnableAILordsCastlesRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("Castle Denar Revenue Multiplier", 0f, 10.0f, "0.00", Order = 4, RequireRestart = false, HintText = "Multiplier for castle prosperity income. [Default: 1.00]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public float DenarsRevenueMultiplierFromCastle { get; set; } = 1.00f;

        [SettingPropertyBool("Enable AI Lords' Villages Revenue", Order = 5, RequireRestart = false, HintText = "Enable villages revenue for AI Lords. [Default: true]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public bool EnableAILordsVillagesRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("Village Denar Revenue Multiplier", 0f, 10.0f, "0.00", Order = 6, RequireRestart = false, HintText = "Multiplier for village hearth income. If set to the default value of 1.00, AI Lords’ income will match the description on the Nexus page. [Default: 1.00]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public float DenarsRevenueMultiplierFromVillage { get; set; } = 1.00f;

        [SettingPropertyBool("Consider Lords' Trade Skill", Order = 7, RequireRestart = false, HintText = "Enable this to consider Lords' trade skill when calculating revenue. For example Trade skill of 75 will increase revenue by 37%. [Default: false]")]
        [SettingPropertyGroup("Main Settings", GroupOrder = 0)]
        public bool ConsiderLordsTradeSkill { get; set; } = false;


        // Same-clan member revenue settings
        [SettingPropertyBool("Other Clan Members Get Revenue", Order = 0, RequireRestart = false, HintText = "When this option is enabled all other clan members (not even clan leader) will get revenue. [Default: true]")]
        [SettingPropertyGroup("Other Members Of The Same Clan Settings", GroupOrder = 1)]
        public bool AllClanMembersGetRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("Other Clan Members Revenue Percentage", 0f, 1.0f, "0.00", Order = 1, RequireRestart = false, HintText = "All other clan members' revenue percentage. Default is 20% of what clan leader gets. [Default: 0.20]")]
        [SettingPropertyGroup("Other Members Of The Same Clan Settings", GroupOrder = 1)]
        public float OtherSameClanMembersRevenueMultiplier { get; set; } = 0.20f;


        // Fiefless (no fiefs) clan settings
        [SettingPropertyBool("Clans With No Fiefs Gets A Share Of Revenue", Order = 0, RequireRestart = false, HintText = "Other clans with no fiefs will earn revenue based on the relationship with the kingdom leader and fiefless clan leader. [Default: true]")]
        [SettingPropertyGroup("Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public bool ClansWithNoFiefsGetsAShareOfRevenue { get; set; } = true;

        [SettingPropertyFloatingInteger("0 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 1, RequireRestart = false, HintText = "Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 0 with the kingdom leader. [Default: 0.05]")]
        [SettingPropertyGroup("Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation0RevenueMultiplier { get; set; } = 0.05f;

        [SettingPropertyFloatingInteger("30 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 2, RequireRestart = false, HintText = "Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 30 with the kingdom leader. [Default: 0.10]")]
        [SettingPropertyGroup("Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation30RevenueMultiplier { get; set; } = 0.10f;

        [SettingPropertyFloatingInteger("60 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 3, RequireRestart = false, HintText = "Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 60 with the kingdom leader. [Default: 0.15]")]
        [SettingPropertyGroup("Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation60RevenueMultiplier { get; set; } = 0.15f;

        [SettingPropertyFloatingInteger("100 Relation Revenue Percentage", 0f, 1.0f, "0.00", Order = 4, RequireRestart = false, HintText = "Fiefless Clans' members revenue percentage when fiefless clan's leader has relation of 100 with the kingdom leader. [Default: 0.20]")]
        [SettingPropertyGroup("Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float Relation100RevenueMultiplier { get; set; } = 0.20f;

        [SettingPropertyFloatingInteger("How Much More Percentage Clan Leader Will Get", 1.0f, 10.0f, "0.00", Order = 5, RequireRestart = false, HintText = "Fiefless Clan leader revenue percentage added after above relationship percentages are calculated. In default setting fiefless clan leader will earn 50% more than other his clan members. [Default: 1.50]")]
        [SettingPropertyGroup("Other Clans With No Fiefs Settings", GroupOrder = 2)]
        public float FieflessClanLeaderRevenueMultiplier { get; set; } = 1.50f;


        // AI Lords' caravans settings
        [SettingPropertyBool("Enable AI Lords' Caravans", Order = 0, RequireRestart = false, HintText = "Enable spawning/handling caravans of AI Lords. [Default: true]")]
        [SettingPropertyGroup("Caravans Settings", GroupOrder = 3)]
        public bool EnableAILordsCaravans { get; set; } = true;

        //[SettingPropertyInteger("Caravans Troops Amount", 10, 100, Order = 1, RequireRestart = false, HintText = "Amount of troops caravans will have when created. [Default: 30]")]
        //[SettingPropertyGroup("Caravans Settings", GroupOrder = 3)]
        // public int CaravansTroopsAmount { get; set; } = 30;

        [SettingPropertyInteger("Caravans Denars Amount", 1000, 100000, Order = 2, RequireRestart = false, HintText = "Amount of denars caravans will have when created. [Default: 8520]")]
        [SettingPropertyGroup("Caravans Settings", GroupOrder = 3)]
        public int CaravansDenarsAmount { get; set; } = 8520;

        [SettingPropertyBool("Enable AI Lords' Trade Experience", Order = 3, RequireRestart = false, HintText = "Enable trade skill experience for AI lords with caravans. [Default: true]")]
        [SettingPropertyGroup("Caravans Settings", GroupOrder = 4)]
        public bool EnableAILordsTradeExperience { get; set; } = true;

        [SettingPropertyInteger("Trade XP Per Caravan Daily", 1, 100, Order = 4, RequireRestart = false, HintText = "Base amount of trade experience per caravan that AI lords receive daily. [Default: 15]")]
        [SettingPropertyGroup("Caravans Settings", GroupOrder = 4)]
        public int TradeExperiencePerCaravanDaily { get; set; } = 15;

        [SettingPropertyBool("Consider Focus Points For Trade XP", Order = 5, RequireRestart = false, HintText = "When enabled, trade experience from caravans will be affected by focus points and attributes. [Default: false]")]
        [SettingPropertyGroup("Caravans Settings", GroupOrder = 4)]
        public bool ConsiderFocusFactorsForTradeXp { get; set; } = false;


        // Arena revenue settings
        [SettingPropertyBool("Enable AI Lords' Arena Revenue", Order = 0, RequireRestart = false, HintText = "Enable arena tournament leaderboard revenue for AI Lords. Every week 10 most distinguished heroes will get monetary rewards based on their rankings and adjusted number below. [Default: true]")]
        [SettingPropertyGroup("Arena Revenue Settings", GroupOrder = 4)]
        public bool EnableAILordsArenaRevenue { get; set; } = true;

        [SettingPropertyInteger("Arena Base Reward", 100, 100000, Order = 1, RequireRestart = false, HintText = "Base reward for arena top leaderboard members. First place gets 2x this amount, 10th place gets 0.5x. [Default: 12460]")]
        [SettingPropertyGroup("Arena Revenue Settings", GroupOrder = 4)]
        public int ArenaBaseReward { get; set; } = 12460;

        [SettingPropertyInteger("Arena Leaderboard Count", 3, 30, Order = 2, RequireRestart = false, HintText = "Number of arena leaderboard members to reward (from 3 to 30). [Default: 12]")]
        [SettingPropertyGroup("Arena Revenue Settings", GroupOrder = 4)]
        public int ArenaLeaderboardCount { get; set; } = 12;


        // Towns settings
        [SettingPropertyBool("Enable Towns Denars Increase", Order = 0, RequireRestart = false, HintText = "Enable towns denars increase. Towns denars will be increased based on village's hearth. Formula: Vanilla amount (1000 denars) + hearth level (for example 243) = 1243 daily village denars. [Default: true]")]
        [SettingPropertyGroup("Towns Settings", GroupOrder = 5)]
        public bool EnableTownsDenarsIncrease { get; set; } = true;


        // Building boost settings
        [SettingPropertyBool("Enable AI Lords' Building Boost", Order = 0, RequireRestart = false, HintText = "Enable automatic building boost for AI lords' settlements. [Default: true]")]
        [SettingPropertyGroup("Building Boost Settings", GroupOrder = 6)]
        public bool EnableAILordsBuildingBoost { get; set; } = true;

        [SettingPropertyFloatingInteger("AI Lords Building Boost Multiplier", 1.0f, 10.0f, "0.00", Order = 1, RequireRestart = false, HintText = "Multiplier for AI lords' building construction speed. Higher values mean faster building construction. [Default: 1.50]")]
        [SettingPropertyGroup("Building Boost Settings", GroupOrder = 6)]
        public float AILordsBuildingBoostMultiplier { get; set; } = 1.50f;

        [SettingPropertyFloatingInteger("Denars In Percent AI Clan Lord Will Invest For Buildings Boost", 0.0f, 100.0f, "0.0", Order = 2, RequireRestart = false, HintText = "Percentage of AI clan lord's gold they will invest in building boosts (0.0 = none, 100.0 = all gold). [Default: 10.0]")]
        [SettingPropertyGroup("Building Boost Settings", GroupOrder = 6)]
        public float AILordsInvestmentPercentage { get; set; } = 10.0f;


        // Player-relevant logging settings
        [SettingPropertyBool("Enable Player-Relevant Logging", Order = 0, RequireRestart = false, HintText = "Show logs relevant to the player's kingdom. [Default: false]")]
        [SettingPropertyGroup("Player Logging", GroupOrder = 7)]
        public bool EnablePlayerRelevantLogging { get; set; } = false;


        // Debug / technical settings
        [SettingPropertyBool("Logging For Debugging", Order = 0, RequireRestart = false, HintText = "Logging for debugging. [Default: disabled]")]
        [SettingPropertyGroup("Technical Settings", GroupOrder = 8)]
        public bool LoggingEnabled { get; set; } = false;
    }
}