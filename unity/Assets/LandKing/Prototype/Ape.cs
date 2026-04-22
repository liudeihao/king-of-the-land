using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>Mono front for one ape. 对应原型中的 Ape + 简化的头顶名.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class Ape : MonoBehaviour
    {
        [SerializeField] private Color maleColor = new Color(0.4f, 0.32f, 0.2f);
        [SerializeField] private Color femaleColor = new Color(0.5f, 0.28f, 0.32f);
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
            sr.color = maleColor;
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
            var sr = GetComponent<SpriteRenderer>();
            transform.position = new Vector3(s.GridX + 0.5f, s.GridY + 0.5f, 0f);
            var sc = s.BodyScale > 0.01f ? s.BodyScale : 1f;
            transform.localScale = new Vector3(sc, sc, 1f);
            if (!s.Alive) sr.color = deadColor;
            else sr.color = s.IsMale ? maleColor : femaleColor;
            if (_label == null) return;
            if (!string.IsNullOrEmpty(s.Nickname)) _label.text = s.Nickname;
            else _label.text = $"ID{s.Id} {(s.IsMale ? "男" : "女")} {StageShort(s.Stage)}";
        }

        private static string StageShort(LifeStage st) => st switch
        {
            LifeStage.Infant => "婴",
            LifeStage.Child => "幼",
            LifeStage.Youth => "少",
            LifeStage.Adult => "成",
            LifeStage.Elder => "长",
            _ => "?"
        };

        public void SetNicknameAndLabel(string n)
        {
            if (_label != null) _label.text = n ?? string.Empty;
        }
    }
}
