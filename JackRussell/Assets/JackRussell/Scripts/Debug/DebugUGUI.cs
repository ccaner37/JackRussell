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

            // Draw left panel (player info)
            if (_player != null)
            {
                DrawLeftPanel();
            }

            // Draw right panel (system info)
            DrawRightPanel();
        }

        private void DrawLeftPanel()
        {
            // Build lines
            string movOverrideStr = _player.HasMovementOverride() ? "YES" : "NO";
            Vector3 ov = _player.GetOverrideVelocity();
            string rotOverrideStr = _player.HasRotationOverride() ? "YES" : "NO";

            string[] lines = new string[]
            {
                $"Locomotion: {_player.LocomotionStateName} ({_player.LocomotionStateTime:F2}s)",
                $"Action    : {_player.ActionStateName} ({_player.ActionStateTime:F2}s)",
                $"Grounded  : {_player.IsGrounded}",
                $"Velocity  : {_player.Rigidbody.linearVelocity.x:F2}, {_player.Rigidbody.linearVelocity.y:F2}, {_player.Rigidbody.linearVelocity.z:F2} (horiz {new Vector3(_player.Rigidbody.linearVelocity.x,0,_player.Rigidbody.linearVelocity.z).magnitude:F2})",
                $"MoveDir   : {_player.MoveDirection.x:F2}, {_player.MoveDirection.y:F2}, {_player.MoveDirection.z:F2}",
                $"Input     : {_player.MoveInput.x:F2}, {_player.MoveInput.y:F2}  Sprint: {_player.SprintRequested}",
                $"AnimatorS : {_player.AnimatorSpeed:F2}",
                $"MovOverride: {movOverrideStr}  Vel: {ov.x:F2},{ov.y:F2},{ov.z:F2}  Exclusive: {_player.IsOverrideExclusive()}  TimeLeft: {_player.MovementOverrideTimeRemaining:F2}s",
                $"RotOverride: {rotOverrideStr}  TimeLeft: {_player.RotationOverrideTimeRemaining:F2}s",
                $"Pressure : {_player.Pressure:F2}",
            };

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

            Rect boxRect = new Rect(10, 10, width, height);
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
        }

        private void DrawRightPanel()
        {
            // Build lines for system info
            long memoryMB = Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024;

            string[] lines = new string[]
            {
                $"FPS: {_fps:F1}",
                $"Frame Time: {Time.deltaTime * 1000:F1}ms",
                $"Memory: {memoryMB}MB",
                $"Time Scale: {_timeScale:F2}",
            };

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
            height += 30f; // slider height

            Rect boxRect = new Rect(Screen.width - width - 10, 10, width, height);
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
            Rect sliderRect = new Rect(x, y, boxRect.width - _padding.x, 20);
            _timeScale = GUI.HorizontalSlider(sliderRect, _timeScale, 0.1f, 2.0f);
            Time.timeScale = _timeScale;
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
