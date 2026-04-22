using LandKing.Simulation;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>Builds 20x20 cell sprites from <see cref="MapData"/>. 原型构建步骤 第一步.</summary>
    public sealed class MapGenerator : MonoBehaviour
    {
        [SerializeField] private Color grass = new Color(0.55f, 0.85f, 0.5f);
        [SerializeField] private Color river = new Color(0.2f, 0.5f, 0.9f);
        [SerializeField] private Color fruit = new Color(0.1f, 0.5f, 0.2f);
        [SerializeField] private Color fruitDry = new Color(0.55f, 0.42f, 0.2f);
        [SerializeField] private float waterDryThreshold = 0.3f;

        private SpriteRenderer[,] _renderers;
        private WorldSimulation _sim;
        private MapData _map;

        public void Build(WorldSimulation sim)
        {
            _sim = sim;
            _map = sim.Map;
            _renderers = new SpriteRenderer[MapData.Size, MapData.Size];
            var sheet = Sprite2DUtil.White1x1;
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++)
            {
                var go = new GameObject($"Cell_{x}_{y}");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Sprite.Create(sheet, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect, Vector4.zero, false, Vector2.zero);
                go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                sr.sortingOrder = 0;
                _renderers[x, y] = sr;
            }
            RefreshColors();
        }

        public void RefreshColors()
        {
            if (_map == null || _renderers == null) return;
            for (var y = 0; y < MapData.Size; y++)
            for (var x = 0; x < MapData.Size; x++)
            {
                var t = _map.Tiles[x, y];
                var sr = _renderers[x, y];
                if (t == TileType.Grass) sr.color = grass;
                else if (t == TileType.River) sr.color = river;
                else
                {
                    var w = x < MapData.RiverX ? _sim.WaterLeft : _sim.WaterRight;
                    sr.color = w < waterDryThreshold
                        ? Color.Lerp(fruit, fruitDry, 1f - w / waterDryThreshold)
                        : fruit;
                }
            }
        }
    }
}
