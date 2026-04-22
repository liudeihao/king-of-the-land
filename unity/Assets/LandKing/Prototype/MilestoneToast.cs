using UnityEngine;
using UnityEngine.UI;

namespace LandKing.Prototype
{
    /// <summary>屏幕顶部高亮，展示本局「首次发现」类叙事，不与通关挂钩。</summary>
    public sealed class MilestoneToast : MonoBehaviour
    {
        public static MilestoneToast Instance { get; private set; }
        [SerializeField] private Text _text;
        private float _remain;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Bind(Text t) => _text = t;

        public void Show(string message)
        {
            if (_text == null) return;
            _text.text = message ?? string.Empty;
            _remain = 9f;
        }

        private void Update()
        {
            if (_text == null) return;
            if (_remain <= 0f)
            {
                if (!string.IsNullOrEmpty(_text.text)) _text.text = string.Empty;
                return;
            }
            _remain -= Time.deltaTime;
        }
    }
}
