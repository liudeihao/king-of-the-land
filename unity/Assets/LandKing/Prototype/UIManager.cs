using LandKing.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LandKing.Prototype
{
    /// <summary>HUD：tick、倍速、选中、命名、降雨、右侧事件列表。</summary>
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private WorldManager _world;
        [SerializeField] private TimeManager _time;
        [SerializeField] private EventLog _eventLog;

        private Text _hud;
        private Text _detail;
        private Ape _current;
        private InputField _nameField;
        private Button _rain;
        private static Font _uiFont;

        public bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void Awake()
        {
            if (_world == null) _world = GetComponent<WorldManager>();
            if (_time == null) _time = GetComponent<TimeManager>();
            if (_eventLog == null) _eventLog = GetComponent<EventLog>();
        }

        public void CreateUi(Transform mainRoot)
        {
            _uiFont ??= Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            var canvas = new GameObject("Canvas");
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.AddComponent<GraphicRaycaster>();
            canvas.transform.SetParent(mainRoot, false);
            _hud = MkText(canvas.transform, 16, new Vector2(12, -8), new Vector2(500, 36), new Vector2(0, 1), new Vector2(0, 1), TextAnchor.UpperLeft);
            _hud.text = "Tick";
            var p = new GameObject("InfoPanel");
            p.transform.SetParent(canvas.transform, false);
            var prt = p.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0.5f);
            prt.anchorMax = new Vector2(0, 0.5f);
            prt.pivot = new Vector2(0, 0.5f);
            prt.anchoredPosition = new Vector2(8, 0);
            prt.sizeDelta = new Vector2(240, 200);
            p.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            _detail = MkText(p.transform, 13, new Vector2(6, -8), new Vector2(220, 130), new Vector2(0, 1), new Vector2(0, 1), TextAnchor.UpperLeft);
            _detail.text = "选中：无";
            _nameField = MkInput(p.transform, new Vector2(6, -145), new Vector2(200, 28));
            _nameField.onEndEdit.AddListener(s =>
            {
                if (_current == null || _world == null) return;
                _world.SetNickname(_current.ApeId, s);
            });
            _rain = MkButton(canvas.transform, "降雨", new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(100, 28));
            _rain.onClick.AddListener(() => _world?.ApplyRain());
            _rain.gameObject.SetActive(false);
            var sc = new GameObject("EventScroll");
            sc.transform.SetParent(canvas.transform, false);
            var srt = sc.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(1, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.offsetMin = new Vector2(-300, 8);
            srt.offsetMax = new Vector2(-8, -8);
            var scr = sc.AddComponent<ScrollRect>();
            var vp = new GameObject("Viewport");
            vp.transform.SetParent(sc.transform, false);
            var vrt = vp.AddComponent<RectTransform>();
            Stretch(vrt);
            vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            var mask = vp.AddComponent<RectMask2D>();
            var ct = new GameObject("Content");
            ct.transform.SetParent(vp.transform, false);
            var ctr = ct.AddComponent<RectTransform>();
            ctr.anchorMin = new Vector2(0, 1);
            ctr.anchorMax = new Vector2(1, 1);
            ctr.pivot = new Vector2(0, 1);
            ctr.anchoredPosition = Vector2.zero;
            var logText = ct.AddComponent<Text>();
            logText.font = _uiFont;
            logText.fontSize = 12;
            logText.color = Color.white;
            logText.alignment = TextAnchor.UpperLeft;
            var fit = ct.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scr.content = ctr;
            scr.viewport = vrt;
            scr.vertical = true;
            if (_eventLog != null) _eventLog.Init(logText, scr);
        }

        private void Update()
        {
            if (_hud == null || _time == null || _world == null || _world.Sim == null) return;
            _hud.text = $"Tick: {Tick()}\n倍速: {_time.TimeScale:0.#}x  [Space]暂停  [1][2][3]倍速\n水位 西:{_world.Sim.WaterLeft:0.00} 东:{_world.Sim.WaterRight:0.00}";
            if (_current != null)
            {
                var st = _world.Sim.FindApe(_current.ApeId);
                if (st.HasValue)
                {
                    var a = st.Value;
                    _detail.text =
                        $"ID: {a.Id}\n" +
                        $"名字: (下方输入后回车)\n" +
                        $"饥饿: {a.Hunger * 100f:0}%\n" +
                        $"健康: {a.Health * 100f:0}%\n" +
                        $"年龄: {a.Age:0.0} 年\n" +
                        $"面: {(a.Side == ApeSide.Left ? "西" : "东")}\n" +
                        (a.Alive
                            ? (a.Hunger < 0.7f ? "状态: 觅食" : "状态: 游荡")
                            : "状态: 死亡");
                }
            }
            if (_rain != null) _rain.gameObject.SetActive(_world.Sim != null && _world.Sim.CanShowRain);
        }

        public void SetSelected(Ape ape)
        {
            _current = ape;
            if (ape == null)
            {
                if (_nameField) _nameField.SetTextWithoutNotify(string.Empty);
                if (_detail) _detail.text = "选中：无";
                return;
            }
            if (_world == null) return;
            var st = _world.Sim.FindApe(ape.ApeId);
            var nick = st.HasValue ? st.Value.Nickname : string.Empty;
            if (_nameField) _nameField.SetTextWithoutNotify(nick);
        }

        private int Tick() => _time.TickCount;

        private Text MkText(Transform p, int size, Vector2 ap, Vector2 wh, Vector2 aMin, Vector2 aMax, TextAnchor align)
        {
            var t = new GameObject("T");
            t.transform.SetParent(p, false);
            var r = t.AddComponent<RectTransform>();
            r.anchorMin = aMin;
            r.anchorMax = aMax;
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = ap;
            r.sizeDelta = wh;
            var tx = t.AddComponent<Text>();
            tx.font = _uiFont;
            tx.fontSize = size;
            tx.alignment = align;
            tx.color = Color.white;
            return tx;
        }

        private InputField MkInput(Transform p, Vector2 ap, Vector2 wh)
        {
            var o = new GameObject("In");
            o.transform.SetParent(p, false);
            var r = o.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = ap;
            r.sizeDelta = wh;
            var f = o.AddComponent<InputField>();
            var bg = new GameObject("B");
            bg.transform.SetParent(o, false);
            var br = bg.AddComponent<RectTransform>();
            Stretch(br);
            bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            f.targetGraphic = bg.GetComponent<Image>();
            var to = new GameObject("Text");
            to.transform.SetParent(o, false);
            var tr = to.AddComponent<RectTransform>();
            Stretch(tr);
            tr.offsetMin = new Vector2(4, 2);
            tr.offsetMax = new Vector2(-4, -2);
            var t = to.AddComponent<Text>();
            t.font = _uiFont;
            t.fontSize = 13;
            t.color = Color.white;
            t.supportRichText = false;
            t.alignment = TextAnchor.MiddleLeft;
            f.textComponent = t;
            return f;
        }

        private Button MkButton(Transform p, string label, Vector2 a, Vector2 ap, Vector2 wh)
        {
            var o = new GameObject("Btn");
            o.transform.SetParent(p, false);
            var r = o.AddComponent<RectTransform>();
            r.anchorMin = a;
            r.anchorMax = a;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = ap;
            r.sizeDelta = wh;
            o.AddComponent<Image>().color = new Color(0.1f, 0.2f, 0.4f, 0.95f);
            var b = o.AddComponent<Button>();
            var txo = new GameObject("L");
            txo.transform.SetParent(o, false);
            var tr = txo.AddComponent<RectTransform>();
            Stretch(tr);
            var tx = txo.AddComponent<Text>();
            tx.font = _uiFont;
            tx.text = label;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            return b;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero;
            r.anchoredPosition = Vector2.zero;
        }
    }
}
