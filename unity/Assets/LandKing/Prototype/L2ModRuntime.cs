using System;
using System.Collections.Generic;
using System.IO;
using LandKing.Simulation;
using MoonSharp.Interpreter;
using UnityEngine;

namespace LandKing.Prototype
{
    /// <summary>按 L1 依赖序加载的 L2（Lua + MoonSharp）；每步在 <see cref="WorldSimulation.Step"/> 之后注入只读 <c>l2</c> 表并执行 <c>after_tick(tick)</c>。首帧在注入 <c>l2</c> 后调用一次 <c>l2_init()</c>（若存在）。API：<c>log</c>、<c>mod_id</c>、<c>l2</c>（见 <c>docs/实现/技术选型.md</c>）。</summary>
    public sealed class L2ModRuntime
    {
        private WorldSimulation _sim;
        private readonly List<L2Instance> _scripts = new List<L2Instance>(4);

        private L2ModRuntime() { }

        public static L2ModRuntime Create(L1ModLoader.Result l1)
        {
            var r = new L2ModRuntime();
            if (l1 == null || !l1.Success || l1.L2ScriptEntries == null) return r;
            for (var i = 0; i < l1.L2ScriptEntries.Count; i++)
            {
                var e = l1.L2ScriptEntries[i];
                if (e == null || string.IsNullOrEmpty(e.filePath) || !FileIOExists(e.filePath)) continue;
                r.TryAddScript(e.modId, e.filePath);
            }

            return r;
        }

        private static bool FileIOExists(string path)
        {
            try { return System.IO.File.Exists(path); } catch (Exception) { return false; }
        }

        public void SetSimulation(WorldSimulation sim) => _sim = sim;

        public void AfterSimulationStep()
        {
            if (_sim == null || _scripts.Count == 0) return;
            var tick = _sim.TickCount;
            for (var i = 0; i < _scripts.Count; i++)
                _scripts[i].Invoke(_sim, tick);
        }

        private void TryAddScript(string modId, string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var sink = new L2LogChannel { ModId = modId ?? string.Empty, Sim = _sim };
            try
            {
                string code;
                try
                {
                    code = File.ReadAllText(filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[L2] " + (modId ?? "?") + " 无法读取脚本: " + ex.Message);
                    return;
                }
                if (code == null) return;
                if (code.Length > L2DataFiles.MaxScriptSourceChars)
                {
                    Debug.LogError("[L2] " + (modId ?? "?") + " 脚本过大 (>" + L2DataFiles.MaxScriptSourceChars + " 字符)，已拒绝。");
                    return;
                }
                var s = new Script(CoreModules.Preset_SoftSandbox);
                s.Globals["log"] = (Action<string>)(msg =>
                {
                    if (string.IsNullOrEmpty(msg)) return;
                    sink.Sim?.AppendL2Chronicle(sink.ModId, msg);
                });
                s.Globals["mod_id"] = modId ?? string.Empty;
                s.DoString(code, null, filePath);
                var init = s.Globals.Get("l2_init");
                var fn = s.Globals.Get("after_tick");
                if (fn.Type == DataType.Function)
                {
                    var pendingInit = init.Type == DataType.Function;
                    _scripts.Add(new L2Instance
                    {
                        Script = s,
                        AfterTick = fn,
                        Log = sink,
                        L2Init = pendingInit ? init : null,
                        L2InitPending = pendingInit
                    });
                }
                else
                    Debug.LogWarning("[L2] 未找到全局函数 after_tick，跳过: " + filePath);
            }
            catch (InterpreterException ex)
            {
                Debug.LogError("[L2] " + (modId ?? "?") + " 脚本错误: " + ex.DecoratedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError("[L2] " + (modId ?? "?") + " 加载失败: " + ex.Message);
            }
        }

        private sealed class L2LogChannel
        {
            public string ModId;
            public WorldSimulation Sim;
        }

        private sealed class L2Instance
        {
            public Script Script;
            public DynValue AfterTick;
            public DynValue L2Init;
            public bool L2InitPending;
            public L2LogChannel Log;

            public void Invoke(WorldSimulation sim, int tick)
            {
                if (Log != null) Log.Sim = sim;
                if (Script == null || AfterTick == null) return;
                try
                {
                    L2ReadOnlyTable.ApplyTo(Script, sim, tick);
                    if (L2InitPending && L2Init != null && L2Init.Type == DataType.Function)
                    {
                        L2InitPending = false;
                        try
                        {
                            Script.Call(L2Init);
                        }
                        catch (InterpreterException iex)
                        {
                            Debug.LogError("[L2] " + (Log?.ModId ?? "?") + " l2_init: " + iex.DecoratedMessage);
                        }
                    }
                    Script.Call(AfterTick, tick);
                }
                catch (InterpreterException ex)
                {
                    Debug.LogError("[L2] " + (Log?.ModId ?? "?") + " after_tick: " + ex.DecoratedMessage);
                }
            }
        }
    }

    /// <summary>每步写入 <c>l2</c> 表：只读快照，不含可写引用（勿暴露 Rng/Map）。</summary>
    internal static class L2ReadOnlyTable
    {
        public static void ApplyTo(Script s, WorldSimulation sim, int tick)
        {
            if (s == null || sim == null) return;
            var t = new Table(s);
            t.Set("tick", DynValue.NewNumber(tick));
            t.Set("seed", DynValue.NewNumber(sim.InitialSeed));
            t.Set("ape_count", DynValue.NewNumber(sim.ApeCount));
            t.Set("water_left", DynValue.NewNumber(sim.WaterLeft));
            t.Set("water_right", DynValue.NewNumber(sim.WaterRight));
            t.Set("drought", DynValue.NewBoolean(sim.DroughtActive));
            t.Set("rain_used", DynValue.NewBoolean(sim.RainUsed));
            t.Set("can_show_rain", DynValue.NewBoolean(sim.CanShowRain));
            t.Set("settlement_west", DynValue.NewString(sim.WestSettlementName ?? string.Empty));
            t.Set("settlement_east", DynValue.NewString(sim.EastSettlementName ?? string.Empty));
            var prey = sim.GetAlivePreyForDisplay();
            t.Set("prey_count", DynValue.NewNumber(prey != null ? prey.Length : 0));
            var pred = sim.GetPredatorsForDisplay();
            t.Set("predator_count", DynValue.NewNumber(pred != null ? pred.Length : 0));
            s.Globals["l2"] = t;
        }
    }
}
