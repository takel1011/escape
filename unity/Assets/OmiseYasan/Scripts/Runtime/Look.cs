// 色とマテリアルの共通ヘルパー。
// Shader.Find を使わず、プリミティブの標準マテリアルを複製するので、URP でも Built-in でもそのまま動く。
using System.Collections.Generic;
using UnityEngine;

namespace OmiseYasan
{
    public static class Look
    {
        // のれんの藍色と、ちょうちんの山吹色
        public static readonly Color Ai = Hex("#24386B");
        public static readonly Color Lantern = Hex("#F4B400");
        public static readonly Color Paper = Hex("#F6F1E6");
        public static readonly Color Ink = Hex("#1B2440");
        public static readonly Color Muted = Hex("#5A647D");
        public static readonly Color Good = Hex("#23824A");
        public static readonly Color Bad = Hex("#BE3B26");
        public static readonly Color Wood = Hex("#B98552");
        public static readonly Color WoodDark = Hex("#7D5631");
        public static readonly Color Skin = Hex("#F1C8A5");

        static Material baseMat;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }

        static Material Base()
        {
            if (baseMat != null) return baseMat;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(tmp);
            return baseMat;
        }

        /// <summary>色・つや・発光を指定したマテリアル。同じ指定なら使い回す。</summary>
        public static Material Mat(Color color, float smooth = 0.3f, Color? emission = null)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + smooth + (emission.HasValue ? ColorUtility.ToHtmlStringRGBA(emission.Value) : "");
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Base());
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            cache[key] = m;
            return m;
        }

        /// <summary>プリミティブを作って、色・位置・大きさを決める。コライダーは不要なので消す。</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
