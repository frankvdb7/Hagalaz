using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Hooks
{
    public class ItemTypeEventHook : ITypeEventHook<IItemType>
    {
        public void AfterDecode(ITypeDecoder<IItemType> decoder, IItemType[] types)
        {
            foreach (var type in types)
            {
                if (type.NoteTemplateId != -1)
                {
                    var note = types.Length > type.NoteId ? types[type.NoteId] : decoder.Decode(type.NoteId);
                    var noteTemplate = types.Length > type.NoteTemplateId ? types[type.NoteTemplateId] : decoder.Decode(type.NoteTemplateId);

                    type.MakeNote(note, noteTemplate);
                }
                if (type.LendTemplateId != -1)
                {
                    var lend = types.Length > type.LendId ? types[type.LendId] : decoder.Decode(type.LendId);
                    var lendTemplate = types.Length > type.LendTemplateId ? types[type.LendTemplateId] : decoder.Decode(type.LendTemplateId);

                    type.MakeLend(lend, lendTemplate);
                }
                if (type.BoughtTemplateId != -1)
                {
                    var bought = types.Length > type.BoughtItemId ? types[type.BoughtItemId] : decoder.Decode(type.BoughtItemId);
                    var boughtTemplate = types.Length > type.BoughtTemplateId ? types[type.BoughtTemplateId] : decoder.Decode(type.BoughtTemplateId);

                    type.MakeBought(bought, boughtTemplate);
                }
            }
        }
    }
}
