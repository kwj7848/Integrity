using UnityEditor;
using UnityEngine;

namespace Integrity.Editor
{
    public class IntegritySettingsProvider : SettingsProvider
    {
        static class Styles
        {
            public static readonly GUIStyle sectionHeader = new(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 6)
            };

            public static readonly GUIStyle description = new(EditorStyles.wordWrappedLabel)
            {
                margin = new RectOffset(0, 0, 0, 10)
            };
        }

        static class Content
        {
            public static readonly GUIContent sectionDesc = new(
                "Blocks Play mode and Build when [SerializeField] private fields are left unassigned in the Inspector.\n" +
                "Add [AllowEmpty] to exclude specific fields from validation.");

            public static readonly GUIContent enableValidation = new(
                "Enable Validation", "Enable null reference validation for SerializeField fields.");

            public static readonly GUIContent blockPlay = new(
                "Block Play Mode", "Prevent entering Play mode when unassigned references are found.");

            public static readonly GUIContent blockBuild = new(
                "Block Build", "Prevent building when unassigned references are found.");
        }

        IntegritySettingsProvider()
            : base("Project/Integrity", SettingsScope.Project) { }

        public override void OnGUI(string searchContext)
        {
            var settings = IntegritySettings.Instance;

            EditorGUI.BeginChangeCheck();

            GUILayout.Space(10);

            // --- Inspector Reference Check ---
            GUILayout.Label("Inspector Reference Check", Styles.sectionHeader);
            GUILayout.Label(Content.sectionDesc, Styles.description);

            settings.validateSerializeFields = EditorGUILayout.Toggle(
                Content.enableValidation, settings.validateSerializeFields);

            using (new EditorGUI.DisabledScope(!settings.validateSerializeFields))
            {
                EditorGUI.indentLevel++;

                settings.blockPlayOnMissing = EditorGUILayout.Toggle(
                    Content.blockPlay, settings.blockPlayOnMissing);

                settings.blockBuildOnMissing = EditorGUILayout.Toggle(
                    Content.blockBuild, settings.blockBuildOnMissing);

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
                IntegritySettings.Save();
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new IntegritySettingsProvider
            {
                keywords = new[] { "integrity", "serialize", "validate", "null", "missing" }
            };
        }
    }
}
