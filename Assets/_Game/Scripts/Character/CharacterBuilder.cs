using System;
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character
{
    /// <summary>
    /// Turns a base prefab instance into one character.
    ///
    /// Everything not chosen is destroyed rather than deactivated. Twenty NPCs each carrying
    /// every preset as a disabled object is a lot of Transforms for a web build to walk every
    /// frame, and the meshes are shared assets, so destroying the objects costs no VRAM.
    ///
    /// Nothing here allocates a Material. The colorway materials are shared assets, so every NPC
    /// wearing navy points at the same one and the SRP Batcher keeps them in a batch.
    /// </summary>
    public static class CharacterBuilder
    {
        static readonly List<Renderer> Buffer = new List<Renderer>();

        public static void Apply(GameObject instance, in CharacterAppearance look,
                                 CharacterCatalog catalog)
        {
            if (instance == null || catalog == null) return;
            if (look.preset == null || look.colorway == null) return;

            // Stature, as a uniform scale on the root. Uniform and not one axis: a bone carries
            // its own rotation, so a non-uniform scale becomes a shear rather than a stretch —
            // legs would lengthen but the head would go egg-shaped and a T-posed arm would only
            // get thicker. What uniform scale costs instead is a head that is off by whatever the
            // scale strays from 1, which is why the model should stand near the population mean.
            var size = catalog.Size(look.gender);
            if (size != null)
                instance.transform.localScale = Vector3.one * size.ScaleFor(look.height);

            // 1. Which preset object survives in each slot class, and what it covers.
            var keep = new HashSet<string>(StringComparer.Ordinal);
            int hidden = 0;

            for (int slot = 0; slot < catalog.slotClasses.Length && slot < look.preset.Length; slot++)
            {
                byte index = look.preset[slot];
                if (index == CharacterAppearance.None) continue;
                if (index >= catalog.PresetCount(look.gender, slot)) continue;

                var preset = catalog.Preset(look.gender, slot, index);
                keep.Add(preset.objectName);
                hidden |= preset.hides;
            }

            // 2. Everything the catalog knows as a preset object but that was not chosen goes,
            //    together with the skin the chosen clothing covers.
            var bundle = catalog.Bundle(look.gender);
            var drop = new HashSet<string>(StringComparer.Ordinal);

            if (bundle != null && bundle.presets != null)
                foreach (var preset in bundle.presets)
                    if (!keep.Contains(preset.objectName))
                        drop.Add(preset.objectName);

            instance.GetComponentsInChildren(true, Buffer);

            for (int i = Buffer.Count - 1; i >= 0; i--)
            {
                var renderer = Buffer[i];
                if (renderer == null) continue;

                string name = renderer.gameObject.name;

                // A body section is recognised from its own name rather than by building
                // "male_body_" + key here, so the builder never has to know what the gender
                // prefix looks like.
                bool covered = BodySectionNames.TryParseObject(name, out var section)
                               && (hidden & (int)section) != 0;

                if (!covered && !drop.Contains(name)) continue;

                Destroy(renderer.gameObject);
                Buffer[i] = null;
            }

            // 3. Recolour what is left.
            foreach (var renderer in Buffer)
            {
                if (renderer == null) continue;

                var map = catalog.SlotMap(look.gender, renderer.gameObject.name);
                if (map == null) continue;

                var materials = renderer.sharedMaterials;
                bool changed = false;

                int slots = Mathf.Min(materials.Length, map.colorSlot.Length);
                for (int i = 0; i < slots; i++)
                {
                    int colorSlot = map.colorSlot[i];
                    if (colorSlot < 0) continue;                        // left as authored
                    if (colorSlot >= look.colorway.Length) continue;

                    byte colorwayIndex = look.colorway[colorSlot];
                    if (colorwayIndex == CharacterAppearance.None) continue;
                    if (colorwayIndex >= catalog.ColorwayCount(colorSlot)) continue;

                    var material = catalog.Colorway(colorSlot, colorwayIndex).For(map.shadeLevel[i]);
                    if (material == null) continue;

                    materials[i] = material;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = materials;
            }

            Buffer.Clear();
        }

        /// <summary>Object.Destroy does not take effect until the end of the frame, which an
        /// EditMode test never reaches. In a build the branch resolves to Destroy.</summary>
        static void Destroy(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
