using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace ImprovedEconomyForAILords
{
    [PrefabExtension("EncyclopediaClanPage", "descendant::ListPanel[@Id='Leader']")]
    public class ClanIncomePrefabExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        [PrefabExtensionFileName(true)]
        public string File => "ClanPageIncomePatch";
    }
}