using System;
using UnityEngine;

namespace SpeedStars.UI.Flow
{
    /// <summary>
    /// Which Build Settings scene each GameScene is. Serialized on the project installer so the names are set in the
    /// Inspector; the defaults are the contract's scene names (§5) and only matter when the field is first created.
    /// </summary>
    [Serializable]
    public sealed class SceneFlowScenes
    {
        [Tooltip("Scene name of the Home (menu) scene, exactly as in Build Settings (file name without .unity).")]
        [SerializeField] private string homeScene = "Home";

        [Tooltip("Scene name of the Race scene, exactly as in Build Settings (file name without .unity).")]
        [SerializeField] private string raceScene = "Race";

        public SceneFlowScenes()
        {
        }

        public SceneFlowScenes(string homeScene, string raceScene)
        {
            this.homeScene = homeScene;
            this.raceScene = raceScene;
        }

        /// <summary>Scene name for <paramref name="scene"/>; null or empty when it is not set.</summary>
        public string NameOf(GameScene scene)
        {
            switch (scene)
            {
                case GameScene.Home: return homeScene;
                case GameScene.Race: return raceScene;
                default: return null;
            }
        }
    }
}
