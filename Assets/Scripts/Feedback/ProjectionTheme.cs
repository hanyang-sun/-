using UnityEngine;

namespace Brick2Life.Feedback
{
    /// <summary>
    /// Shape language for the three battle projection actors (monsters, route, towers).
    /// Background and palette are separate: only geometry/texture changes here.
    /// </summary>
    public enum ProjectionTheme { Octopus = 0, Gummy = 1, Robot = 2, Ghost = 3 }

    public static class ProjectionThemeInfo
    {
        public static string Label(ProjectionTheme theme)
        {
            switch (theme)
            {
                case ProjectionTheme.Gummy: return "软糖";
                case ProjectionTheme.Robot: return "像素机器人";
                case ProjectionTheme.Ghost: return "幽灵";
                default: return "章鱼泡泡";
            }
        }

        public static bool UsesOctopusTower(ProjectionTheme theme) => theme == ProjectionTheme.Octopus;
    }
}
