using System;
using UnityEngine;

namespace Brick2Life.Feedback
{
    /// <summary>Cartoon styles for the projection layer, aimed at a children audience.</summary>
    public enum ProjectionStyleKind { BubbleOcean = 0, CandyPop = 1, CrayonPaper = 2, NeonToy = 3 }

    [Serializable]
    public sealed class ProjectionArtStyle
    {
        public string Name;
        public Color Background;
        public Color Ring;
        public Color RingInvaded;
        public Color Path;
        public Color Enemy;
        public Color BarBack;
        public Color BarLow;
        public Color BarFull;
        public Color Id;
        public Color Spawn;
        public Color Splash;
        public Color CoreHit;
        public Color WallRipple;
        public Color Accent;

        private static readonly ProjectionArtStyle BubbleOceanStyle = new ProjectionArtStyle
        {
            Name = "泡泡海洋",
            Background = new Color(0.02f, 0.09f, 0.16f),
            Ring = new Color(0.35f, 0.85f, 1f, 0.6f),
            RingInvaded = new Color(0.56f, 1f, 0.93f, 0.95f),
            Path = new Color(0.35f, 0.85f, 1f, 0.28f),
            Enemy = new Color(1f, 0.45f, 0.4f, 0.95f),
            BarBack = new Color(0.04f, 0.12f, 0.2f, 0.85f),
            BarLow = new Color(1f, 0.35f, 0.3f, 0.95f),
            BarFull = new Color(0.4f, 1f, 0.7f, 0.95f),
            Id = new Color(0.85f, 1f, 1f, 0.95f),
            Spawn = new Color(0.2f, 0.95f, 1f, 0.9f),
            Splash = new Color(0.58f, 1f, 0.96f, 1f),
            CoreHit = new Color(1f, 0.3f, 0.35f, 0.95f),
            WallRipple = new Color(0.75f, 0.9f, 1f, 0.7f),
            Accent = new Color(1f, 0.85f, 0.4f, 0.9f)
        };

        private static readonly ProjectionArtStyle CandyPopStyle = new ProjectionArtStyle
        {
            Name = "糖果马卡龙",
            Background = new Color(0.24f, 0.16f, 0.24f),
            Ring = new Color(1f, 0.78f, 0.92f, 0.7f),
            RingInvaded = new Color(1f, 0.62f, 0.55f, 0.95f),
            Path = new Color(1f, 0.95f, 0.7f, 0.42f),
            Enemy = new Color(1f, 0.42f, 0.55f, 1f),
            BarBack = new Color(0.28f, 0.18f, 0.3f, 0.9f),
            BarLow = new Color(1f, 0.45f, 0.55f, 1f),
            BarFull = new Color(0.62f, 1f, 0.78f, 1f),
            Id = new Color(1f, 0.98f, 1f, 1f),
            Spawn = new Color(1f, 0.9f, 0.55f, 0.95f),
            Splash = new Color(1f, 0.85f, 0.95f, 1f),
            CoreHit = new Color(1f, 0.5f, 0.6f, 0.95f),
            WallRipple = new Color(0.85f, 0.75f, 1f, 0.75f),
            Accent = new Color(0.65f, 1f, 0.85f, 0.95f)
        };

        private static readonly ProjectionArtStyle CrayonPaperStyle = new ProjectionArtStyle
        {
            Name = "蜡笔手绘",
            Background = new Color(0.34f, 0.27f, 0.19f),
            Ring = new Color(0.95f, 0.8f, 0.45f, 0.75f),
            RingInvaded = new Color(0.95f, 0.55f, 0.3f, 0.95f),
            Path = new Color(0.95f, 0.88f, 0.62f, 0.4f),
            Enemy = new Color(0.82f, 0.25f, 0.2f, 1f),
            BarBack = new Color(0.22f, 0.16f, 0.12f, 0.9f),
            BarLow = new Color(0.85f, 0.3f, 0.22f, 1f),
            BarFull = new Color(0.45f, 0.75f, 0.35f, 1f),
            Id = new Color(1f, 0.95f, 0.8f, 1f),
            Spawn = new Color(0.4f, 0.72f, 0.9f, 0.9f),
            Splash = new Color(0.55f, 0.8f, 0.95f, 1f),
            CoreHit = new Color(0.9f, 0.4f, 0.25f, 0.95f),
            WallRipple = new Color(0.95f, 0.85f, 0.6f, 0.8f),
            Accent = new Color(0.98f, 0.72f, 0.25f, 0.95f)
        };

        private static readonly ProjectionArtStyle NeonToyStyle = new ProjectionArtStyle
        {
            Name = "霓虹玩具",
            Background = new Color(0.06f, 0.02f, 0.12f),
            Ring = new Color(0.2f, 1f, 0.95f, 0.75f),
            RingInvaded = new Color(1f, 0.25f, 0.75f, 0.95f),
            Path = new Color(0.75f, 0.3f, 1f, 0.45f),
            Enemy = new Color(1f, 0.3f, 0.75f, 1f),
            BarBack = new Color(0.1f, 0.03f, 0.18f, 0.9f),
            BarLow = new Color(1f, 0.35f, 0.7f, 1f),
            BarFull = new Color(0.35f, 1f, 0.85f, 1f),
            Id = new Color(0.95f, 0.9f, 1f, 1f),
            Spawn = new Color(0.35f, 1f, 0.9f, 0.95f),
            Splash = new Color(0.5f, 1f, 0.95f, 1f),
            CoreHit = new Color(1f, 0.3f, 0.6f, 0.95f),
            WallRipple = new Color(0.7f, 0.5f, 1f, 0.8f),
            Accent = new Color(1f, 0.9f, 0.2f, 0.95f)
        };

        public static ProjectionArtStyle Get(ProjectionStyleKind kind)
        {
            switch (kind)
            {
                case ProjectionStyleKind.CandyPop: return CandyPopStyle;
                case ProjectionStyleKind.CrayonPaper: return CrayonPaperStyle;
                case ProjectionStyleKind.NeonToy: return NeonToyStyle;
                default: return BubbleOceanStyle;
            }
        }
    }
}
