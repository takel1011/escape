// 画面（仮の UI）。Unity の IMGUI (OnGUI) で書いているので、シーンに何も置かなくても動く。
// 本番の見た目にするときは UI Toolkit か uGUI + TextMeshPro に作り直す。ルールは Game クラスにあるので UI だけ差し替えればよい。
using System.Collections.Generic;
using System.Linq;
using OmiseYasan.Core;
using UnityEngine;

namespace OmiseYasan
{
    public class GameUI : MonoBehaviour
    {
        GameController gc;
        Vector2 scroll;
        Page lastPage;
        string growTab = "menu";
        bool confirmNew;

        // スタイル
        GUIStyle panel, h1, h2, h3, text, small, card, btn, btnPrimary, btnSelected, status, statusBig, bubble, good, bad, meh, tabOn, tabOff, field;
        readonly Dictionary<Color, Texture2D> texCache = new Dictionary<Color, Texture2D>();
        const float LogicalWidth = 520f;

        public void Bind(GameController controller) { gc = controller; }

        Texture2D Tex(Color c)
        {
            Texture2D t;
            if (texCache.TryGetValue(c, out t) && t != null) return t;
            t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply();
            texCache[c] = t;
            return t;
        }

        GUIStyle Box(Color bg, Color fg, int size, FontStyle fs = FontStyle.Normal, int pad = 10)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = size, fontStyle = fs, wordWrap = true, richText = true,
                padding = new RectOffset(pad, pad, pad, pad), margin = new RectOffset(0, 0, 4, 4)
            };
            s.normal.background = bg.a > 0 ? Tex(bg) : null;
            s.normal.textColor = fg;
            return s;
        }

        GUIStyle Button(Color bg, Color fg, Color hover, int size = 17)
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                fontSize = size, fontStyle = FontStyle.Bold, wordWrap = true, richText = true, alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 10, 10), margin = new RectOffset(0, 0, 4, 4), border = new RectOffset(0, 0, 0, 0)
            };
            s.normal.background = Tex(bg); s.normal.textColor = fg;
            s.hover.background = Tex(hover); s.hover.textColor = fg;
            s.active.background = Tex(Color.Lerp(hover, Color.black, 0.15f)); s.active.textColor = fg;
            s.focused.background = Tex(bg); s.focused.textColor = fg;
            return s;
        }

        void MakeStyles()
        {
            if (panel != null) return;
            var clear = new Color(0, 0, 0, 0);
            panel = Box(Look.Paper, Look.Ink, 16, FontStyle.Normal, 16);
            h1 = Box(clear, Look.Ink, 28, FontStyle.Bold, 2);
            h2 = Box(clear, Look.Ink, 21, FontStyle.Bold, 2);
            h3 = Box(clear, Look.Ink, 17, FontStyle.Bold, 2);
            text = Box(clear, Look.Ink, 16, FontStyle.Normal, 2);
            small = Box(clear, Look.Muted, 13, FontStyle.Normal, 2);
            card = Box(Color.white, Look.Ink, 15, FontStyle.Normal, 10);
            good = Box(Look.Hex("#E2F4E8"), Look.Ink, 15, FontStyle.Normal, 12);
            bad = Box(Look.Hex("#FBE6E1"), Look.Ink, 15, FontStyle.Normal, 12);
            meh = Box(Look.Hex("#FDF2CC"), Look.Ink, 15, FontStyle.Normal, 12);
            status = Box(Look.Ai, Look.Paper, 14, FontStyle.Bold, 10);
            statusBig = Box(clear, Look.Paper, 20, FontStyle.Bold, 0);
            statusBig.alignment = TextAnchor.MiddleCenter;
            bubble = Box(Color.white, Look.Ink, 18, FontStyle.Bold, 10);
            bubble.alignment = TextAnchor.MiddleCenter;
            btn = Button(Color.white, Look.Ink, Look.Hex("#EEF1F5"));
            btn.alignment = TextAnchor.MiddleLeft;
            btnPrimary = Button(Look.Lantern, Look.Ink, Look.Hex("#FFC933"), 19);
            btnSelected = Button(Look.Ai, Look.Paper, Look.Hex("#2E4682"));
            btnSelected.alignment = TextAnchor.MiddleLeft;
            tabOn = Button(Look.Ai, Look.Paper, Look.Hex("#2E4682"), 15);
            tabOff = Button(Color.white, Look.Ink, Look.Hex("#EEF1F5"), 15);
            field = new GUIStyle(GUI.skin.textField) { fontSize = 22, padding = new RectOffset(10, 10, 10, 10) };
        }

        void OnGUI()
        {
            if (gc == null) return;
            MakeStyles();
            if (gc.Page != lastPage) { scroll = Vector2.zero; lastPage = gc.Page; }

            float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;
            bool landscape = W >= H;
            Rect px = landscape ? new Rect(W * 0.58f, 0, W * 0.42f, H) : new Rect(0, H * 0.5f, W, H * 0.5f);
            float s = Mathf.Clamp(px.width / LogicalWidth, 0.6f, 3f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));

            DrawBubble(s);

            var area = new Rect(px.x / s, px.y / s, px.width / s, px.height / s);
            GUI.Box(area, GUIContent.none, panel);
            GUILayout.BeginArea(new Rect(area.x + 14, area.y + 10, area.width - 28, area.height - 20));
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            switch (gc.Page)
            {
                case Page.Title: Title(); break;
                case Page.PickShop: PickShop(); break;
                case Page.PickLoc: PickLoc(); break;
                case Page.PickName: PickName(); break;
                case Page.Serve: Serve(); break;
                case Page.Report: ReportPage(); break;
                case Page.Grow: Grow(); break;
                case Page.Win: Win(); break;
            }
            GUILayout.Space(20);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ---------- 3D の上のふきだし ----------
        void DrawBubble(float s)
        {
            if (gc.Page != Page.Serve || gc.Game == null || gc.Game.Current == null) return;
            var anchor = gc.Crowd.BubbleAnchor();
            if (!anchor.HasValue) return;
            var sp = gc.Cam.WorldToScreenPoint(anchor.Value);
            if (sp.z < 0) return;
            var c = gc.Game.Current;
            string line = gc.Result != null ? gc.Result.Line : gc.Heard ? c.Say2 : c.Say1;
            var content = new GUIContent(line);
            float w = Mathf.Min(300, bubble.CalcSize(content).x + 4);
            float h = bubble.CalcHeight(content, w);
            float x = sp.x / s - w / 2, y = (UnityEngine.Screen.height - sp.y) / s - h - 8;
            var st = new GUIStyle(bubble);
            if (gc.Result != null) st.normal.background = Tex(gc.Result.Delighted ? Look.Hex("#E2F4E8") : !gc.Result.Bought ? Look.Hex("#FBE6E1") : Color.white);
            GUI.Box(new Rect(x - 2, y - 2, w + 4, h + 4), GUIContent.none, Box(Look.Ink, Look.Ink, 1, FontStyle.Normal, 0));
            GUI.Label(new Rect(x, y, w, h), content, st);
        }

        // ---------- 共通 ----------
        void StatusBar(string time)
        {
            var g = gc.Game;
            GUILayout.BeginHorizontal(status);
            GUILayout.Label(string.Format("{0}日目\n{1}・{2}", g.S.Day, g.Stage.Name, g.Loc.Name), Box(new Color(0, 0, 0, 0), Look.Paper, 13, FontStyle.Bold, 0), GUILayout.Width(130));
            GUILayout.Label(time, statusBig, GUILayout.ExpandWidth(true));
            var right = Box(new Color(0, 0, 0, 0), Look.Paper, 17, FontStyle.Bold, 0);
            right.alignment = TextAnchor.MiddleRight;
            GUILayout.Label(string.Format("{0}\n<color=#F4B400><size=13>{1}</size></color>", Game.Yen(g.S.Money), Game.Stars(g.S.Rep)), right, GUILayout.Width(140));
            GUILayout.EndHorizontal();
        }

        bool Btn(string label, GUIStyle style = null, params GUILayoutOption[] opts) { return GUILayout.Button(label, style ?? btn, opts); }

        void Row(string k, string v)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(k, text);
            GUILayout.FlexibleSpace();
            GUILayout.Label(v, h3);
            GUILayout.EndHorizontal();
        }

        // ---------- タイトル ----------
        void Title()
        {
            GUILayout.Label("おみせやさん物語", h1);
            GUILayout.Label("お客さんのようすとつぶやきから「ほしいもの」を読みとって、ぴったりの商品と値段をすすめよう。屋台からはじめて、めざせチェーン店！", text);
            GUILayout.Space(6);
            GUILayout.Label("<b>お店を開く</b>　ラーメン屋・美容院・ケーキ屋を、駅前・住宅街・学生街に。\n<b>お客さんを読む</b>　時間、人数、服装、持ち物、つぶやき。\n<b>すすめて値段を決める</b>　ぴったりなら「また来るね！」。\n<b>お店を育てる</b>　新商品・人を雇う・お店を広げる。", card);
            GUILayout.Space(8);
            var save = gc.PeekSave();
            if (save != null && Btn(string.Format("つづきから（{0}・{1}日目）", save.ShopName, save.Day), btnPrimary)) gc.Continue();
            if (Btn(save != null ? "はじめから" : "お店をはじめる", save != null ? btn : btnPrimary))
            {
                if (save != null) confirmNew = true; else gc.Page = Page.PickShop;
            }
            if (confirmNew)
            {
                GUILayout.Label("いまのお店のデータは消えます。はじめからにしますか？", bad);
                GUILayout.BeginHorizontal();
                if (Btn("はじめから遊ぶ", btnPrimary)) { gc.DeleteSave(); confirmNew = false; gc.Page = Page.PickShop; }
                if (Btn("やめる")) confirmNew = false;
                GUILayout.EndHorizontal();
            }
        }

        // ---------- 開店準備 ----------
        void PickShop()
        {
            GUILayout.Label("1 / 3　お店えらび", small);
            GUILayout.Label("どんなお店を開く？", h2);
            foreach (var sh in GameData.Shops.Values)
            {
                string starters = string.Join("・", sh.Products.Where(p => p.IsStarter).Select(p => p.Name));
                if (Btn(string.Format("<size=20>{0}</size>\n<size=14>{1}\n営業 {2}時〜{3}時 ／ はじめの商品：{4}</size>", sh.Name, sh.Desc, sh.OpenHour, sh.CloseHour, starters)))
                {
                    gc.PickedShop = sh.Key;
                    gc.PickedName = sh.DefaultName;
                    gc.View.Build(sh, 0, sh.DefaultName, gc.Cam);
                    gc.Page = Page.PickLoc;
                }
            }
            if (Btn("もどる")) gc.Page = Page.Title;
        }

        void PickLoc()
        {
            var sh = GameData.Shops[gc.PickedShop];
            GUILayout.Label("2 / 3　場所えらび", small);
            GUILayout.Label(sh.Name + "をどこに出す？", h2);
            foreach (var l in GameData.Locations.Values)
            {
                var top = GameData.Archetypes.Values.OrderByDescending(a => a.Weights[l.Index]).Take(3).Select(a => a.Name);
                int rent = Mathf.RoundToInt(l.Rent * sh.Scale * GameData.Stages[0].RentMul / 10f) * 10;
                string purse = l.Budget > 1.05f ? "多め" : l.Budget < 0.95f ? "少なめ" : "ふつう";
                string traffic = new string('●', l.Traffic - 3) + new string('○', 7 - l.Traffic);
                if (Btn(string.Format("<size=20>{0}</size>\n<size=14>{1}\n家賃 {2}/日　人通り {3}　お財布 {4}\nよく来る：{5}</size>",
                        l.Name, l.Desc, Game.Yen(rent), traffic, purse, string.Join("・", top))))
                {
                    gc.PickedLoc = l.Key;
                    gc.Page = Page.PickName;
                }
            }
            if (Btn("もどる")) gc.Page = Page.PickShop;
        }

        void PickName()
        {
            var sh = GameData.Shops[gc.PickedShop];
            GUILayout.Label("3 / 3　お店の名前", small);
            GUILayout.Label("お店の名前をつけよう", h2);
            GUILayout.Label("のれんに書く名前です。そのままでもOK。", small);
            GUI.SetNextControlName("shopname");
            gc.PickedName = GUILayout.TextField(gc.PickedName ?? "", 14, field);
            GUILayout.Label(string.Format("{0} ／ {1} ／ 開店資金 {2}", sh.Name, GameData.Locations[gc.PickedLoc].Name, Game.Yen(Mathf.RoundToInt(10000 * sh.Scale))), card);
            if (Btn("開店する！", btnPrimary)) gc.StartNew();
            if (Btn("もどる")) gc.Page = Page.PickLoc;
        }

        // ---------- 接客 ----------
        void Serve()
        {
            var g = gc.Game;
            var c = g.Current;
            var a = GameData.Archetypes[c.ArchKey];
            var r = gc.Result;
            StatusBar(c.TimeText);

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("お客さん {0} / {1}組目", g.S.Today.Index + 1, g.S.Today.Customers.Count), small);
            GUILayout.FlexibleSpace();
            GUILayout.Label("常連さん " + g.S.Regulars.Count + "人", small);
            GUILayout.EndHorizontal();

            if (g.S.Day == 1 && g.S.Today.Index == 0 && r == null)
                GUILayout.Label("<b>はじめてのお客さん</b>\n夜9時、スーツで1人、「はぁ…疲れた」。手がかりを組み合わせると…？ メニューの「元気★」「すぐ★」などを見て、合いそうな商品を選ぼう。", meh);

            var reg = g.RegularOf(c);
            if (reg != null)
                GUILayout.Label(string.Format("常連の<b>{0}さん</b>（{1}回目の来店）。前回は「{2}」を {3} で気に入ってくれた。",
                    reg.Name, reg.Visits + 1, g.ProductById(reg.FavoriteId).Name, Game.Yen(reg.LastPrice)), good);

            // 手がかり
            string members = string.Join("・", c.Members.GroupBy(m => m).Select(x => BodyName(x.Key) + (x.Count() > 1 ? "×" + x.Count() : "")));
            string says = "「" + c.Say1 + "」" + (gc.Heard ? "\n「" + c.Say2 + "」" : "");
            GUILayout.Label(string.Format("<b>来た時間</b>　{0}\n<b>人数</b>　{1}人（{2}）\n<b>服装</b>　{3}\n<b>持ち物</b>　{4}\n<b>つぶやき</b>　{5}",
                c.TimeText, c.Size, members, c.Look, c.Item, says), card);

            if (r == null)
            {
                GUILayout.BeginHorizontal();
                GUI.enabled = !gc.Heard;
                if (Btn("耳をすます", btn)) gc.Heard = true;
                GUI.enabled = g.S.Today.Hints > 0 && !gc.HintShown;
                if (Btn("ヒント（のこり" + g.S.Today.Hints + "）", btn)) gc.UseHint();
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (gc.HintShown)
                    GUILayout.Label("ほしいもの：" + string.Join("・", a.Needs.Select(NeedInfo.Label)), meh);
                MenuAndPrice();
            }
            else
            {
                var st = r.Delighted ? good : !r.Bought ? bad : meh;
                string money = r.Sales > 0 ? string.Format("\n売上 {0}（{1}{2}）　儲け <b>{3}</b>", Game.Yen(r.Sales), r.Qty, g.Shop.Unit, Game.Yen(r.Profit)) : "";
                GUILayout.Label(string.Format("<size=20><b>{0}</b></size>{1}\n{2}", r.Title, money, r.Why()), st);
                int left = g.CustomersLeft;
                if (Btn(left > 0 ? string.Format("次のお客さん（あと{0}組）", left) : "今日はおしまい。お店を閉める", btnPrimary)) gc.Next();
            }
        }

        static string BodyName(Body b)
        {
            switch (b)
            {
                case Body.Child: return "子ども";
                case Body.Elder: return "お年寄り";
                case Body.Baby: return "赤ちゃん";
                default: return "大人";
            }
        }

        void MenuAndPrice()
        {
            var g = gc.Game;
            GUILayout.Label("なにをすすめる？", h3);
            var menu = g.Menu;
            for (int i = 0; i < menu.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + 2, menu.Count); j++)
                {
                    var p = menu[j];
                    string label = string.Format("{0}{1}\n<size=13>{2}</size>", p.Name, p.IsShare ? "（グループで1つ）" : "", p.FitText());
                    if (Btn(label, gc.SelectedId == p.Id ? btnSelected : btn, GUILayout.Width(236), GUILayout.MinHeight(58))) gc.Select(p.Id);
                }
                GUILayout.EndHorizontal();
            }
            if (gc.SelectedId == null) return;

            var sel = g.ProductById(gc.SelectedId);
            int step = Game.PriceStep(sel.Base);
            int qty = sel.IsShare ? 1 : g.Current.Payers;
            int margin = gc.Price - sel.Cost;
            GUILayout.BeginVertical(card);
            GUILayout.BeginHorizontal();
            GUILayout.Label(sel.Name, h3);
            GUILayout.FlexibleSpace();
            GUILayout.Label("相場 " + Game.Yen(sel.Base), small);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Btn("−", btn, GUILayout.Width(60), GUILayout.Height(52))) gc.Price = Mathf.Max(step, gc.Price - step);
            var priceStyle = Box(new Color(0, 0, 0, 0), Look.Ink, 34, FontStyle.Bold, 0);
            priceStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(Game.Yen(gc.Price), priceStyle, GUILayout.Height(52), GUILayout.ExpandWidth(true));
            if (Btn("＋", btn, GUILayout.Width(60), GUILayout.Height(52))) gc.Price += step;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Btn("少し安く", tabOff)) gc.Price = Game.RoundTo(sel.Base * 0.85f, step);
            if (Btn("相場", tabOff)) gc.Price = sel.Base;
            if (Btn("少し高く", tabOff)) gc.Price = Game.RoundTo(sel.Base * 1.2f, step);
            GUILayout.EndHorizontal();

            string mc = margin >= 0 ? "#23824A" : "#BE3B26";
            GUILayout.Label(string.Format("仕入れ値 {0}　／　1{1}の儲け <color={2}><b>{3}</b></color>　／　{4}{1}で <color={2}><b>{5}</b></color>",
                Game.Yen(sel.Cost), g.Shop.Unit, mc, Game.Yen(margin), qty, Game.Yen(margin * qty)), small);
            if (Btn("この値段ですすめる", btnPrimary)) gc.Recommend();
            GUILayout.EndVertical();
        }

        // ---------- 決算 ----------
        void ReportPage()
        {
            var g = gc.Game; var R = gc.Report; var L = R.Log;
            StatusBar("閉店");
            GUILayout.Label(R.Day + "日目の決算", h2);
            int total = L.Happy + L.Left + L.Grudging;
            GUILayout.Label(string.Format("満足したお客さん {0}/{1}　　新しい常連さん {2}人　　評判 <color=#C99400>{3}</color>", L.Happy, total, L.NewRegulars, Game.Stars(g.S.Rep)), card);
            GUILayout.BeginVertical(card);
            Row("売上", Game.Yen(L.Sales));
            Row("仕入れ（原価）", "−" + Game.Yen(L.Cost));
            Row(string.Format("家賃（{0}・{1}）", g.Stage.Name, g.Loc.Name), "−" + Game.Yen(R.Rent));
            if (R.Wages > 0) Row(string.Format("お給料（スタッフ{0}人）", g.S.Staff), "−" + Game.Yen(R.Wages));
            if (R.StaffProfit > 0) Row(string.Format("スタッフの接客（{0}組）", R.StaffCustomers), "+" + Game.Yen(R.StaffProfit));
            if (R.Branch > 0) Row("支店からの利益", "+" + Game.Yen(R.Branch));
            string pc = R.Profit >= 0 ? "#23824A" : "#BE3B26";
            Row("今日の利益", string.Format("<color={0}>{1}{2}</color>", pc, R.Profit >= 0 ? "+" : "", Game.Yen(R.Profit)));
            GUILayout.EndVertical();

            GUILayout.Label("<b>店長メモ</b>\n・" + string.Join("\n・", g.Tips(R)), meh);
            if (g.S.Money < 0) GUILayout.Label("お金がマイナスです。家賃の安い場所や、儲けの出る値段を考えてみよう。（このまま続けられます）", bad);
            if (Btn("お店を育てる", btnPrimary)) gc.Page = Page.Grow;
        }

        // ---------- お店を育てる ----------
        void Grow()
        {
            var g = gc.Game;
            StatusBar("開店前");
            GUILayout.Label("お店を育てる", h2);
            GUILayout.BeginHorizontal();
            if (Btn("商品をつくる", growTab == "menu" ? tabOn : tabOff)) growTab = "menu";
            if (Btn("人を雇う", growTab == "staff" ? tabOn : tabOff)) growTab = "staff";
            if (Btn("店を広げる", growTab == "expand" ? tabOn : tabOff)) growTab = "expand";
            GUILayout.EndHorizontal();

            if (growTab == "menu")
            {
                GUILayout.Label("新しい商品があると、いろんなお客さんにぴったりのものをすすめられる。", small);
                var locked = g.Locked;
                if (locked.Count == 0) GUILayout.Label("全部の商品がそろいました！", card);
                foreach (var p in locked)
                {
                    GUILayout.BeginHorizontal(card);
                    GUILayout.Label(string.Format("<b>{0}</b>\n<size=13>{1}　相場 {2}・仕入れ {3}{4}</size>", p.Name, p.FitText(), Game.Yen(p.Base), Game.Yen(p.Cost), p.IsShare ? "・グループで1つ" : ""), text);
                    GUI.enabled = g.S.Money >= p.DevCost;
                    if (Btn("開発 " + Game.Yen(p.DevCost), tabOff, GUILayout.Width(130))) gc.Develop(p.Id);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
                GUILayout.Label("いまのメニュー：" + string.Join("、", g.Menu.Select(p => p.Name)), small);
            }
            else if (growTab == "staff")
            {
                GUILayout.Label("スタッフは、あなたが接客していないお客さんを毎日接客してくれる。評判が上がるほど、たくさん接客してくれる。お給料は毎日かかる。", small);
                GUILayout.BeginHorizontal(card);
                GUILayout.Label(string.Format("<b>アルバイト</b>\n<size=13>お給料 {0}/日・1人で {1}組くらい接客</size>", Game.Yen(g.Wage), g.StaffServes), text);
                GUI.enabled = g.S.Staff > 0;
                if (Btn("−", tabOff, GUILayout.Width(48))) gc.Hire(-1);
                GUI.enabled = true;
                GUILayout.Label(string.Format("{0} / {1}人", g.S.Staff, g.Stage.StaffMax), h3, GUILayout.Width(70));
                GUI.enabled = g.S.Staff < g.Stage.StaffMax;
                if (Btn("＋", tabOff, GUILayout.Width(48))) gc.Hire(1);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (g.Stage.StaffMax == 0) GUILayout.Label("屋台はせまくて人を雇えない。「店を広げる」で小さなお店にしよう。", meh);
            }
            else
            {
                GUILayout.Label("お店を広げると、お客さんが増えて、少し高くても買ってもらえる。でも家賃も上がる。", small);
                for (int i = 0; i < GameData.Stages.Length; i++)
                {
                    var st = GameData.Stages[i];
                    GUILayout.BeginHorizontal(i == g.S.Stage ? good : card);
                    string mark = i < g.S.Stage ? "済" : i == g.S.Stage ? "いまここ" : "";
                    GUILayout.Label(string.Format("<b>{0}</b>　<size=13>{1}</size>\n<size=13>{2} 家賃 {3}/日</size>", st.Name, mark, st.Desc, Game.Yen(g.RentFor(st))), text);
                    if (i == g.S.Stage + 1)
                    {
                        GUI.enabled = g.S.Money >= g.StageCost(st);
                        if (Btn(Game.Yen(g.StageCost(st)), btnPrimary, GUILayout.Width(130))) gc.Expand();
                        GUI.enabled = true;
                    }
                    else if (i > g.S.Stage) GUILayout.Label(Game.Yen(g.StageCost(st)), small, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Space(8);
            if (Btn(g.S.Day + "日目の営業をはじめる", btnPrimary)) gc.NextDay();
        }

        void Win()
        {
            var g = gc.Game;
            GUILayout.Label("チェーン店になった！", h1);
            GUILayout.Label(string.Format("屋台からはじめた「{0}」は、{1}日目で街じゅうに広がりました。", g.S.ShopName, g.S.Day), text);
            GUILayout.Label(string.Format("買ってくれた組 {0}　／　できた常連さん {1}　／　評判 {2}", g.S.Stats.Served, g.S.Stats.RegularsMade, Game.Stars(g.S.Rep)), card);
            GUILayout.Label("ちがうお店や場所でも遊んでみよう。場所が変わると、来るお客さんも作戦も変わるよ。", small);
            if (Btn("このまま続ける", btnPrimary)) gc.Page = Page.Grow;
            if (Btn("タイトルへ")) gc.Page = Page.Title;
        }
    }
}
