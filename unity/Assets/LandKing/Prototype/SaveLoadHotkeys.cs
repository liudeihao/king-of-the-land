using UnityEngine;

namespace LandKing.Prototype
{
    public sealed class SaveLoadHotkeys : MonoBehaviour
    {
        [SerializeField] private WorldManager _world;

        private void Awake()
        {
            if (_world == null) _world = GetComponent<WorldManager>();
        }

        private void Update()
        {
            if (_world == null) return;
            if (Input.GetKeyDown(KeyCode.F5)) GameSaveV1File.Write(_world);
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (GameSaveV1File.TryRead(_world, out var err)) return;
                Debug.LogWarning("[LandKing] 读档失败: " + err);
            }
        }
    }
}
