using UnityEngine;

namespace LandKing.Prototype
{
    public sealed class PrototypeGameRoot : MonoBehaviour
    {
        [SerializeField] private string _logPrefix = "LandKing";

        private void Awake()
        {
            var main = Camera.main;
            if (main != null)
            {
                main.orthographic = true;
                main.orthographicSize = 10.5f;
                main.transform.position = new Vector3(10, 10, -10f);
            }
            var mods = L1ModLoader.Load();
            if (!mods.Success && mods.Errors != null)
            {
                foreach (var line in mods.Errors)
                {
                    var p = _logPrefix != null ? $"[{_logPrefix}] " : string.Empty;
                    Debug.LogError(p + "L1Mod: " + line);
                }
            }
            var w = gameObject.AddComponent<WorldManager>();
            var t = gameObject.AddComponent<TimeManager>();
            var e = gameObject.AddComponent<EventLog>();
            var u = gameObject.AddComponent<UIManager>();
            _ = gameObject.AddComponent<SelectionManager>();
            w.SetEventLog(e);
            w.Build(42, mods.Sim);
            u.CreateUi(transform);
            u.SetLoadedMods(mods);
            if (mods.Success)
            {
                if (_logPrefix != null)
                    Debug.Log($"[{_logPrefix}] Prototype: L1 mod(s)={mods.ModFolderNames?.Count ?? 0}, 依赖序 OK。");
            }
            else
            {
                if (_logPrefix != null) Debug.LogWarning($"[{_logPrefix}] L1 使用默认 SimParams（见 HUD / Log）。");
            }
        }
    }
}
