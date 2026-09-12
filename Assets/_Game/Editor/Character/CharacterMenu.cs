using UnityEditor;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// The Character menu, in the order it is run.
    ///
    /// Same shape as Routine: numbered, because a Unity menu item cannot carry a tooltip and the
    /// order is not free — the shade materials have to exist before the catalog can reference
    /// them.
    /// </summary>
    public static class CharacterMenu
    {
        const int Scan = 100;
        const int Generate = 130;

        [MenuItem("Character/1 — Report", priority = Scan)]
        static void Step1() => CharacterReport.Run();

        [MenuItem("Character/2 — Generate Shades", priority = Generate)]
        static void Step2() => ShadeMaterialGenerator.Run();

        [MenuItem("Character/3 — Bake Catalog", priority = Generate + 1)]
        static void Step3() => CharacterCatalogBaker.Run();

        /// <summary>Off the numbered path: only needed after a colour was picked on a material
        /// rather than written into the register.</summary>
        [MenuItem("Character/Sync Shades From Materials", priority = Generate + 20)]
        static void SyncShades() => ColorwaySync.Run();
    }
}
