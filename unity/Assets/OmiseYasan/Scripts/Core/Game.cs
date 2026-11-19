// ゲームのルール本体。お客さんを作る、すすめた商品と値段を判定する、1日を締める、お店を育てる。
using System;
using System.Collections.Generic;
using System.Linq;

namespace OmiseYasan.Core
{
    public enum Outcome { Mismatch, Expensive, Grudging, Ok, CheapOk, Regular, CheapRegular }

    public class ServeResult
    {
        public Outcome Kind;
        public Product Product;
        public Customer Customer;
        public int Price, Will, Qty, Sales, Profit;
        public float Fit;
        public string Line;               // お客さんのセリフ
        public string NewRegularName;     // 新しく常連になった人の名前（なければ null）

        public bool Bought { get { return Kind != Outcome.Mismatch && Kind != Outcome.Expensive; } }
        public bool Delighted { get { return Kind == Outcome.Regular || Kind == Outcome.CheapRegular; } }

        public string Title
        {
            get
            {
                switch (Kind)
                {
                    case Outcome.Mismatch: return "帰ってしまった…";
                    case Outcome.Expensive: return "高すぎて帰ってしまった…";
                    case Outcome.Grudging: return "しぶしぶ買ってくれた";
                    case Outcome.Ok: return "買ってくれた";
                    case Outcome.CheapOk: return "安さに喜んでくれた";
                    default: return "大満足！ 常連さんになった";
                }
            }
        }

        /// <summary>なぜこの結果になったのか。答え合わせの文章。</summary>
        public string Why()
        {
            var a = GameData.Archetypes[Customer.ArchKey];
            string needs = string.Join("」「", a.Needs.Select(NeedInfo.Label));
            string fitWord = Fit >= 0.9f ? "ぴったり！" : Fit >= 0.6f ? "よく合っていた" : Fit >= 0.3f ? "少しだけ合っていた" : "合っていなかった";
            string s = string.Format("この人（{0}）がほしかったのは「{1}」。「{2}」は{3}。", a.Name, needs, Product.Name, fitWord);
            if (Kind == Outcome.Expensive || Kind == Outcome.Grudging) s += string.Format(" 出してもいい値段は {0} くらいだった。", Game.Yen(Will));
            if (Kind == Outcome.CheapOk || Kind == Outcome.CheapRegular) s += string.Format(" 実は {0} くらいまでなら払ってくれたみたい。", Game.Yen(Will));
            if (Kind == Outcome.Ok) s += " もっとぴったりの商品なら、常連になってくれたかも。";
            if (NewRegularName != null) s += string.Format(" {0}さんとして常連リストに入ったよ。", NewRegularName);
            return s;
        }
    }

    public class DayReport
    {
        public DayLog Log;
        public int Day, Rent, Wages, StaffCustomers, StaffProfit, Branch, Profit;
    }

    public class Game
    {
        public GameState S;
        readonly Random rng;

        public Game(GameState state = null, int? seed = null)
        {
            S = state;
            rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ---------- helpers ----------
        public static string Yen(int n) { return (n < 0 ? "−¥" : "¥") + Math.Abs(n).ToString("N0"); }
        public static string Stars(float rep)
        {
            int f = Math.Max(0, Math.Min(5, (int)Math.Round(rep)));
            return new string('★', f) + new string('☆', 5 - f);
        }
        public static int PriceStep(int basePrice) { return basePrice >= 3000 ? 100 : basePrice >= 1000 ? 50 : 10; }
        public static int RoundTo(float v, int step) { return Math.Max(step, (int)Math.Round(v / step) * step); }

        int Ri(int a, int b) { return rng.Next(a, b + 1); }
        T Pick<T>(IList<T> list) { return list[rng.Next(list.Count)]; }
        static float Clamp(float v, float a, float b) { return Math.Max(a, Math.Min(b, v)); }

        public ShopType Shop { get { return GameData.Shops[S.ShopKey]; } }
        public Location Loc { get { return GameData.Locations[S.LocKey]; } }
        public Stage Stage { get { return GameData.Stages[S.Stage]; } }
        public Stage NextStage { get { return S.Stage + 1 < GameData.Stages.Length ? GameData.Stages[S.Stage + 1] : null; } }
        public List<Product> Menu { get { return Shop.Products.Where(p => S.Unlocked.Contains(p.Id)).ToList(); } }
        public List<Product> Locked { get { return Shop.Products.Where(p => !S.Unlocked.Contains(p.Id)).ToList(); } }
        public Product ProductById(string id) { return Shop.Products.First(p => p.Id == id); }
        public Customer Current { get { return S.InDay ? S.Today.Customers[S.Today.Index] : null; } }
        public Regular RegularOf(Customer c) { return c.IsRegular ? S.Regulars.FirstOrDefault(r => r.Id == c.RegularId) : null; }
        public int RentFor(Stage st) { return (int)Math.Round(Loc.Rent * Shop.Scale * st.RentMul / 10f) * 10; }
        public int RentToday { get { return RentFor(Stage); } }
        public int Wage { get { return (int)Math.Round(1500 * Shop.Scale / 10f) * 10; } }
        public int StageCost(Stage st) { return (int)Math.Round(st.Cost * Shop.Scale); }
        public int StaffServes { get { return 2 + (int)Math.Floor(S.Rep / 1.5f); } }

        // ---------- new game ----------
        public void NewGame(string shopKey, string locKey, string shopName)
        {
            var shop = GameData.Shops[shopKey];
            S = new GameState
            {
                ShopKey = shopKey, LocKey = locKey,
                ShopName = string.IsNullOrWhiteSpace(shopName) ? shop.DefaultName : shopName.Trim(),
                Money = (int)Math.Round(10000 * shop.Scale),
                Unlocked = shop.Products.Where(p => p.IsStarter).Select(p => p.Id).ToList(),
            };
            StartDay();
        }

        // ---------- customers ----------
        Customer MakeCustomer(string archKey, int hour, string say1 = null, string regularId = "")
        {
            var a = GameData.Archetypes[archKey];
            int size = Ri(a.SizeMin, a.SizeMax);
            var c = new Customer
            {
                ArchKey = archKey, Hour = hour, Minute = Ri(0, 5) * 10 + Ri(0, 9), Size = size,
                Payers = a.Payers > 0 ? a.Payers : size,
                Look = Pick(a.Looks), Item = Pick(a.Items), RegularId = regularId ?? "",
            };
            for (int i = 0; i < size; i++)
                c.Members.Add(a.MixMembers ? Pick(a.Members) : a.Members[Math.Min(i, a.Members.Length - 1)]);
            var says = a.Says.OrderBy(_ => rng.Next()).ToList();
            c.Say1 = say1 ?? says[0];
            c.Say2 = says.First(x => x != c.Say1);
            return c;
        }

        string PickArchetype(int hour)
        {
            var keys = GameData.Archetypes.Keys.ToList();
            var w = keys.Select(k =>
            {
                var a = GameData.Archetypes[k];
                bool inHours = hour >= a.HourFrom && hour < a.HourTo;
                return a.Weights[Loc.Index] * (inHours ? 1f : 0.04f);
            }).ToList();
            float r = (float)rng.NextDouble() * w.Sum();
            for (int i = 0; i < keys.Count; i++) { r -= w[i]; if (r <= 0) return keys[i]; }
            return keys[keys.Count - 1];
        }

        public void StartDay()
        {
            int n = (int)Math.Round((Loc.Traffic + Stage.Traffic + S.Rep * 0.8f) * Shop.Traffic);
            n = Math.Max(3, Math.Min(12, n));
            int open = Shop.OpenHour, close = Shop.CloseHour;
            var list = Enumerable.Range(0, n).Select(_ => Ri(open, close - 1)).OrderBy(h => h)
                .Select(h => MakeCustomer(PickArchetype(h), h)).ToList();

            // 常連さんがまた来る
            var back = S.Regulars.Where(_ => rng.NextDouble() < Loc.RegularRate).Take((n + 1) / 2).ToList();
            foreach (var r in back)
            {
                var a = GameData.Archetypes[r.ArchKey];
                int lo = Math.Max(open, a.HourFrom), hi = Math.Min(close, a.HourTo) - 1;
                int h = hi >= lo ? Ri(lo, hi) : Ri(open, close - 1);
                list[rng.Next(list.Count)] = MakeCustomer(r.ArchKey, h, null, r.Id);
            }
            list = list.OrderBy(c => c.Hour).ThenBy(c => c.Minute).ToList();

            // 1日目の最初のお客さんは、チュートリアル用の「疲れた会社員」
            if (S.Day == 1)
            {
                var first = MakeCustomer("overtime", Math.Max(open, Math.Min(21, close - 1)), "はぁ…疲れた");
                first.Minute = 10;
                list.RemoveAt(list.Count - 1);
                list.Insert(0, first);
            }

            S.Today = new DayState { Customers = list };
            S.InDay = true;
        }

        // ---------- judgement ----------
        public ServeResult Evaluate(Customer c, Product p, int price)
        {
            var a = GameData.Archetypes[c.ArchKey];
            float p1 = p.FitOf(a.Needs[0]);
            float p2 = a.Needs.Length > 1 ? p.FitOf(a.Needs[1]) : 0;
            float fit = Math.Min(1f, (p1 + 0.6f * p2) / 3f);
            var reg = RegularOf(c);
            float budget = a.Budget * Loc.Budget * Stage.Budget * (reg != null ? 1.1f : 1f);
            float will = p.Base * budget * (0.7f + 0.5f * fit);

            Outcome kind;
            if (fit < 0.3f) kind = Outcome.Mismatch;
            else if (price > will * 1.12f) kind = Outcome.Expensive;
            else if (price > will) kind = Outcome.Grudging;
            else if (fit < 0.6f) kind = price < will * 0.72f ? Outcome.CheapOk : Outcome.Ok;
            else kind = price < will * 0.72f ? Outcome.CheapRegular : Outcome.Regular;

            return new ServeResult
            {
                Kind = kind, Product = p, Customer = c, Price = price, Fit = fit,
                Will = (int)Math.Round(will), Qty = p.IsShare ? 1 : c.Payers,
            };
        }

        static readonly Dictionary<Outcome, string[]> Lines = new Dictionary<Outcome, string[]>
        {
            [Outcome.Mismatch] = new[] { "うーん、今はそういう気分じゃないんだ…", "ほしいのと、ちょっとちがうかな…", "また今度にします…" },
            [Outcome.Expensive] = new[] { "えっ、高っ…！ やめておきます", "うーん、お財布的にムリかな…", "ちょっと予算オーバーだなぁ" },
            [Outcome.Grudging] = new[] { "ちょっと高いけど…まあいいか", "うーん…今日だけね", "高いなぁ。でもほしいし…" },
            [Outcome.Ok] = new[] { "ありがとう", "まあまあかな", "ふつうに良かったよ" },
            [Outcome.CheapOk] = new[] { "えっ、こんなに安いの？", "安くて助かる〜" },
            [Outcome.Regular] = new[] { "これこれ！ また来るね！", "最高！ ぜったいまた来ます！", "わかってるねぇ！ また来るよ" },
            [Outcome.CheapRegular] = new[] { "こんなに安くていいの!? また来るね！", "最高で安い！ 毎日来ちゃうかも！" },
        };

        /// <summary>いまのお客さんに商品をすすめる。お金・評判・常連が更新される。</summary>
        public ServeResult Recommend(string productId, int price)
        {
            var c = Current;
            var p = ProductById(productId);
            var r = Evaluate(c, p, price);
            var L = S.Today.Log;
            var reg = RegularOf(c);
            float repDelta = 0;

            if (r.Bought)
            {
                r.Sales = price * r.Qty;
                int cost = p.Cost * r.Qty;
                r.Profit = r.Sales - cost;
                L.Sales += r.Sales; L.Cost += cost; S.Stats.Served++;
                S.Money += r.Profit;
            }
            else { L.Left++; S.Stats.Left++; }

            switch (r.Kind)
            {
                case Outcome.Mismatch: L.Mismatch++; repDelta = -0.08f; break;
                case Outcome.Expensive: L.Expensive++; repDelta = -0.1f; break;
                case Outcome.Grudging: L.Grudging++; break;
                case Outcome.Ok: L.Happy++; repDelta = 0.03f; break;
                case Outcome.CheapOk: L.Happy++; L.Cheap++; repDelta = 0.03f; break;
                case Outcome.CheapRegular: L.Cheap++; goto case Outcome.Regular;
                case Outcome.Regular:
                    L.Happy++; repDelta = 0.12f;
                    if (reg != null) { reg.Visits++; reg.FavoriteId = p.Id; reg.LastPrice = price; }
                    else
                    {
                        var nr = new Regular
                        {
                            Id = "r" + DateTime.Now.Ticks + Ri(0, 999), Name = Pick(GameData.Surnames),
                            ArchKey = c.ArchKey, FavoriteId = p.Id, LastPrice = price, Visits = 1
                        };
                        S.Regulars.Add(nr);
                        if (S.Regulars.Count > 30) S.Regulars.RemoveAt(0);
                        L.NewRegulars++; S.Stats.RegularsMade++;
                        r.NewRegularName = nr.Name;
                    }
                    break;
            }
            // 常連さんをがっかりさせると、もう来ないかも
            if (reg != null && !r.Bought && rng.NextDouble() < 0.5) S.Regulars.Remove(reg);

            S.Rep = Clamp(S.Rep + repDelta, 0, 5);
            r.Line = Pick(Lines[r.Kind]);
            return r;
        }

        public int CustomersLeft { get { return S.Today.Customers.Count - S.Today.Index - 1; } }

        /// <summary>次のお客さんへ。今日のお客さんが終わったら false。</summary>
        public bool NextCustomer()
        {
            S.Today.Index++;
            return S.Today.Index < S.Today.Customers.Count;
        }

        public bool UseHint()
        {
            if (S.Today.Hints <= 0) return false;
            S.Today.Hints--;
            return true;
        }

        // ---------- end of day ----------
        public DayReport CloseDay()
        {
            var L = S.Today.Log;
            var menu = Menu;
            float avgMargin = menu.Average(p => p.Base * Stage.Budget - p.Cost);
            var rep = new DayReport { Log = L, Day = S.Day };
            rep.StaffCustomers = S.Staff * StaffServes;
            rep.StaffProfit = (int)Math.Round(rep.StaffCustomers * avgMargin * 1.6f * 0.7f / 10f) * 10;
            rep.Wages = S.Staff * Wage;
            rep.Branch = (int)Math.Round(Stage.Branches * 5000 * Shop.Scale * (0.5f + S.Rep / 5f) / 10f) * 10;
            rep.Rent = RentToday;
            rep.Profit = L.Sales - L.Cost - rep.Rent - rep.Wages + rep.StaffProfit + rep.Branch;
            S.Money += -rep.Rent - rep.Wages + rep.StaffProfit + rep.Branch;
            S.Stats.Profit += rep.Profit;
            S.InDay = false;
            S.Day++;
            return rep;
        }

        /// <summary>その日の結果に合わせた「店長メモ」。経営の考え方を1つずつ伝える。</summary>
        public List<string> Tips(DayReport R)
        {
            var L = R.Log;
            var tips = new List<string>();
            if (L.Sales > 0)
            {
                int ratio = (int)Math.Round(L.Cost * 100f / L.Sales);
                string judge = ratio > Shop.CostGuide + 10 ? "値段が安すぎるかも。" : ratio < Shop.CostGuide - 12 ? "しっかり儲けが出ているね。" : "いい感じ！";
                tips.Add(string.Format("今日の原価率は {0}%（売上のうち、仕入れにかかった割合）。{1}は {2}% くらいが目安。{3}", ratio, Shop.Name, Shop.CostGuide, judge));
            }
            if (L.Expensive >= 2) tips.Add(string.Format("値段が高すぎて帰った人が {0}人。学生や子どもはお財布が小さく、観光客や記念日の人は大きい。お客さんを見て値段を変えよう。", L.Expensive));
            if (L.Cheap >= 2) tips.Add(string.Format("「安い！」と驚かれたのが {0}回。喜ばれるけど、売値 − 仕入れ値 = 儲け。もう少し上げても買ってくれたかも。", L.Cheap));
            if (L.Mismatch >= 2) tips.Add(string.Format("ほしいものとちがって帰った人が {0}人。合う商品がないなら、新しい商品をつくろう。", L.Mismatch));
            if (L.NewRegulars > 0) tips.Add(string.Format("新しい常連さんが {0}人！ 常連さんはまた来てくれて、少し高くても買ってくれる。", L.NewRegulars));
            if (R.Profit < 0) tips.Add(string.Format("今日は赤字…。家賃（{0}）は、お客さんが来なくても毎日かかる。家賃の高い場所ほど、たくさん売る必要がある。", Yen(R.Rent)));
            if (R.StaffCustomers > 0) tips.Add(string.Format("スタッフが {0}組を接客してくれた。お給料（{1}）より稼いでくれれば、雇った意味がある。", R.StaffCustomers, Yen(R.Wages)));
            if (tips.Count < 3)
            {
                var extra = new[]
                {
                    string.Format("場所選びは「家賃」と「来るお客さん」のバランス。{0}は{1}。", Loc.Name, Loc.Desc.Split('。')[0]),
                    "1回きりのお客さんより、何度も来てくれる常連さんがお店をささえる。",
                    "評判の★が上がると、お客さんが増える。いい接客がいちばんの宣伝。",
                    "グループ客は人数分買ってくれるから、1組で大きな売上になる。",
                };
                tips.Add(extra[S.Day % extra.Length]);
            }
            return tips.Take(3).ToList();
        }

        // ---------- grow ----------
        public bool Develop(string id)
        {
            var p = ProductById(id);
            if (S.Unlocked.Contains(id) || S.Money < p.DevCost) return false;
            S.Money -= p.DevCost; S.Unlocked.Add(id);
            return true;
        }

        /// <summary>お店を広げる。チェーン店になったら true を返す（初めての時だけ）。</summary>
        public bool Expand(out bool justWon)
        {
            justWon = false;
            var next = NextStage;
            if (next == null || S.Money < StageCost(next)) return false;
            S.Money -= StageCost(next); S.Stage++;
            if (S.Stage == GameData.Stages.Length - 1 && !S.Won) { S.Won = true; justWon = true; }
            return true;
        }

        public bool Hire(int delta)
        {
            int n = S.Staff + delta;
            if (n < 0 || n > Stage.StaffMax) return false;
            S.Staff = n;
            return true;
        }
    }
}
