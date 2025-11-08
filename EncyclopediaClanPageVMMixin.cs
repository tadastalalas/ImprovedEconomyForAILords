using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Bannerlord.UIExtenderEx.ViewModels;
using MCM.Common;

namespace ImprovedEconomyForAILords
{
    [ViewModelMixin(nameof(EncyclopediaClanPageVM.RefreshValues), true)]
    public class EncyclopediaClanPageVMMixin : BaseViewModelMixin<EncyclopediaClanPageVM>
    {
        private readonly Clan? _clan;

        public EncyclopediaClanPageVMMixin(EncyclopediaClanPageVM vm) : base(vm)
        {
            _clan = vm.Obj as Clan;

            IncomeText = "";
            IncomeInfo = new MBBindingList<StringPairItemVM>();
        }

        [DataSourceProperty]
        public string IncomeText { get; set; }

        [DataSourceProperty]
        public MBBindingList<StringPairItemVM> IncomeInfo { get; set; }

        public override void OnRefresh()
        {
            IncomeText = new TextObject("Improved Economy Income").ToString();
            IncomeInfo.Clear();

            if (_clan is null || ViewModel is null)
            {
                return;
            }

            var behavior = Campaign.Current?.GetCampaignBehavior<ImprovedEconomyForAILordsBehavior>();

            if (behavior != null && behavior.ClanIncomeTotal.TryGetValue(_clan, out string clanIncome))
            {
                var clanIncomeHeader = new TextObject("Daily Clan Income:");
                IncomeInfo.AddPair(clanIncomeHeader, $"{clanIncome} denars");
            }
            else
            {
                var clanIncomeHeader = new TextObject("Daily Clan Income:");
                IncomeInfo.AddPair(clanIncomeHeader, "0 denars");
            }
        }
    }
}