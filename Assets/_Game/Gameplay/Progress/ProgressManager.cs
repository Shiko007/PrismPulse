using UnityEngine;

namespace PrismPulse.Gameplay.Progress
{
    /// <summary>
    /// Persists level completion and star ratings via PlayerPrefs.
    /// </summary>
    public static class ProgressManager
    {
        private const string KeyPrefix = "level_";
        private const string StarsSuffix = "_stars";

        public static int GetStars(string levelId)
        {
            return PlayerPrefs.GetInt(KeyPrefix + levelId + StarsSuffix, 0);
        }

        /// <summary>
        /// Save stars for a level. Only overwrites if the new rating is better.
        /// </summary>
        public static void SetStars(string levelId, int stars)
        {
            int existing = GetStars(levelId);
            if (stars > existing)
            {
                PlayerPrefs.SetInt(KeyPrefix + levelId + StarsSuffix, stars);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Level 0 is always unlocked. Others unlock when the previous level has 1+ stars.
        /// </summary>
        public static bool IsUnlocked(int levelIndex, string previousLevelId)
        {
            if (levelIndex <= 0) return true;
            return GetStars(previousLevelId) > 0;
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
