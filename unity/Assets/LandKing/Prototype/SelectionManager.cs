using System.Collections.Generic;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>点击选取；[Tab]/[Shift+Tab] 在存活族人间循环（不点到 UI 时）。</summary>
    public sealed class SelectionManager : MonoBehaviour
    {
        [SerializeField] private WorldManager _world;
        [SerializeField] private UIManager _ui;
        private Ape _selected;
        public Ape Selected => _selected;

        private void Awake()
        {
            if (_world == null) _world = GetComponent<WorldManager>();
            if (_ui == null) _ui = GetComponent<UIManager>();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) && (_ui == null || !_ui.IsPointerOverUi()))
            {
                var shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                CycleSelection(shift);
            }
            if (UnityEngine.Input.GetMouseButtonDown(0) && _ui != null && !_ui.IsPointerOverUi())
            {
                var cam = Camera.main;
                if (cam == null) return;
                var w = cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
                w.z = 0f;
                var hit = Physics2D.OverlapPoint(w);
                if (hit)
                {
                    var ape = hit.GetComponent<Ape>();
                    if (ape != null) Set(ape);
                }
            }
        }

        private void CycleSelection(bool reverse)
        {
            if (_world == null || _world.Sim == null) return;
            var cands = new List<Ape>(8);
            foreach (var a in _world.Apes)
            {
                var st = _world.Sim.FindApe(a.ApeId);
                if (st.HasValue && st.Value.Alive) cands.Add(a);
            }
            cands.Sort((a, b) => a.ApeId.CompareTo(b.ApeId));
            if (cands.Count == 0) { Set(null); return; }
            if (cands.Count == 1) { Set(cands[0]); return; }
            var idx = 0;
            if (_selected != null)
            {
                idx = cands.FindIndex(x => x == _selected);
                if (idx < 0) idx = 0;
            }
            if (reverse) idx = (idx - 1 + cands.Count) % cands.Count;
            else idx = (idx + 1) % cands.Count;
            Set(cands[idx]);
        }

        public void Set(Ape ape)
        {
            if (_selected == ape) return;
            if (_selected != null) _selected.SetSelected(false);
            _selected = ape;
            if (_selected != null) _selected.SetSelected(true);
            _ui?.SetSelected(ape);
        }
    }
}
