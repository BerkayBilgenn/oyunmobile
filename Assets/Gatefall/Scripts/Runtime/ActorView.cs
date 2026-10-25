using UnityEngine;
using UnityEngine.Rendering;

namespace Gatefall
{
    /// <summary>Reusable procedural actor, avoiding per-frame instantiate/destroy.</summary>
    public sealed class ActorView
    {
        public GameObject Root;
        public Transform LeftLeg, RightLeg, LeftArm, RightArm, Head;
        public Vector3 BaseScale;
        public int Id;
        public float Flash;
        private Transform body;

        public static ActorView Robot(Transform parent, Material bodyMaterial, Material darkMaterial, Material eyeMaterial, bool boss = false)
        {
            var actor = new ActorView { Root = new GameObject(boss ? "Titan" : "Raider") };
            actor.Root.transform.SetParent(parent, false);
            actor.body = Part("Torso", PrimitiveType.Cube, actor.Root.transform, new Vector3(0, .65f, 0), new Vector3(.4f, .48f, .29f), bodyMaterial);
            actor.Head = Part("Head", PrimitiveType.Sphere, actor.Root.transform, new Vector3(0, 1.03f, 0), Vector3.one * .36f, bodyMaterial);
            Part("Visor", PrimitiveType.Cube, actor.Head, new Vector3(0, .03f, -.42f), new Vector3(.72f, .19f, .22f), darkMaterial);
            Part("Eyes", PrimitiveType.Cube, actor.Head, new Vector3(0, .03f, -.55f), new Vector3(.46f, .09f, .12f), eyeMaterial);
            actor.LeftLeg = Limb("Left leg", actor.Root.transform, new Vector3(-.12f, .44f, 0), new Vector3(.16f, .42f, .2f), darkMaterial, -.2f);
            actor.RightLeg = Limb("Right leg", actor.Root.transform, new Vector3(.12f, .44f, 0), new Vector3(.16f, .42f, .2f), darkMaterial, -.2f);
            actor.LeftArm = Limb("Left arm", actor.Root.transform, new Vector3(-.27f, .83f, 0), new Vector3(.14f, .4f, .17f), bodyMaterial, -.17f);
            actor.RightArm = Limb("Right arm", actor.Root.transform, new Vector3(.27f, .83f, 0), new Vector3(.14f, .4f, .17f), bodyMaterial, -.17f);
            if (boss)
            {
                for (int i = 0; i < 3; i++)
                    Part("Armor rib", PrimitiveType.Cube, actor.body, new Vector3(0, .25f - i * .3f, -.54f), new Vector3(1.2f, .12f, .16f), darkMaterial);
                Part("Core", PrimitiveType.Sphere, actor.body, new Vector3(0, .02f, -.65f), Vector3.one * .45f, eyeMaterial);
                Part("Left pauldron", PrimitiveType.Sphere, actor.LeftArm, new Vector3(0, -.01f, 0), Vector3.one * .4f, bodyMaterial);
                Part("Right pauldron", PrimitiveType.Sphere, actor.RightArm, new Vector3(0, -.01f, 0), Vector3.one * .4f, bodyMaterial);
            }
            actor.BaseScale = Vector3.one;
            return actor;
        }

        public static ActorView Scout(Transform parent, Material material, Material glow)
        {
            var actor = new ActorView { Root = new GameObject("Blue scout") };
            actor.Root.transform.SetParent(parent, false);
            Part("Body", PrimitiveType.Capsule, actor.Root.transform, new Vector3(0, .29f, 0), new Vector3(.22f, .19f, .22f), material);
            Part("Helmet", PrimitiveType.Sphere, actor.Root.transform, new Vector3(0, .53f, 0), Vector3.one * .23f, glow);
            Part("Trail", PrimitiveType.Cube, actor.Root.transform, new Vector3(0, .13f, -.24f), new Vector3(.08f, .06f, .36f), glow);
            actor.BaseScale = Vector3.one;
            return actor;
        }

        public void Animate(float time, float scale, bool boss, bool playing)
        {
            float phase = time * (boss ? 3f : 11f) + Id * 1.3f;
            if (LeftLeg != null)
            {
                float stride = playing ? Mathf.Sin(phase) * 25 : 0;
                LeftLeg.localRotation = Quaternion.Euler(stride, 0, 0);
                RightLeg.localRotation = Quaternion.Euler(-stride, 0, 0);
                LeftArm.localRotation = Quaternion.Euler(-stride, 0, boss ? -14 : -7);
                RightArm.localRotation = Quaternion.Euler(stride, 0, boss ? 14 : 7);
            }
            float hit = 1 + Flash * .1f;
            Root.transform.localScale = Vector3.one * scale * hit;
        }

        public static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            var collider = obj.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            return obj.transform;
        }

        private static Transform Limb(string name, Transform parent, Vector3 position, Vector3 scale, Material material, float offset)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false); pivot.localPosition = position;
            Part("Segment", PrimitiveType.Cube, pivot, new Vector3(0, offset, 0), scale, material);
            return pivot;
        }
    }
}
