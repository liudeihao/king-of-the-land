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
            var w = gameObject.AddComponent<WorldManager>();
            var t = gameObject.AddComponent<TimeManager>();
            var e = gameObject.AddComponent<EventLog>();
            var u = gameObject.AddComponent<UIManager>();
            _ = gameObject.AddComponent<SelectionManager>();
            w.SetEventLog(e);
            w.Build();
            u.CreateUi(transform);
            if (_logPrefix != null) Debug.Log($"[{_logPrefix}] Prototype: map 20x20, 10 apes, tick sim ready.");
        }
    }
}
