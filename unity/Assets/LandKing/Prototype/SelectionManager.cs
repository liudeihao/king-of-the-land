using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>点击选取猿. 对应原型 第五步.</summary>
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
