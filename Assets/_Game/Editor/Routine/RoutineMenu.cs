using UnityEditor;

namespace FriWorld.Routine
{
    /// <summary>
    /// The Routine menu: the object pipeline in the order it is run.
    ///
    /// Unity menu items cannot carry a tooltip, so the numbers are the only ordering cue the
    /// menu bar can give. What each step actually does lives in RoutinePipeline and is shown by
    /// the Object Pipeline window — open that when you need the explanation.
    ///
    /// Each item just forwards to the tool that owns the behaviour. Nothing is duplicated here.
    /// </summary>
    public static class RoutineMenu
    {
        // Priorities keep the order fixed. Unity draws a separator wherever two priorities are
        // more than ten apart, which is what splits the window, the scan steps, the optional
        // step and the generators into groups.
        const int OpenWindow = 0;
        const int Scan = 100;
        const int Optional = 130;
        const int Generate = 160;

        [MenuItem("Routine/Object Pipeline…", priority = OpenWindow)]
        static void OpenPipelineWindow() => RoutineWindow.Open();

        [MenuItem("Routine/1 — Report On Selection", priority = Scan)]
        static void Step1() => RoutinePipeline.Run(RoutinePipeline.Steps[0]);

        [MenuItem("Routine/2 — Seed Missing Types", priority = Scan + 1)]
        static void Step2() => RoutinePipeline.Run(RoutinePipeline.Steps[1]);

        [MenuItem("Routine/3 — Add Prefixes", priority = Scan + 2)]
        static void Step3() => RoutinePipeline.Run(RoutinePipeline.Steps[2]);

        [MenuItem("Routine/4 — Sync Room Platforms (only if no prefix changed)", priority = Optional)]
        static void Step4() => RoutinePipeline.Run(RoutinePipeline.Steps[3]);

        [MenuItem("Routine/5 — Generate Colliders", priority = Generate)]
        static void Step5() => RoutinePipeline.Run(RoutinePipeline.Steps[4]);

        [MenuItem("Routine/6 — Assign Layers And Static", priority = Generate + 1)]
        static void Step6() => RoutinePipeline.Run(RoutinePipeline.Steps[5]);

        [MenuItem("Routine/7 — Setup Interactables", priority = Generate + 2)]
        static void Step7() => RoutinePipeline.Run(RoutinePipeline.Steps[6]);

        [MenuItem("Routine/8 — Room Gates", priority = Generate + 3)]
        static void Step8() => RoutinePipeline.Run(RoutinePipeline.Steps[7]);
    }
}
