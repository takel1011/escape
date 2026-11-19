// 全体のまとめ役。再生ボタンを押すと、空のシーンでも自動でお店・カメラ・UI を組み立てる。
using OmiseYasan.Core;
using UnityEngine;

namespace OmiseYasan
{
    public enum Page { Title, PickShop, PickLoc, PickName, Serve, Report, Grow, Win }

    public class GameController : MonoBehaviour
    {
        /// <summary>false にすると、自動で組み立てない（自分でシーンに置きたいとき）。</summary>
        public static bool AutoBoot = true;
        const string SaveKey = "omise-monogatari-save-v1";

        public Game Game { get; private set; }
        public Page Page = Page.Title;
        public ShopView View { get; private set; }
        public CustomerCrowd Crowd { get; private set; }
        public Camera Cam { get; private set; }

        // 接客中の画面の状態
        public string SelectedId;
        public int Price;
        public bool Heard, HintShown;
        public ServeResult Result;
        public DayReport Report;

        // 新しいゲームを始めるときの選択
        public string PickedShop, PickedLoc, PickedName = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!AutoBoot || FindFirstObjectByType<GameController>() != null) return;
            new GameObject("OmiseYasan").AddComponent<GameController>();
        }

        void Awake()
        {
            Cam = Camera.main;
            if (Cam == null) Cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.fieldOfView = 40;
            Cam.transform.position = new Vector3(0.2f, 5.2f, -8.2f);
            Cam.transform.rotation = Quaternion.LookRotation(new Vector3(0.2f, 1.0f, 1.6f) - Cam.transform.position);

            // シーンに最初からあるライトは消して、お店の明かりだけにする
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.transform.parent == null) l.gameObject.SetActive(false);

            View = new GameObject("ShopView").AddComponent<ShopView>();
            Crowd = new GameObject("Customers").AddComponent<CustomerCrowd>();
            gameObject.AddComponent<GameUI>().Bind(this);
            View.Build(GameData.Shops["ramen"], 1, "おみせやさん 物語", Cam);
            View.SetHour(17);
        }

        void Update()
        {
            // 画面の向きに合わせて、3Dを映す場所を変える（横長なら左、縦長なら上）
            bool landscape = UnityEngine.Screen.width >= UnityEngine.Screen.height;
            Cam.rect = landscape ? new Rect(0, 0, 0.58f, 1) : new Rect(0, 0.5f, 1, 0.5f);
            Cam.fieldOfView = landscape ? 40 : 52;
        }

        // ---------- save ----------
        public bool HasSave { get { return PlayerPrefs.HasKey(SaveKey); } }
        public void Save() { if (Game != null && Game.S != null) { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Game.S)); PlayerPrefs.Save(); } }
        public GameState PeekSave()
        {
            if (!HasSave) return null;
            try { return JsonUtility.FromJson<GameState>(PlayerPrefs.GetString(SaveKey)); } catch { return null; }
        }
        public void DeleteSave() { PlayerPrefs.DeleteKey(SaveKey); }

        // ---------- flow ----------
        public void StartNew()
        {
            Game = new Game();
            Game.NewGame(PickedShop, PickedLoc, PickedName);
            RebuildShop();
            Save();
            BeginCustomer();
        }

        public void Continue()
        {
            var s = PeekSave();
            if (s == null) { Page = Page.Title; return; }
            Game = new Game(s);
            RebuildShop();
            if (s.InDay) BeginCustomer();
            else Page = Page.Grow;
        }

        void RebuildShop()
        {
            Crowd.LeaveAll();
            View.Build(Game.Shop, Game.S.Stage, Game.S.ShopName, Cam);
        }

        void BeginCustomer()
        {
            Page = Page.Serve;
            SelectedId = null; Price = 0; Heard = false; HintShown = false; Result = null;
            var c = Game.Current;
            View.SetHour(c.Hour);
            Crowd.Spawn(c);
        }

        public void Select(string productId)
        {
            SelectedId = productId;
            Price = Game.ProductById(productId).Base;
        }

        public void Recommend()
        {
            if (SelectedId == null || Result != null) return;
            Result = Game.Recommend(SelectedId, Price);
            Crowd.React(Result);
            Save();
        }

        public void UseHint() { if (Game.UseHint()) { HintShown = true; Save(); } }

        public void Next()
        {
            if (Game.NextCustomer()) { Save(); BeginCustomer(); return; }
            Crowd.LeaveAll();
            Report = Game.CloseDay();
            Save();
            View.SetHour(22);
            Page = Page.Report;
        }

        public void Develop(string id) { if (Game.Develop(id)) Save(); }
        public void Hire(int d) { if (Game.Hire(d)) Save(); }

        public void Expand()
        {
            bool won;
            if (!Game.Expand(out won)) return;
            Save();
            RebuildShop();
            if (won) Page = Page.Win;
        }

        public void NextDay()
        {
            Game.StartDay();
            Save();
            BeginCustomer();
        }
    }
}
