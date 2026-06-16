using UnityEngine;
using PurenailCore.ModUtil;

namespace GrubDay2026Plando
{
    internal class Preloader : PurenailCore.ModUtil.Preloader
    {
        public static Preloader Instance { get; private set; } = new();

        // For jars
        [Preload("Crossroads_31", "Grub Bottle")]
        public GameObject GrubJar { get; private set; }

        [Preload("Ruins1_23", "Ruins Vial Empty/Active/soul_cache (1)")]
        public GameObject SoulJar { get; private set; }

        // Pickler phase 1
        [Preload("Crossroads_19", "_Enemies/Spitter")]
        public GameObject Aspid { get; private set; }

        [Preload("Tutorial_01", "_Enemies/Buzzer 2")]
        public GameObject Squit { get; private set; }

        [Preload("Crossroads_ShamanTemple", "_Enemies/Roller 6")]
        public GameObject Baldur { get; private set; }

        // Pickler phase 2
        [Preload("Room_Colosseum_Gold", "Colosseum Manager/Waves/Wave 4/Colosseum Cage Small")]
        public GameObject ArmoredBaldurCage { get; private set; }

        [Preload("Room_Colosseum_Gold", "Colosseum Manager/Waves/Wave 12/Colosseum Cage Small (3)")]
        public GameObject ArmoredSquitCage { get; private set; }

        [Preload("Room_Colosseum_Gold", "Colosseum Manager/Waves/Wave 17/Colosseum Cage Small (2)")]
        public GameObject PrimalAspidCage { get; private set; }

        // Pickler phase 3
        [Preload("Ruins1_30", "Mage")]
        public GameObject SoulTwister { get; private set; }

        [Preload("Ruins1_30", "Mage Balloon")]
        public GameObject Folly { get; private set; }

        [Preload("Room_Colosseum_Gold", "Colosseum Manager/Waves/Wave 7/Colosseum Cage Small (1)")]
        public GameObject InfectedVengefly { get; private set; }

        // Pickler phase 4
        [Preload("Deepnest_26b", "Centipede Hatcher (7)")]
        public GameObject DirtcarverHatcher { get; private set; }

        [Preload("Abyss_09", "Siblings/Shade Sibling")]
        public GameObject Sibling { get; private set; }

        [Preload("Ruins2_04", "Great Shield Zombie")]
        public GameObject GreatHuskSentry { get; private set; }

        [Preload("Fungus3_28", "Jelly Egg Bomb")]
        public GameObject JellyEggBomb { get; private set; }


        [Preload("Fungus2_10", "Bounce Shrooms 2/Bounce Shroom B (2)")]
        public GameObject BounchShroom { get; private set; }
    }

    internal static class GameObjectExtensions
    {
        internal static GameObject ExtractFromCage(this GameObject self) => self.LocateMyFSM("Spawn").Fsm.GetFsmGameObject("Enemy Type").Value;
    }
}
