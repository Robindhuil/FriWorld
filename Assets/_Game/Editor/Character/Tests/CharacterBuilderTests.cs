using FriWorld.Character;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class CharacterBuilderTests
    {
        CharacterCatalog catalog;
        GameObject instance;
        Material navy;
        Material navyShade;
        Material leather;

        [SetUp]
        public void SetUp()
        {
            navy = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "navy_1" };
            navyShade = new Material(navy) { name = "navy_11" };
            leather = new Material(navy) { name = "char_leather_1" };

            catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.slotClasses = new[] { "torso" };
            catalog.colorClasses = new[] { "torso" };
            catalog.tags = new string[0];

            catalog.colorSlotClass = new[] { 0 };
            catalog.colorSlotKey = new[] { 1 };

            catalog.colorways = new[]
            {
                new ColorwayEntry { colorSlot = 0, id = "navy", material = navy, shade = navyShade },
            };
            catalog.colorwayStart = new[] { 0, 1 };

            catalog.male = new GenderBundle
            {
                presets = new[]
                {
                    new PresetEntry
                    {
                        slotClass = 0, objectName = "shirt_1", gender = GenderGate.Any,
                        hides = (int)(BodySection.Chest | BodySection.Abdomen), weight = 1,
                    },
                    new PresetEntry
                    {
                        slotClass = 0, objectName = "t-shirt_1", gender = GenderGate.Any,
                        hides = 0, weight = 1,
                    },
                },
                presetStart = new[] { 0, 2 },
                slotMaps = new[]
                {
                    new RendererSlotMap
                    {
                        objectName = "shirt_1",
                        colorSlot = new[] { 0, 0, -1 },
                        shadeLevel = new[] { 0, 1, 0 },
                    },
                },
            };
            catalog.female = new GenderBundle();

            instance = new GameObject("character_male");
            // Section objects carry the gender prefix, exactly as they come out of Blender.
            AddRenderer("male_body_chest", 1);
            AddRenderer("male_body_abdomen", 1);
            AddRenderer("male_body_hand_L", 1);
            AddRenderer("shirt_1", 3);
            AddRenderer("t-shirt_1", 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(navy);
            Object.DestroyImmediate(navyShade);
            Object.DestroyImmediate(leather);
        }

        void AddRenderer(string name, int slots)
        {
            var child = new GameObject(name);
            child.transform.SetParent(instance.transform);

            var renderer = child.AddComponent<MeshRenderer>();
            var materials = new Material[slots];
            for (int i = 0; i < slots; i++) materials[i] = leather;
            renderer.sharedMaterials = materials;
        }

        Transform Find(string name) => instance.transform.Find(name);

        static CharacterAppearance Look(byte preset, byte colorway) => new CharacterAppearance
        {
            gender = Gender.Male,
            preset = new[] { preset },
            colorway = new[] { colorway },
        };

        [Test]
        public void TheUnchosenPresetIsGone()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            Assert.IsNotNull(Find("shirt_1"));
            Assert.IsNull(Find("t-shirt_1"));
        }

        [Test]
        public void TheHiddenSectionsAreGone()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            Assert.IsNull(Find("male_body_chest"));
            Assert.IsNull(Find("male_body_abdomen"));
            Assert.IsNotNull(Find("male_body_hand_L"));
        }

        [Test]
        public void APresetThatHidesNothingLeavesTheSkinAlone()
        {
            CharacterBuilder.Apply(instance, Look(1, 0), catalog);

            Assert.IsNotNull(Find("male_body_chest"));
            Assert.IsNotNull(Find("male_body_abdomen"));
        }

        [Test]
        public void TheColourwayMaterialsLandOnTheRightSlots()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            var materials = Find("shirt_1").GetComponent<MeshRenderer>().sharedMaterials;

            Assert.AreSame(navy, materials[0]);
            Assert.AreSame(navyShade, materials[1]);
        }

        [Test]
        public void ASlotOutsideTheColourClassesIsLeftAsAuthored()
        {
            CharacterBuilder.Apply(instance, Look(0, 0), catalog);

            var materials = Find("shirt_1").GetComponent<MeshRenderer>().sharedMaterials;

            Assert.AreSame(leather, materials[2]);
        }

        [Test]
        public void AnEmptySlotClassStripsEveryPresetOfThatClass()
        {
            CharacterBuilder.Apply(instance, Look(CharacterAppearance.None, 0), catalog);

            Assert.IsNull(Find("shirt_1"));
            Assert.IsNull(Find("t-shirt_1"));
        }
    }
}
