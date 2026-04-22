using UnityEngine;
using UnityEngine.SceneManagement;

namespace LandKing.Prototype
{
    /// <summary>Play 时自动在 SampleScene 中生成原型根。无需手摆场景即可运行。</summary>
    public static class PrototypeEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterScene()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene") return;
            if (Object.FindFirstObjectByType<PrototypeGameRoot>(FindObjectsInactive.Exclude) != null) return;
            var r = new GameObject("PrototypeGameRoot");
            r.AddComponent<PrototypeGameRoot>();
        }
    }
}
