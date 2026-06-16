using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Internal.Preloaders;
using Modding;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GrubDay2026Plando.Modules
{
    // A modified version of Purenail's The Hoarder
    // Original source version by Purenail:
    // https://github.com/dplochcoder/HollowKnight.SpicyRando/blob/main/SpicyRando/IC/HoarderModule.cs


    // This version of The Pickler exists for the "directors cut" version of the plando.
    internal class UltraJarSpawnAdjuster : MonoBehaviour
    {
        internal record JarSpawn
        {
            internal Func<GameObject> spawner;
            internal StatSelector<int> hp;
            internal float yBump = 0;
            internal float yVelBump = 0;
            internal float rXVelBump = 0;
            internal Action<GameObject>? customHook;
            internal bool isEnemy = true;

            internal int index;

            internal void Apply(FsmState state, UltraJarSpawnAdjuster adjuster, UltraPickler mod)
            {
                var prefab = spawner.Invoke();
                mod.SetPostSpawnHook(prefab, PostSpawnHooks);

                var action = state.GetFirstActionOfType<SetSpawnJarContents>();
                action.enemyPrefab.Value = prefab;
                if (isEnemy)
                {
                    action.enemyHealth.Value = adjuster.Select(hp);
                }
            }

            private void PostSpawnHooks(GameObject obj)
            {
                var egg = obj.GetComponent<JellyEgg>();
                if (egg == null)
                {
                    isEnemy = true;
                } else
                {
                    isEnemy = false;
                }

                if (isEnemy)
                {
                    if (yBump != 0) obj.transform.SetPositionY(obj.transform.position.y + yBump);
                    if (yVelBump != 0 || rXVelBump != 0)
                    {
                        var rb2d = obj.GetComponent<Rigidbody2D>();
                        rb2d.velocity += new Vector2(rXVelBump * UnityEngine.Random.Range(-1f, 1f), yVelBump);
                    }
                    obj.AddComponent<EnemyCleanup>().index = index;

                    // No geo farming.
                    var health = obj.GetComponent<HealthManager>();
                    health.SetGeoSmall(0);
                    health.SetGeoMedium(0);
                    health.SetGeoLarge(0);

                    MakeBombImmune(obj);
                }
                else
                {
                    obj.AddComponent<ObjectCleanup>().index = index;
                }

                customHook?.Invoke(obj);
            }
        }

        internal class EnemyCleanup : MonoBehaviour
        {
            internal int? index;
            private bool updated = false;

            private void Update()
            {
                if (!updated && index != null)
                {
                    updated = true;
                    JarSpawnThreshold.OnCleanupIndex += Cleanup;
                }
            }

            private void Cleanup(int cleanupIndex)
            {
                if (index <= cleanupIndex)
                {
                    var health = gameObject.GetComponent<HealthManager>();
                    health.ApplyExtraDamage(999);
                }
            }

            private void OnDestroy() => JarSpawnThreshold.OnCleanupIndex -= Cleanup;
        }

        internal class ObjectCleanup : MonoBehaviour
        {
            internal int? index;
            private bool updated = false;

            private void Update()
            {
                if (!updated && index != null)
                {
                    updated = true;
                    JarSpawnThreshold.OnCleanupIndex += Cleanup;
                }
            }

            private void Cleanup(int cleanupIndex)
            {
                if (index <= cleanupIndex)
                {
                    gameObject.SetActive(false);
                    GameObject.Destroy(gameObject);
                }
            }

            private void OnDestroy() => JarSpawnThreshold.OnCleanupIndex -= Cleanup;
        }

        internal record JarSpawnThreshold
        {
            internal static event Action<int>? OnCleanupIndex;
            internal static void CleanupIndex(int index) => OnCleanupIndex?.Invoke(index);

            internal (StatSelector<int>, StatSelector<int>) spawnCounts;
            internal StatSelector<int> hpSize;
            internal JarSpawn spawn1;
            internal JarSpawn spawn2;
            internal JarSpawn spawn3;

            // Derived fields
            private int index;
            internal int hpThreshold;

            internal void SetIndex(int index)
            {
                this.index = index;
                spawn1.index = index;
                spawn2.index = index;
                spawn3.index = index;
            }

            internal int GetIndex() => index;

            internal void Apply(PlayMakerFSM fsm, UltraJarSpawnAdjuster adjuster, UltraPickler mod)
            {
                spawn1.Apply(fsm.GetState("Buzzer"), adjuster, mod);
                spawn2.Apply(fsm.GetState("Spitter"), adjuster, mod);
                spawn3.Apply(fsm.GetState("Roller"), adjuster, mod);
            }
        }

        internal static List<JarSpawnThreshold> SpawnLists()
        {
            return
            [
            new()
            {
                spawnCounts = (new(5), new(5)),
                hpSize = new(600),
                spawn1 = new()
                {
                    spawner = Preloader.Instance.ArmoredSquitCage.ExtractFromCage,
                    hp = new(45),
                    yBump = 0.3f,
                    yVelBump = 1,
                },
                spawn2 = new()
                {
                    spawner = Preloader.Instance.ArmoredBaldurCage.ExtractFromCage,
                    hp = new(45),
                    yBump = 0.1f,
                },
                spawn3 = new()
                {
                    spawner = Preloader.Instance.PrimalAspidCage.ExtractFromCage,
                    hp = new(45),
                    yBump = 0.25f,
                    yVelBump = 2,
                }
            },
            new()
            {
                spawnCounts = (new(4), new(8)),
                hpSize = new(600),
                spawn1 = new()
                {
                    spawner = Preloader.Instance.InfectedVengefly.ExtractFromCage,
                    hp = new(80),
                    yBump = 0.25f,
                    yVelBump = 0.25f,
                },
                spawn2 = new()
                {
                    spawner = Preloader.Instance.InfectedVengefly.ExtractFromCage,
                    hp = new(45),
                    yBump = 0.3f,
                    yVelBump = 1,
                },
                spawn3 = new()
                {
                    spawner = Preloader.Instance.InfectedVengefly.ExtractFromCage,
                    hp = new(30),
                    yBump = 0.3f,
                    yVelBump = 1,
                }
            },
            new()
            {
                spawnCounts = (new(5), new(5)),
                hpSize = new(500, 500),
                spawn1 = new()
                {
                    spawner = Preloader.Instance.InfectedVengefly.ExtractFromCage,
                    hp = new(30),
                    yBump = 0.3f,
                    yVelBump = 1,
                },
                spawn2 = new()
                {
                    spawner = () => Preloader.Instance.Sibling,
                    hp = new(80),
                    yBump = 0.3f,
                    yVelBump = 1,
                },
                spawn3 = new()
                {
                    spawner = () => Preloader.Instance.GreatHuskSentry,
                    hp = new(60),
                    yBump = 1f,
                    yVelBump = 1,
                    customHook = AdjustGHS,
                }
            },
            new()
            {
                spawnCounts = (new(10), new(20)),
                hpSize = new(500),
                spawn1 = new()
                {
                    spawner = () => Preloader.Instance.JellyEggBomb,
                    isEnemy = false,
                    customHook = PopEggBomb
                },
                spawn2 = new()
                {
                    spawner = () => Preloader.Instance.JellyEggBomb,
                    isEnemy = false,
                    customHook = PopEggBomb
                },
                spawn3 = new()
                {
                    spawner = () => Preloader.Instance.JellyEggBomb,
                    isEnemy = false,
                    customHook = PopEggBomb
                }
            },
            new()
            {
                spawnCounts = (new(5), new(10)),
                hpSize = new(700),
                spawn1 = new()
                {
                    spawner = RandomSpawner,
                    isEnemy = false,
                    customHook = PopEggBomb
                },
                spawn2 = new()
                {
                    spawner = RandomSpawner,
                    isEnemy = false,
                    customHook = PopEggBomb
                },
                spawn3 = new()
                {
                    spawner = RandomSpawner,
                    isEnemy = false,
                    customHook = PopEggBomb
                }
            },
        ];
        }

        static Func<GameObject>[] options = new Func<GameObject>[]
        {
                () => Preloader.Instance.JellyEggBomb,
                () => Preloader.Instance.JellyEggBomb,
                Preloader.Instance.InfectedVengefly.ExtractFromCage,
                () => Preloader.Instance.GreatHuskSentry,
                () => Preloader.Instance.Sibling,
        };

        private static GameObject RandomSpawner()
        {
            int check = UnityEngine.Random.Range(0, 5);


            return options[check]();
        }

        private readonly List<JarSpawnThreshold> thresholds = SpawnLists();
        private readonly FsmInt phase2hp = new(0);
        private UltraPickler? mod;
        private bool plando;
        private bool initialized = false;

        private HealthManager? healthManager;
        private PlayMakerFSM? collectorFsm;
        private JarSpawnThreshold? currentThreshold;

        internal void SetMod(UltraPickler module, bool plando)
        {
            mod = module;
            this.plando = plando;
        }

        internal T Select<T>(StatSelector<T> ns) => ns.Get(plando);

        private bool blockMultiSummon = false;

        private bool MaybeInit()
        {
            if (initialized) return true;
            if (mod == null) return false;

            initialized = true;
            healthManager = GetComponent<HealthManager>();
            collectorFsm = gameObject.LocateMyFSM("Control");

            if (!plando)
            {
                collectorFsm.GetState("Summon?").AddFirstAction(new Lambda(() =>
                {
                    blockMultiSummon = currentThreshold!.GetIndex() >= 3 && (GameObject.FindGameObjectsWithTag("Boss")?.Length ?? 0) >= 2;
                }));
                collectorFsm.GetState("Resummon?").AddFirstAction(new Lambda(() =>
                {
                    if (blockMultiSummon) collectorFsm.SendEvent("END");
                }));
            }

            int total = 0;
            for (int i = thresholds.Count - 1; i >= 0; --i)
            {
                var t = thresholds[i];
                t.SetIndex(i);

                t.hpThreshold = total;
                total += Select(t.hpSize);
            }
            phase2hp.Value = thresholds[2].hpThreshold + thresholds[2].hpSize.Get(plando) / 2;
            return true;
        }

        private const int STARTING_THRESHOLD = 0;

        internal int CollectorHp() => Select(thresholds[STARTING_THRESHOLD].hpSize) + thresholds[STARTING_THRESHOLD].hpThreshold;

        internal FsmInt Phase2Hp() => phase2hp;

        private JarSpawnThreshold? GetCurrentThreshold() => thresholds.Where(t => t.hpThreshold <= healthManager!.hp).FirstOrDefault();

        private void Update()
        {
            if (!MaybeInit()) return;

            var next = GetCurrentThreshold();
            if (next != null && next.hpThreshold != (currentThreshold?.hpThreshold ?? -1))
            {
                currentThreshold = next;
                currentThreshold.Apply(collectorFsm!, this, mod!);

                // Don't allow hoarding of easy enemies.
                JarSpawnThreshold.CleanupIndex(currentThreshold.GetIndex() - 2);

                var vars = gameObject.LocateMyFSM("Control").FsmVariables;
                var (min, max) = currentThreshold.spawnCounts;
                vars.GetFsmInt("Spawn Min").Value = Select(min);
                vars.GetFsmInt("Spawn Max").Value = Select(max);
            }
        }

        private const float OBLOBBLE_SCALE = 0.6f;

        private static void MakeBombImmune(GameObject obj)
        {
            obj.AddComponent<JellyEggBombImmunity>();

        }

        private static void AdjustGHS(GameObject obj)
        {
            obj.transform.localScale = new(OBLOBBLE_SCALE, OBLOBBLE_SCALE, OBLOBBLE_SCALE);
            obj.AddComponent<ZFixer>();
        }

        internal static void BumpUp()
        {
            var hc = HeroController.instance;
            hc.ShroomBounce();

            var rb2d = hc.gameObject.GetComponent<Rigidbody2D>();
            var v = rb2d.velocity;
            v.y *= 2;
            rb2d.velocity = v;
        }

        private static void PopEggBomb(GameObject obj)
        {
            var egg = obj.GetComponent<JellyEgg>();
            if (egg == null)
            {
                return;
            }

            int check = UnityEngine.Random.Range(0, 10);
            switch (check)
            {
                case 0:
                    // 1/10 to give you back a soul charge
                    SoulOrbSpawner.SpawnSoul(obj.transform);
                    break;
                case 1:
                    // 1/10 to bump you up
                    BumpUp();
                    break;
            }

            obj.GetComponent<JellyEgg>().Invoke("Burst", 0f);
            GameObject.Destroy(obj);
        }

        //private static void AdjustVengefly(GameObject obj)
        //{
        //    // Trying to make explosion not hurt enemies.
        //    GameObject.Find("Gas Explosion L").LocateMyFSM("damages_enemy").enabled = false;
        //}

        private const float MARMU_SCALE = 0.8f;

        private const float WATCHER_KNIGHT_SCALE = 0.725f;

    }

    internal class UltraPickler : ItemChanger.Modules.Module
    {
        private static readonly FsmID CONTROL = new("Jar Collector", "Control");
        private static readonly FsmID PHASE_CONTROL = new("Jar Collector", "Phase Control");
        private static readonly FsmID STUN_CONTROL = new("Jar Collector", "Stun Control");

        private ILHook? spawnHook;

        public bool? ForPlando;
        public int NumAttempts = 0;
        private Texture2D _tex;

        public void SetupTextureSwap(GameObject jarCollector)
        {
            jarCollector.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = _tex;
        }


        public void LoadTextures()
        {
            var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("GrubDay2026Plando.Resources.Pickler.png");
            byte[] buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            _tex = new Texture2D(2, 2);
            _tex.LoadImage(buffer, markNonReadable: true);
        }

        public override void Initialize()
        {
            Events.AddFsmEdit(CONTROL, ModifyCollectorFight);
            Events.AddFsmEdit(PHASE_CONTROL, BuffPhaseControl);
            Events.AddFsmEdit(STUN_CONTROL, EliminateStunControl);
            spawnHook = new(typeof(SpawnJarControl).GetMethod("Behaviour", BindingFlags.Instance | BindingFlags.NonPublic).GetStateMachineTarget(), HookSpawnCustomJar);
            ModHooks.LanguageGetHook += LanguageGetHook;

            LoadTextures();
        }

        public override void Unload()
        {
            Events.RemoveFsmEdit(CONTROL, ModifyCollectorFight);
            Events.RemoveFsmEdit(PHASE_CONTROL, BuffPhaseControl);
            Events.RemoveFsmEdit(STUN_CONTROL, EliminateStunControl);
            spawnHook?.Dispose();
            ModHooks.LanguageGetHook -= LanguageGetHook;
        }

        private static void BroadcastAll(PlayMakerFSM fsm, string eventName)
        {
            FsmEventTarget target = new()
            {
                target = FsmEventTarget.EventTarget.BroadcastAll
            };
            fsm.Fsm.Event(target, eventName);
        }

        private static void TightenBattleGate(GameObject gate)
        {
            gate.name = $"Renamed Gate {gate.name}";

            var fsm = gate.LocateMyFSM("BG Control");
            fsm.AddFsmBool("REALLY OPEN", false);

            fsm.GetState("Destroy").RemoveActionsOfType<DestroySelf>();
            fsm.GetState("Quick Open").AddFirstAction(new Lambda(() => fsm.SetState("Close 2")));
            fsm.GetState("Open").AddFirstAction(new Lambda(() =>
            {
                if (!fsm.FsmVariables.GetFsmBool("REALLY OPEN").Value) fsm.SetState("Close 2");
            }));
        }

        private static void ReallyOpen(GameObject gate)
        {
            var fsm = gate.LocateMyFSM("BG Control");
            fsm.FsmVariables.GetFsmBool("REALLY OPEN").Value = true;
            fsm.SetState("Open");
        }

        private static int ChooseHops(bool lunged)
        {
            var c = UnityEngine.Random.Range(0f, 1f);
            if (lunged) return c < 0.5f ? 1 : 2;
            else
            {
                if (c < 0.25f) return 1;
                else if (c < 0.75f) return 2;
                else return 3;
            }
        }

        private static bool ChooseLunge(List<bool> lunged)
        {
            if (!lunged[0] && UnityEngine.Random.Range(0f, 1f) < 0.6f)
            {
                lunged[0] = true;
                return true;
            }
            else
            {
                lunged[0] = false;
                return false;
            }
        }

        private void ModifyCollectorFight(PlayMakerFSM fsm)
        {
            SetupTextureSwap(fsm.gameObject);

            // Adjust jar spawns.
            var jarAdjuster = fsm.gameObject.GetOrAddComponent<UltraJarSpawnAdjuster>();
            jarAdjuster.SetMod(this, true);

            // Adapt hp.
            var healthManager = fsm.gameObject.GetComponent<HealthManager>();
            fsm.gameObject.AddComponent<JellyEggBombImmunity>();

            fsm.GetState("Start Fall").AddFirstAction(new Lambda(() => healthManager.hp = jarAdjuster.CollectorHp()));

            if (!(true))
            {
                List<bool> lunged = [false];
                var setHops = fsm.GetState("Set Hops");
                setHops.RemoveActionsOfType<RandomInt>();
                setHops.AddLastAction(new Lambda(() => fsm.FsmVariables.GetFsmInt("Hops").Value = ChooseHops(lunged[0])));

                var moveChoice = fsm.GetState("Move Choice");
                moveChoice.RemoveActionsOfType<SendRandomEventV2>();
                moveChoice.AddFirstAction(new Lambda(() => fsm.SendEvent(ChooseLunge(lunged) ? "LUNGE" : "JUMP AWAY")));

                fsm.GetState("Jump Antic").AddFirstAction(new Lambda(() => lunged[0] = false));
            }

            var roar = fsm.GetState("Roar");
            roar.AddFirstAction(new Lambda(() => roar.GetFirstActionOfType<SetFsmString>().setValue = LanguageKey(++NumAttempts)));

            // Fix up the gates. Some enemies try to open them when they die.
            var bg1 = GameObject.Find("Battle Gate");
            var bg2 = GameObject.Find("Battle Gate (1)");

            var battleScene = fsm.gameObject.transform.parent.gameObject;
            var bFsm = battleScene.LocateMyFSM("Control");
            bFsm.GetState("End").AddLastAction(new Lambda(() =>
            {
                ReallyOpen(bg1);
                ReallyOpen(bg2);
            }));

            TightenBattleGate(bg1);
            TightenBattleGate(bg2);
        }

        private void BuffPhaseControl(PlayMakerFSM fsm)
        {
            var jarAdjuster = fsm.gameObject.GetOrAddComponent<UltraJarSpawnAdjuster>();
            fsm.GetState("Check").GetFirstActionOfType<IntCompare>().integer2 = jarAdjuster.Phase2Hp();

            var phase2 = fsm.GetState("Phase 2");
            while (phase2.Actions.Length > 0) phase2.RemoveAction(0);

            phase2.AddLastAction(new Lambda(() =>
            {
                var vars = fsm.gameObject.LocateMyFSM("Control").FsmVariables;
                vars.GetFsmFloat("Resummon Pause").Value = 0.35f;
                vars.GetFsmFloat("Hop X Speed").Value = -12.5f;
            }));
        }

        private void EliminateStunControl(PlayMakerFSM fsm)
        {
            var vars = fsm.FsmVariables;
            vars.FindFsmInt("Stun Combo").Value = 999;
            vars.FindFsmInt("Stun Hit Max").Value = 999;
        }

        private void HookSpawnCustomJar(ILContext il)
        {
            ILCursor cursor = new(il);
            cursor.Goto(0);
            cursor.GotoNext(i => i.MatchLdfld<SpawnJarControl>("enemyToSpawn"));
            cursor.GotoNext(i => i.MatchCall(typeof(ObjectPoolExtensions).FullName, nameof(ObjectPoolExtensions.Spawn)));
            cursor.Remove();
            cursor.EmitDelegate(SpawnCustomJar);
        }

        private readonly Dictionary<GameObject, Action<GameObject>> postSpawnHooks = [];

        internal void SetPostSpawnHook(GameObject prefab, Action<GameObject> hook) => postSpawnHooks[prefab] = hook;

        private GameObject SpawnCustomJar(GameObject self, Vector3 position)
        {
            var obj = UnityEngine.Object.Instantiate(self);
            UnityEngine.Object.Destroy(obj.GetComponent<PersistentBoolItem>());

            obj.transform.position = position;
            obj.SetActive(true);
            if (postSpawnHooks.TryGetValue(self, out var hook)) hook.Invoke(obj);

            return obj;
        }

        private static string LanguageKey(int attempt) => "PICKLER";

        private static string LanguageGetHook(string key, string sheetTitle, string orig)
        {
            return key switch
            {
                "PICKLER_SUPER" => "Ultra",
                "PICKLER_MAIN" => "Pickler",
                "PICKLER_SUB" => "",
                _ => orig,
            };
        }
    }
}