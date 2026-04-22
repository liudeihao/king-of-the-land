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

        public void Add(in WorldEventRecord e) => PushLine(WorldEventFormatting.ToDisplayString(e));

        public void Add(string line)
        {
            if (line == null) return;
            PushLine(line);
        }

        /// <summary>读档时：以存档内编年史重绘，再追加一条系统提示；之后模拟产生的新事件仍用 <c>Add</c> 重载逐条追加。</summary>
        public void RebuildFromChronicle(IReadOnlyList<WorldEventRecord> chronicle, string systemTail = null)
        {
            _lines.Clear();
            if (chronicle != null)
            {
                for (var i = 0; i < chronicle.Count; i++)
                {
                    _lines.Add(WorldEventFormatting.ToDisplayString(chronicle[i]));
                    while (_lines.Count > _max) _lines.RemoveAt(0);
                }
            }
            if (!string.IsNullOrEmpty(systemTail))
            {
                _lines.Add(systemTail);
                while (_lines.Count > _max) _lines.RemoveAt(0);
            }
            Rebuild();
        }

        private void PushLine(string s, bool trimHead = true)
        {
            _lines.Add(s);
            while (trimHead && _lines.Count > _max) _lines.RemoveAt(0);
            Rebuild();
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
