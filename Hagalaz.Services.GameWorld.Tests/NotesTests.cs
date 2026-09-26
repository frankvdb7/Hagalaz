using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class NotesTests
{
    [TestMethod]
    public void Add_ToEmptyNotes_AllocatesZero()
    {
        var notes = CreateNotes();

        notes.Add("first");

        AssertSnapshot(notes, (0, "first"));
    }

    [TestMethod]
    public void Add_ToPopulatedNotes_AllocatesSequentialIds()
    {
        var notes = CreateNotes();

        notes.Add("first");
        notes.Add("second");
        notes.Add("third");

        AssertSnapshot(notes, (0, "first"), (1, "second"), (2, "third"));
    }

    [TestMethod]
    [DataRow(0, "first", "second", "third")]
    [DataRow(1, "first", "second", "third")]
    [DataRow(2, "first", "second", "third")]
    public void Add_AfterDeletingNote_AllocatesFirstUnusedId(
        int deletedId,
        string firstText,
        string secondText,
        string thirdText)
    {
        var notes = CreateNotes();
        notes.Add(firstText);
        notes.Add(secondText);
        notes.Add(thirdText);

        notes.Delete(deletedId);
        notes.Add("replacement");

        AssertSnapshot(notes,
            (0, deletedId == 0 ? "replacement" : firstText),
            (1, deletedId == 1 ? "replacement" : secondText),
            (2, deletedId == 2 ? "replacement" : thirdText));
    }

    [TestMethod]
    public void Add_ToSparseHydratedNotes_AllocatesFirstUnusedId()
    {
        var notes = CreateNotes(new HydratedNotesDto
        {
            Notes =
            [
                new HydratedNotesDto.NoteDto { Id = 0, Text = "zero" },
                new HydratedNotesDto.NoteDto { Id = 2, Text = "two" }
            ]
        });

        notes.Add("one");

        AssertSnapshot(notes, (0, "zero"), (2, "two"), (1, "one"));
    }

    [TestMethod]
    public void Add_AfterRepeatedDeleteAndReadd_PreservesEveryNoteOnce()
    {
        var notes = CreateNotes();
        notes.Add("one");
        notes.Add("two");
        notes.Add("three");

        for (var cycle = 0; cycle < 4; cycle++)
        {
            notes.Delete(0);
            notes.Add($"replacement-{cycle}");

            AssertSnapshot(notes,
                (0, $"replacement-{cycle}"),
                (1, "two"),
                (2, "three"));
        }
    }

    private static Notes CreateNotes(HydratedNotesDto? hydration = null)
    {
        var notes = new Notes(Substitute.For<ICharacter>());
        if (hydration is not null)
        {
            ((IHydratable<HydratedNotesDto>)notes).Hydrate(hydration);
        }

        return notes;
    }

    private static void AssertSnapshot(Notes notes, params (int Id, string Text)[] expected)
    {
        var snapshot = ((IDehydratable<HydratedNotesDto>)notes).Dehydrate().Notes;
        var ids = snapshot.Select(note => note.Id).ToArray();
        var texts = snapshot.Select(note => note.Text).ToArray();

        Assert.AreEqual(expected.Length, snapshot.Count);
        Assert.AreEqual(ids.Length, ids.Distinct().Count(), "Snapshot contains duplicate note IDs.");
        CollectionAssert.AreEquivalent(expected.Select(note => note.Id).ToArray(), ids);
        CollectionAssert.AreEquivalent(expected.Select(note => note.Text).ToArray(), texts);

        foreach (var expectedNote in expected)
        {
            var actual = snapshot.Single(note => note.Id == expectedNote.Id);
            Assert.AreEqual(expectedNote.Text, actual.Text);
        }
    }
}
