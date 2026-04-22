using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>Tick 推进与倍速。对应原型 第三步、第六步 旱灾在 Sim 内.</summary>
    public sealed class TimeManager : MonoBehaviour
    {
        [SerializeField] private float tickInterval = 1f;
        [SerializeField] private WorldManager _world;

        public float TimeScale = 1f;
        public bool Paused;
        public int TickCount => _world != null && _world.Sim != null ? _world.Sim.TickCount : 0;

        private float _acc;

        private void Awake()
        {
            if (_world == null) _world = GetComponent<WorldManager>();
        }

        private void Update()
        {
            if (_world == null || _world.Sim == null) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) Paused = !Paused;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) TimeScale = 1f;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) TimeScale = 2f;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) TimeScale = 4f;
            if (Paused) return;
            _acc += Time.deltaTime * TimeScale;
            while (_acc >= tickInterval)
            {
                _acc -= tickInterval;
                _world.StepSimulation();
            }
        }
    }
}
