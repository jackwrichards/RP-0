using System;
using System.Collections.Generic;
using UnityEngine;

namespace RP0
{
    public class Tooltip
    {
        private const float TooltipMaxWidth = 400f;
        private const double TooltipShowDelay = 0;

        private static readonly int _tooltipWindowId = "RP0Tooltip".GetHashCode();
        private static GUIStyle _tooltipStyle;
        private static Tooltip _instance;

        private readonly Dictionary<int, string> _windowTooltipTexts = new Dictionary<int, string>();
        private Rect _tooltipRect;
        private DateTime _tooltipBeginDt;
        private bool _isTooltipChanged;

        public static Tooltip Instance
        {
            get
            {
                if (_instance == null) RecreateInstance();
                return _instance;
            }
            private set => _instance = value;
        }

        private Tooltip() { }

        public static void RecreateInstance()
        {
            if (_tooltipStyle == null)
            {
                _tooltipStyle = new GUIStyle(HighLogic.Skin.label);
                _tooltipStyle.normal.textColor = new Color32(240, 240, 240, 255);
                _tooltipStyle.padding = new RectOffset(10, 10, 8, 8);
                _tooltipStyle.alignment = TextAnchor.UpperLeft;
                _tooltipStyle.wordWrap = true;
                _tooltipStyle.fontSize = 13;
                _tooltipStyle.border = new RectOffset(1, 1, 1, 1);
            }

            // The texture needs to be re-applied after every scene change
            // Darker background with subtle outline
            Texture2D backTex = new Texture2D(3, 3, TextureFormat.ARGB32, false);
            // Fill center with dark background
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (x == 0 || x == 2 || y == 0 || y == 2)
                    {
                        // Subtle outline (slightly lighter than background)
                        backTex.SetPixel(x, y, new Color(0.25f, 0.25f, 0.25f, 0.95f));
                    }
                    else
                    {
                        // Darker background
                        backTex.SetPixel(x, y, new Color(0.15f, 0.15f, 0.15f, 0.95f));
                    }
                }
            }
            backTex.Apply();
            _tooltipStyle.normal.background = backTex;

            Instance = new Tooltip();
        }

        public void RecordTooltip(int windowId, bool skipOnEvent = true, string overrideTooltip = null)
        {
            if (skipOnEvent && Event.current.type != EventType.Repaint) return;

            if (!_windowTooltipTexts.TryGetValue(windowId, out string tooltipText))
            {
                tooltipText = string.Empty;
            }

            string newTooltipStr = overrideTooltip != null ? overrideTooltip : GUI.tooltip;
            if (tooltipText != newTooltipStr)
            {
                _isTooltipChanged = true;
                if (!string.IsNullOrEmpty(tooltipText))
                {
                    _tooltipBeginDt = DateTime.UtcNow;
                }
                _windowTooltipTexts[windowId] = newTooltipStr;
            }
        }

        public void ShowTooltip(int windowId, TextAnchor contentAlignment = TextAnchor.UpperLeft)
        {
            if (_windowTooltipTexts.TryGetValue(windowId, out string tooltipText) && !string.IsNullOrEmpty(tooltipText) &&
                (DateTime.UtcNow - _tooltipBeginDt).TotalMilliseconds > TooltipShowDelay)
            {
                if (_isTooltipChanged)
                {
                    var c = new GUIContent(tooltipText);
                    _tooltipStyle.CalcMinMaxWidth(c, out _, out float width);
                    _tooltipStyle.alignment = contentAlignment;

                    width = Math.Min(width, TooltipMaxWidth);
                    float height = _tooltipStyle.CalcHeight(c, TooltipMaxWidth);
                    _tooltipRect = new Rect(
                        Math.Min(Screen.width - width, Input.mousePosition.x + 15),
                        Math.Min(Screen.height - height, Screen.height - Input.mousePosition.y + 10),
                        width, height);
                    _isTooltipChanged = false;
                }

                GUI.Window(
                    _tooltipWindowId,
                    _tooltipRect,
                    (_) => { },
                    tooltipText,
                    _tooltipStyle);
                GUI.BringWindowToFront(_tooltipWindowId);
            }
        }
    }
}
