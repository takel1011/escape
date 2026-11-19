// 店内の3D。プリミティブで組み立てる仮の見た目。
// あとで本物の3Dモデルに差し替えるときは、Build() の中の各パーツを Prefab の Instantiate に置き換える。
using System.Collections.Generic;
using OmiseYasan.Core;
using UnityEngine;

namespace OmiseYasan
{
    public class ShopView : MonoBehaviour
    {
        // 店内の座標。手前（-Z）がカメラ側で、奥の壁の右側に入口がある。
        public static readonly Vector3 DoorOutside = new Vector3(3.2f, 0, 5.2f);
        public static readonly Vector3 DoorInside = new Vector3(3.2f, 0, 3.2f);
        public static readonly Vector3 CounterFront = new Vector3(-1.9f, 0, 1.0f);
        public static readonly Rect BrowseArea = new Rect(-1.2f, -1.6f, 4.6f, 4.0f); // x, z, 幅, 奥行き

        Transform root;
        Light sun;
        readonly List<Light> lamps = new List<Light>();
        Renderer windowRenderer;
        Camera cam;
        float targetDay = 1f, day = 1f; // 1=昼, 0=夜

        public void Build(ShopType shop, int stage, string shopName, Camera camera)
        {
            cam = camera;
            if (root != null) Destroy(root.gameObject);
            lamps.Clear();
            root = new GameObject("Shop").transform;
            root.SetParent(transform, false);

            var floorMat = Look.Mat(Look.Hex("#C9A77E"), 0.45f);
            var wallMat = Look.Mat(stage >= 2 ? Look.Hex("#F3EEE4") : Look.Hex("#E9DFCD"), 0.1f);
            var trimMat = Look.Mat(Look.WoodDark, 0.3f);

            // 床と壁
            Look.Prim(PrimitiveType.Cube, root, new Vector3(0, -0.05f, 1), new Vector3(11, 0.1f, 8), floorMat, "Floor");
            for (int i = -5; i <= 5; i++) // 床板の目地
                Look.Prim(PrimitiveType.Cube, root, new Vector3(i, 0.001f, 1), new Vector3(0.02f, 0.01f, 8), trimMat);
            Look.Prim(PrimitiveType.Cube, root, new Vector3(-1.45f, 1.75f, 5), new Vector3(8.1f, 3.5f, 0.2f), wallMat, "BackWall");
            Look.Prim(PrimitiveType.Cube, root, new Vector3(4.65f, 1.75f, 5), new Vector3(1.7f, 3.5f, 0.2f), wallMat);
            Look.Prim(PrimitiveType.Cube, root, new Vector3(3.2f, 3.05f, 5), new Vector3(1.2f, 0.9f, 0.2f), wallMat);
            Look.Prim(PrimitiveType.Cube, root, new Vector3(-5.5f, 1.75f, 1), new Vector3(0.2f, 3.5f, 8), wallMat, "LeftWall");
            Look.Prim(PrimitiveType.Cube, root, new Vector3(5.5f, 1.75f, 1), new Vector3(0.2f, 3.5f, 8), wallMat, "RightWall");
            Look.Prim(PrimitiveType.Cube, root, new Vector3(0, 0.1f, 4.88f), new Vector3(11, 0.2f, 0.05f), trimMat); // 幅木

            // 窓（時間で空の色が変わる）
            var win = Look.Prim(PrimitiveType.Quad, root, new Vector3(-1.5f, 2.0f, 4.86f), new Vector3(3.2f, 1.4f, 1), Look.Mat(Color.white, 0.9f, Color.white), "Window");
            windowRenderer = win.GetComponent<Renderer>();
            windowRenderer.material = new Material(windowRenderer.sharedMaterial);
            Look.Prim(PrimitiveType.Cube, root, new Vector3(-1.5f, 2.0f, 4.9f), new Vector3(3.4f, 1.6f, 0.05f), trimMat);
            Look.Prim(PrimitiveType.Cube, root, new Vector3(-1.5f, 2.0f, 4.85f), new Vector3(0.05f, 1.4f, 0.05f), trimMat);

            // 入口とのれん
            Look.Prim(PrimitiveType.Cube, root, new Vector3(3.2f, 1.3f, 5.3f), new Vector3(1.2f, 2.6f, 0.05f), Look.Mat(Look.Hex("#3A3F4F"), 0.2f), "DoorDark");
            BuildNoren(shopName);

            // カウンターと店主
            BuildCounter(shop.Key);
            var owner = PersonFactory.Build(Body.Adult, Look.Hex("#FFFFFF"), root, "Owner");
            owner.transform.localPosition = new Vector3(-4.2f, 0, 1.6f);
            owner.transform.localRotation = Quaternion.Euler(0, 90, 0);
            Look.Prim(PrimitiveType.Cylinder, owner.transform, new Vector3(0, 1.95f, 0), new Vector3(0.36f, 0.12f, 0.36f), Look.Mat(Color.white, 0.2f)); // コック帽

            // お店ごとの家具。お店が大きくなるほど増える
            int sets = Mathf.Clamp(1 + stage, 1, 4);
            if (shop.Key == "ramen") BuildRamen(sets);
            else if (shop.Key == "salon") BuildSalon(sets);
            else BuildCake(sets);

            if (stage >= 2) // 観葉植物
            {
                Plant(new Vector3(4.8f, 0, 4.3f));
                Plant(new Vector3(-4.9f, 0, 4.3f));
            }
            if (stage >= 3) Poster(new Vector3(1.6f, 2.2f, 4.88f), "2号店\nOPEN");

            // 明かり
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            int lampCount = 2 + Mathf.Min(stage, 2);
            for (int i = 0; i < lampCount; i++)
            {
                float x = Mathf.Lerp(-3.5f, 3.5f, lampCount == 1 ? 0.5f : i / (float)(lampCount - 1));
                Lamp(new Vector3(x, 3.0f, 1.5f));
            }
            ApplyLight(true);
        }

        void BuildNoren(string shopName)
        {
            var parts = SplitNoren(shopName);
            float total = 1.5f, gap = 0.04f;
            float w = (total - gap * (parts.Count - 1)) / parts.Count;
            var cloth = Look.Mat(Look.Ai, 0.15f);
            Look.Prim(PrimitiveType.Cylinder, root, new Vector3(3.2f, 2.62f, 4.82f), new Vector3(0.04f, 0.85f, 0.04f), Look.Mat(Look.WoodDark), "NorenPole")
                .transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < parts.Count; i++)
            {
                float x = 3.2f - total / 2 + w / 2 + i * (w + gap);
                var panel = Look.Prim(PrimitiveType.Cube, root, new Vector3(x, 2.25f, 4.8f), new Vector3(w, 0.7f, 0.02f), cloth, "Noren");
                Label3D(panel.transform.parent, parts[i], new Vector3(x, 2.25f, 4.78f), 0.11f / Mathf.Max(1, parts[i].Length * 0.45f), Look.Paper);
            }
        }

        /// <summary>のれんの布1枚ずつに入れる文字。単語ごとに分け、長い単語は4文字ずつに切る。</summary>
        public static List<string> SplitNoren(string name)
        {
            var outp = new List<string>();
            foreach (var word in name.Split(new[] { ' ', '　' }, System.StringSplitOptions.RemoveEmptyEntries))
                for (int i = 0; i < word.Length; i += 4) outp.Add(word.Substring(i, Mathf.Min(4, word.Length - i)));
            if (outp.Count == 1 && outp[0].Length > 2)
            {
                string s = outp[0]; int h = (s.Length + 1) / 2;
                outp = new List<string> { s.Substring(0, h), s.Substring(h) };
            }
            if (outp.Count == 0) outp.Add("店");
            return outp.GetRange(0, Mathf.Min(5, outp.Count));
        }

        void BuildCounter(string shopKey)
        {
            bool showcase = shopKey == "cake";
            var top = Look.Mat(showcase ? Look.Hex("#DDEFF5") : Look.Wood, showcase ? 0.95f : 0.5f);
            Look.Prim(PrimitiveType.Cube, root, new Vector3(-3.2f, 0.5f, 1.6f), new Vector3(0.9f, 1.0f, 4.2f), Look.Mat(Look.WoodDark, 0.3f), "Counter");
            Look.Prim(PrimitiveType.Cube, root, new Vector3(-3.2f, 1.03f, 1.6f), new Vector3(1.1f, 0.06f, 4.4f), top);
        }

        void BuildRamen(int sets)
        {
            var bowl = Look.Mat(Look.Hex("#C23B2A"), 0.7f);
            for (int i = 0; i < 4; i++)
            {
                Look.Prim(PrimitiveType.Cylinder, root, new Vector3(-3.2f, 1.12f, 0.2f + i * 0.9f), new Vector3(0.3f, 0.06f, 0.3f), bowl);
                Stool(new Vector3(-2.35f, 0, 0.2f + i * 0.9f));
            }
            for (int i = 0; i < sets; i++) Table(new Vector3(0.4f + (i % 2) * 2.4f, 0, -0.6f + (i / 2) * 2.2f));
            // 赤ちょうちん
            for (int i = 0; i < 2; i++)
                Look.Prim(PrimitiveType.Sphere, root, new Vector3(1.9f + i * 2.6f, 2.55f, 4.6f), new Vector3(0.4f, 0.55f, 0.4f),
                    Look.Mat(Look.Hex("#D33A2C"), 0.2f, Look.Hex("#D33A2C") * 0.8f), "Chochin");
        }

        void BuildSalon(int sets)
        {
            var mirror = Look.Mat(Look.Hex("#CFE6F2"), 0.98f, Look.Hex("#3D4A55"));
            var chair = Look.Mat(Look.Hex("#22252D"), 0.6f);
            for (int i = 0; i < sets + 1; i++)
            {
                float x = -0.6f + i * 1.5f;
                if (x > 2.2f) break;
                Look.Prim(PrimitiveType.Cube, root, new Vector3(x, 1.6f, 4.85f), new Vector3(0.9f, 1.4f, 0.05f), mirror, "Mirror");
                Look.Prim(PrimitiveType.Cube, root, new Vector3(x, 0.45f, 4.2f), new Vector3(0.6f, 0.15f, 0.6f), chair, "Chair");
                Look.Prim(PrimitiveType.Cube, root, new Vector3(x, 0.8f, 4.48f), new Vector3(0.6f, 0.6f, 0.1f), chair);
                Look.Prim(PrimitiveType.Cylinder, root, new Vector3(x, 0.2f, 4.2f), new Vector3(0.12f, 0.2f, 0.12f), Look.Mat(Look.Hex("#9AA0A8"), 0.9f));
            }
            Look.Prim(PrimitiveType.Cube, root, new Vector3(-3.2f, 1.25f, 0.6f), new Vector3(0.3f, 0.4f, 0.3f), Look.Mat(Look.Hex("#E8D9C8"), 0.4f), "Vase");
            Plant(new Vector3(-0.2f, 0, -1.8f));
        }

        void BuildCake(int sets)
        {
            var cakes = new[] { "#F7E7EE", "#5B3A29", "#F2C14E", "#E57373", "#FFF8E7" };
            for (int i = 0; i < 8; i++)
            {
                var c = Look.Mat(Look.Hex(cakes[i % cakes.Length]), 0.4f);
                Look.Prim(PrimitiveType.Cylinder, root, new Vector3(-3.35f + (i % 2) * 0.35f, 1.13f, 0.1f + (i / 2) * 0.9f), new Vector3(0.24f, 0.08f, 0.24f), c, "Cake");
            }
            var shelf = Look.Mat(Look.Wood, 0.4f);
            for (int s = 0; s < Mathf.Min(sets, 3); s++)
            {
                float x = 0.2f + s * 1.6f;
                Look.Prim(PrimitiveType.Cube, root, new Vector3(x, 0.9f, 4.6f), new Vector3(1.3f, 1.8f, 0.4f), shelf, "Shelf");
                for (int k = 0; k < 3; k++)
                    Look.Prim(PrimitiveType.Cube, root, new Vector3(x - 0.4f + k * 0.4f, 1.25f, 4.35f), new Vector3(0.28f, 0.22f, 0.22f),
                        Look.Mat(Look.Hex(k % 2 == 0 ? "#F4D9E0" : "#E8D7B5"), 0.3f), "GiftBox");
            }
            if (sets >= 2) Table(new Vector3(1.2f, 0, -0.8f));
        }

        void Table(Vector3 p)
        {
            var wood = Look.Mat(Look.Wood, 0.5f);
            Look.Prim(PrimitiveType.Cylinder, root, p + new Vector3(0, 0.75f, 0), new Vector3(1.0f, 0.03f, 1.0f), wood, "Table");
            Look.Prim(PrimitiveType.Cylinder, root, p + new Vector3(0, 0.37f, 0), new Vector3(0.08f, 0.37f, 0.08f), Look.Mat(Look.WoodDark));
            Stool(p + new Vector3(-0.75f, 0, 0));
            Stool(p + new Vector3(0.75f, 0, 0));
        }

        void Stool(Vector3 p)
        {
            Look.Prim(PrimitiveType.Cylinder, root, p + new Vector3(0, 0.45f, 0), new Vector3(0.36f, 0.04f, 0.36f), Look.Mat(Look.Hex("#C23B2A"), 0.5f), "Stool");
            Look.Prim(PrimitiveType.Cylinder, root, p + new Vector3(0, 0.22f, 0), new Vector3(0.06f, 0.22f, 0.06f), Look.Mat(Look.Hex("#6D7280"), 0.8f));
        }

        void Plant(Vector3 p)
        {
            Look.Prim(PrimitiveType.Cylinder, root, p + new Vector3(0, 0.2f, 0), new Vector3(0.4f, 0.2f, 0.4f), Look.Mat(Look.Hex("#8B5A3C"), 0.3f), "Pot");
            Look.Prim(PrimitiveType.Sphere, root, p + new Vector3(0, 0.75f, 0), new Vector3(0.7f, 0.9f, 0.7f), Look.Mat(Look.Hex("#3F7D4E"), 0.2f), "Leaves");
        }

        void Poster(Vector3 p, string text)
        {
            Look.Prim(PrimitiveType.Cube, root, p, new Vector3(0.8f, 0.6f, 0.02f), Look.Mat(Look.Lantern, 0.2f), "Poster");
            Label3D(root, text, p + new Vector3(0, 0, -0.02f), 0.05f, Look.Ink);
        }

        void Lamp(Vector3 p)
        {
            Look.Prim(PrimitiveType.Cylinder, root, p + new Vector3(0, 0.35f, 0), new Vector3(0.02f, 0.35f, 0.02f), Look.Mat(Look.Ink));
            Look.Prim(PrimitiveType.Sphere, root, p, new Vector3(0.35f, 0.25f, 0.35f), Look.Mat(Look.Paper, 0.1f, Look.Hex("#FFD58A")), "LampShade");
            var l = new GameObject("LampLight").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.localPosition = p + Vector3.down * 0.2f;
            l.type = LightType.Point;
            l.range = 5.5f;
            l.color = Look.Hex("#FFC777");
            lamps.Add(l);
        }

        public static TextMesh Label3D(Transform parent, string text, Vector3 pos, float size, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.color = color;
            var font = BuiltinFont();
            if (font != null) { tm.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            return tm;
        }

        public static Font BuiltinFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        /// <summary>お客さんが来た時間に合わせて、昼・夕方・夜の明るさにする。</summary>
        public void SetHour(int hour)
        {
            targetDay = hour < 16 ? 1f : hour < 18 ? 0.6f : hour < 19 ? 0.35f : 0f;
        }

        void Update()
        {
            if (sun == null) return;
            day = Mathf.MoveTowards(day, targetDay, Time.deltaTime * 0.6f);
            ApplyLight(false);
        }

        void ApplyLight(bool instant)
        {
            if (instant) day = targetDay;
            var noon = Look.Hex("#FFF4E0"); var dusk = Look.Hex("#FF9B54");
            sun.color = day > 0.5f ? Color.Lerp(dusk, noon, (day - 0.5f) * 2) : dusk;
            sun.intensity = Mathf.Lerp(0.05f, 1.1f, day);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(Look.Hex("#2A2F4A"), Look.Hex("#9AA6B8"), day);
            foreach (var l in lamps) l.intensity = Mathf.Lerp(2.6f, 0.8f, day);

            var sky = day > 0.5f ? Color.Lerp(Look.Hex("#F7A35C"), Look.Hex("#9FD3F2"), (day - 0.5f) * 2)
                                 : Color.Lerp(Look.Hex("#141B33"), Look.Hex("#F7A35C"), day * 2);
            if (windowRenderer != null)
            {
                var m = windowRenderer.material;
                m.color = sky;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", sky);
                m.SetColor("_EmissionColor", sky * 0.9f);
            }
            if (cam != null) cam.backgroundColor = Color.Lerp(Look.Hex("#0F1424"), Look.Hex("#DDE5EE"), day);
        }
    }
}
