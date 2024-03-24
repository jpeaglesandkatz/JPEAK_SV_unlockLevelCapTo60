using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;

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
            Harmony.CreateAndPatchAll(typeof(AIpatches));

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
        public static bool CreateEnemy_pref(ref float spawnInterval, GameManager __instance)
        {

            //GameObject bse = GameObject.Find("Base");
            if (__instance == null)
            {
                return false;
            }

            if (__instance == null)
            {
                return false;
            }

            if (__instance.Player == null)
            {
                __instance.Player = GameObject.FindGameObjectWithTag("Player");
            }
            if (__instance.Player == null || __instance.performingSpawnFleet || __instance.currSector.isStarterSector)
            {
                return false;
            }
            int difficulty = GameData.data.difficulty;
            int level = PChar.Char.level;
            int bounty = __instance.Player.GetComponent<SpaceShip>().stats.bounty;
            if (bounty <= 0 && ((difficulty == -1 && level < 5) || (difficulty == 0 && level < 3)))
            {
                return false;
            }
            int num = -1;
            if (PChar.GetRep(__instance.currSector.factionControl) <= -1000)
            {
                num = __instance.currSector.factionControl;
            }
            else
            {
                int num2 = bounty;
                if (__instance.currSector.factionControl <= 0)
                {
                    num = __instance.currSector.factions.GetEnemyFaction();
                    if (num == -1)
                    {
                        num2 += 20 + __instance.currSector.GetHideoutQuantity(HideoutType.Marauder, true) * 15;
                    }
                    if (__instance.currSector.type == SectorType.ShipGraveyard)
                    {
                        num2 += 20;
                    }
                }
                if (__instance.currSector.factionControl == 0)
                {
                    num2 += __instance.currSector.GetHideoutQuantity(HideoutType.Marauder, true) * 12;
                }
                if (__instance.currSector.factionControl > 0)
                {
                    num2 -= 10;
                    if (num2 < 0)
                    {
                        num2 = 0;
                    }
                    num2 += __instance.currSector.GetHideoutQuantity(HideoutType.Marauder, true) * 5;
                }
                if (num == -1 && UnityEngine.Random.Range(1, 101) <= num2)
                {
                    num = 0;
                }
            }
            if (num == -1)
            {
                __instance.spawnEnemyTime = 120f;
                return false;
            }
            int num3 = PChar.Char.level - __instance.currSector.level;
            if (PChar.Char.level < __instance.currSector.level)
            {
                num3 /= 2;
            }
            int num4 = (int)(PChar.SKMod(16) * 5f);
            int num5 = CfgMaxLevel.Value - num3 * 7 - num4;
            int num6 = num5 - CfgMaxLevel.Value;
            num5 += __instance.spawnEnemyPlusChance;
            num5 += ((bounty > 0) ? bounty : (bounty * 2));
            if (UnityEngine.Random.Range(1, 140) <= num5)
            {
                __instance.spawnEnemyTime = (float)(180 - num6);
                __instance.spawnEnemyPlusChance = 2;
                if (__instance.spawnEnemyTime < 50f)
                {
                    __instance.spawnEnemyTime = 50f;
                }

                num = +2;
                __instance.StartCoroutine(__instance.CreateEnemyRoutine(spawnInterval, num));
                Debug.LogWarning($"PluginName: {PluginName}, Enhanced Create Enemy {bounty}, {num3}, {num4}, {num5}, {num6}, {spawnInterval}, {num}");
                return false;
            }
            __instance.spawnEnemyTime = 30f;
            __instance.spawnEnemyPlusChance += 15;
            if (PChar.GetRepRank(__instance.currSector.factionControl) > -3 && __instance.spawnEnemyPlusChance > CfgMaxLevel.Value + 5 - num4)
            {
                __instance.spawnEnemyPlusChance = CfgMaxLevel.Value + 20 - num4;

            }
            return false;
        }

        [HarmonyPatch(typeof(GameManager), "SpawnShip")]
        [HarmonyPostfix]
        public static Transform SpawnShip_post(Transform rettype, ref Transform __result)
        {
            Transform shipobj = __result;

            AIControl aIControl = shipobj.GetComponent<AIControl>();
            SpaceShip aIss = shipobj.GetComponent<SpaceShip>();
            shipobj.gameObject.SetActive(false);

            aIControl.Char.pilotLevel = PChar.Char.level + 5;
            aIss.ApplyFleetBonuses();
            aIss.ApplyPerks(1);
            aIss.armorMod = 4f + UnityEngine.Random.Range(0f, 3f);
            aIss.DamageMod(1 + UnityEngine.Random.Range(0, 2));
            aIss.DamageResist(3 + UnityEngine.Random.Range(1, 3));
            aIss.dmgBonus = UnityEngine.Random.Range(0, 2);
            aIss.AIControl.Update();
            shipobj.gameObject.SetActive(true);
            aIControl.SearchForEnemies();
            //rettype = shipobj;
            Debug.LogWarning($"PluginName: {PluginName}, Postfixed enemy! {aIss.armorMod}, {aIss.AIControl}, {aIss.armor}");
            return __result = rettype;

        }

        public class AIpatches

        {

            [HarmonyPatch(typeof(GameManager), "SpawnAIChar", new Type[] { typeof(Vector3), typeof(AICharacter), typeof(HideoutControl) })]
            [HarmonyPrefix]
            public static bool SpawnAIChar_pre(Vector3 location, AICharacter aiChar, HideoutControl hc, GameManager __instance)
            {

                //MethodInfo method = typeof(GameManager).GetMethod(nameof(GameManager.SpawnAIChar), new object[] { Vector3 location, AICharacter aiChar, HideoutControl hc});


                GameObject gameObject = null;
                if (aiChar.posX != -1f && aiChar.posZ != -1f)
                {
                    location = new Vector3(aiChar.posX, 0f, aiChar.posZ);
                }
                location = GameManager.GetSafePosition(location, 20f, false);
                if (hc != null)
                {
                    if (hc.hideout.type == HideoutType.Marauder)
                    {
                        __instance.marauderBaseObj.SetActive(false);
                        gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.marauderBaseObj, location, default(Quaternion));
                        gameObject.GetComponent<AIMarauder>().Char = aiChar;
                        gameObject.GetComponent<AIMarauder>().hc = hc;
                        gameObject.GetComponent<AIMarauder>().guardPosition = location;
                        gameObject.GetComponent<AIMarauder>().destination = location;
                        gameObject.name = aiChar.name;
                        Debug.LogWarning($"PluginName: {PluginName}, Marauder FIXED!");
                    }
                    if (hc.hideout.type == HideoutType.Mercenary)
                    {
                        __instance.mercenaryBaseObj.SetActive(false);
                        gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.mercenaryBaseObj, location, default(Quaternion));
                        gameObject.GetComponent<AIMercenary>().Char = aiChar;
                        gameObject.GetComponent<AIMercenary>().hc = hc;
                        gameObject.GetComponent<AIMercenary>().destination = location;
                        gameObject.name = aiChar.name;
                    }
                }
                else
                {
                    __instance.spaceShipBaseObj.SetActive(false);
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.spaceShipBaseObj, location, default(Quaternion));
                    gameObject.GetComponent<AIControl>().Char = aiChar;
                    gameObject.name = aiChar.name;
                }
                gameObject.GetComponent<AIControl>().shipType = FactionDB.GetFaction(0).shipTypes[0];
                if (__instance.spaceshipsGroup != null)
                {
                    gameObject.transform.SetParent(__instance.spaceshipsGroup);
                }
                gameObject.SetActive(true);
                __instance.marauderBaseObj.SetActive(true);
                __instance.mercenaryBaseObj.SetActive(true);
                __instance.spaceShipBaseObj.SetActive(true);
                return false;
            }

        }

    }

 }


