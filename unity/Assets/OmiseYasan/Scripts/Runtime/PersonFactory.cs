// 仮の人間（カプセルと球）を組み立てる。
// リアルなキャラに替えるときは、Build() が Prefab を Instantiate して PersonRig を付けるように変えればよい。
using OmiseYasan.Core;
using UnityEngine;

namespace OmiseYasan
{
    /// <summary>動かすときに使う体の部品。</summary>
    public class PersonRig : MonoBehaviour
    {
        public Transform Body, Head, ArmL, ArmR;
        public float Height = 1.7f;
        public bool InStroller;
    }

    public static class PersonFactory
    {
        static readonly string[] HairColors = { "#1E1A18", "#2B211C", "#3B2A20", "#5A3B26" };

        public static GameObject Build(Body kind, Color outfit, Transform parent, string name = "Person", int seed = 0)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<PersonRig>();

            if (kind == Body.Baby) { BuildStroller(root.transform, rig); return root; }

            float h = kind == Body.Child ? 0.65f : kind == Body.Elder ? 0.95f : 1f;
            rig.Height = 1.7f * h;
            var hair = Look.Mat(kind == Body.Elder ? Look.Hex("#C9C9C9") : Look.Hex(HairColors[Mathf.Abs(seed) % HairColors.Length]), 0.35f);
            var skin = Look.Mat(Look.Skin, 0.3f);
            var cloth = Look.Mat(outfit, 0.25f);
            var dark = Look.Mat(Look.Hex("#2A2A30"), 0.3f);

            // 足、胴体、腕、頭
            Look.Prim(PrimitiveType.Capsule, root.transform, new Vector3(-0.1f * h, 0.38f * h, 0), new Vector3(0.15f, 0.38f, 0.15f) * h, dark, "LegL");
            Look.Prim(PrimitiveType.Capsule, root.transform, new Vector3(0.1f * h, 0.38f * h, 0), new Vector3(0.15f, 0.38f, 0.15f) * h, dark, "LegR");
            var body = Look.Prim(PrimitiveType.Capsule, root.transform, new Vector3(0, 0.98f * h, 0), new Vector3(0.44f, 0.36f, 0.3f) * h, cloth, "Body");
            if (kind == Body.Elder) body.transform.localRotation = Quaternion.Euler(8, 0, 0);
            rig.Body = body.transform;
            rig.ArmL = Arm(root.transform, -0.27f * h, h, cloth);
            rig.ArmR = Arm(root.transform, 0.27f * h, h, cloth);

            var head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.localPosition = new Vector3(0, 1.5f * h, 0);
            rig.Head = head;
            Look.Prim(PrimitiveType.Sphere, head, Vector3.zero, Vector3.one * 0.32f * h, skin, "Face");
            Look.Prim(PrimitiveType.Sphere, head, new Vector3(0, 0.06f * h, -0.02f * h), new Vector3(0.34f, 0.24f, 0.34f) * h, hair, "Hair");
            Look.Prim(PrimitiveType.Sphere, head, new Vector3(-0.06f * h, 0.01f * h, 0.15f * h), Vector3.one * 0.04f * h, dark, "EyeL");
            Look.Prim(PrimitiveType.Sphere, head, new Vector3(0.06f * h, 0.01f * h, 0.15f * h), Vector3.one * 0.04f * h, dark, "EyeR");
            return root;
        }

        static Transform Arm(Transform root, float x, float h, Material cloth)
        {
            // 肩を支点に振れるよう、空の親を肩の位置に置く
            var shoulder = new GameObject("Shoulder").transform;
            shoulder.SetParent(root, false);
            shoulder.localPosition = new Vector3(x, 1.22f * h, 0);
            Look.Prim(PrimitiveType.Capsule, shoulder, new Vector3(0, -0.3f * h, 0), new Vector3(0.11f, 0.3f, 0.11f) * h, cloth, "Arm");
            Look.Prim(PrimitiveType.Sphere, shoulder, new Vector3(0, -0.62f * h, 0), Vector3.one * 0.1f * h, Look.Mat(Look.Skin, 0.3f), "Hand");
            return shoulder;
        }

        static void BuildStroller(Transform root, PersonRig rig)
        {
            rig.InStroller = true;
            rig.Height = 0.9f;
            var frame = Look.Mat(Look.Hex("#5C6B7A"), 0.6f);
            var hood = Look.Mat(Look.Hex("#B7A4C9"), 0.2f);
            Look.Prim(PrimitiveType.Cube, root, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.3f, 0.7f), hood, "Basket");
            Look.Prim(PrimitiveType.Sphere, root, new Vector3(0, 0.62f, 0.15f), new Vector3(0.45f, 0.35f, 0.4f), hood, "Hood");
            for (int i = 0; i < 4; i++)
            {
                var w = Look.Prim(PrimitiveType.Cylinder, root, new Vector3(i % 2 == 0 ? -0.22f : 0.22f, 0.12f, i < 2 ? -0.25f : 0.25f),
                    new Vector3(0.22f, 0.02f, 0.22f), Look.Mat(Look.Ink), "Wheel");
                w.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            Look.Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.8f, -0.45f), new Vector3(0.03f, 0.25f, 0.03f), frame, "Handle")
                .transform.localRotation = Quaternion.Euler(-30, 0, 0);
            var head = Look.Prim(PrimitiveType.Sphere, root, new Vector3(0, 0.66f, -0.05f), Vector3.one * 0.18f, Look.Mat(Look.Skin, 0.3f), "BabyHead");
            rig.Head = head.transform;
            rig.Body = head.transform;
        }

        /// <summary>手に持ち物を持たせる（スーツケース・花束など、色だけの仮の形）。</summary>
        public static void Hold(PersonRig rig, string item)
        {
            if (rig.ArmR == null || item.Contains("ベビーカー")) return; // ベビーカーは別の体として出るので手には持たない
            Color c = Look.Hex("#8A6A4A");
            Vector3 size = new Vector3(0.12f, 0.16f, 0.06f);
            if (item.Contains("ビジネスバッグ")) { c = Look.Hex("#2D2A26"); size = new Vector3(0.32f, 0.24f, 0.08f); }
            else if (item.Contains("花束")) { c = Look.Hex("#E86A8E"); size = new Vector3(0.16f, 0.26f, 0.16f); }
            else if (item.Contains("スーツケース")) { c = Look.Hex("#3E6FB0"); size = new Vector3(0.36f, 0.5f, 0.2f); }
            else if (item.Contains("カメラ")) { c = Look.Hex("#1E1E1E"); size = new Vector3(0.14f, 0.1f, 0.08f); }
            else if (item.Contains("ヨガマット")) { c = Look.Hex("#7AC1A0"); size = new Vector3(0.1f, 0.55f, 0.1f); }
            else if (item.Contains("風船")) { c = Look.Hex("#E64545"); size = new Vector3(0.3f, 0.36f, 0.3f); }
            else if (item.Contains("ぬいぐるみ")) { c = Look.Hex("#C49A6C"); size = new Vector3(0.18f, 0.2f, 0.14f); }
            else if (item.Contains("スマホ")) { c = Look.Hex("#111111"); size = new Vector3(0.07f, 0.13f, 0.02f); }
            else if (item.Contains("ボール")) { c = Look.Hex("#E07A2E"); size = Vector3.one * 0.22f; }
            else if (item.Contains("リュック") || item.Contains("スポーツバッグ")) { c = Look.Hex("#3C5A3C"); size = new Vector3(0.3f, 0.32f, 0.16f); }
            else if (item.Contains("紙袋") || item.Contains("買い物袋")) { c = Look.Hex("#D9C3A0"); size = new Vector3(0.26f, 0.3f, 0.12f); }
            float h = rig.Height / 1.7f;
            var type = item.Contains("風船") || item.Contains("ボール") ? PrimitiveType.Sphere : PrimitiveType.Cube;
            var pos = item.Contains("風船") ? new Vector3(0, 0.3f, 0.1f) : new Vector3(0, -0.72f * h, 0.06f);
            Look.Prim(type, rig.ArmR, pos, size, Look.Mat(c, 0.35f), "Item");
        }
    }
}
