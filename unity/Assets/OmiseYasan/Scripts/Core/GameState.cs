// セーブされるゲームの状態。Unity の JsonUtility で保存できるよう、public フィールドと List だけで作る。
using System;
using System.Collections.Generic;

namespace OmiseYasan.Core
{
    [Serializable]
    public class Customer
    {
        public string ArchKey;
        public int Hour, Minute, Size, Payers;
        public List<Body> Members = new List<Body>();
        public string Look, Item, Say1, Say2;
        public string RegularId = "";   // 常連さんなら ID

        public bool IsRegular { get { return !string.IsNullOrEmpty(RegularId); } }
        public string TimeText { get { return string.Format("{0:00}:{1:00}", Hour % 24, Minute); } }
    }

    [Serializable]
    public class Regular
    {
        public string Id, Name, ArchKey, FavoriteId;
        public int LastPrice, Visits;
    }

    [Serializable]
    public class DayLog
    {
        public int Sales, Cost, Happy, Left, NewRegulars, Expensive, Cheap, Mismatch, Grudging;
    }

    [Serializable]
    public class DayState
    {
        public List<Customer> Customers = new List<Customer>();
        public int Index, Hints = 3;
        public DayLog Log = new DayLog();
    }

    [Serializable]
    public class Stats
    {
        public int Served, Left, RegularsMade, Profit;
    }

    [Serializable]
    public class GameState
    {
        public string ShopKey, LocKey, ShopName;
        public int Day = 1, Money, Stage, Staff;
        public float Rep = 1f;
        public List<string> Unlocked = new List<string>();
        public List<Regular> Regulars = new List<Regular>();
        public bool Won;
        public Stats Stats = new Stats();
        public bool InDay;               // 営業中なら true（JsonUtility は null を保存できないためフラグで持つ）
        public DayState Today = new DayState();
    }
}
