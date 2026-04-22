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
                // Bias view toward west bank (primary band); east band has the second troop.
                main.transform.position = new Vector3(5f, 10f, -10f);
            }
            var mods = L1ModLoader.Load();
            L1ModSession.ApplyFrom(mods);
            L2ModSession.ApplyFrom(mods);
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
            _ = gameObject.AddComponent<SaveLoadHotkeys>();
            w.SetEventLog(e);
            w.Build(42, mods.Sim, mods.Wildlife, mods.Culture, mods);
            u.CreateUi(transform);
            u.SetLoadedMods(mods);
            if (mods.Success)
            {
                if (_logPrefix != null)
                {
                    var nL2 = mods.L2ScriptEntries?.Count ?? 0;
                    Debug.Log(
                        nL2 > 0
                            ? $"[{_logPrefix}] Prototype: L1 mod(s)={mods.ModFolderNames?.Count ?? 0}，L2 脚本条数={nL2}，依赖序 OK。"
                            : $"[{_logPrefix}] Prototype: L1 mod(s)={mods.ModFolderNames?.Count ?? 0}，依赖序 OK。");
                }
            }
            else
            {
                if (_logPrefix != null) Debug.LogWarning($"[{_logPrefix}] L1 使用默认 SimParams（见 HUD / Log）。");
            }
            if (main != null)
            {
                if (main.GetComponent<CameraFollowSelection>() == null)
                {
                    var camF = main.gameObject.AddComponent<CameraFollowSelection>();
                    camF.SetSelection(GetComponent<SelectionManager>());
                }
            }
        }
    }
}
