using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>Mono front for one ape. 对应原型中的 Ape + 简化的头顶名.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class Ape : MonoBehaviour
    {
        [SerializeField] private Color aliveColor = new Color(0.45f, 0.3f, 0.12f);
        [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f);

        public int ApeId { get; private set; }
        private WorldManager _world;
        private SpriteRenderer _selectRing;
        private TextMesh _label;

        public void Init(WorldManager world, int id)
        {
            _world = world;
            ApeId = id;
            var sr = GetComponent<SpriteRenderer>();
            sr.sprite = Sprite2DUtil.CreateSprite(24, 24, Color.white);
            sr.color = aliveColor;
            sr.sortingOrder = 2;
            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = 0.32f;
            var ring = new GameObject("SelectRing");
            ring.transform.SetParent(transform, false);
            _selectRing = ring.AddComponent<SpriteRenderer>();
            _selectRing.sprite = Sprite2DUtil.CreateSprite(40, 40, new Color(1f, 1f, 1f, 0.3f));
            _selectRing.color = Color.white;
            _selectRing.drawMode = SpriteDrawMode.Sliced;
            _selectRing.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            _selectRing.sortingOrder = 1;
            _selectRing.enabled = false;
            var textGo = new GameObject("Name");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = new Vector3(0, 0.55f, 0f);
            _label = textGo.AddComponent<TextMesh>();
            _label.characterSize = 0.08f;
            _label.anchor = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.text = string.Empty;
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 3;
        }

        public void SetSelected(bool on)
        {
            if (_selectRing != null) _selectRing.enabled = on;
        }

        public void SyncFromState(ApeState s)
        {
            transform.position = new Vector3(s.GridX + 0.5f, s.GridY + 0.5f, 0f);
            if (!s.Alive) GetComponent<SpriteRenderer>().color = deadColor;
            var name = !string.IsNullOrEmpty(s.Nickname) ? s.Nickname : string.Empty;
            if (_label != null) _label.text = name;
        }

        public void SetNicknameAndLabel(string n)
        {
            if (_label != null) _label.text = n ?? string.Empty;
        }
    }
}
