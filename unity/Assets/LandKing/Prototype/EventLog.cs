using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LandKing.Prototype
{
    /// <summary>右侧滚动事件记录。对应原型 第七步.</summary>
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

        public void Add(string line)
        {
            if (line == null) return;
            _lines.Add(line);
            while (_lines.Count > _max) _lines.RemoveAt(0);
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
