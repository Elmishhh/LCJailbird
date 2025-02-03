// Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Shovel
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using LCJailbird;
using Unity.Netcode;
using UnityEngine;

public class JailbirdShovel : GrabbableObject
{
    public bool chargeTimerFinished;
    public bool chargeTimerActive;
    public RaycastHit RCHit;
    public Rigidbody playerRB;
    public CharacterController characterController;

    public bool checkEnemyInfront;
    public bool doAttack;
    public bool doExplode;

    public int durability = 1;

    public int jailbirdHitForce = 5;

    public bool reelingUp;

    public bool isHoldingButton;

    public RaycastHit rayHit;

    public Coroutine reelingUpCoroutine;

    public RaycastHit[] objectsHitByJailbird;
    public List<RaycastHit> objectsHitByJailbirdList = new List<RaycastHit>();

    public AudioClip reelUpSFX;
    public AudioClip swingSFX;
    public AudioClip[] hitSFX;
    public AudioClip chargeSFX;
    public AudioSource jailbirdAudio;

    public PlayerControllerB previousPlayerHeldBy;

    public int jailbirdMaskint = 1084754248;
    public LayerMask jailbirdMask = (1 << 3) | (1 << 19);

    public override void GrabItem()
    {
        Plugin.Logger.LogInfo("jailbird grabbed");
        playerRB = playerHeldBy.transform.GetComponent<Rigidbody>();
        characterController = playerHeldBy.transform.GetComponent<CharacterController>();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        if (playerHeldBy == null)
        {
            return;
        }
        isHoldingButton = buttonDown;
        if (!reelingUp && buttonDown)
        {
            reelingUp = true;
            previousPlayerHeldBy = playerHeldBy;
            if (reelingUpCoroutine != null)
            {
                StopCoroutine(reelingUpCoroutine);
            }
            reelingUpCoroutine = StartCoroutine(reelUpShovel());
        }
    }

    public IEnumerator reelUpShovel()
    {
        playerHeldBy.activatingItem = true;
        playerHeldBy.twoHanded = true;

        playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
        playerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: true);
        jailbirdAudio.PlayOneShot(reelUpSFX);
        ReelUpSFXServerRpc();

        yield return new WaitForSeconds(5.5f);
        //yield return new WaitUntil(() => !isHoldingButton || !isHeld);

        StartCoroutine(chargeTimer(3)); // max charge time
        jailbirdAudio.PlayOneShot(chargeSFX); // starts charge SFX
        checkEnemyInfront = true;
        yield return new WaitUntil(() => (chargeTimerFinished && !chargeTimerActive) || doAttack);
        jailbirdAudio.Stop(); // stops charge SFX if it's still playing
        if (durability <= 1) { doExplode = doAttack; }
        checkEnemyInfront = false;
        doAttack = false;
        chargeTimerFinished = false;
        chargeTimerActive = false;

        SwingShovel(!isHeld);

        //yield return new WaitForSeconds(0.13f);
        //yield return new WaitForEndOfFrame();

        try
        {
            HitShovel(!isHeld);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning("error when hitting with a shovel,");
            Plugin.Logger.LogError(e);
        }

        yield return new WaitForSeconds(0.3f);

        Plugin.Logger.LogInfo(1);
        if (durability == 0)
        {
            Plugin.Logger.LogInfo(2);
            if (doExplode)
            {
                Plugin.Logger.LogInfo(3);
                //StartCoroutine(Plugin.ExplodeAtDelayed(previousPlayerHeldBy.transform.position, 0.1f));
                Landmine.SpawnExplosion(playerHeldBy.transform.position + playerHeldBy.transform.forward * 0.5f, true, 3, 3, 50);
                yield return new WaitForFixedUpdate();
            }
            Plugin.Logger.LogInfo(4);
            previousPlayerHeldBy.DespawnHeldObject();
        }
        Plugin.Logger.LogInfo(5);
        if (durability > 0)
        {
            Plugin.Logger.LogInfo(6);
            durability--;
            Plugin.Logger.LogInfo(7);
            if (durability == 0)
            {
                Plugin.Logger.LogInfo(8);
                this.transform.Find("jailbird/Mesh_Jailbird/red_glow").gameObject.SetActive(true);
                Plugin.Logger.LogInfo(9);
                this.transform.Find("jailbird/Mesh_Jailbird/blue_glow").gameObject.SetActive(false);
                Plugin.Logger.LogInfo(10);
            }
        }
        Plugin.Logger.LogInfo(11);

        reelingUp = false;
        reelingUpCoroutine = null;
        playerHeldBy.twoHanded = false;
        previousPlayerHeldBy.activatingItem = false;
    }
    public void FixedUpdate()
    {
        if (checkEnemyInfront)
        {
            doAttack = Physics.SphereCast(origin: previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 0.65f, previousPlayerHeldBy.gameplayCamera.transform.forward, out RCHit, 1.85f, jailbirdMaskint, QueryTriggerInteraction.Collide);
            Plugin.Logger.LogInfo(doAttack);
        }
        if (chargeTimerActive && !chargeTimerFinished)
        {
            if (doAttack)
            {
                previousPlayerHeldBy.externalForces = Vector3.zero;
            }
            else
            {
                previousPlayerHeldBy.externalForces = new Vector3(previousPlayerHeldBy.gameplayCamera.transform.forward.x, 0, previousPlayerHeldBy.gameplayCamera.transform.forward.z) * 25;
            }
        }
    }
    public IEnumerator chargeTimer(float seconds)
    {
        chargeTimerActive = true;
        chargeTimerFinished = false;
        yield return new WaitForSeconds(seconds);
        chargeTimerActive = false;
        chargeTimerFinished = true;
        if (!checkEnemyInfront)
        {
            chargeTimerFinished = false;
        }
    }

    [ServerRpc]
    public void ReelUpSFXServerRpc()
    {
        ReelUpSFXClientRpc();
    }

    [ClientRpc]
    public void ReelUpSFXClientRpc()
    {
        jailbirdAudio.PlayOneShot(reelUpSFX);
    }

    public override void DiscardItem()
    {
        if (playerHeldBy != null)
        {
            playerHeldBy.activatingItem = false;
        }
        base.DiscardItem();
    }

    public void SwingShovel(bool cancel = false)
    {
        previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: false);
        if (!cancel)
        {
            jailbirdAudio.PlayOneShot(swingSFX);
            previousPlayerHeldBy.UpdateSpecialAnimationValue(specialAnimation: true, (short)previousPlayerHeldBy.transform.localEulerAngles.y, 0.4f);
        }
    }

    public void HitShovel(bool cancel = false)
    {
        /*
        if (previousPlayerHeldBy == null)
        {
            return;
        }
        previousPlayerHeldBy.activatingItem = false;
        bool flag = false;
        int hitSurfaceID = -1;
        if (!cancel)
        {
            previousPlayerHeldBy.twoHanded = false;
            Debug.DrawRay(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, previousPlayerHeldBy.gameplayCamera.transform.forward * 1.85f, Color.blue, 5f);
            objectsHitByJailbird = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 0.75f, previousPlayerHeldBy.gameplayCamera.transform.forward, 1.85f, jailbirdMaskint, QueryTriggerInteraction.Collide);
            objectsHitByJailbirdList = objectsHitByJailbird.OrderBy((x) => x.distance).ToList();
            Vector3 start = previousPlayerHeldBy.gameplayCamera.transform.position;
            for (int i = 0; i < objectsHitByJailbirdList.Count; i++)
            {
                IHittable component;
                RaycastHit hitInfo;
                if (objectsHitByJailbirdList[i].transform.gameObject.layer == 8 || objectsHitByJailbirdList[i].transform.gameObject.layer == 11)
                {
                    start = objectsHitByJailbirdList[i].point + objectsHitByJailbirdList[i].normal * 0.01f;
                    flag = true;
                    string text = objectsHitByJailbirdList[i].collider.gameObject.tag;
                    for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
                    {
                        if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == text)
                        {
                            jailbirdAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[j].hitSurfaceSFX);
                            WalkieTalkie.TransmitOneShotAudio(jailbirdAudio, StartOfRound.Instance.footstepSurfaces[j].hitSurfaceSFX);
                            hitSurfaceID = j;
                            break;
                        }
                    }
                }
                else if (objectsHitByJailbirdList[i].transform.TryGetComponent(out component) && !(objectsHitByJailbirdList[i].transform == previousPlayerHeldBy.transform) && (objectsHitByJailbirdList[i].point == Vector3.zero || !Physics.Linecast(start, objectsHitByJailbirdList[i].point, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault)))
                {
                    flag = true;
                    Vector3 forward = previousPlayerHeldBy.gameplayCamera.transform.forward;
                }
            }
        }
        if (flag)
        {
            var soundID = RoundManager.PlayRandomClip(jailbirdAudio, hitSFX);
            FindObjectOfType<RoundManager>().PlayAudibleNoise(transform.position, 17f, 0.8f);
            playerHeldBy.playerBodyAnimator.SetTrigger("shovelHit");
            HitShovelServerRpc(soundID);
        }*/
        if (this.previousPlayerHeldBy == null)
        {
            Debug.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
            return;
        }
        //previousPlayerHeldBy.activatingItem = false;
        bool flag = false;
        bool flag2 = false;
        bool flag3 = false;
        int num = -1;
        if (!cancel)
        {
            previousPlayerHeldBy.twoHanded = false;
            objectsHitByJailbird = Physics.SphereCastAll(this.previousPlayerHeldBy.gameplayCamera.transform.position + this.previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 0.85f, this.previousPlayerHeldBy.gameplayCamera.transform.forward, 1.5f, jailbirdMaskint, QueryTriggerInteraction.Collide);
            objectsHitByJailbirdList = (from x in objectsHitByJailbird orderby x.distance select x).ToList();
            List<EnemyAI> list = new List<EnemyAI>();
            for (int i = 0; i < this.objectsHitByJailbirdList.Count; i++)
            {
                IHittable hittable;
                RaycastHit raycastHit;
                if (this.objectsHitByJailbirdList[i].transform.gameObject.layer == 8 || this.objectsHitByJailbirdList[i].transform.gameObject.layer == 11)
                {
                    if (!this.objectsHitByJailbirdList[i].collider.isTrigger)
                    {
                        flag = true;
                        string tag = this.objectsHitByJailbirdList[i].collider.gameObject.tag;
                        for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
                        {
                            if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == tag)
                            {
                                num = j;
                                break;
                            }
                        }
                    }
                }
                else if (this.objectsHitByJailbirdList[i].transform.TryGetComponent<IHittable>(out hittable) && !(this.objectsHitByJailbirdList[i].transform == this.previousPlayerHeldBy.transform) && (this.objectsHitByJailbirdList[i].point == Vector3.zero || !Physics.Linecast(this.previousPlayerHeldBy.gameplayCamera.transform.position, this.objectsHitByJailbirdList[i].point, out raycastHit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
                {
                    flag = true;
                    Vector3 forward = this.previousPlayerHeldBy.gameplayCamera.transform.forward;
                    try
                    {
                        EnemyAICollisionDetect component = this.objectsHitByJailbirdList[i].transform.GetComponent<EnemyAICollisionDetect>();
                        if (component != null)
                        {
                            if (component.mainScript == null || list.Contains(component.mainScript))
                            {
                                goto IL_361;
                            }
                        }
                        else if (this.objectsHitByJailbirdList[i].transform.GetComponent<PlayerControllerB>() != null)
                        {
                            if (flag3)
                            {
                                goto IL_361;
                            }
                            flag3 = true;
                        }
                        bool flag4 = hittable.Hit(jailbirdHitForce, forward, this.previousPlayerHeldBy, true, 1);
                        if (flag4 && component != null)
                        {
                            list.Add(component.mainScript);
                        }
                        if (!flag2)
                        {
                            flag2 = flag4;
                        }
                    }
                    catch (Exception arg)
                    {
                        Debug.Log(string.Format("Exception caught when hitting object with shovel from player #{0}: {1}", this.previousPlayerHeldBy.playerClientId, arg));
                    }
                }
            IL_361:;
            }
        }
        if (flag)
        {
            RoundManager.PlayRandomClip(jailbirdAudio, this.hitSFX, true, 1f, 0, 1000);
            UnityEngine.Object.FindObjectOfType<RoundManager>().PlayAudibleNoise(base.transform.position, 17f, 0.8f, 0, false, 0);
            if (!flag2 && num != -1)
            {
                jailbirdAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
                WalkieTalkie.TransmitOneShotAudio(jailbirdAudio, StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX, 1f);
            }
            playerHeldBy.playerBodyAnimator.SetTrigger("shovelHit");
            HitShovelServerRpc(num);
        }
    }

    [ServerRpc]
    public void HitShovelServerRpc(int soundID)
    {
        HitShovelClientRpc(soundID);
    }

    [ClientRpc]
    public void HitShovelClientRpc(int soundID)
    {
        HitSurfaceWithShovel(soundID);
    }

    public void HitSurfaceWithShovel(int hitSurfaceID)
    {
        jailbirdAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
        WalkieTalkie.TransmitOneShotAudio(jailbirdAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
    }
    /*
    [RuntimeInitializeOnLoadMethod]
    public static void InitializeRPCS_Shovel()
    {
        NetworkManager.__rpc_func_table.Add(4113335123u, __rpc_handler_4113335123);
        NetworkManager.__rpc_func_table.Add(2042054613u, __rpc_handler_2042054613);
        NetworkManager.__rpc_func_table.Add(2096026133u, __rpc_handler_2096026133);
        NetworkManager.__rpc_func_table.Add(275435223u, __rpc_handler_275435223);
    }
    
    public static void __rpc_handler_4113335123(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
    {
        NetworkManager networkManager = target.NetworkManager;
        if ((object)networkManager == null || !networkManager.IsListening)
        {
            return;
        }
        if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
        {
            if (networkManager.LogLevel <= LogLevel.Normal)
            {
                Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
            }
        }
        else
        {
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.Server;
            ((JailbirdShovel)target).ReelUpSFXServerRpc();
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.None;
        }
    }

    public static void __rpc_handler_2042054613(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
    {
        NetworkManager networkManager = target.NetworkManager;
        if ((object)networkManager != null && networkManager.IsListening)
        {
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.Client;
            ((JailbirdShovel)target).ReelUpSFXClientRpc();
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.None;
        }
    }

    public static void __rpc_handler_2096026133(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
    {
        NetworkManager networkManager = target.NetworkManager;
        if ((object)networkManager == null || !networkManager.IsListening)
        {
            return;
        }
        if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
        {
            if (networkManager.LogLevel <= LogLevel.Normal)
            {
                Debug.LogError("Only the owner can invoke a ServerRpc that requires ownership!");
            }
        }
        else
        {
            ByteUnpacker.ReadValueBitPacked(reader, out int value);
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.Server;
            ((JailbirdShovel)target).HitShovelServerRpc(value);
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.None;
        }
    }

    public static void __rpc_handler_275435223(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
    {
        NetworkManager networkManager = target.NetworkManager;
        if ((object)networkManager != null && networkManager.IsListening)
        {
            ByteUnpacker.ReadValueBitPacked(reader, out int value);
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.Client;
            ((JailbirdShovel)target).HitShovelClientRpc(value);
            ((JailbirdShovel)target).__rpc_exec_stage = __RpcExecStage.None;
        }
    }*/
}
