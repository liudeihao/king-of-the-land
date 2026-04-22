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
        [SerializeField] private SelectionManager _selection;
        [SerializeField] private CameraFollowSelection _cameraFollow;

        private Text _hud;
        private Text _detail;
        private Ape _current;
        private InputField _nameField;
        private Button _rain;
        private static Font _uiFont;
        private L1ModLoader.Result _mods;
        private Text _chronicleTitleText;

        public bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void Awake()
        {
            if (_world == null) _world = GetComponent<WorldManager>();
            if (_time == null) _time = GetComponent<TimeManager>();
            if (_eventLog == null) _eventLog = GetComponent<EventLog>();
            if (_selection == null) _selection = GetComponent<SelectionManager>();
        }

        public void SetLoadedMods(L1ModLoader.Result mods) => _mods = mods;

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
            _hud = MkText(canvas.transform, 16, new Vector2(12, -8), new Vector2(720, 96), new Vector2(0, 1), new Vector2(0, 1), TextAnchor.UpperLeft);
            _hud.text = "Tick";
            var p = new GameObject("InfoPanel");
            p.transform.SetParent(canvas.transform, false);
            var prt = p.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0.5f);
            prt.anchorMax = new Vector2(0, 0.5f);
            prt.pivot = new Vector2(0, 0.5f);
            prt.anchoredPosition = new Vector2(8, 0);
            prt.sizeDelta = new Vector2(280, 400);
            p.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            _detail = MkText(p.transform, 13, new Vector2(6, -8), new Vector2(260, 268), new Vector2(0, 1), new Vector2(0, 1), TextAnchor.UpperLeft);
            _detail.text = "选中：无";
            _nameField = MkInput(p.transform, new Vector2(6, -300), new Vector2(200, 28));
            _nameField.onEndEdit.AddListener(s =>
            {
                if (_current == null || _world == null) return;
                _world.SetNickname(_current.ApeId, s);
            });
            _rain = MkButton(canvas.transform, "降雨", new Vector2(0.5f, 1f), new Vector2(0, -32), new Vector2(100, 28));
            _rain.onClick.AddListener(() => _world?.ApplyRain());
            _rain.gameObject.SetActive(false);
            if (_cameraFollow == null && Camera.main != null) _cameraFollow = Camera.main.GetComponent<CameraFollowSelection>();
            var chTitle = MkText(canvas.transform, 12, new Vector2(-8, -8), new Vector2(360, 22), new Vector2(1, 1), new Vector2(1, 1), TextAnchor.UpperRight);
            chTitle.alignment = TextAnchor.UpperRight;
            var chRt = chTitle.GetComponent<RectTransform>();
            chRt.pivot = new Vector2(1, 1);
            _chronicleTitleText = chTitle;
            if (_eventLog != null)
            {
                var bAll = MkTinyTextButton(canvas.transform, "全部", new Vector2(1, 1), new Vector2(-8, -30));
                var bHi = MkTinyTextButton(canvas.transform, "要事", new Vector2(1, 1), new Vector2(-64, -30));
                bAll.onClick.AddListener(() =>
                {
                    if (_eventLog == null) return;
                    _eventLog.ViewFilter = ChronicleViewFilter.All;
                    UpdateChronicleTitle();
                });
                bHi.onClick.AddListener(() =>
                {
                    if (_eventLog == null) return;
                    _eventLog.ViewFilter = ChronicleViewFilter.Highlights;
                    UpdateChronicleTitle();
                });
            }
            UpdateChronicleTitle();
            var sc = new GameObject("EventScroll");
            sc.transform.SetParent(canvas.transform, false);
            var srt = sc.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(1, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.offsetMin = new Vector2(-300, 8);
            srt.offsetMax = new Vector2(-8, -40);
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
            var toastGo = new GameObject("MilestoneToast");
            toastGo.transform.SetParent(canvas.transform, false);
            var toastRt = toastGo.AddComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 1f);
            toastRt.anchorMax = new Vector2(0.5f, 1f);
            toastRt.pivot = new Vector2(0.5f, 1f);
            toastRt.anchoredPosition = new Vector2(0f, -36f);
            toastRt.sizeDelta = new Vector2(920f, 96f);
            var toastTx = toastGo.AddComponent<Text>();
            toastTx.font = _uiFont;
            toastTx.fontSize = 15;
            toastTx.alignment = TextAnchor.UpperCenter;
            toastTx.color = new Color(1f, 0.94f, 0.72f, 1f);
            toastTx.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastTx.verticalOverflow = VerticalWrapMode.Overflow;
            toastGo.AddComponent<MilestoneToast>().Bind(toastTx);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.H) && _eventLog != null && !IsPointerOverUi())
            {
                _eventLog.ViewFilter = _eventLog.ViewFilter == ChronicleViewFilter.All
                    ? ChronicleViewFilter.Highlights
                    : ChronicleViewFilter.All;
                UpdateChronicleTitle();
            }
            if (_hud == null || _time == null || _world == null || _world.Sim == null) return;
            var modLine = GetModHudLine();
            var followLine = GetCameraFollowLine();
            var pauseS = _time.Paused ? "  已暂停" : string.Empty;
            var popG = GetPopulationGeneticsHud();
            var s = _world.Sim;
            var setHud = s != null
                ? $"\n西岸聚落「{s.WestSettlementName}」  东岸聚落「{s.EastSettlementName}」"
                : string.Empty;
            _hud.text = $"Tick: {Tick()}{pauseS}\n倍速: {_time.TimeScale:0.#}x  [Space]暂停  [1][2][3]  [F5]存 [F9]读  [Tab]切选中  [V]随镜头  [H]编年史筛选\n{followLine}\n{modLine}\nseed:{_world.Sim.InitialSeed}  西:{_world.Sim.WaterLeft:0.00} 东:{_world.Sim.WaterRight:0.00}{popG}{setHud}";
            if (_current != null)
            {
                var st = _world.Sim.FindApe(_current.ApeId);
                if (st.HasValue)
                {
                    var a = st.Value;
                    _detail.text =
                        $"ID: {a.Id}\n" +
                        $"名字: (下方输入后回车)\n" +
                        $"性别: {(a.IsMale ? "雄" : "雌")}   生命阶段: {StageName(a.Stage)}\n" +
                        $"体型: {a.BodyScale:0.00}   勇气/好奇: {a.Courage:0.0#} / {a.Curiosity:0.0#}\n" +
                        $"遗传: 学习力 {a.GenLearn * 100f:0}％   体质 {a.GenVigor * 100f:0}％   社会性 {a.GenSocial * 100f:0}％\n" +
                        $"部族: 「{_world.Sim.GetSettlementNameForSide(a.Side)}」·{(a.Side == ApeSide.Left ? "西" : "东")}岸\n" +
                        $"亲缘: {ParentsText(a.ParentId0, a.ParentId1)}\n" +
                        $"饥饿: {a.Hunger * 100f:0}%   健康: {a.Health * 100f:0}%\n" +
                        $"压力: {a.Stress * 100f:0}% ({StressWord(a.Stress)})\n" +
                        $"地点记忆: {FoodMemoryLine(a.FoodMemoryStrength)}\n" +
                        $"同族印象: {PeerImpressionLine(a.PeerImpressionId, a.PeerImpressionStrength)}\n" +
                        $"文化: {CultureText.FormatLine(_world.Sim.CultureDefinitions, a.CultureSkillIds)}\n" +
                        CultureDetailBlock(a) +
                        $"年龄: {a.Age:0.0} 年   面: {(a.Side == ApeSide.Left ? "西" : "东")}\n" +
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

        private string GetCameraFollowLine()
        {
            if (_cameraFollow == null && Camera.main != null) _cameraFollow = Camera.main.GetComponent<CameraFollowSelection>();
            if (_cameraFollow == null) return "镜头: —";
            if (!_cameraFollow.IsFollowing) return "镜头: 自由（[V] 开启随选中）";
            var sel = _selection != null ? _selection : GetComponent<SelectionManager>();
            if (sel == null || sel.Selected == null) return "镜头: 跟随开—点选一只猿以「正在追踪」";
            return "镜头: 正在追踪";
        }

        private void UpdateChronicleTitle()
        {
            if (_chronicleTitleText == null) return;
            var f = _eventLog != null ? _eventLog.ViewFilter : ChronicleViewFilter.All;
            var hint = f == ChronicleViewFilter.Highlights ? "不列猎食/传艺" : "全列";
            _chronicleTitleText.text = $"编年史（{ChronicleViewFilterUtil.Label(f)}，{hint}，最新在底）";
        }

        private Button MkTinyTextButton(Transform p, string label, Vector2 a, Vector2 ap)
        {
            var o = new GameObject("ChFilterBtn");
            o.transform.SetParent(p, false);
            var r = o.AddComponent<RectTransform>();
            r.anchorMin = a;
            r.anchorMax = a;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = ap;
            r.sizeDelta = new Vector2(50, 22);
            o.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.9f);
            var b = o.AddComponent<Button>();
            var txo = new GameObject("L");
            txo.transform.SetParent(o.transform, false);
            var tr = txo.AddComponent<RectTransform>();
            Stretch(tr);
            var tx = txo.AddComponent<Text>();
            tx.font = _uiFont;
            tx.text = label;
            tx.fontSize = 11;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = new Color(0.9f, 0.9f, 0.85f, 1f);
            return b;
        }

        private string GetPopulationGeneticsHud()
        {
            if (_world?.Sim == null) return string.Empty;
            var list = _world.Sim.GetApeStates();
            var n = 0;
            double sL = 0, sV = 0, sS = 0;
            for (var i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (!a.Alive) continue;
                n++;
                sL += a.GenLearn;
                sV += a.GenVigor;
                sS += a.GenSocial;
            }
            if (n == 0) return string.Empty;
            return $"  |  存活{n}  群均遗传 学{(sL / n) * 100:0}%·体{(sV / n) * 100:0}%·社{(sS / n) * 100:0}%";
        }

        private string GetModHudLine()
        {
            if (_mods == null) return "L1 Mod: 未初始化";
            if (!_mods.Success && _mods.Errors != null && _mods.Errors.Count > 0)
                return "L1 错误: " + _mods.Errors[0] + ( _mods.Errors.Count > 1 ? $" (共{_mods.Errors.Count}条，见Console)" : "");
            if (_mods.ModDisplayNames == null || _mods.ModDisplayNames.Count == 0)
                return "L1 Mod: 无 (Mods 下无 mod.json 且含 sim_params 的包)";
            return "L1: " + string.Join(" -> ", _mods.ModDisplayNames);
        }

        private static string StressWord(float s)
        {
            if (s < 0.25f) return "平静";
            if (s < 0.5f) return "略紧";
            if (s < 0.75f) return "偏紧";
            return "高压力";
        }

        private static string FoodMemoryLine(float m)
        {
            if (m < 0.06f) return "无";
            if (m < 0.35f) return $"淡薄 ({m * 100f:0}%)";
            if (m < 0.7f) return $"清楚 ({m * 100f:0}%)";
            return $"清晰 ({m * 100f:0}%)";
        }

        private static string PeerImpressionLine(int id, float s)
        {
            if (s < 0.06f) return "无";
            return id >= 0 ? $"ID {id}（{s * 100f:0}%）" : "无";
        }

        private string CultureDetailBlock(ApeState a)
        {
            var block = CultureText.FormatSkillDescriptionsBlock(_world.Sim.CultureDefinitions, a.CultureSkillIds);
            if (string.IsNullOrEmpty(block)) return string.Empty;
            return "技艺说明:\n" + block + "\n";
        }

        private static string StageName(LifeStage s) => s switch
        {
            LifeStage.Infant => "婴儿",
            LifeStage.Child => "幼年",
            LifeStage.Youth => "青年",
            LifeStage.Adult => "成年",
            LifeStage.Elder => "老年",
            _ => s.ToString()
        };

        private static string ParentsText(int a, int b)
        {
            if (a < 0 && b < 0) return "无";
            if (a >= 0 && b >= 0) return $"ID {a} & {b}";
            return a >= 0 ? $"ID {a}" : $"ID {b}";
        }

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
            bg.transform.SetParent(o.transform, false);
            var br = bg.AddComponent<RectTransform>();
            Stretch(br);
            bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            f.targetGraphic = bg.GetComponent<Image>();
            var to = new GameObject("Text");
            to.transform.SetParent(o.transform, false);
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
            txo.transform.SetParent(o.transform, false);
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
