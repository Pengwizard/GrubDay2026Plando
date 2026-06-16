using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Modules;
using System.Linq;
using UnityEngine;

namespace GrubDay2026Plando.Modules
{
    // I screwed up the item pool entirely lmao so you can swim without the item.
    // This module makes all water a hazard, permanently. Useful for plando makers that forgot to put swim in Seal of Binding or something.
    internal class DisableSwimming : Module
    {
        public bool canSwim { get; set; }

        public override void Initialize()
        {
            GrubDay2026PlandoMod.Instance.Log("Initializing disable swim");
            Events.AddFsmEdit(new("Surface Water Region"), EditWaterSurface);
            Modding.ModHooks.GetPlayerBoolHook += SkillBoolGetOverride;
            Modding.ModHooks.SetPlayerBoolHook += SkillBoolSetOverride;
            canSwim = false;
        }

        public override void Unload()
        {
            Events.RemoveFsmEdit(new("Surface Water Region"), EditWaterSurface);
            Modding.ModHooks.GetPlayerBoolHook -= SkillBoolGetOverride;
            Modding.ModHooks.SetPlayerBoolHook -= SkillBoolSetOverride;
            canSwim = true;
        }

        private bool SkillBoolGetOverride(string boolName, bool value)
        {
            return boolName switch
            {
                nameof(canSwim) => canSwim,
                _ => value,
            };
        }

        private bool SkillBoolSetOverride(string boolName, bool value)
        {
            switch (boolName)
            {
                case nameof(canSwim):
                    canSwim = value;
                    PlayMakerFSM.BroadcastEvent("SWIM GET");
                    break;
            }
            return value;
        }

        private void EditWaterSurface(PlayMakerFSM fsm)
        {
            if (fsm.gameObject.LocateMyFSM("Acid Armour Check") != null) return; // acid

            GameObject splashSurface = fsm.transform.Find("Splash Surface").gameObject;
            splashSurface.layer = 17; // orig is 8, which can enable seam jumping when it intersects with other terrain colliders
            splashSurface.AddComponent<NonBouncer>();

            FsmState idle = fsm.GetState("Idle");
            FsmState checkSwim = fsm.AddState("Check Swim");
            FsmState damageHero = fsm.AddState("Damage Hero");
            FsmState bigSplash = fsm.GetState("Big Splash?");

            idle.Transitions[0].SetToState(checkSwim);
            checkSwim.AddFirstAction(new DelegateBoolTest(() => canSwim, "SWIM", "DAMAGE"));
            checkSwim.AddTransition("SWIM", bigSplash);
            checkSwim.AddTransition("DAMAGE", damageHero);

            damageHero.SetActions(fsm.GetState("Splash In Norm").Actions.Where(a => a is not SetPosition).ToArray()); // play splash audio and fling splash particles
            damageHero.AddLastAction(new Lambda(() => HeroController.instance.TakeDamage(fsm.gameObject, CollisionSide.bottom, 1, 2)));
            damageHero.AddTransition(FsmEvent.Finished, idle);
        }
    }
}
