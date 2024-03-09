using HarmonyLib;
using UnityEngine;
using GameNetcodeStuff;
using System.Collections.Generic;

namespace ravingdead.FriendlyCompany.patch;

public class FriendlyBees : MonoBehaviour
{
    private RedLocustBees __instance;

    public List<PlayerControllerB> MeanPlayers;

    #region UtilityMethods
    // __instance.hive.playerHeldBy
    public PlayerControllerB GetPlayerHeldBy() { return __instance.hive.playerHeldBy; }

    // __instance.hive.isHeld
    public bool GetIsHeld() { return __instance.hive.isHeld; }

    // __instance.hive.isHeldByEnemy
    public bool GetIsHeldByEnemy() { return __instance.hive.isHeldByEnemy; }

    public bool IsPlayerMean(PlayerControllerB player)
    {
        return MeanPlayers.Contains(player);
    }

    // Debug print to get all mean players.
    public void PrintMeanies()
    {
        if (MeanPlayers.Count != 0)
        {
            Debug.Log($"Mean players (Source: {__instance.GetInstanceID()})");
            for (int i = 0; i < MeanPlayers.Count; i++)
            {
                PlayerControllerB p = MeanPlayers[i];
                Debug.Log($"[BLAME] {p.playerUsername} (client: {p.playerClientId} - actual: {p.actualClientId} - steam: {p.playerSteamId})");
            }
        }
    }
    #endregion

    // Class initializer. Sets up required variables.
    public void Initialize(RedLocustBees instance)
    {
        Plugin.Log.LogInfo($"Initializing FriendlyBees component on {instance.__getTypeName()} instance {GetInstanceID()}...");
        __instance = instance;
        MeanPlayers = new List<PlayerControllerB>();
    }

    // Add mean players not in array. Returns true if a new player was added to list.
    public void AddMeanPlayer()
    {
        if (!IsPlayerMean(GetPlayerHeldBy()))
        {
            Plugin.Log.LogFatal($"Player {GetPlayerHeldBy().playerUsername} is now logged as a mean person >:O");
            MeanPlayers.Add(GetPlayerHeldBy());
        }
        else
        {
            Plugin.Log.LogFatal($"Player {GetPlayerHeldBy().playerUsername} was already a mean person >:(");
        }
    }

    // Corrects behavior if targeted player was friendly.
    public void TargetMeanPlayer()
    {
        Plugin.Log.LogInfo($"Target {__instance.targetPlayer.playerUsername} is friendly, switching behavior...");
        PlayerControllerB victim = FindNearestMeanPlayer(no_default: true);
        if (victim == null)
        {
            __instance.wasInChase = false;

            if (__instance.IsHiveMissing())
            {
                __instance.SwitchToBehaviourState(1);
                __instance.StartSearch(base.transform.position, __instance.searchForHive);
            }
            else 
            {
                __instance.SwitchToBehaviourState(0);
                __instance.targetPlayer = victim; // Stop targetting friendlies!
            }
        }
    }

    // Finds nearest meanie. Returns null if no nearby player matches the criterias.
    public PlayerControllerB FindNearestMeanPlayer(bool strict = false, bool no_default = false)
    {
        PlayerControllerB[] visiblePlayers = __instance.GetAllPlayersInLineOfSight(360f, 16);
        PlayerControllerB target = null;
        if (visiblePlayers != null)
        {
            float nearDist = 3000f; // Max distance bees will seek out players
            int nearPlayerIndex = 0;
            int tieBreakerIndex = -1;
            for (int i = 0; i < visiblePlayers.Length; i++)
            {
                if (visiblePlayers[i].currentlyHeldObjectServer != null)
                {
                    if (tieBreakerIndex == -1 && visiblePlayers[i].currentlyHeldObjectServer.itemProperties.itemId == 1531)
                    {
                        tieBreakerIndex = i; // Nearest player with any hive that does not belong to this instance.
                        continue;
                    }

                    if (visiblePlayers[i].currentlyHeldObjectServer == __instance.hive)
                    {
                        Plugin.Log.LogInfo("Defaulting to hive thief (source: You shouldn't have done that.)");
                        tieBreakerIndex = -1; // Highest priority, don't use tie breaker
                        target = visiblePlayers[i];
                        break;
                    }
                }

                // If player is closer than the previous, we update the nearest found mean player / distance.
                if (!strict || (strict && __instance.targetPlayer == null))
                {
                    if (IsPlayerMean(visiblePlayers[i]))
                    {
                        float playerDistance = Vector3.Distance(base.transform.position, visiblePlayers[i].transform.position);
                        if (playerDistance < nearDist)
                        {
                            nearDist = playerDistance;
                            nearPlayerIndex = i;
                        }
                    }
                }
            }

            if (tieBreakerIndex != -1 && Vector3.Distance(base.transform.position, visiblePlayers[tieBreakerIndex].transform.position) - nearDist > 7f)
            {
                Plugin.Log.LogInfo("Defaulting to nearest player (source: Small float difference to tie breaker.)");
                target = visiblePlayers[nearPlayerIndex];
            }
            else if (target == null && !no_default)
            {
                Plugin.Log.LogInfo("Defaulting to nearest player (source: No hive in sight.)");
                target = visiblePlayers[nearPlayerIndex];
            }
            else if (target == null && no_default)
            {
                Plugin.Log.LogInfo("Defaulting to null (source: No hive or mean player in sight.)");
            }
        }
        return target;
    }
}

/// <summary>
/// Patch to modify the behavior of bees.
/// </summary>
[HarmonyPatch(typeof(RedLocustBees))]
public class RedLocustBeesPatch
{
    static FriendlyBees friendlyBees;

    static FriendlyBees GetComponent(RedLocustBees instance)
    {
        FriendlyBees comp;

        // try { comp = ((Component)instance).gameObject.GetComponent<FriendlyBees>(); }
        // catch { comp = null; }
        comp = ((Component)instance).gameObject.GetComponent<FriendlyBees>();

        return comp;
    }

    [HarmonyPatch(typeof(RedLocustBees), "Start")]
    [HarmonyPrefix]
    static void StartPostFix(ref RedLocustBees __instance)
    {
        Plugin.Log.LogDebug("Bees found!");
        friendlyBees = ((Component)__instance).gameObject.AddComponent<FriendlyBees>();
        friendlyBees.Initialize(__instance);
    }

    [HarmonyPatch(typeof(RedLocustBees), "DoAIInterval")]
    [HarmonyPrefix]
    static void DoAIInterval_Pre(ref RedLocustBees __instance)
    {
        // friendlyBees.PrintMeanies();
        if (friendlyBees.GetIsHeld() && !friendlyBees.GetIsHeldByEnemy())
            friendlyBees.AddMeanPlayer();
        if (__instance.targetPlayer != null && !friendlyBees.IsPlayerMean(__instance.targetPlayer))
            friendlyBees.TargetMeanPlayer();
        return;
    }

    [HarmonyPatch(typeof(RedLocustBees), "Update")]
    [HarmonyPostfix]
    static void Update_Post(ref RedLocustBees __instance)
    {
        if (__instance.currentBehaviourStateIndex == 1)
        {
            PlayerControllerB nearPlayer = __instance.GetClosestPlayer();
            if (!friendlyBees.IsPlayerMean(nearPlayer))
            {
                float volumeTimeDialation = Time.deltaTime * 0.7f;
                __instance.beesZappingMode = 0;
                __instance.ResetBeeZapTimer();

                __instance.agent.speed = 4f;
                __instance.agent.acceleration = 13f;
                if (!__instance.overrideBeeParticleTarget)
                {
                    float hiveDist = Vector3.Distance(__instance.transform.position, __instance.hive.transform.position);
                    if (__instance.hive != null && (hiveDist < 2f || (hiveDist < 5f && !Physics.Linecast(__instance.eye.position, __instance.hive.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))))
                    {
                        __instance.beeParticlesTarget.position = __instance.hive.transform.position;
                    }
                    else
                    {
                        __instance.beeParticlesTarget.position = __instance.transform.position + Vector3.up * 1.5f;
                    }
                }

                __instance.beesIdle.volume = Mathf.Min(__instance.beesIdle.volume + volumeTimeDialation, 1f);
                if (!__instance.beesIdle.isPlaying)
                {
                    __instance.beesIdle.Play();
                }

                __instance.beesDefensive.volume = Mathf.Max(__instance.beesDefensive.volume - volumeTimeDialation, 0f);
                if (__instance.beesDefensive.isPlaying && __instance.beesDefensive.volume <= 0f)
                {
                    __instance.beesDefensive.Stop();
                }

                __instance.beesAngry.volume = Mathf.Max(__instance.beesAngry.volume - volumeTimeDialation, 0f);
                if (__instance.beesAngry.isPlaying && __instance.beesAngry.volume <= 0f)
                {
                    __instance.beesAngry.Stop();
                }
            }
        }
        return;
    }

    [HarmonyPatch(typeof(RedLocustBees), "ChaseWithPriorities")]
    [HarmonyPrefix]
    static bool ChaseWithPriorities_Pre(ref RedLocustBees __instance, ref PlayerControllerB __result)
    {
        __result = friendlyBees.FindNearestMeanPlayer(strict: true);
        return false;
    }

    [HarmonyPatch("OnCollideWithPlayer")]
    [HarmonyPrefix]
    static bool OnCollideWithPlayer_Pre(Collider other, ref RedLocustBees __instance)
    {
        if (__instance.debugEnemyAI)
        {
            {
                Debug.Log(__instance.gameObject.name + ": Collided with player!");
            }
        }
        
        if (__instance.timeSinceHittingPlayer >= 0.4f)
        {
            PlayerControllerB targetPlayer = __instance.MeetsStandardPlayerCollisionConditions(other, false, false);
            if (targetPlayer != null && friendlyBees.IsPlayerMean(targetPlayer))
            {
                friendlyBees.PrintMeanies();
                __instance.timeSinceHittingPlayer = 0f;
                if (targetPlayer.health <= 10 || targetPlayer.criticallyInjured)
                {
                    __instance.BeeKillPlayerOnLocalClient((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                    __instance.BeeKillPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                }
                else
                {
                    targetPlayer.DamagePlayer(10, true, true, CauseOfDeath.Electrocution, 3, false, default(Vector3));
                }
                if (__instance.beesZappingMode != 3)
                {
                    __instance.beesZappingMode = 3;
                    __instance.EnterAttackZapModeServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                }
            }
        }
        return false;
    }
}

