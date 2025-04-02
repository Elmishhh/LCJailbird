// Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// Shovel
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameNetcodeStuff;
using LCJailbird;
using LCJailbird.HelperBehaviour;
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

    public int durability = 4;
    public int jailbirdChargeSpeed = 35;
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
            reelingUpCoroutine = StartCoroutine(reelUpJailbird());
        }
    }

    public IEnumerator reelUpJailbird()
    {
        playerHeldBy.activatingItem = true;
        playerHeldBy.twoHanded = true;

        playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
        playerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: true);
        ReelUpSFXServerRpc();

        yield return new WaitForSeconds(5.5f);

        StartCoroutine(chargeTimer(3)); // max charge time = 3
        ChargingSFXServerRpc();
        checkEnemyInfront = true;

        yield return new WaitUntil(() => (chargeTimerFinished && !chargeTimerActive) || doAttack);
        StopSFXServerRpc();
        if (durability <= 1) { doExplode = doAttack; }
        checkEnemyInfront = false;
        doAttack = false;
        chargeTimerFinished = false;
        chargeTimerActive = false;

        SwingJailbird(!isHeld);

        try
        {
            HitJailbird(!isHeld);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("error when hitting with a shovel,");
            Plugin.Logger.LogError(ex);
        }

        yield return new WaitForSeconds(0.3f);

        reelingUp = false;
        reelingUpCoroutine = null;
        playerHeldBy.twoHanded = false;
        previousPlayerHeldBy.activatingItem = false;
    }
    public void FixedUpdate()
    {
        if (checkEnemyInfront)
        {
            Vector3 direction = new Vector3(previousPlayerHeldBy.gameplayCamera.transform.forward.x, 0, previousPlayerHeldBy.gameplayCamera.transform.forward.z).normalized;
            doAttack = Physics.SphereCast(origin: previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 0.55f, direction, out RCHit, 1.85f, jailbirdMaskint, QueryTriggerInteraction.Collide);
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
                previousPlayerHeldBy.externalForces = new Vector3(previousPlayerHeldBy.gameplayCamera.transform.forward.x, 0, previousPlayerHeldBy.gameplayCamera.transform.forward.z).normalized * jailbirdChargeSpeed;
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

    #region reel up SFX RPCs
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
    #endregion
    #region charing SFX RPCs
    [ServerRpc]
    public void ChargingSFXServerRpc()
    {
        ChargingSFXClientRpc();
    }

    [ClientRpc]
    public void ChargingSFXClientRpc()
    {
        jailbirdAudio.PlayOneShot(chargeSFX);
    }


    [ServerRpc]
    public void StopSFXServerRpc()
    {
        StopSFXClientRpc();
    }

    [ClientRpc]
    public void StopSFXClientRpc()
    {
        jailbirdAudio.Stop();
    }
    #endregion
    #region Swing SFX RPCs
    [ServerRpc]
    public void SwingSFXServerRpc()
    {
        SwingSFXClientRpc();
    }
    [ClientRpc]
    public void SwingSFXClientRpc()
    {
        jailbirdAudio.PlayOneShot(swingSFX);
    }
    #endregion
    #region Hit RPCs
    [ServerRpc]
    public void HitJailbirdServerRpc(int soundID)
    {
        HitJailbirdClientRpc(soundID);
    }

    [ClientRpc]
    public void HitJailbirdClientRpc(int soundID)
    {
        HitSurfaceWithJailbird(soundID);
    }
    #endregion
    #region stun enemies RPCs
    [ServerRpc]
    public void StunEnemiesServerRpc(ulong enemyID)
    {
        StunEnemiesClientRpc(enemyID);
    }
    [ClientRpc]
    public void StunEnemiesClientRpc(ulong enemyID)
    {
        TryStunEnemy(enemyID);
    }
    #endregion
    #region durability RPCs
    [ServerRpc]
    public void ChangeDurabilityServerRpc(int newDurability)
    {
        ChangeDurabilityClientRpc(newDurability);
    }
    [ClientRpc]
    public void ChangeDurabilityClientRpc(int newDurability)
    {
        ChangeDurability(newDurability);
    }
    #endregion
    public override void DiscardItem()
    {
        if (playerHeldBy != null)
        {
            playerHeldBy.activatingItem = false;
        }
        base.DiscardItem();
    }

    public void SwingJailbird(bool cancel = false)
    {
        previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: false);
        if (!cancel)
        {
            jailbirdAudio.PlayOneShot(swingSFX);
            SwingSFXServerRpc();
            previousPlayerHeldBy.UpdateSpecialAnimationValue(specialAnimation: true, (short)previousPlayerHeldBy.transform.localEulerAngles.y, 0.4f);
        }
    }

    public void HitJailbird(bool cancel = false)
    {
        if (this.previousPlayerHeldBy == null)
        {
            Debug.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
            return;
        }
        bool flag = false;
        bool flag2 = false;
        bool flag3 = false;
        int num = -1;
        if (!cancel)
        {
            previousPlayerHeldBy.twoHanded = false;
            objectsHitByJailbird = Physics.SphereCastAll(this.previousPlayerHeldBy.gameplayCamera.transform.position + this.previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 1f, this.previousPlayerHeldBy.gameplayCamera.transform.forward, 1.5f, jailbirdMaskint, QueryTriggerInteraction.Collide);
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
                        bool flag4 = hittable.Hit(jailbirdHitForce, forward, previousPlayerHeldBy, true, 1);
                        if (flag4)
                        {
                            ChangeDurabilityServerRpc(durability - 1);
                        }
                        if (flag4 && component != null)
                        {
                            list.Add(component.mainScript);
                            StunEnemiesServerRpc(component.mainScript.NetworkObjectId);
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
            HitJailbirdServerRpc(num);
        }
    }

    public void HitSurfaceWithJailbird(int hitSurfaceID)
    {
        try
        {
            jailbirdAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
            WalkieTalkie.TransmitOneShotAudio(jailbirdAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogWarning("Error when playing shovel hit sound, are you spawning things that arent naturally on that moon?");
            Plugin.Logger.LogWarning(e);
        }
    }
    public void ChangeDurability(int newDurability)
    {
        Plugin.Logger.LogInfo($"new durability is: {newDurability}");
        if (newDurability == 0 && durability == 1)
        {
            this.transform.Find("jailbird/Mesh_Jailbird/red_glow").gameObject.SetActive(true);
            this.transform.Find("jailbird/Mesh_Jailbird/blue_glow").gameObject.SetActive(false);
        }
        else if (newDurability == -1 && durability == 0)
        {
            Vector3 explosionPosition = playerHeldBy.transform.position + playerHeldBy.transform.forward * 0.5f;
            DestroyObjectInHand(playerHeldBy);
            ExplosionHelper._instance.ExplodeDelayed(explosionPosition);
        }
        durability = newDurability;
    }

    public void TryStunEnemy(ulong enemyID)
    {
        RoundManager.Instance.RefreshEnemiesList();
        foreach (EnemyAI spawnedEnemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (spawnedEnemy.NetworkObjectId == enemyID)
            {
                Plugin.Logger.LogMessage($"found enemy with id {enemyID}");
                spawnedEnemy.SetEnemyStunned(true, 4.5f);
                return;
            }
        }
        Plugin.Logger.LogError($"could not find enemy with id {enemyID}");
    }
}
