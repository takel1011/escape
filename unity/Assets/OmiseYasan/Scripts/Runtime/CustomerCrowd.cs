// お客さんの3D: 入口から入ってきて、店内をウロウロし、結果に合わせて喜んだり帰ったりする。
using System.Collections.Generic;
using OmiseYasan.Core;
using UnityEngine;

namespace OmiseYasan
{
    public class PersonAgent : MonoBehaviour
    {
        enum Mode { Wait, Walk, Idle, Leave, Gone }

        public PersonAgent Follow;          // ベビーカーはママについていく
        public float Speed = 1.3f;
        PersonRig rig;
        Mode mode = Mode.Wait;
        readonly Queue<Vector3> path = new Queue<Vector3>();
        float timer, phase, hop, shake;
        Vector3 lookAt;

        public bool Gone { get { return mode == Mode.Gone; } }

        void Awake()
        {
            rig = GetComponent<PersonRig>();
            phase = Random.value * 10f;
        }

        public void Enter(float delay)
        {
            timer = delay;
            mode = Mode.Wait;
            path.Clear();
            path.Enqueue(ShopView.DoorInside + new Vector3(Random.Range(-0.3f, 0.3f), 0, 0));
            path.Enqueue(RandomSpot());
        }

        public void Leave()
        {
            if (mode == Mode.Gone) return;
            path.Clear();
            path.Enqueue(ShopView.DoorInside + new Vector3(Random.Range(-0.3f, 0.3f), 0, 0));
            path.Enqueue(ShopView.DoorOutside);
            mode = Mode.Leave;
            timer = Random.Range(0f, 0.4f);
        }

        public void Hop() { hop = 1.2f; }
        public void ShakeHead() { shake = 1.0f; }

        static Vector3 RandomSpot()
        {
            var a = ShopView.BrowseArea;
            return new Vector3(Random.Range(a.xMin, a.xMax), 0, Random.Range(a.yMin, a.yMax));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (mode == Mode.Gone) return;
            if (timer > 0) { timer -= dt; Animate(false, dt); return; }

            if (Follow != null && !Follow.Gone && mode != Mode.Leave)
            {
                // ママの右うしろについていく
                var target = Follow.transform.position + Follow.transform.right * 0.55f + Follow.transform.forward * 0.4f;
                MoveTowards(target, dt, 0.15f);
                return;
            }

            if (path.Count > 0)
            {
                if (MoveTowards(path.Peek(), dt, 0.05f)) path.Dequeue();
                return;
            }

            if (mode == Mode.Leave) { mode = Mode.Gone; Destroy(gameObject); return; }

            // 立ち止まって、商品やカウンターを眺める
            if (mode != Mode.Idle)
            {
                mode = Mode.Idle;
                timer = 0;
                lookAt = Random.value < 0.6f ? ShopView.CounterFront + Vector3.left * 2f : transform.position + Random.insideUnitSphere;
                idleFor = Random.Range(1.5f, 4f);
            }
            idleFor -= dt;
            Face(lookAt, dt);
            Animate(false, dt);
            if (idleFor <= 0) { mode = Mode.Walk; path.Enqueue(RandomSpot()); }
        }

        float idleFor;

        bool MoveTowards(Vector3 target, float dt, float stopDist)
        {
            if (mode == Mode.Wait || mode == Mode.Idle) mode = Mode.Walk;
            var pos = transform.position;
            var to = target - pos; to.y = 0;
            if (to.magnitude <= stopDist) { Animate(false, dt); return true; }
            transform.position = pos + to.normalized * Mathf.Min(to.magnitude, Speed * dt);
            Face(target, dt);
            Animate(true, dt);
            return false;
        }

        void Face(Vector3 target, float dt)
        {
            var dir = target - transform.position; dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 6f);
        }

        void Animate(bool walking, float dt)
        {
            if (rig == null) return;
            phase += dt * (walking ? 9f : 2f);
            float swing = walking ? Mathf.Sin(phase) * 28f : 0;
            if (rig.ArmL != null) rig.ArmL.localRotation = Quaternion.Euler(swing, 0, 4);
            if (rig.ArmR != null) rig.ArmR.localRotation = Quaternion.Euler(-swing * 0.6f, 0, -4);

            float y = walking ? Mathf.Abs(Mathf.Sin(phase)) * 0.03f : Mathf.Sin(phase) * 0.004f;
            if (hop > 0) { hop -= dt; y += Mathf.Abs(Mathf.Sin(hop * Mathf.PI * 2f)) * 0.25f; }
            var p = transform.position; p.y = y; transform.position = p;

            if (rig.Head != null && !rig.InStroller)
            {
                float yaw = 0;
                if (shake > 0) { shake -= dt; yaw = Mathf.Sin(shake * 25f) * 25f; }
                rig.Head.localRotation = Quaternion.Euler(0, yaw, 0);
            }
        }
    }

    public class CustomerCrowd : MonoBehaviour
    {
        readonly List<PersonAgent> current = new List<PersonAgent>();
        PersonRig leader;

        public void Spawn(Customer c)
        {
            LeaveAll();
            var a = GameData.Archetypes[c.ArchKey];
            var outfit = Look.Hex(a.Outfit);
            PersonAgent mother = null;
            for (int i = 0; i < c.Members.Count; i++)
            {
                var kind = c.Members[i];
                // グループの服は少しずつ色を変える
                var col = Color.Lerp(outfit, i % 2 == 0 ? Color.white : Color.black, (i % 3) * 0.15f);
                var go = PersonFactory.Build(kind, col, transform, a.Name + i, c.Hour * 7 + i);
                go.transform.position = ShopView.DoorOutside + new Vector3(Random.Range(-0.3f, 0.3f), 0, i * 0.3f);
                go.transform.rotation = Quaternion.Euler(0, 180, 0);
                var agent = go.AddComponent<PersonAgent>();
                if (kind == Body.Child) agent.Speed = 1.6f;
                if (kind == Body.Elder) agent.Speed = 0.8f;
                if (kind == Body.Baby) agent.Follow = mother;
                else if (mother == null) mother = agent;
                agent.Enter(i * 0.5f);
                current.Add(agent);
            }
            leader = current.Count > 0 ? current[0].GetComponent<PersonRig>() : null;
            if (leader != null) PersonFactory.Hold(leader, c.Item);
        }

        public void React(ServeResult r)
        {
            foreach (var p in current)
            {
                if (p == null) continue;
                if (r.Delighted) p.Hop();
                else if (!r.Bought) { p.ShakeHead(); p.Leave(); }
            }
            if (!r.Bought) current.Clear();
        }

        public void LeaveAll()
        {
            foreach (var p in current) if (p != null) p.Leave();
            current.Clear();
            leader = null;
        }

        /// <summary>ふきだしを出す位置（先頭の人の頭の上）。いなければ null。</summary>
        public Vector3? BubbleAnchor()
        {
            if (leader == null || leader.Head == null) return null;
            return leader.Head.position + Vector3.up * 0.35f;
        }
    }
}
