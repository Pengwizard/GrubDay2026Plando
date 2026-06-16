using Modding;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Modules;

//replicated Knight of Nights/Networking ShadelessModule with permission from purenail/themathgeek_314
namespace GrubDay2026Plando.Modules
{
    internal class Shadeless : Module
    {
        private static readonly FsmID deathAnimId = new("Hero Death", "Hero Death Anim");

        public bool IsShadeless;

        public override void Initialize()
        {
            GrubDay2026PlandoMod.Instance.Log("Initializing disable swim");
            Events.AddFsmEdit(deathAnimId, ModifyDeathAnim);
            ModHooks.GetPlayerBoolHook += HookIsShadeless;
            IsShadeless = true;
        }

        public override void Unload()
        {
            Events.RemoveFsmEdit(deathAnimId, ModifyDeathAnim);
            ModHooks.GetPlayerBoolHook -= HookIsShadeless;
            IsShadeless = false;
        }

        private void ModifyDeathAnim(PlayMakerFSM fsm)
        {
            fsm.GetState("Remove Geo").ClearActions();
            fsm.GetState("Limit Soul").ClearActions();

            var setShadeState = fsm.GetState("Set Shade");
            setShadeState.AddTransition("SKIP", "Save");
            setShadeState.AddFirstAction(new Lambda(() => {
                fsm.FsmVariables.GetFsmGameObject("Self").Value = fsm.gameObject;
                fsm.SendEvent("SKIP");
            }));

            fsm.GetState("End").RemoveFirstActionOfType<SendMessage>();
        }

        private bool HookIsShadeless(string name, bool orig) => name == nameof(IsShadeless) ? IsShadeless : orig;
    }
}