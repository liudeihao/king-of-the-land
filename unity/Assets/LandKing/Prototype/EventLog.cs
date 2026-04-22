using System.Collections.Generic;
using System.Text;
using LandKing.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace LandKing.Prototype
{
    /// <summary>右侧滚动编年史；模拟事件带 tick 与类型标签，系统消息可为纯文本。</summary>
    public sealed class EventLog : MonoBehaviour
    {
        [SerializeField] private int _max = 50;
        private readonly List<string> _lines = new List<string>(64);
        private Text _text;
        private ScrollRect _scroll;

        public void Init(Text text, ScrollRect scroll)
        {
            _text = text;
            _scroll = scroll;
            if (text == null) return;
            var rt = text.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(280, rt.sizeDelta.y);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public void Add(in WorldEventRecord e) => PushLine(FormatLine(e));

        public void Add(string line)
        {
            if (line == null) return;
            PushLine(line);
        }

        private void PushLine(string s)
        {
            _lines.Add(s);
            while (_lines.Count > _max) _lines.RemoveAt(0);
            Rebuild();
        }

        private static string FormatLine(in WorldEventRecord e)
        {
            if (e.Tick < 0) return e.Message ?? string.Empty;
            var tag = e.Kind switch
            {
                WorldEventKind.DroughtStart => "旱起",
                WorldEventKind.DroughtSevere => "旱情",
                WorldEventKind.Rain => "降雨",
                WorldEventKind.Birth => "出生",
                WorldEventKind.Starvation => "饥亡",
                WorldEventKind.NaturalDeath => "寿终",
                WorldEventKind.FoodDepleted => "果尽",
                WorldEventKind.EastShore => "东岸",
                _ => e.Kind.ToString()
            };
            return $"[t{e.Tick}][{tag}] {e.Message}";
        }

        private void Rebuild()
        {
            if (_text == null) return;
            var b = new StringBuilder();
            for (var i = 0; i < _lines.Count; i++) b.AppendLine(_lines[i]);
            _text.text = b.ToString();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 0f;
        }
    }
}
