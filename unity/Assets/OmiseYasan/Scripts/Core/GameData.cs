// おみせやさん物語: ゲームのデータ（お店・場所・お客さん・商品）
// UnityEngine に依存しない純粋な C#。Unity の外でもテストできる。
using System.Collections.Generic;

namespace OmiseYasan.Core
{
    /// <summary>お客さんが欲しいもの。商品はこれにどれだけ合うかを 0〜3 で持つ。</summary>
    public enum Need { Tired, Quick, Cheap, Celebrate, Kids, Healthy, Share, Trendy }

    public static class NeedInfo
    {
        public static string Label(Need n)
        {
            switch (n)
            {
                case Need.Tired: return "元気が出る";
                case Need.Quick: return "すぐできる";
                case Need.Cheap: return "お手ごろ";
                case Need.Celebrate: return "特別な日";
                case Need.Kids: return "子ども向け";
                case Need.Healthy: return "からだにやさしい";
                case Need.Share: return "みんなで";
                default: return "映える";
            }
        }

        /// <summary>メニューに並べる短い名前。</summary>
        public static string Short(Need n)
        {
            switch (n)
            {
                case Need.Tired: return "元気";
                case Need.Quick: return "すぐ";
                case Need.Cheap: return "安い";
                case Need.Celebrate: return "特別";
                case Need.Kids: return "子ども";
                case Need.Healthy: return "ヘルシー";
                case Need.Share: return "みんな";
                default: return "映え";
            }
        }
    }

    public class Product
    {
        public string Id, Name;
        public int Cost, Base, DevCost;
        public bool IsStarter, IsShare;
        public Dictionary<Need, int> Fit;

        public Product(string id, string name, int cost, int basePrice, Dictionary<Need, int> fit, int devCost = 0, bool share = false)
        {
            Id = id; Name = name; Cost = cost; Base = basePrice; Fit = fit;
            DevCost = devCost; IsStarter = devCost == 0; IsShare = share;
        }

        public int FitOf(Need n) { int v; return Fit.TryGetValue(n, out v) ? v : 0; }

        /// <summary>例: 「すぐ★★ 安い★★ 元気★」</summary>
        public string FitText()
        {
            var parts = new List<string>();
            foreach (var kv in Fit) if (kv.Value > 0) parts.Add(NeedInfo.Short(kv.Key) + new string('★', kv.Value));
            return string.Join(" ", parts);
        }
    }

    public class ShopType
    {
        public string Key, Name, Unit, DefaultName, Desc;
        public int OpenHour, CloseHour, CostGuide;
        public float Scale, Traffic;
        public List<Product> Products;
    }

    public class Location
    {
        public string Key, Name, Desc;
        public int Index, Rent, Traffic;
        public float Budget, RegularRate;
    }

    public class Stage
    {
        public string Name, Desc;
        public int Cost, Traffic, StaffMax, Branches;
        public float RentMul, Budget;
    }

    /// <summary>見た目の種類。Unity 側で体の大きさを決めるのに使う。</summary>
    public enum Body { Adult, Child, Elder, Baby }

    public class Archetype
    {
        public string Key, Name;
        public Need[] Needs;
        public float Budget;
        public int HourFrom, HourTo, SizeMin, SizeMax, Payers; // Payers=0 なら人数ぶん買う
        public Body[] Members;     // 先頭から人数ぶん使う
        public bool MixMembers;    // 学生グループなど、メンバーをランダムに混ぜる
        public string[] Looks, Items, Says;
        public float[] Weights;    // [駅前, 住宅街, 学生街]
        public string Outfit;      // 服の色 (#RRGGBB)
    }

    public static class GameData
    {
        static Dictionary<Need, int> F(params object[] kv)
        {
            var d = new Dictionary<Need, int>();
            for (int i = 0; i < kv.Length; i += 2) d[(Need)kv[i]] = (int)kv[i + 1];
            return d;
        }

        public static readonly Dictionary<string, ShopType> Shops = new Dictionary<string, ShopType>
        {
            ["ramen"] = new ShopType
            {
                Key = "ramen", Name = "ラーメン屋", Unit = "杯", OpenHour = 11, CloseHour = 24, Scale = 1f, Traffic = 1f, CostGuide = 30,
                DefaultName = "麺処 ひだまり",
                Desc = "夜おそくまで営業。疲れた人やおなかペコペコの学生がやってくる。",
                Products = new List<Product>
                {
                    new Product("shoyu", "醤油ラーメン", 220, 750, F(Need.Quick, 2, Need.Cheap, 2, Need.Tired, 1)),
                    new Product("miso", "味噌ラーメン", 260, 850, F(Need.Tired, 2, Need.Kids, 1)),
                    new Product("shio", "塩ラーメン", 240, 800, F(Need.Healthy, 2, Need.Quick, 1)),
                    new Product("don", "まかない丼", 150, 480, F(Need.Quick, 3, Need.Cheap, 3), 3500),
                    new Product("kids", "お子さまラーメン", 150, 500, F(Need.Kids, 3, Need.Cheap, 1), 3000),
                    new Product("gyoza", "大皿ぎょうざ", 450, 1600, F(Need.Share, 3, Need.Cheap, 1), 4000, true),
                    new Product("tonkotsu", "にんにく豚骨", 330, 1000, F(Need.Tired, 3), 4000),
                    new Product("tanmen", "野菜タンメン", 280, 900, F(Need.Healthy, 3, Need.Tired, 1), 4000),
                    new Product("tokusei", "特製全部のせ", 550, 1600, F(Need.Celebrate, 3, Need.Tired, 2), 6000),
                    new Product("rainbow", "映えレインボー麺", 400, 1300, F(Need.Trendy, 3, Need.Celebrate, 1), 6000),
                }
            },
            ["salon"] = new ShopType
            {
                Key = "salon", Name = "美容院", Unit = "人", OpenHour = 9, CloseHour = 20, Scale = 3f, Traffic = 0.65f, CostGuide = 15,
                DefaultName = "HAIR こもれび",
                Desc = "1人あたりの値段が高い。そのぶん家賃も高め。記念日やおしゃれ好きが狙い目。",
                Products = new List<Product>
                {
                    new Product("cut", "カット", 400, 3500, F(Need.Trendy, 1, Need.Cheap, 1, Need.Quick, 1)),
                    new Product("quick", "10分クイックカット", 200, 1500, F(Need.Quick, 3, Need.Cheap, 2)),
                    new Product("shampoo", "シャンプー＆ブロー", 300, 2500, F(Need.Tired, 2, Need.Quick, 1)),
                    new Product("gakuwari", "学割カット", 300, 2200, F(Need.Cheap, 3, Need.Share, 1), 9000),
                    new Product("kids", "キッズカット", 250, 1800, F(Need.Kids, 3, Need.Cheap, 1), 9000),
                    new Product("spa", "ヘッドスパ", 600, 4500, F(Need.Tired, 3, Need.Healthy, 2), 12000),
                    new Product("osoroi", "おそろいヘア", 500, 3000, F(Need.Share, 3, Need.Trendy, 2), 12000),
                    new Product("color", "トレンドカラー", 1500, 7500, F(Need.Trendy, 3), 15000),
                    new Product("treat", "髪質改善トリートメント", 1200, 6500, F(Need.Healthy, 3, Need.Trendy, 1), 15000),
                    new Product("arrange", "記念日ヘアセット", 900, 6500, F(Need.Celebrate, 3, Need.Trendy, 1), 18000),
                }
            },
            ["cake"] = new ShopType
            {
                Key = "cake", Name = "ケーキ屋", Unit = "個", OpenHour = 10, CloseHour = 20, Scale = 0.6f, Traffic = 1.1f, CostGuide = 30,
                DefaultName = "パティスリー 森のこみち",
                Desc = "1個の値段は安いけど、お客さんが多い。お祝いとおみやげの日はかきいれ時。",
                Products = new List<Product>
                {
                    new Product("short", "ショートケーキ", 150, 480, F(Need.Kids, 2, Need.Celebrate, 1)),
                    new Product("choux", "シュークリーム", 60, 220, F(Need.Cheap, 3, Need.Quick, 2)),
                    new Product("choco", "濃厚チョコケーキ", 160, 520, F(Need.Tired, 3)),
                    new Product("pudding", "どうぶつプリン", 80, 320, F(Need.Kids, 3, Need.Cheap, 1, Need.Trendy, 1), 2000),
                    new Product("soy", "豆乳ロール", 110, 400, F(Need.Healthy, 2, Need.Quick, 1, Need.Cheap, 1), 2000),
                    new Product("assort", "焼き菓子アソート", 500, 2000, F(Need.Share, 3, Need.Quick, 1), 2000, true),
                    new Product("lowcarb", "低糖質チーズケーキ", 180, 560, F(Need.Healthy, 3), 2500),
                    new Product("tart", "宝石フルーツタルト", 230, 680, F(Need.Trendy, 3, Need.Celebrate, 1), 3000),
                    new Product("whole", "ホールケーキ", 900, 3600, F(Need.Celebrate, 3, Need.Share, 3), 3000, true),
                }
            },
        };

        public static readonly Dictionary<string, Location> Locations = new Dictionary<string, Location>
        {
            ["station"] = new Location { Key = "station", Index = 0, Name = "駅前", Rent = 3500, Traffic = 7, Budget = 1.15f, RegularRate = 0.25f,
                Desc = "人通りが多く、お金に余裕のあるお客さんも多い。でも家賃が高い。常連にはなりにくい。" },
            ["residential"] = new Location { Key = "residential", Index = 1, Name = "住宅街", Rent = 1500, Traffic = 5, Budget = 1.0f, RegularRate = 0.5f,
                Desc = "家賃が安い。人は少なめだけど、気に入ってくれたらまた来てくれる。" },
            ["student"] = new Location { Key = "student", Index = 2, Name = "学生街", Rent = 2200, Traffic = 6, Budget = 0.9f, RegularRate = 0.35f,
                Desc = "にぎやかで若いお客さんがいっぱい。ただしお財布はさみしめ。" },
        };

        public static readonly Stage[] Stages =
        {
            new Stage { Name = "屋台", Cost = 0, RentMul = 0.4f, Traffic = 0, StaffMax = 0, Branches = 0, Budget = 0.9f, Desc = "小さな屋台からスタート。" },
            new Stage { Name = "小さなお店", Cost = 15000, RentMul = 1f, Traffic = 1, StaffMax = 1, Branches = 0, Budget = 1.0f, Desc = "お客さんが増え、アルバイトを1人雇える。" },
            new Stage { Name = "人気のお店", Cost = 40000, RentMul = 1.6f, Traffic = 2, StaffMax = 2, Branches = 0, Budget = 1.1f, Desc = "店がりっぱになり、少し高くても買ってもらえる。" },
            new Stage { Name = "2号店オープン", Cost = 80000, RentMul = 2f, Traffic = 2, StaffMax = 3, Branches = 1, Budget = 1.15f, Desc = "となり町に2号店。毎日の利益がプラスされる。" },
            new Stage { Name = "チェーン店", Cost = 160000, RentMul = 2.5f, Traffic = 3, StaffMax = 4, Branches = 4, Budget = 1.2f, Desc = "街じゅうにあなたのお店！ ゴール！" },
        };

        static Archetype A(string key, string name, Need[] needs, float budget, int from, int to, int smin, int smax,
            Body[] members, string[] looks, string[] items, string[] says, float[] w, string outfit, int payers = 0, bool mix = false)
        {
            return new Archetype
            {
                Key = key, Name = name, Needs = needs, Budget = budget, HourFrom = from, HourTo = to, SizeMin = smin, SizeMax = smax,
                Members = members, Looks = looks, Items = items, Says = says, Weights = w, Outfit = outfit, Payers = payers, MixMembers = mix
            };
        }

        static Body[] B(params Body[] b) { return b; }
        static string[] S(params string[] s) { return s; }
        static float[] W(float a, float b, float c) { return new[] { a, b, c }; }

        public static readonly Dictionary<string, Archetype> Archetypes = new Dictionary<string, Archetype>
        {
            ["overtime"] = A("overtime", "残業帰りの会社員", new[] { Need.Tired, Need.Quick }, 1.05f, 18, 24, 1, 1, B(Body.Adult),
                S("スーツ・ネクタイをゆるめている"), S("ビジネスバッグ"), S("はぁ…疲れた", "明日も朝早いんだよな…", "今日もこんな時間か…"), W(3, 1, 1), "#2B3040"),
            ["lunch"] = A("lunch", "昼休みの会社員", new[] { Need.Quick, Need.Cheap }, 0.95f, 11, 14, 1, 1, B(Body.Adult),
                S("スーツ・首から社員証"), S("腕時計を何度も見ている", "スマホで予定を確認している"), S("昼休みあと30分しかない…", "午後イチで会議なんだよな", "給料日前だしなぁ"), W(3, 0.5f, 1), "#3A4766"),
            ["students"] = A("students", "大学生グループ", new[] { Need.Cheap, Need.Share }, 0.8f, 12, 22, 3, 4, B(Body.Adult),
                S("パーカーにジーンズ"), S("リュック", "分厚い教科書"), S("今月ピンチなんだよね〜", "みんなで分けようぜ！", "バイト代まだ入ってないんだよ…"), W(0.8f, 0.5f, 4), "#7A8FA6", 0, true),
            ["club"] = A("club", "部活帰りの高校生", new[] { Need.Tired, Need.Cheap }, 0.85f, 16, 21, 2, 3, B(Body.Adult, Body.Child),
                S("ジャージ姿・汗だく"), S("ボール", "スポーツバッグ"), S("練習きつかった〜", "もうヘトヘト…", "おこづかい残りちょっとなんだよね"), W(0.8f, 1, 3), "#1F5FA8", 0, true),
            ["family"] = A("family", "家族連れ", new[] { Need.Kids, Need.Share }, 1.0f, 10, 20, 3, 4, B(Body.Adult, Body.Adult, Body.Child, Body.Child),
                S("動きやすいカジュアル"), S("ぬいぐるみ", "風船"), S("ママ〜、ぼくのは？", "子どもが喜ぶのがいいな", "みんなで分けられるといいね"), W(0.8f, 4, 1), "#C9873A"),
            ["couple"] = A("couple", "記念日のカップル", new[] { Need.Celebrate, Need.Trendy }, 1.4f, 17, 23, 2, 2, B(Body.Adult, Body.Adult),
                S("いつもよりおしゃれ"), S("花束", "プレゼントの紙袋"), S("今日でちょうど1年だね", "今日は特別な日だから", "写真撮っておこうよ"), W(2.5f, 1, 1), "#8C2F4B"),
            ["yoga"] = A("yoga", "ヨガ帰りの人", new[] { Need.Healthy }, 1.15f, 9, 17, 1, 1, B(Body.Adult),
                S("スポーツウェア"), S("ヨガマット", "マイ水筒"), S("最近ちょっと体型が気になって…", "体にいいものがいいな", "ヨガのあとって気持ちいいね"), W(1, 3, 0.8f), "#5E9E7E"),
            ["jk"] = A("jk", "女子高生", new[] { Need.Trendy, Need.Cheap }, 0.85f, 15, 20, 2, 3, B(Body.Adult),
                S("制服"), S("スマホを構えている", "キラキラのキーホルダー"), S("これ映えるかな？", "SNSにあげたい！", "おこづかい足りるかな…"), W(1, 1, 3), "#2D3A5C", 0, true),
            ["elderly"] = A("elderly", "近所のおじいちゃん・おばあちゃん", new[] { Need.Healthy, Need.Cheap }, 1.0f, 9, 16, 1, 2, B(Body.Elder, Body.Elder),
                S("カーディガン"), S("杖", "商店街の買い物袋"), S("あっさりしたのがいいねぇ", "最近、胃がもたれてねぇ", "年金暮らしだからねぇ"), W(0.6f, 4, 0.5f), "#9C8466"),
            ["tourist"] = A("tourist", "観光客", new[] { Need.Trendy, Need.Celebrate }, 1.45f, 10, 19, 2, 2, B(Body.Adult, Body.Adult),
                S("旅行っぽい服に帽子"), S("スーツケース", "大きなカメラ"), S("せっかくの旅行だし！", "ここでしか味わえないものある？", "旅の思い出にしたいな"), W(3.5f, 0.3f, 0.8f), "#D9B25F"),
            ["mama"] = A("mama", "赤ちゃん連れのママ", new[] { Need.Quick, Need.Kids }, 1.0f, 9, 16, 2, 2, B(Body.Adult, Body.Baby),
                S("ゆったりした服"), S("ベビーカー", "ほにゅうびん"), S("この子が寝てるうちに…", "お迎えまであと少し", "この子も一緒で大丈夫かな？"), W(0.6f, 3, 0.5f), "#B7A4C9", 1),
            ["nurse"] = A("nurse", "夜勤明けの看護師", new[] { Need.Tired, Need.Healthy }, 1.05f, 9, 13, 1, 1, B(Body.Adult),
                S("白いスニーカー・目の下にクマ"), S("大きなトートバッグ", "病院の名札"), S("夜勤明けでヘトヘト…", "体にやさしいのがいいな", "帰ったらすぐ寝たい…"), W(1.5f, 1.5f, 0.5f), "#E6EEF2"),
            ["party"] = A("party", "お祝い中の会社員グループ", new[] { Need.Celebrate, Need.Share }, 1.25f, 18, 24, 4, 4, B(Body.Adult, Body.Adult, Body.Adult, Body.Adult),
                S("スーツ・みんな笑顔"), S("寄せ書きの色紙", "紙袋"), S("プロジェクト成功、おつかれ！", "今日はパーッといこう！", "みんなで乾杯しよう"), W(3, 0.3f, 1), "#262A35"),
        };

        public static readonly string[] Surnames =
        {
            "サトウ", "スズキ", "タカハシ", "タナカ", "ワタナベ", "イトウ", "ヤマモト", "ナカムラ", "コバヤシ", "カトウ",
            "ヨシダ", "ヤマダ", "ササキ", "マツモト", "イノウエ", "キムラ", "ハヤシ", "シミズ", "モリ", "イケダ"
        };
    }
}
