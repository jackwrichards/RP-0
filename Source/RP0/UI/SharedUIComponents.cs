using System;
using System.Collections.Generic;
using UnityEngine;

namespace RP0.UI
{
    /// <summary>
    /// Shared UI components and styles used across Budget and Science UIs
    /// </summary>
    public static class SharedUIComponents
    {
        // Color scheme for modern UI
        public static class Colors
        {
            public static readonly Color Income = new Color(0.2f, 0.8f, 0.4f);           // Green
            public static readonly Color Expense = new Color(0.9f, 0.3f, 0.3f);          // Red
            public static readonly Color Neutral = new Color(0.8f, 0.8f, 0.8f);          // Lighter Gray
            public static readonly Color Positive = new Color(0.3f, 0.9f, 0.5f);         // Bright Green
            public static readonly Color Negative = new Color(1.0f, 0.4f, 0.4f);         // Bright Red
            public static readonly Color Warning = new Color(1.0f, 0.7f, 0.2f);          // Orange
            public static readonly Color Critical = new Color(1.0f, 0.2f, 0.2f);         // Bright Red
            public static readonly Color Info = new Color(0.4f, 0.7f, 1.0f);             // Blue
            public static readonly Color CardBackground = new Color(0.15f, 0.15f, 0.18f); // Dark
            public static readonly Color CardBorder = new Color(0.3f, 0.3f, 0.35f);      // Medium Dark
            public static readonly Color HeaderBackground = new Color(0.1f, 0.1f, 0.12f); // Very Dark
            public static readonly Color TextPrimary = new Color(0.95f, 0.95f, 0.95f);   // Almost White
            public static readonly Color TextSecondary = new Color(0.85f, 0.85f, 0.87f); // Brighter Light Gray
            public static readonly Color Accent = new Color(0.3f, 0.6f, 1.0f);           // Blue Accent
        }

        // Cached GUIStyles
        private static GUIStyle _cardStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _smallLabelStyle;

        // Cached textures to prevent garbage collection
        private static Dictionary<Color, Texture2D> _cachedTextures = new Dictionary<Color, Texture2D>();

        /// <summary>
        /// Initialize or refresh all styles
        /// </summary>
        public static void InitializeStyles()
        {
            // Card style
            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, Colors.CardBackground) },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(2, 2, 2, 2)
            };

            // Header style
            _headerStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, Colors.HeaderBackground), textColor = Colors.TextPrimary },
                border = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(0, 0, 0, 2),
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            // Title style
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Colors.TextPrimary },
                alignment = TextAnchor.MiddleLeft
            };

            // Subtitle style
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Colors.TextSecondary },
                alignment = TextAnchor.MiddleLeft
            };

            // Small label style
            _smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = Colors.TextSecondary },
                alignment = TextAnchor.MiddleLeft
            };
        }

        /// <summary>
        /// Create a solid color texture (cached to prevent GC issues)
        /// </summary>
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            // Use cached texture if available
            if (_cachedTextures.TryGetValue(col, out Texture2D cached))
                return cached;

            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            
            // Cache the texture to prevent garbage collection
            _cachedTextures[col] = result;
            
            return result;
        }

        /// <summary>
        /// Render a modern card container
        /// </summary>
        public static void BeginCard(string title = null)
        {
            if (_cardStyle == null) InitializeStyles();

            GUILayout.BeginVertical(_cardStyle);
            if (!string.IsNullOrEmpty(title))
            {
                GUILayout.Label(title, _headerStyle);
            }
        }

        public static void EndCard()
        {
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Render a summary metric card with fixed height
        /// </summary>
        public static void RenderMetricCard(string label, string value, Color valueColor, string subtitle = null, string tooltip = null)
        {
            if (_cardStyle == null) InitializeStyles();

            GUILayout.BeginVertical(_cardStyle, GUILayout.MinWidth(130), GUILayout.Height(60));
            
            var labelContent = string.IsNullOrEmpty(tooltip) ? new GUIContent(label) : new GUIContent(label, tooltip);
            GUILayout.Label(labelContent, _subtitleStyle);
            
            var valueStyle = new GUIStyle(_titleStyle) { normal = { textColor = valueColor } };
            var valueContent = string.IsNullOrEmpty(tooltip) ? new GUIContent(value) : new GUIContent(value, tooltip);
            GUILayout.Label(valueContent, valueStyle);
            
            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleContent = string.IsNullOrEmpty(tooltip) ? new GUIContent(subtitle) : new GUIContent(subtitle, tooltip);
                GUILayout.Label(subtitleContent, _smallLabelStyle);
            }
            else
            {
                // Add spacing to maintain consistent height when no subtitle
                GUILayout.Label("", _smallLabelStyle);
            }
            
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Get a secondary text style for empty states and hints
        /// </summary>
        public static GUIStyle GetSecondaryTextStyle()
        {
            if (_smallLabelStyle == null) InitializeStyles();
            return _smallLabelStyle;
        }

        /// <summary>
        /// Render a chart time range selector with configurable options
        /// </summary>
        public static int RenderChartRangeSelector(int currentMonths, string label = "Charts:")
        {
            var subtleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleRight
            };
            GUILayout.Label(label, subtleStyle, GUILayout.Width(45));
            
            // Create pressed button style that matches the top bar
            var pressedStyle = new GUIStyle(HighLogic.Skin.button);
            pressedStyle.normal = pressedStyle.active;
            
            int newMonths = currentMonths;
            if (GUILayout.Button("1y", currentMonths == 12 ? pressedStyle : HighLogic.Skin.button))
                newMonths = 12;
            if (GUILayout.Button("5y", currentMonths == 60 ? pressedStyle : HighLogic.Skin.button))
                newMonths = 60;
            if (GUILayout.Button("10y", currentMonths == 120 ? pressedStyle : HighLogic.Skin.button))
                newMonths = 120;
            if (GUILayout.Button("20y", currentMonths == 240 ? pressedStyle : HighLogic.Skin.button))
                newMonths = 240;
            if (GUILayout.Button("30y", currentMonths == 360 ? pressedStyle : HighLogic.Skin.button))
                newMonths = 360;
            
            return newMonths;
        }
    }
}
