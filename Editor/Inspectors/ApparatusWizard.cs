using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    /// <summary>
    /// Step-by-step setup window for an <see cref="ApparatusConfig"/>. Walks the
    /// user through geometry, doors/windows and object options with a live
    /// prefab preview on the right; the final page is the full config editor.
    /// </summary>
    public sealed class ApparatusWizard : EditorWindow
    {
        enum Step { Welcome, Geometry, DoorsWindows, Objects, Review }

        static readonly string[] StepTitles =
        {
            "Welcome", "Geometry", "Doors & Windows", "Objects", "Review & Refine",
        };

        ApparatusConfig _config;
        SerializedObject _so;
        Step _step = Step.Welcome;

        UnityEditor.Editor _configEditor;
        Vector2 _contentScroll;

        // Live preview.
        PreviewRenderUtility _preview;
        GameObject _previewInstance;
        GameObject _previewedPrefab;
        Vector2 _orbit = new Vector2(120f, -20f);
        float _distance;

        const float HeaderHeight = 54f;
        const float NavBarHeight = 46f;

        public static void Open(ApparatusConfig config)
        {
            if (config == null) return;
            var window = CreateInstance<ApparatusWizard>();
            window.titleContent = new GUIContent("Apparatus Setup");
            window._config = config;
            window._so = new SerializedObject(config);
            window.minSize = new Vector2(860f, 560f);
            window.Show();
        }

        /// <summary>
        /// Creates a new <see cref="ApparatusConfig"/> next to the given source
        /// apparatus asset and opens the wizard on it.
        /// </summary>
        public static void CreateFor(string sourceAssetPath, ApparatusAsset source)
        {
            if (source == null) return;

            var config = CreateInstance<ApparatusConfig>();
            config.Source = source;

            string dir = string.IsNullOrEmpty(sourceAssetPath)
                ? "Assets"
                : System.IO.Path.GetDirectoryName(sourceAssetPath);
            string baseName = string.IsNullOrEmpty(sourceAssetPath)
                ? "Apparatus"
                : System.IO.Path.GetFileNameWithoutExtension(sourceAssetPath);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{baseName}_Config.asset");

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Open(config);
        }

        void OnDisable()
        {
            _preview?.Cleanup();
            _preview = null;
            if (_configEditor != null) DestroyImmediate(_configEditor);
        }

        void OnGUI()
        {
            if (_config == null) { Close(); return; }
            _so.Update();

            DrawHeader();

            var body = new Rect(0f, HeaderHeight, position.width,
                                position.height - HeaderHeight - NavBarHeight);
            float previewWidth = Mathf.Clamp(body.width * 0.45f, 280f, 460f);
            var leftRect = new Rect(body.x + 12f, body.y + 8f,
                                    body.width - previewWidth - 32f, body.height - 16f);
            var rightRect = new Rect(leftRect.xMax + 12f, body.y + 8f,
                                     previewWidth, body.height - 16f);

            EditorGUI.BeginChangeCheck();
            GUILayout.BeginArea(leftRect);
            _contentScroll = EditorGUILayout.BeginScrollView(_contentScroll);
            DrawStep();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            bool changed = EditorGUI.EndChangeCheck();

            DrawPreview(rightRect);
            DrawNavBar();

            bool applied = _so.ApplyModifiedProperties();
            if ((applied || changed) && _config.Source != null)
                ScheduleRegenerate();
        }

        // ---- Header ----

        void DrawHeader()
        {
            var rect = new Rect(0f, 0f, position.width, HeaderHeight);
            EditorGUI.DrawRect(rect, new Color(0.20f, 0.22f, 0.28f, 1f));

            var title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15, normal = { textColor = Color.white },
                padding = new RectOffset(14, 14, 8, 0),
            };
            var sub = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.85f, 0.85f, 0.9f) },
                padding = new RectOffset(14, 14, 0, 6),
            };
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 26f), StepTitles[(int)_step], title);
            GUI.Label(new Rect(rect.x, rect.y + 26f, rect.width, 20f),
                $"Step {(int)_step + 1} of {StepTitles.Length}", sub);
        }

        // ---- Step content ----

        void DrawStep()
        {
            switch (_step)
            {
                case Step.Welcome:      DrawWelcome(); break;
                case Step.Geometry:     DrawGeometry(); break;
                case Step.DoorsWindows: DrawDoorsWindows(); break;
                case Step.Objects:      DrawObjects(); break;
                case Step.Review:       DrawReview(); break;
            }
        }

        void DrawWelcome()
        {
            EditorGUILayout.HelpBox(
                "This wizard configures an apparatus from an imported .oae / .oapp " +
                "source. Step through geometry, doors, windows and objects; the " +
                "preview updates as you go. The last page is the full editor.",
                MessageType.Info);
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_so.FindProperty("Source"));
            if (_config.Source == null)
                EditorGUILayout.HelpBox("Assign a source apparatus to continue.",
                    MessageType.Warning);
        }

        void DrawGeometry()
        {
            EditorGUILayout.LabelField("What to generate", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_so.FindProperty("GenerateFloors"));
            EditorGUILayout.PropertyField(_so.FindProperty("GenerateCeilings"));
            EditorGUILayout.PropertyField(_so.FindProperty("GenerateWalls"));
            EditorGUILayout.PropertyField(_so.FindProperty("GenerateExteriorWalls"));
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Origin", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_so.FindProperty("OriginRoomId"));
            EditorGUILayout.HelpBox("Origin Room Id centres the apparatus on that " +
                "room; -1 keeps the source origin.", MessageType.None);
        }

        void DrawDoorsWindows()
        {
            EditorGUILayout.HelpBox(
                "Default door and window prefabs fill every opening, auto-scaled " +
                "to fit. Per-room overrides live on the Review page.",
                MessageType.Info);
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_so.FindProperty("DefaultDoorPrefab"));
            EditorGUILayout.PropertyField(_so.FindProperty("DefaultWindowPrefab"));
        }

        void DrawObjects()
        {
            EditorGUILayout.HelpBox(
                "Snap object positions to a grid. Per-object-type prefabs and Y " +
                "overrides live on the Review page.",
                MessageType.Info);
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_so.FindProperty("ObjectSnapGridSize"));
        }

        void DrawReview()
        {
            EditorGUILayout.HelpBox(
                "The full configuration. Edit anything here; the preview updates.",
                MessageType.None);
            EditorGUILayout.Space(4);
            if (_configEditor == null || _configEditor.target != _config)
            {
                if (_configEditor != null) DestroyImmediate(_configEditor);
                _configEditor = UnityEditor.Editor.CreateEditor(_config);
            }
            _configEditor.OnInspectorGUI();
        }

        // ---- Preview ----

        void DrawPreview(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f, 1f));

            var prefab = _config.GeneratedPrefab;
            if (prefab == null)
            {
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    { wordWrap = true };
                GUI.Label(rect, "Preview appears once a prefab is generated.", style);
                return;
            }

            EnsurePreview(prefab);
            HandleOrbit(rect);

            _preview.BeginPreview(rect, GUIStyle.none);
            var camera = _preview.camera;
            var rot = Quaternion.Euler(-_orbit.y, _orbit.x, 0f);
            camera.transform.position = rot * new Vector3(0f, 0f, -_distance);
            camera.transform.rotation = rot;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = _distance * 4f + 50f;
            _preview.camera.Render();
            var texture = _preview.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        void EnsurePreview(GameObject prefab)
        {
            if (_preview == null) _preview = new PreviewRenderUtility();

            if (_previewedPrefab != prefab || _previewInstance == null)
            {
                if (_previewInstance != null) DestroyImmediate(_previewInstance);
                _previewInstance = Instantiate(prefab);
                _previewInstance.hideFlags = HideFlags.HideAndDontSave;
                _preview.AddSingleGO(_previewInstance);
                _previewedPrefab = prefab;

                var bounds = ComputeBounds(_previewInstance);
                _previewInstance.transform.position = -bounds.center;
                _distance = Mathf.Max(2f, bounds.extents.magnitude * 2.2f);
            }
        }

        void HandleOrbit(Rect rect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
            {
                _orbit.x += e.delta.x;
                _orbit.y = Mathf.Clamp(_orbit.y + e.delta.y, -89f, 89f);
                e.Use();
                Repaint();
            }
        }

        static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        // ---- Nav bar ----

        void DrawNavBar()
        {
            var rect = new Rect(0f, position.height - NavBarHeight, position.width, NavBarHeight);
            EditorGUI.DrawRect(rect, new Color(0.13f, 0.14f, 0.16f, 1f));

            const float pad = 12f, h = 26f;
            float y = rect.y + (NavBarHeight - h) * 0.5f;

            if (GUI.Button(new Rect(rect.x + pad, y, 80f, h), "Cancel")) Close();

            float x = rect.xMax - pad;
            bool isLast = _step == Step.Review;
            var nextLabel = isLast ? "Finish" : "Next";
            var nextRect = new Rect(x - 110f, y, 110f, h);
            x = nextRect.x - 8f;
            var backRect = new Rect(x - 80f, y, 80f, h);

            using (new EditorGUI.DisabledScope(_step == Step.Welcome))
                if (GUI.Button(backRect, "Back"))
                    _step = (Step)Mathf.Max(0, (int)_step - 1);

            using (new EditorGUI.DisabledScope(_config.Source == null))
            {
                if (GUI.Button(nextRect, nextLabel))
                {
                    if (isLast) Close();
                    else _step = (Step)((int)_step + 1);
                }
            }
        }

        // ---- Regeneration ----

        double _regenAt;
        bool _regenScheduled;

        void ScheduleRegenerate()
        {
            _regenAt = EditorApplication.timeSinceStartup + 0.4;
            if (_regenScheduled) return;
            _regenScheduled = true;
            EditorApplication.update += RegenTick;
        }

        void RegenTick()
        {
            if (EditorApplication.timeSinceStartup < _regenAt) return;
            EditorApplication.update -= RegenTick;
            _regenScheduled = false;
            if (_config != null && _config.Source != null)
            {
                ApparatusPrefabBuilder.Regenerate(_config);
                // Force the preview to rebuild from the regenerated prefab even
                // when SaveAsPrefabAsset returns the same asset reference.
                _previewedPrefab = null;
            }
            Repaint();
        }
    }
}
