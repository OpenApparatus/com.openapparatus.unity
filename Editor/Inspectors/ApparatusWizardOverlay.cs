using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    /// <summary>
    /// Scene-view overlay for setting up an apparatus. "Create Apparatus" opens
    /// the generated prefab in Prefab Mode — so the real Scene view renders it —
    /// and points this overlay at the config. The overlay panel hosts the
    /// config editor (Apparatus / Rooms / Object Types tabs); changes regenerate
    /// the prefab and refresh the open stage.
    ///
    /// Shows up under the Scene view's overlay menu as "Apparatus Setup".
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Apparatus Setup")]
    public sealed class ApparatusWizardOverlay : Overlay
    {
        const string OverlayId = "openapparatus-apparatus-setup";

        static ApparatusConfig s_target;
        UnityEditor.Editor _configEditor;

        public override VisualElement CreatePanelContent()
        {
            var container = new IMGUIContainer(DrawGUI);
            container.style.minWidth = 360f;
            container.style.minHeight = 420f;
            return container;
        }

        public override void OnWillBeDestroyed()
        {
            if (_configEditor != null) Object.DestroyImmediate(_configEditor);
            base.OnWillBeDestroyed();
        }

        void DrawGUI()
        {
            if (s_target == null)
            {
                EditorGUILayout.HelpBox(
                    "No apparatus selected. Use Create Apparatus on an imported " +
                    ".oae / .oapp asset to begin.",
                    MessageType.Info);
                return;
            }

            if (_configEditor == null || _configEditor.target != s_target)
            {
                if (_configEditor != null) Object.DestroyImmediate(_configEditor);
                _configEditor = UnityEditor.Editor.CreateEditor(s_target);
            }
            _configEditor.OnInspectorGUI();
        }

        /// <summary>
        /// Creates an <see cref="ApparatusConfig"/> for the source apparatus,
        /// generates its prefab, opens that prefab in Prefab Mode, and targets
        /// this overlay at the config.
        /// </summary>
        public static void CreateFor(string sourceAssetPath, ApparatusAsset source)
        {
            if (source == null) return;

            var config = ScriptableObject.CreateInstance<ApparatusConfig>();
            config.Source = source;

            string directory = string.IsNullOrEmpty(sourceAssetPath)
                ? "Assets"
                : System.IO.Path.GetDirectoryName(sourceAssetPath);
            string baseName = string.IsNullOrEmpty(sourceAssetPath)
                ? "Apparatus"
                : System.IO.Path.GetFileNameWithoutExtension(sourceAssetPath);
            string configPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{directory}/{baseName}_Config.asset");
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();

            var prefab = ApparatusPrefabBuilder.Regenerate(config);
            s_target = config;
            Selection.activeObject = config;

            if (prefab != null)
                PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(prefab));

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.Focus();
                if (sceneView.TryGetOverlay(OverlayId, out var overlay))
                    overlay.displayed = true;
            }
        }
    }
}
