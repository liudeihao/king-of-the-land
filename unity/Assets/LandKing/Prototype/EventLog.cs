using System.Collections.Generic;
using System.Text;
using LandKing.Simulation;
using UnityEngine.UI;

namespace LandKing.Prototype
{
    /// <summary>右侧滚动编年史；按 <see cref="ChronicleViewFilter"/> 可筛掉高频行（猎食/传艺）。</summary>
    public sealed class EventLog : MonoBehaviour
    {
        [SerializeField] private int _max = 50;
        private readonly List<WorldEventRecord> _records = new List<WorldEventRecord>(64);
        private Text _text;
        private ScrollRect _scroll;
        private ChronicleViewFilter _filter = ChronicleViewFilter.All;

        public ChronicleViewFilter ViewFilter
        {
            get => _filter;
            set
            {
                _filter = value;
                RebuildDisplay();
            }
        }

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

        public void Add(in WorldEventRecord e) => PushRecord(e);

        public void Add(string line)
        {
            if (line == null) return;
            PushRecord(new WorldEventRecord
            {
                Tick = -1,
                Kind = WorldEventKind.System,
                Message = line
            });
        }

        /// <summary>读档时：以存档内编年史重绘，再追加一条系统提示；之后新事件走 <see cref="Add(in WorldEventRecord)"/>。</summary>
        public void RebuildFromChronicle(IReadOnlyList<WorldEventRecord> chronicle, string systemTail = null)
        {
            _records.Clear();
            if (chronicle != null)
            {
                for (var i = 0; i < chronicle.Count; i++)
                {
                    _records.Add(chronicle[i]);
                    while (_records.Count > _max) _records.RemoveAt(0);
                }
            }
            if (!string.IsNullOrEmpty(systemTail))
            {
                _records.Add(new WorldEventRecord { Tick = -1, Kind = WorldEventKind.System, Message = systemTail });
                while (_records.Count > _max) _records.RemoveAt(0);
            }
            RebuildDisplay();
        }

        private void PushRecord(in WorldEventRecord e)
        {
            _records.Add(e);
            while (_records.Count > _max) _records.RemoveAt(0);
            RebuildDisplay();
        }

        private void RebuildDisplay()
        {
            if (_text == null) return;
            var b = new StringBuilder();
            for (var i = 0; i < _records.Count; i++)
            {
                var e = _records[i];
                if (!ChronicleViewFilterUtil.IsKindVisible(e.Kind, _filter)) continue;
                b.AppendLine(WorldEventFormatting.ToDisplayString(e));
            }
            _text.text = b.ToString();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 0f;
        }
    }
}
