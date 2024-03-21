using HarmonyLib;
using UnityEngine;
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
                if (num == -1 && Random.Range(1, 101) <= num2)
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
            if (Random.Range(1, 101) <= num5)
            {
                __instance.spawnEnemyTime = (float)(180 - num6);
                __instance.spawnEnemyPlusChance = 1;
                if (__instance.spawnEnemyTime < 100f)
                {
                    __instance.spawnEnemyTime = 100f;
                }

                num = +3;
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

            [HarmonyPatch(typeof(GameManager), "CreateEnemyRoutine")]
            static System.Collections.Generic.IEnumerable<YieldInstruction> CreateEnemyRoutine(float spawnInterval, int factionIndex, GameManager __instance)
        {
            int currSectorIndex = GameData.data.currentSectorIndex;
            __instance.performingSpawnFleet = true;
            __instance.cancelSpawnFleet = false;
            int bounty = __instance.Player.GetComponent<SpaceShip>().stats.bounty;
            bounty += GameData.data.difficulty * 4;
            int repValue = PChar.GetRep(factionIndex) * -1;
            int strengthBoost = bounty + PChar.GetRep(factionIndex) * -1 / 1500;
            if (strengthBoost < 0)
            {
                strengthBoost = 0;
            }
            if (!__instance.TargetInSafeZone(__instance.Player.transform, -1))
            {
                Debug.Log("Creating enemy! strengthBoost: " + strengthBoost);
                InfoPanelControl.inst.ShowWarning(Lang.Get(6, 100), 7, strengthBoost > 5);
                yield return new WaitForSeconds(2f);
                if (GameData.data.currentSectorIndex != currSectorIndex || __instance.cancelSpawnFleet)
                {
                    __instance.performingSpawnFleet = false;
                    __instance.cancelSpawnFleet = false;
                    __instance.spawnEnemyTime += 120f;
                    Debug.LogWarning($"PluginName: {PluginName}, Cancel spawn");
                    yield break;
                }
                int bonusLevel = strengthBoost / 6;
                float rotationY = (float)UnityEngine.Random.Range(0, 359);
                bool allowNonCombatRoleShips = true;
                if (__instance.currSector.level >= 52)
                {
                    allowNonCombatRoleShips = false;
                }
                AICharacter aiChar = new AICharacter();
                aiChar.level = __instance.RandomizeShipLevel(0f, bonusLevel);
                aiChar.factionIndex = factionIndex;
                aiChar.rotationY = rotationY;
                aiChar.SetBehaviour(0, false);
                if (UnityEngine.Random.Range(1, 101) <= (strengthBoost - 10) * 3)
                {
                    aiChar.rank = 3;
                }
                else if (UnityEngine.Random.Range(1, 101) <= strengthBoost * 12)
                {
                    aiChar.rank = 2;
                }
                Vector2 coordenatesNearPlayer = __instance.GetCoordenatesNearPlayer();
                Vector3 location = new Vector3(coordenatesNearPlayer.x, 0f, coordenatesNearPlayer.y);
                aiChar.posX = location.x;
                aiChar.posZ = location.z;
                int qntExtras = (int)(((float)bounty + Mathf.Sqrt((float)repValue * 0.003f) + (float)repValue * 0.0001f) / 2f);
                int waveMode = Random.Range(0, 3);
                int minSize = Mathf.Clamp((int)((float)strengthBoost * 0.1f), 2, 5);
                int maxSize = Mathf.Clamp((int)((float)strengthBoost * 0.15f), 3, 6);
                if (waveMode == 0)
                {
                    qntExtras = (int)((float)qntExtras * 0.6f);
                }
                else if (waveMode == 1)
                {
                    minSize = 5;
                    maxSize = 8;
                }
                else
                {
                    qntExtras = (int)((float)qntExtras * 0.3f);
                    if (aiChar.level > 15 && minSize < 4)
                    {
                        minSize = 6;
                        maxSize = 10;
                    }
                }
                if (factionIndex > 0)
                {
                    ShipModelData randomModel = ShipDB.GetRandomModel(minSize, maxSize, factionIndex, 99, false, true, allowNonCombatRoleShips);
                    aiChar.shipData = new SpaceShipData
                    {
                        shipModelID = randomModel.id
                    };
                }
                yield return null;
                bool flag = factionIndex > 0 && aiChar.level > 20 && aiChar.behavior.role != 1;
                __instance.SpawnShip(location, true, factionIndex, aiChar, true, flag);
                yield return new WaitForSeconds(spawnInterval);
                if (qntExtras > 0)
                {
                    Debug.LogWarning($"PluginName: {PluginName}, Spawned Ships.... did you see them?");
                    if (waveMode == 0)
                    {
                        minSize = Mathf.Clamp((int)((float)strengthBoost * 0.05f), 1, 2);
                        maxSize = Mathf.Clamp((int)((float)strengthBoost * 0.06f), 2, 4);
                    }
                    else if (waveMode == 1)
                    {
                        minSize = 1;
                        maxSize = Mathf.Clamp((int)((float)strengthBoost * 0.04f), 1, 2);
                    }
                    else
                    {
                        minSize = Mathf.Clamp((int)((float)strengthBoost * 0.06f), 3, 5);
                        maxSize = Mathf.Clamp((int)((float)strengthBoost * 0.07f), 3, 6);
                    }
                    int num2;
                    for (int i = 0; i < qntExtras; i = num2 + 1)
                    {
                        if (GameData.data.currentSectorIndex != currSectorIndex || __instance.cancelSpawnFleet)
                        {
                            __instance.performingSpawnFleet = false;
                            __instance.cancelSpawnFleet = false;
                            __instance.spawnEnemyTime += 120f;
                            yield break;
                        }
                        aiChar = new AICharacter();
                        aiChar.level = __instance.RandomizeShipLevel(0f, bonusLevel - 2);
                        aiChar.factionIndex = factionIndex;
                        aiChar.rotationY = rotationY;
                        location += new Vector3((float)Random.Range(-50 - qntExtras, 50 + qntExtras), 0f, (float)Random.Range(-50 - qntExtras, 50 + qntExtras));
                        aiChar.posX = location.x;
                        aiChar.posZ = location.z;
                        int num = 0;
                        if (factionIndex > 0 && Random.Range(1, 11) <= 3)
                        {
                            num = 1;
                        }
                        aiChar.SetBehaviour(num, false);
                        flag = (factionIndex > 0 && aiChar.level > 20 && aiChar.behavior.role != 1);
                        int maxShipClass;
                        if (aiChar.behavior.role == 1)
                        {
                            maxShipClass = 3;
                        }
                        else
                        {
                            maxShipClass = maxSize;
                        }
                        if (factionIndex == 0)
                        {
                            aiChar.AIType = 2;
                        }
                        else
                        {
                            aiChar.AIType = 3;
                        }
                        if (Random.Range(1, 101) <= (strengthBoost - 10) * 3)
                        {
                            aiChar.rank = 2;
                        }
                        else if (Random.Range(1, 101) <= strengthBoost * 10)
                        {
                            aiChar.rank = 1;
                        }
                        if (factionIndex > 0)
                        {
                            ShipModelData randomModel2 = ShipDB.GetRandomModel(minSize, maxShipClass, factionIndex, 99, false, num == 0, allowNonCombatRoleShips);
                            aiChar.shipData = new SpaceShipData
                            {
                                shipModelID = randomModel2.id
                            };
                        }
                        __instance.SpawnShip(location, aiChar.behavior.role != 1, factionIndex, aiChar, true, flag && aiChar.behavior.role == 0);
                        
                        yield return new WaitForSeconds(spawnInterval);
                        num2 = i;
                    }
                    Debug.LogWarning($"PluginName: {PluginName}, Spawned Ships.... did you see them?");
                }
                __instance.spawnEnemyTime += (float)(strengthBoost * 12);
                __instance.shortenFleetCooldown = 180f;
                PChar.ShowTutorial(1, true);
                aiChar = null;
            }
            __instance.performingSpawnFleet = false;
            yield break;
           
        }

        }
    }

