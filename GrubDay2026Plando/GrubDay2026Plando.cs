using Architect.Attributes.Config;
using Architect.Content;
using Architect.Content.Elements;
using Architect.Content.Groups;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Internal;
using Modding;
using Satchel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using static UnityEngine.ParticleSystem;
using UObject = UnityEngine.Object;

namespace GrubDay2026Plando
{
    public class Dummy : MonoBehaviour { }
    public class Dummy2 : MonoBehaviour { }

    public class SoulOrbSpawner : MonoBehaviour
    {
        public static void SpawnSoul(Transform transform)
        {
            var prefab = ObjectCache.SoulOrb;
            Destroy(prefab.Spawn());
            prefab.SetActive(true);

            // Give 17 soul per usage (somehow thats have a cast).
            // double if dream wielder is equipped
            // max if can dream gate.
            int soulToGive = 17;

            FlingUtils.Config config = new()
            {
                Prefab = prefab,
                AmountMin = soulToGive,
                AmountMax = soulToGive,
                SpeedMin = 5,
                SpeedMax = 10,
                AngleMin = 0,
                AngleMax = 360,
            };
            var objs = FlingUtils.SpawnAndFling(config, transform, Vector3.zero);

            IEnumerator Routine()
            {
                yield return new WaitForSeconds(10f);
                foreach (var o in objs)
                {
                    Destroy(o);
                }
            }

            GameManager.instance.gameObject.GetOrAddComponent<SoulOrbSpawner>().StartCoroutine(Routine());
            Destroy(prefab);
        }
    }


    [Serializable]
    public class EmbeddedSprite : ISprite
    {
        private static SpriteManager EmbeddedSpriteManager = new(typeof(EmbeddedSprite).Assembly, "GrubDay2026Plando.Resources.");

        public string key;
        public EmbeddedSprite(string key)
        {
            this.key = key;
        }

        [Newtonsoft.Json.JsonIgnore]
        public Sprite Value => EmbeddedSpriteManager.GetSprite(key);
        public ISprite Clone() => (ISprite)MemberwiseClone();
    }

    public class GrubDay2026PlandoMod : Mod
    {
        public static int waitCounter = 0;

        public override List<(string, string)> GetPreloadNames() => Preloader.Instance.GetPreloadNames();

        private static GrubDay2026PlandoMod _instance;

        internal static GrubDay2026PlandoMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(GrubDay2026PlandoMod)} was never constructed");
                }
                return _instance;
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public GrubDay2026PlandoMod() : base("GrubDay2026Plando")
        {
            _instance = this;
        }

        public static float scale = 2f;

        internal static void BumpUp()
        {
            var hc = HeroController.instance;
            hc.ShroomBounce();

            var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
            var v = rb2d.velocity;
            v.y *= scale;
            rb2d.velocity = v;
        }

        internal static void BumpRight()
        {
            var hc = HeroController.instance;
            hc.ShroomBounce();

            var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
            var v = rb2d.velocity;
            v.x *= scale;
            rb2d.velocity = v;
        }

        public GameObject soulSwirl;

        private static Sprite[] sprites;

        public void SetupSprites()
        {
            sprites = new Sprite[7];
            for (int i = 0; i < sprites.Length; ++i)
            {
                sprites[i] = new EmbeddedSprite($"fireball_animation.frame_{i}").Value;
            }
        }

        public GameObject AddSoulSwirl(GameObject parent)
        {
            GameObject soulSwirlInsatlce = UObject.Instantiate(soulSwirl, parent.transform.position + new Vector3(-0.1f, -0.5f), Quaternion.identity);
            soulSwirlInsatlce.SetActive(true);
            soulSwirlInsatlce.transform.SetScaleX(parent.transform.GetScaleX() * 1.2f);
            soulSwirlInsatlce.transform.SetScaleY(parent.transform.GetScaleY() * 1.2f);
            soulSwirlInsatlce.transform.SetParent(parent.transform);
            soulSwirlInsatlce.FindChild("white_light").SetActive(false);
            soulSwirlInsatlce.FindChild("small_soul_cache_glow").SetActive(false);

            return soulSwirlInsatlce;
        }

        public GameObject AddBouncyFungal(GameObject parent)
        {
            GameObject bouncyShroom = UObject.Instantiate(Preloader.Instance.BounchShroom, parent.transform.position, Quaternion.identity);
            bouncyShroom.SetActive(true);
            bouncyShroom.transform.SetScaleX(parent.transform.GetScaleX());
            bouncyShroom.transform.SetScaleY(parent.transform.GetScaleY());
            bouncyShroom.transform.SetParent(parent.transform);
            bouncyShroom.GetComponent<BounceShroom>().enabled = false;
            bouncyShroom.GetComponent<CircleCollider2D>().enabled = false;
            bouncyShroom.GetComponent<tk2dSprite>().color = new Color(1,1,1,0.4f);
            bouncyShroom.FindChild("Phys Box").SetActive(false);

            return bouncyShroom;
        }

        static float modifier = 1f;

        public void RegisterPlandoPack(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            SetupSprites();
            var grubJarPreload2 = Preloader.Instance.GrubJar;
            soulSwirl = Preloader.Instance.SoulJar;

            var bottlePhysical = grubJarPreload2.FindChild("Bottle Physical");
            if (bottlePhysical != null)
            {   
                var origianlCollider = bottlePhysical.GetComponent<BoxCollider2D>();
                origianlCollider.enabled = false;

                var topWall = bottlePhysical.AddComponent<BoxCollider2D>();
                topWall.size = new Vector2(2.5f, 0.2f);
                topWall.offset = new Vector2(0, 0.75f);

                var floor = bottlePhysical.AddComponent<BoxCollider2D>();
                floor.size = new Vector2(2.5f, 0.2f);
                floor.offset = new Vector2(0, -1.5f);

                var rightWall = bottlePhysical.AddComponent<BoxCollider2D>();
                rightWall.size = new Vector2(0.2f, 2.46f);
                rightWall.offset = new Vector2(1.15f, -0.37f);

                var leftWall = bottlePhysical.AddComponent<BoxCollider2D>();
                leftWall.size = new Vector2(0.2f, 2.46f);
                leftWall.offset = new Vector2(-1.15f, -0.37f);

            }

            var grubjarsprite_c = new EmbeddedSprite("specialJar_container");
            var grubjarsprite_s = new EmbeddedSprite("specialJar_sturdy");
            var grubjarsprite_t = new EmbeddedSprite("specialJar_tangible");
            var grubjarsprite_r = new EmbeddedSprite("specialJar_rocket");

            grubJarPreload2.RemoveComponent<PersistentBoolItem>();

            var defaultNoGrub = new ConfigGroup(ConfigGroup.Generic,
                Architect.Attributes.ConfigManager.RegisterConfigType(
                    new BoolConfigType("Contains Grub", (o, value) =>
                    {
                        if (!value.GetValue())
                        {
                            o.transform.GetChild(1).gameObject.SetActive(false);
                            var bottleControlFSM = o.LocateMyFSM("Bottle Control");
                            var shatterState = FsmUtil.GetValidState(bottleControlFSM, "Shatter");
                            FsmUtil.GetFirstActionOfType<IncrementPlayerDataInt>(shatterState).Enabled = false;
                        }
                    }).WithDefaultValue(false), "has_grub"),
                Architect.Attributes.ConfigManager.RegisterConfigType(
                    new BoolConfigType("Respawn", (o, value) =>
                    {
                        if (!value.GetValue())
                        {

                            var item = o.GetComponent<PersistentBoolItem>();

                            if (!item)
                            {
                                item = o.AddComponent<PersistentBoolItem>();
                                item.persistentBoolData = new PersistentBoolData
                                {
                                    id = o.name,
                                    sceneName = o.scene.name
                                };
                                item.enabled = true;
                            }

                            item.semiPersistent = false;
                            item.persistentBoolData.semiPersistent = false;
                        }
                    }).WithDefaultValue(true), "respawn"),
                Architect.Attributes.ConfigManager.RegisterConfigType(new BoolConfigType("Breakable", (o, value) =>
                {
                    if (value.GetValue())
                    {
                        return;
                    }

                    o.RemoveComponent<PlayMakerFSM>();
                    o.RemoveComponent<HealthCocoon>();

                    if (GameManager.instance.sceneName == SceneNames.Town)
                    {
                        return;
                    }

                    var particleB = GameObject.Find("Knight/Effects/NA Charged");
                    GameObject particleBClone = UObject.Instantiate(particleB, o.transform.position + new Vector3(-0.1f, -0.5f), Quaternion.identity);
                    particleBClone.SetActive(true);
                    particleBClone.transform.SetScaleX(o.transform.GetScaleX());
                    particleBClone.transform.SetScaleY(o.transform.GetScaleY());
                    particleBClone.transform.SetParent(o.transform);

                }), "can_be_broken"));

            var sturdyJar = new ConfigGroup(defaultNoGrub,
                Architect.Attributes.ConfigManager.RegisterConfigType(
                    new BoolConfigType("Sturdy", (o, value) =>
                    {
                        if (value.GetValue())
                        {

                            var soulSwirlInsatlce = AddSoulSwirl(o);

                            var bottleControlFSM = o.LocateMyFSM("Bottle Control");
                            var idleState = FsmUtil.GetValidState(bottleControlFSM, "Idle");

                            var sr = o.FindChild("Bottle Physical/Dream Dialogue").AddComponent<SpriteRenderer>();
                            sr.transform.SetParent(o.transform);
                            sr.transform.SetPositionY(sr.transform.GetPositionY() - 0.1f);
                            sr.sprite = sprites[0];

                            Trigger2dEvent spellHit = idleState.Actions[0] as Trigger2dEvent;
                            spellHit.collideTag = "Hero Spell";
                            spellHit.collideLayer = "";
                            // Makes the object not pogoable
                            o.layer = LayerMask.NameToLayer("Hero Box");

                            idleState.AddCustomAction(() =>
                            {
                                if (o.GetComponent<Dummy2>() == null)
                                {
                                    o.AddComponent<Dummy2>().StartCoroutine(UpdateAnimation(sr));
                                }
                            });

                            var destroySelfState = FsmUtil.GetValidState(bottleControlFSM, "Destroy Self");

                            destroySelfState.AddCustomAction(() =>
                            {
                                soulSwirlInsatlce.SetActive(false);
                                o.GetComponent<Dummy2>().StopAllCoroutines();
                                var sr = o.FindChild("Bottle Physical/Dream Dialogue").GetComponent<SpriteRenderer>();
                                sr.enabled = false;
                                SoulOrbSpawner.SpawnSoul(o.transform);
                            });

                        }

                    }).WithDefaultValue(true), "is_sturdy"));

            var rocketJar = new ConfigGroup(defaultNoGrub,
                Architect.Attributes.ConfigManager.RegisterConfigType(
                    new BoolConfigType("Rocket", (o, value) =>
                    {
                        if (value.GetValue())
                        {
                            var fungal = AddBouncyFungal(o);
                            var bottleControlFSM = o.LocateMyFSM("Bottle Control");
                            var destroySelfState = FsmUtil.GetValidState(bottleControlFSM, "Destroy Self");

                            o.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0, 0.8f);

                            destroySelfState.AddCustomAction(() =>
                            {
                                fungal.SetActive(false);
                                BumpUp();
                            });
                        }
                    }).WithDefaultValue(true), "is_rocket"));

            var intangibleJarGroup = new ConfigGroup(defaultNoGrub,
                Architect.Attributes.ConfigManager.RegisterConfigType(
                    new BoolConfigType("Intangible", (o, value) =>
                    {
                        if (!value.GetValue())
                        {
                            var soulSwirlInsatlce = AddSoulSwirl(o);

                            var bottleControlFSM = o.LocateMyFSM("Bottle Control");
                            var idleState = FsmUtil.GetValidState(bottleControlFSM, "Idle");
                            var tangible = FsmUtil.AddState(bottleControlFSM, "Make Tangible");
                            FsmUtil.AddTransition(tangible, "FINISHED", "Idle");

                            Trigger2dEvent spellHit = idleState.Actions[0] as Trigger2dEvent;
                            spellHit.collideTag = "Hero Spell";
                            spellHit.collideLayer = "";

                            // var fbsr = o.FindChild("Pt Glass L");
                            var bp = o.FindChild("Bottle Physical/Dream Dialogue").AddComponent<SpriteRenderer>();
                            bp.transform.SetParent(o.transform);
                            bp.transform.SetPositionY(bp.transform.GetPositionY() - 0.1f);
                            bp.sprite = sprites[0];

                            tangible.AddCustomAction(() => {
                                var d = o.GetComponent<Dummy>();
                                d.StopAllCoroutines();
                                d.enabled = false;
                                bottleControlFSM.SetState("Idle");
                                idleState.RemoveTransitionsOn("NAIL HIT");
                                FsmUtil.AddTransition(idleState, "NAIL HIT", "Shatter");

                                 o.FindChild("Bottle Physical").SetActive(true);
                                 o.GetComponent<BoxCollider2D>().enabled = true;
                                 o.layer = LayerMask.NameToLayer("Interactive Object");

                                spellHit.collideTag = "Nail Attack";
                                spellHit.collideLayer = "";

                                var sr = o.GetComponent<SpriteRenderer>();
                                sr.color = new Color(1f, 1f, 1f, 1f);

                                var fbsr = o.FindChild("Bottle Physical/Dream Dialogue").GetComponent<SpriteRenderer>();
                                fbsr.enabled = false;
                            });

                            idleState.RemoveTransitionsOn("NAIL HIT");
                            FsmUtil.AddTransition(idleState, "NAIL HIT", "Make Tangible");

                            var sr = o.GetComponent<SpriteRenderer>();
                            sr.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);

                            o.FindChild("Bottle Physical").SetActive(false);
                            o.layer = LayerMask.NameToLayer("Hero Box");

                            idleState.AddCustomAction(() =>
                            {
                                if (o.GetComponent<Dummy>() == null)
                                {
                                    var d = o.AddComponent<Dummy>();
                                    d.StartCoroutine(UpdateOpacity(sr, d));
                                }
                                if (o.GetComponent<Dummy2>() == null)
                                {
                                    o.AddComponent<Dummy2>().StartCoroutine(UpdateAnimation(bp));
                                }
                            });

                            var destroySelfState = FsmUtil.GetValidState(bottleControlFSM, "Destroy Self");

                            destroySelfState.AddCustomAction(() =>
                            {
                                soulSwirlInsatlce.SetActive(false);
                                SoulOrbSpawner.SpawnSoul(o.transform);
                            });
                        }
                    }).WithDefaultValue(false), "is_tangible")
            );

            var customJars = new ContentPack("Grub day 2026", "Assets for thinking with Jars")
            {
                // new SimplePackElement(grubJarPreload1, "MyGrubJar1", "Interactable"), //, grubjarsprite.Value, 1).WithConfigGroup(ConfigGroup.Grub).WithRotationGroup(RotationGroup.Four)
                new SimplePackElement(grubJarPreload2, "Container Jar", "Grub Plando", grubjarsprite_c.Value, 1).WithConfigGroup(defaultNoGrub).WithRotationGroup(RotationGroup.Four),
                new SimplePackElement(grubJarPreload2, "Sturdy Jar", "Grub Plando", grubjarsprite_s.Value, 1).WithConfigGroup(sturdyJar).WithRotationGroup(RotationGroup.Four),
                new SimplePackElement(grubJarPreload2, "Intangible Jar", "Grub Plando", grubjarsprite_t.Value, 1).WithConfigGroup(intangibleJarGroup).WithRotationGroup(RotationGroup.Four),
                new SimplePackElement(grubJarPreload2, "Rocket Jar", "Grub Plando", grubjarsprite_r.Value, 1).WithConfigGroup(rocketJar).WithRotationGroup(RotationGroup.Four),
            };

            ContentPacks.RegisterPack(customJars);
        }
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");
            try
            {
                Preloader.Instance.Initialize(preloadedObjects);
            } catch (KeyNotFoundException e)
            {
                Log(e.Message);
                throw;
            }

            RegisterPlandoPack(preloadedObjects);

            Log("Initialized");
        }

        public IEnumerator UpdateOpacity(SpriteRenderer sr, Dummy d)
        {
            float so = 0.5f;
            float delta = 0.05f;

            while (d.enabled)
            {
                sr.color = new Color(so, so, so, so);
                so += delta;
                if (so >= 1) delta = -0.05f;
                if (so <= 0.5f) delta = 0.05f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        public IEnumerator UpdateAnimation(SpriteRenderer sr)
        {
            int nextFrame = 0;
            while (true)
            {
                sr.sprite = sprites[nextFrame];
                nextFrame++;
                if (nextFrame >= sprites.Length)
                {
                    nextFrame = 0;
                }

                yield return new WaitForSecondsRealtime(0.08f);
            }
        }
    }
}

