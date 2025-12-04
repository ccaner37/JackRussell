using UnityEngine;
using UnityEngine.Profiling;

namespace JackRussell.DebugTools
{
    /// <summary>
    /// Simple on-screen debug overlay using OnGUI.
    /// Attach to any GameObject and assign the Player reference (or leave blank to find one at runtime).
    /// Draws a compact multi-line status panel in the top-left and system debug info in top-right.
    /// </summary>
    public class DebugUGUI : MonoBehaviour
    {
        [SerializeField] private JackRussell.Player _player;
        [SerializeField] private bool _enabledOverlay = true;
        [SerializeField] private int _fontSize = 14;
        [SerializeField] private Vector2 _padding = new Vector2(8, 8);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _bgColor = new Color(0f, 0f, 0f, 0.5f);

        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;

        // FPS calculation
        private float _fpsAccumulator = 0f;
        private int _fpsFrameCount = 0;
        private float _fps = 0f;


        // Timescale slider
        private float _timeScale = 1f;

        private void Awake()
        {
            if (_player == null)
            {
                _player = FindObjectOfType<JackRussell.Player>();
            }

            _labelStyle = new GUIStyle(EditorGUIUtilitySafe.skinLabel)
            {
                fontSize = _fontSize,
                normal = { textColor = _textColor }
            };

            _boxStyle = new GUIStyle(EditorGUIUtilitySafe.boxStyle);

            _timeScale = Time.timeScale;
        }

        private void Update()
        {
            // FPS calculation
            _fpsAccumulator += Time.deltaTime;
            _fpsFrameCount++;
            if (_fpsAccumulator >= 0.5f)
            {
                _fps = _fpsFrameCount / _fpsAccumulator;
                _fpsAccumulator = 0f;
                _fpsFrameCount = 0;
            }
        }

        private void OnGUI()
        {
            if (!_enabledOverlay) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorGUIUtilitySafe.skinLabel)
                {
                    fontSize = _fontSize,
                    normal = { textColor = _textColor }
                };
            }

            DrawCombinedPanel();
        }

        private void DrawCombinedPanel()
        {
            System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
            if (_player != null)
            {
                lines.Add($"Locomotion: {_player.LocomotionStateName} ({_player.LocomotionStateTime:F2}s)");
                lines.Add($"Action    : {_player.ActionStateName} ({_player.ActionStateTime:F2}s)");
                lines.Add($"Grounded  : {_player.IsGrounded}");
                lines.Add($"Velocity  : {_player.KinematicController.Velocity.x:F2}, {_player.KinematicController.Velocity.y:F2}, {_player.KinematicController.Velocity.z:F2} (horiz {new Vector3(_player.KinematicController.Velocity.x,0,_player.KinematicController.Velocity.z).magnitude:F2})");
                lines.Add($"MoveDir   : {_player.MoveDirection.x:F2}, {_player.MoveDirection.y:F2}, {_player.MoveDirection.z:F2}");
                lines.Add($"Input     : {_player.MoveInput.x:F2}, {_player.MoveInput.y:F2}  Sprint: {_player.SprintRequested}");
                // hidden: AnimatorS, MovOverride, RotOverride, Pressure
            }

            // system lines
            long memoryMB = Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024;
            lines.Add($"FPS: {_fps:F1}");
            lines.Add($"Frame Time: {Time.deltaTime * 1000:F1}ms");
            lines.Add($"Memory: {memoryMB}MB");
            lines.Add($"Time Scale: {_timeScale:F2}");

            // compute size
            float width = 0f;
            float height = 0f;
            foreach (var line in lines)
            {
                Vector2 size = _labelStyle.CalcSize(new GUIContent(line));
                if (size.x > width) width = size.x;
                height += size.y;
            }
            width += _padding.x * 2f;
            height += _padding.y * 2f;

            // Add space for slider
            height += 30f;

            Rect boxRect = new Rect(Screen.width - width - 10, Screen.height - height - 10, width, height);
            // draw background
            Color prevColor = GUI.color;
            GUI.color = _bgColor;
            GUI.Box(boxRect, GUIContent.none, _boxStyle);
            GUI.color = prevColor;

            // draw lines
            float y = boxRect.y + _padding.y / 2f;
            float x = boxRect.x + _padding.x / 2f;
            foreach (var line in lines)
            {
                GUI.Label(new Rect(x, y, boxRect.width - _padding.x, _fontSize + 6), line, _labelStyle);
                y += _labelStyle.lineHeight;
            }

            // Draw timescale slider
            //Rect sliderRect = new Rect(x, y, boxRect.width - _padding.x, 20);
            //_timeScale = GUI.HorizontalSlider(sliderRect, _timeScale, 0.1f, 2.0f);
            //Time.timeScale = _timeScale;
        }
    }

    // Helper to safely access default GUI skins even in WebGL / Player builds where EditorGUIUtility isn't available.
    static class EditorGUIUtilitySafe
    {
        public static GUIStyle skinLabel
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorStyles.label;
#else
                return GUI.skin.label ?? new GUIStyle();
#endif
            }
        }

        public static GUIStyle boxStyle
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorStyles.helpBox;
#else
                return GUI.skin.box ?? new GUIStyle();
#endif
            }
        }
    }
}
