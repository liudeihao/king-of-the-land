using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>小格点：猎物(橙)与掠食者(暗红)。</summary>
    public sealed class WildlifeDot : MonoBehaviour
    {
        [SerializeField] private Color preyColor = new Color(0.95f, 0.5f, 0.2f);
        [SerializeField] private Color predColor = new Color(0.55f, 0.08f, 0.1f);
        [SerializeField] private bool isPredator;

        public int EntityId { get; private set; }
        public bool IsPredator => isPredator;

        public void Init(bool predator, int id)
        {
            isPredator = predator;
            EntityId = id;
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite2DUtil.CreateSprite(14, 14, Color.white);
            sr.color = predator ? predColor : preyColor;
            sr.sortingOrder = 1;
        }

        public void Sync(PreyView p)
        {
            transform.position = new Vector3(p.X + 0.5f, p.Y + 0.4f, 0f);
            if (!isPredator && !string.IsNullOrEmpty(p.SpeciesId)) TintFromId(p.SpeciesId);
        }

        public void Sync(PredatorView p)
        {
            transform.position = new Vector3(p.X + 0.5f, p.Y + 0.5f, 0f);
            if (isPredator && !string.IsNullOrEmpty(p.SpeciesId)) TintFromId(p.SpeciesId);
        }

        private void TintFromId(string id)
        {
            var h = id != null ? id.GetHashCode() : 0;
            var u = (uint)h;
            var r = 0.45f + (u & 0xff) / 512f;
            var g = 0.3f + ((u >> 8) & 0xff) / 512f;
            var b = 0.2f + ((u >> 16) & 0xff) / 512f;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(r, g, b, 1f);
        }
    }
}
