using System.Collections.Generic;
using LandKing.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LandKing.Prototype
{
    /// <summary>Spawns sim + apes, syncs state. 对应原型中的 WorldManager.</summary>
    public sealed class WorldManager : MonoBehaviour
    {
        private WorldSimulation _sim;
        private MapGenerator _map;
        private readonly List<Ape> _apes = new List<Ape>(10);
        private Transform _apeRoot;
        public EventLog EventLog { get; private set; }

        public WorldSimulation Sim => _sim;
        public IReadOnlyList<Ape> Apes => _apes;
        public MapGenerator Map => _map;

        public void SetEventLog(EventLog log) => EventLog = log;

        public void Build(int randomSeed = 42, SimParams simParams = null)
        {
            _apeRoot = new GameObject("Apes").transform;
            _apeRoot.SetParent(transform, false);
            var go = new GameObject("Map");
            go.transform.SetParent(transform, false);
            _map = go.AddComponent<MapGenerator>();
            _sim = new WorldSimulation(randomSeed, simParams);
            SpawnApeAndMapViews();
        }

        public void ReplaceSimulation(WorldSimulation sim, bool clearSelection = false)
        {
            if (sim == null) return;
            if (_apeRoot == null) return;
            _sim = sim;
            for (var i = _apeRoot.childCount - 1; i >= 0; i--) Object.Destroy(_apeRoot.GetChild(i).gameObject);
            _apes.Clear();
            if (_map != null) _map.Build(_sim);
            SpawnApeViewObjects();
            if (EventLog != null) EventLog.Add("已从存档恢复世界。");
            if (clearSelection) GetComponent<SelectionManager>()?.Set(null);
        }

        private void SpawnApeViewObjects()
        {
            var states = _sim.GetApeStates();
            for (var i = 0; i < states.Count; i++)
            {
                var s = states[i];
                var o = new GameObject($"Ape_{s.Id}");
                o.transform.SetParent(_apeRoot, false);
                var ape = o.AddComponent<Ape>();
                ape.Init(this, s.Id);
                _apes.Add(ape);
            }
            SyncAll();
        }

        private void SpawnApeAndMapViews()
        {
            _map.Build(_sim);
            SpawnApeViewObjects();
        }

        public void StepSimulation()
        {
            _sim.Step();
            if (EventLog != null)
            {
                foreach (var line in _sim.StealLogQueue()) EventLog.Add(line);
            }
            foreach (var newId in _sim.PullNewApeViewIds())
            {
                var o = new GameObject($"Ape_{newId}");
                o.transform.SetParent(_apeRoot, false);
                var ape = o.AddComponent<Ape>();
                ape.Init(this, newId);
                _apes.Add(ape);
            }
            _map?.RefreshColors();
            SyncAll();
        }

        public void SetNickname(int id, string n)
        {
            _sim.SetApeNickname(id, n);
            for (var i = 0; i < _apes.Count; i++)
            {
                if (_apes[i].ApeId == id) _apes[i].SetNicknameAndLabel(n);
            }
        }

        public void ApplyRain() => _sim.ApplyRain();

        public void SyncAll()
        {
            for (var i = 0; i < _apes.Count; i++)
            {
                var st = _sim.FindApe(_apes[i].ApeId);
                if (st.HasValue) _apes[i].SyncFromState(st.Value);
            }
        }
    }
}
