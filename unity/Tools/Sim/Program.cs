// 使い方: dotnet run -- [読みのうまさ 0〜1] [日数]
// 「読みのうまさ」の確率でお客さんの欲しいものを当て、払ってくれる値段の少し下をつける。
using System;
using System.Linq;
using OmiseYasan.Core;

static class Program
{
    static int Main(string[] args)
    {
        double skill = args.Length > 0 ? double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture) : 0.7;
        int days = args.Length > 1 ? int.Parse(args[1]) : 40;
        var rng = new Random(1);
        foreach (var shop in GameData.Shops.Keys)
        foreach (var loc in GameData.Locations.Keys)
        {
            int[] winDays = Enumerable.Range(0, 20).Select(seed => Play(shop, loc, skill, days, seed, rng)).ToArray();
            var won = winDays.Where(d => d > 0).ToArray();
            Console.WriteLine($"{shop,-6} {loc,-12} クリア {won.Length,2}/20  平均 {(won.Length > 0 ? won.Average().ToString("0.0") : "-"),5}日");
        }
        return 0;
    }

    static int Play(string shop, string loc, double skill, int days, int seed, Random rng)
    {
        var g = new Game(null, seed);
        g.NewGame(shop, loc, "");
        for (int d = 1; d <= days; d++)
        {
            do
            {
                var c = g.Current;
                var needs = GameData.Archetypes[c.ArchKey].Needs;
                bool knows = rng.NextDouble() < skill;
                var p = knows
                    ? g.Menu.OrderByDescending(x => x.FitOf(needs[0]) + 0.6 * (needs.Length > 1 ? x.FitOf(needs[1]) : 0)).First()
                    : g.Menu[rng.Next(g.Menu.Count)];
                int price = knows ? (int)Math.Min(g.Evaluate(c, p, 0).Will * 0.97, p.Base * 1.3) : p.Base;
                g.Recommend(p.Id, price);
            } while (g.NextCustomer());
            g.CloseDay();
            if (g.Expand(out bool won) && won) return d;
            while (g.S.Staff < g.Stage.StaffMax && g.S.Money > 8000 * g.Shop.Scale) g.Hire(1);
            foreach (var p in g.Locked) if (g.S.Money > p.DevCost * 2) g.Develop(p.Id);
            g.StartDay();
        }
        return 0;
    }
}
