using System.Collections.Generic;
using System.Linq;
using FriWorld.Character;
using FriWorld.Character.Editor;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class CharacterValidationTests
    {
        static ClassRegistry Classes() => new ClassRegistry
        {
            colorClasses = new List<ColorClassDef>
            {
                new ColorClassDef { name = "torso", mainColors = 2,
                                    shadeValue = 0.62f, shadeSaturation = 1.12f },
            },
            slotClasses = new List<string> { "torso" },
        };

        static ColorwayRegistry Colorways() => new ColorwayRegistry
        {
            colorways = new List<ColorwayDef>
            {
                new ColorwayDef { colorClass = "torso", id = "navy", displayName = "Tmavomodrá",
                                  colors = new List<string> { "#243B6B", "#C8CEDA" } },
            },
        };

        static PresetRegistry Presets() => new PresetRegistry
        {
            presets = new List<PresetDef>
            {
                new PresetDef { slotClass = "torso", objectName = "shirt_1",
                                displayName = "Košeľa", gender = "any",
                                hides = new List<string> { "chest" },
                                tags = new List<string> { "formal" },
                                conflicts = new List<string>(), weight = 1 },
            },
        };

        /// A body carrying every section plus the one preset, all names well formed.
        static ScannedBody Body(Gender gender)
        {
            var body = new ScannedBody { gender = gender, prefabPath = "test.prefab" };

            string prefix = gender == Gender.Male ? "male_body_" : "female_body_";
            foreach (var entry in BodySectionNames.All)
                body.objects.Add(new ScannedObject
                {
                    name = prefix + entry.key,
                    materialNames = new[] { "char_skin_1" },
                });

            body.objects.Add(new ScannedObject
            {
                name = "shirt_1",
                materialNames = new[] { "char_torso_1", "char_torso_2", "char_torso_11" },
            });

            return body;
        }

        static List<Issue> Run(ScannedBody body) => CharacterValidation.Check(
            Classes(), Colorways(), Presets(), new[] { body });

        static bool HasError(IEnumerable<Issue> issues, string fragment) =>
            issues.Any(i => i.severity == Severity.Error && i.text.Contains(fragment));

        [Test]
        public void ACleanSetOnlyProducesNotes()
        {
            var body = Body(Gender.Male);
            // "skin" is not declared as a colour class in this fixture, so every section slot is
            // an ignored note rather than an error.
            var issues = Run(body);

            Assert.IsFalse(issues.Any(i => i.severity == Severity.Error),
                string.Join("\n", issues.Select(i => i.text)));
        }

        [Test]
        public void AMissingBodySectionIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.RemoveAll(o => o.name == "male_body_chest");

            Assert.IsTrue(HasError(Run(body), "chest"));
        }

        [Test]
        public void AMissingPresetObjectIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.RemoveAll(o => o.name == "shirt_1");

            Assert.IsTrue(HasError(Run(body), "shirt_1"));
        }

        [Test]
        public void ADuplicateObjectNameIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.Add(new ScannedObject { name = "male_body_chest", materialNames = new string[0] });

            Assert.IsTrue(HasError(Run(body), "DUPLICATE"));
        }

        [Test]
        public void AnUnparseableMaterialNameIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.Add(new ScannedObject
            {
                name = "extra_1",
                materialNames = new[] { "char_torzo" },
            });

            Assert.IsTrue(HasError(Run(body), "UNPARSED"));
        }

        [Test]
        public void AMaterialOutsideTheColourClassesIsANoteNotAnError()
        {
            var body = Body(Gender.Male);
            body.objects.First(o => o.name == "shirt_1").materialNames =
                new[] { "char_torso_1", "char_leather_1" };

            var issues = Run(body);

            Assert.IsFalse(issues.Any(i => i.severity == Severity.Error),
                string.Join("\n", issues.Select(i => i.text)));
            Assert.IsTrue(issues.Any(i => i.text.Contains("char_leather_1")));
        }

        [Test]
        public void AColourBeyondMainColorsIsAnError()
        {
            var body = Body(Gender.Male);
            body.objects.First(o => o.name == "shirt_1").materialNames =
                new[] { "char_torso_3" };

            Assert.IsTrue(HasError(Run(body), "RANGE"));
        }

        [Test]
        public void AColorwayWithTheWrongNumberOfColoursIsAnError()
        {
            var colorways = Colorways();
            colorways.colorways[0].colors = new List<string> { "#243B6B" };

            var issues = CharacterValidation.Check(
                Classes(), colorways, Presets(), new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "COUNT"));
        }

        [Test]
        public void AConflictOnATagNobodyProvidesIsAnError()
        {
            var presets = Presets();
            presets.presets[0].conflicts = new List<string> { "backpack" };

            var issues = CharacterValidation.Check(
                Classes(), Colorways(), presets, new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "DEAD"));
        }

        [Test]
        public void AHidesEntryThatIsNotASectionIsAnError()
        {
            var presets = Presets();
            presets.presets[0].hides = new List<string> { "torso" };

            var issues = CharacterValidation.Check(
                Classes(), Colorways(), presets, new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "SECTION"));
        }

        [Test]
        public void ASlotClassWithNoPresetForThisGenderIsAnError()
        {
            var presets = Presets();
            presets.presets[0].gender = "female";

            var issues = CharacterValidation.Check(
                Classes(), Colorways(), presets, new[] { Body(Gender.Male) });

            Assert.IsTrue(HasError(issues, "EMPTY"));
        }
    }
}
