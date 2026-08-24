using UnityEditor;
using UnityEngine;

namespace FriWorld.Routine
{
    /// <summary>
    /// Routine > Object Pipeline.
    ///
    /// The steps in the order they are run, each with the explanation the menu bar cannot show.
    /// This exists because the pipeline is seven tools spread across five submenus and the order
    /// matters: layers before interactables, interactables before gates. Getting it wrong is
    /// quiet, not loud.
    /// </summary>
    public class RoutineWindow : EditorWindow
    {
        Vector2 scroll;

        public static void Open()
        {
            var window = GetWindow<RoutineWindow>(false, "Object Pipeline", true);
            window.minSize = new Vector2(520, 520);
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Run these top to bottom. Select the objects you imported in the Hierarchy first "
                + "— most steps work on the selection. Adding another instance of a type that "
                + "already exists needs only steps 5 to 8.",
                MessageType.Info);

            bool hasSelection = Selection.gameObjects != null && Selection.gameObjects.Length > 0;
            if (!hasSelection)
                EditorGUILayout.HelpBox("Nothing is selected in the Hierarchy. The steps marked "
                                      + "\"needs a selection\" will do nothing.", MessageType.Warning);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < RoutinePipeline.Steps.Length; i++)
                DrawStep(i + 1, RoutinePipeline.Steps[i], hasSelection);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(RoutinePipeline.AfterwardsNote, MessageType.Warning);

            EditorGUILayout.EndScrollView();
        }

        void DrawStep(int number, RoutinePipeline.Step step, bool hasSelection)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string heading = number + ". " + step.title + (step.optional ? "  (optional)" : "");
                    EditorGUILayout.LabelField(heading, EditorStyles.boldLabel);

                    using (new EditorGUI.DisabledScope(step.needsSelection && !hasSelection))
                        if (GUILayout.Button("Run", GUILayout.Width(70)))
                            RoutinePipeline.Run(step);
                }

                EditorGUILayout.LabelField(step.description, WrappedLabel);

                string footer = step.menuPath
                              + (step.needsSelection ? "     •  needs a selection" : "");
                EditorGUILayout.LabelField(footer, EditorStyles.miniLabel);
            }
        }

        static GUIStyle wrapped;

        static GUIStyle WrappedLabel
        {
            get
            {
                if (wrapped == null)
                    wrapped = new GUIStyle(EditorStyles.label) { wordWrap = true };
                return wrapped;
            }
        }
    }
}
