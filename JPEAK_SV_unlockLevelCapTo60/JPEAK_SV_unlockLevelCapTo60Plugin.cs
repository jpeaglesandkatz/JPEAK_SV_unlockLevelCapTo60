using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JPEAK_SV_unlockLevelCapTo60
{

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class JPEAK_SV_unlockLevelCapTo60Plugin : BaseUnityPlugin
    {

        private const string MyGUID = "com.jpb.JPEAK_SV_unlockLevelCapTo60";
        private const string PluginName = "JPEAK_SV_unlockLevelCapTo60";
        private const string VersionString = "1.0.0";

        public static string CfgMaxLevelKey = "Max Level Cap";


        public static ConfigEntry<int> CfgMaxLevel;


        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        private void Awake()
        {

            CfgMaxLevel = Config.Bind("Max level",
                CfgMaxLevelKey,
                60,
                new ConfigDescription("Set Max level Cap",
                    new AcceptableValueRange<int>(50, 100)));

            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.CreateAndPatchAll(typeof(JPEAK_SV_unlockLevelCapTo60Plugin), null);
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            Log = Logger;
        }


        [HarmonyPatch(typeof(PChar), "UpdateChar")]
        [HarmonyPrefix]
        static bool UpdateCharprefix(ref int ___maxLevel, ref int __state)
        {
            ___maxLevel = CfgMaxLevel.Value;
            return true;

        }

        [HarmonyPatch(typeof(GameManager), "CreateEnemy")]
        [HarmonyPrefix]
        public static bool CreateEnemy_pref(ref float spawnInterval, GameManager ___instance)
        {

            //GameObject bse = GameObject.Find("Base");
            if (___instance == null)
            {
                return false;
            }

            if (___instance == null)
            {
                return false;
            }

            if (___instance.Player == null)
            {
                ___instance.Player = GameObject.FindGameObjectWithTag("Player");
            }
            if (___instance.Player == null || ___instance.performingSpawnFleet || ___instance.currSector.isStarterSector)
            {
                return false;
            }
            int difficulty = GameData.data.difficulty;
            int level = PChar.Char.level;
            int bounty = ___instance.Player.GetComponent<SpaceShip>().stats.bounty;
            if (bounty <= 0 && ((difficulty == -1 && level < 5) || (difficulty == 0 && level < 3)))
            {
                return false;
            }
            int num = -1;
            if (PChar.GetRep(___instance.currSector.factionControl) <= -1000)
            {
                num = ___instance.currSector.factionControl;
            }
            else
            {
                int num2 = bounty;
                if (___instance.currSector.factionControl <= 0)
                {
                    num = ___instance.currSector.factions.GetEnemyFaction();
                    if (num == -1)
                    {
                        num2 += 20 + ___instance.currSector.GetHideoutQuantity(HideoutType.Marauder, true) * 15;
                    }
                    if (___instance.currSector.type == SectorType.ShipGraveyard)
                    {
                        num2 += 20;
                    }
                }
                if (___instance.currSector.factionControl == 0)
                {
                    num2 += ___instance.currSector.GetHideoutQuantity(HideoutType.Marauder, true) * 12;
                }
                if (___instance.currSector.factionControl > 0)
                {
                    num2 -= 10;
                    if (num2 < 0)
                    {
                        num2 = 0;
                    }
                    num2 += ___instance.currSector.GetHideoutQuantity(HideoutType.Marauder, true) * 5;
                }
                if (num == -1 && Random.Range(1, 101) <= num2)
                {
                    num = 0;
                }
            }
            if (num == -1)
            {
                ___instance.spawnEnemyTime = 120f;
                return false;
            }
            int num3 = PChar.Char.level - ___instance.currSector.level;
            if (PChar.Char.level < ___instance.currSector.level)
            {
                num3 /= 2;
            }
            int num4 = (int)(PChar.SKMod(16) * 5f);
            int num5 = CfgMaxLevel.Value - num3 * 5 - num4;
            int num6 = num5 - CfgMaxLevel.Value;
            num5 += ___instance.spawnEnemyPlusChance;
            num5 += ((bounty > 0) ? bounty : (bounty * 2));
            if (Random.Range(1, 101) <= num5)
            {
                ___instance.spawnEnemyTime = (float)(200 - num6);
                ___instance.spawnEnemyPlusChance = 0;
                if (___instance.spawnEnemyTime < 120f)
                {
                    ___instance.spawnEnemyTime = 120f;
                }
                //MethodInfo CreateEnemy = AccessTools.Method(typeof(GameManager), "CreateEnemy");
                //GameManager.instance.StartCoroutine(
                //        (System.Collections.IEnumerator)CreateEnemy.Invoke(GameManager.instance, new object[] { spawnInterval, num })
                //        );

                ___instance.StartCoroutine(___instance.CreateEnemyRoutine(spawnInterval, num));
                return false;
            }
            ___instance.spawnEnemyTime = 30f;
            ___instance.spawnEnemyPlusChance += 5;
            if (PChar.GetRepRank(___instance.currSector.factionControl) > -3 && ___instance.spawnEnemyPlusChance > CfgMaxLevel.Value + 5 - num4)
            {
                ___instance.spawnEnemyPlusChance = CfgMaxLevel.Value + 5 - num4;

            }
            return false;
        }
    }
}

