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
    [ViewModelMixin(nameof(EncyclopediaHeroPageVM.RefreshValues), true)]
    public class EncyclopediaHeroPageVMMixin : BaseViewModelMixin<EncyclopediaHeroPageVM>
    {
        private readonly Hero? _hero;

        public EncyclopediaHeroPageVMMixin(EncyclopediaHeroPageVM vm) : base(vm)
        {
            _hero = vm.Obj as Hero;

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

            if (_hero is null || ViewModel is null)
            {
                return;
            }

            var behavior = Campaign.Current?.GetCampaignBehavior<ImprovedEconomyForAILordsBehavior>();

            if (behavior != null && behavior.HeroIncomeTotal.TryGetValue(_hero, out string heroIncome))
            {
                var heroIncomeHeader = new TextObject("Daily Hero Income:");
                IncomeInfo.AddPair(heroIncomeHeader, $"{heroIncome} denars");
            }
            else
            {
                var heroIncomeHeader = new TextObject("Daily Hero Income:");
                IncomeInfo.AddPair(heroIncomeHeader, "0 denars");
            }
        }
    }
}