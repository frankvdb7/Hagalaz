using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Hooks
{
    public class ItemTypeEventHook : ITypeEventHook<IItemType>
    {
        public void AfterDecode(ITypeProvider<IItemType> provider, IItemType[] types)
        {
            foreach (var type in types)
            {
                if (type.NoteTemplateId != -1)
                {
                    var note = types.Length > type.NoteId ? types[type.NoteId] : provider.Decode(type.NoteId);
                    var noteTemplate = types.Length > type.NoteTemplateId ? types[type.NoteTemplateId] : provider.Decode(type.NoteTemplateId);

                    type.MakeNote(note, noteTemplate);
                }
                if (type.LendTemplateId != -1)
                {
                    var lend = types.Length > type.LendId ? types[type.LendId] : provider.Decode(type.LendId);
                    var lendTemplate = types.Length > type.LendTemplateId ? types[type.LendTemplateId] : provider.Decode(type.LendTemplateId);

                    type.MakeLend(lend, lendTemplate);
                }
                if (type.BoughtTemplateId != -1)
                {
                    var bought = types.Length > type.BoughtItemId ? types[type.BoughtItemId] : provider.Decode(type.BoughtItemId);
                    var boughtTemplate = types.Length > type.BoughtTemplateId ? types[type.BoughtTemplateId] : provider.Decode(type.BoughtTemplateId);

                    type.MakeBought(bought, boughtTemplate);
                }
            }
        }
    }
}
