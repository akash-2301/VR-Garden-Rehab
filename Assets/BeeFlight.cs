using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeeData1
{
    public Transform bee;
    public Vector3 initialPosition;
}

public class BeeFlight : MonoBehaviour
{
    [Header("Bee Settings")]
    [HideInInspector] public Level6Object lastActiveObject = null;

    public BeeData1[] bees;
    public Transform[] sitPoints;
    public float beeMoveDelay = 5f;
    public float moveDuration = 2f;

    [Header("Runtime")]
    public HashSet<Transform> beesInFlight = new HashSet<Transform>();
    private Dictionary<Transform, Coroutine> activeBeeCoroutines = new Dictionary<Transform, Coroutine>();

    private int maxBees => bees != null ? bees.Length : 0;
    public bool beeMovementPaused = false;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    void OnEnable()
    {
        DebugLog("[BeeFlight] OnEnable called");
        
        StopAllCoroutines();
        beesInFlight.Clear();
        activeBeeCoroutines.Clear();

        // Reset bees and start movement cycle
        ResetAllBees();
    }

    /// <summary>
    /// Reset all bees to their initial positions and assign them to valid sit points instantly.
    /// </summary>
    public void ResetAllBees()
    {
        DebugLog("[BeeFlight] ResetAllBees called");
        
        // Stop ALL coroutines including the movement cycle
        StopAllCoroutines();
        
        // Stop running coroutines for bees (movement)
        foreach (var kv in activeBeeCoroutines)
            if (kv.Value != null) StopCoroutine(kv.Value);
        activeBeeCoroutines.Clear();
        beesInFlight.Clear();

        if (bees != null)
        {
            foreach (var beeData in bees)
            {
                if (beeData == null || beeData.bee == null) continue;

                // notify previous parent (if any) that bee is being removed
                Transform prevParent = beeData.bee.parent;
                if (prevParent != null)
                {
                    var prevL6 = prevParent.GetComponentInParent<Level6Object>();
                    if (prevL6 != null)
                    {
                        prevL6.OnBeeReleased();
                        prevL6.manager?.ReevaluateCuesImmediately();
                    }
                }

                beeData.bee.gameObject.SetActive(true);
                beeData.bee.SetParent(null);
                beeData.bee.position = beeData.initialPosition;
                beeData.bee.rotation = Quaternion.identity;
            }
        }

        // Assign bees to sit points immediately (one bee per sit point)
        StartCoroutine(AssignBeesToSitPointsInstantly());
    }
    
    /// <summary>
    /// Called by Level6StageManager when updating sit points for a new stage.
    /// This refreshes the bee assignments for the new stage objects.
    /// </summary>
    public void RefreshBeesForNewStage(Transform[] newSitPoints)
    {
        DebugLog($"[BeeFlight] RefreshBeesForNewStage called with {newSitPoints?.Length ?? 0} sit points");
        
        // Update sit points array
        sitPoints = newSitPoints;
        
        // Stop all current movement and reset
        StopAllCoroutines();
        activeBeeCoroutines.Clear();
        beesInFlight.Clear();
        
        // Move all bees back to initial positions first
        if (bees != null)
        {
            foreach (var beeData in bees)
            {
                if (beeData == null || beeData.bee == null) continue;

                // Notify previous parent
                Transform prevParent = beeData.bee.parent;
                if (prevParent != null)
                {
                    var prevL6 = prevParent.GetComponentInParent<Level6Object>();
                    if (prevL6 != null)
                    {
                        prevL6.OnBeeReleased();
                        prevL6.manager?.ReevaluateCuesImmediately();
                    }
                }

                beeData.bee.gameObject.SetActive(true);
                beeData.bee.SetParent(null);
                beeData.bee.position = beeData.initialPosition;
                beeData.bee.rotation = Quaternion.identity;
            }
        }
        
        // Now assign to new stage's sit points and restart movement
        StartCoroutine(AssignBeesToSitPointsInstantly());
    }

    IEnumerator AssignBeesToSitPointsInstantly()
    {
        DebugLog("[BeeFlight] AssignBeesToSitPointsInstantly started");
        
        // wait a frame for stage/objects to initialize
        yield return null;

        var validSitPoints = GetValidSitPoints();
        DebugLog($"[BeeFlight] Found {validSitPoints.Count} valid sit points for initial assignment");
        
        if (validSitPoints.Count == 0)
        {
            Debug.LogWarning("[BeeFlight] No valid sit points found at instant assignment.");
            // Still start movement cycle even if no sit points initially
            StartCoroutine(BeeMovementCycle());
            yield break;
        }

        ShuffleList(validSitPoints);

        // track used sit points so we don't double-assign
        HashSet<Transform> usedPoints = new HashSet<Transform>();

        int assignCount = Mathf.Min(maxBees, validSitPoints.Count);
        int beeIndex = 0;

        for (int i = 0; i < validSitPoints.Count && beeIndex < assignCount; i++)
        {
            Transform sitPoint = validSitPoints[i];
            if (sitPoint == null) continue;
            if (sitPoint.childCount > 0) continue; // already occupied
            
            if (beeIndex >= bees.Length) break;
            
            var beeData = bees[beeIndex];
            if (beeData == null || beeData.bee == null) 
            { 
                beeIndex++; 
                continue; 
            }

            beeData.bee.position = sitPoint.position;
            beeData.bee.rotation = sitPoint.rotation;
            beeData.bee.SetParent(sitPoint);

            // Notify Level6Object (if present) that a bee arrived
            var l6 = sitPoint.GetComponentInParent<Level6Object>();
            if (l6 != null)
            {
                l6.OnBeeAssigned();
                l6.manager?.ReevaluateCuesImmediately();
            }

            usedPoints.Add(sitPoint);
            beeIndex++;
            
            DebugLog($"[BeeFlight] Assigned bee {beeIndex} to sit point {sitPoint.name}");
        }

        DebugLog($"[BeeFlight] Initial assignment complete. Assigned {beeIndex} bees.");
        
        // NOW start the movement cycle after initial assignment is done
        yield return new WaitForSeconds(0.5f); // Small delay before starting movement
        StartCoroutine(BeeMovementCycle());
    }

    public IEnumerator BeeMovementCycle()
    {
        DebugLog("[BeeFlight] BeeMovementCycle started");
        
        // Wait a bit before first movement
        yield return new WaitForSeconds(beeMoveDelay);
        
        while (true)
        {
            if (beeMovementPaused)
            {
                DebugLog("[BeeFlight] Movement paused by beeMovementPaused flag");
                yield return null;
                continue;
            }
            
            if (Time.timeScale == 0f)
            {
                DebugLog("[BeeFlight] Movement paused - Time.timeScale is 0");
                yield return null;
                continue;
            }

            List<Transform> availablePoints = GetValidSitPoints();
            
            if (availablePoints.Count == 0)
            {
                DebugLog("[BeeFlight] No valid targets right now");
                // no valid targets right now
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            DebugLog($"[BeeFlight] Movement cycle tick - {availablePoints.Count} valid sit points available");

            ShuffleList(availablePoints);
            HashSet<Transform> reservedTargets = new HashSet<Transform>();

            int loopCount = Mathf.Min(maxBees, bees != null ? bees.Length : 0);
            int beesMovedThisCycle = 0;
            
            for (int i = 0; i < loopCount; i++)
            {
                if (bees == null || bees[i] == null) continue;
                Transform bee = bees[i].bee;
                if (bee == null || beesInFlight.Contains(bee)) continue;

                // find first unreserved & unoccupied sit point
                Transform target = null;
                foreach (Transform p in availablePoints)
                {
                    if (p == null) continue;
                    if (reservedTargets.Contains(p)) continue;
                    if (p.childCount > 0) continue; // already has a bee sitting
                    target = p;
                    reservedTargets.Add(p);
                    break;
                }

                if (target == null) continue;

                // notify target owner that a bee is being targeted (in-flight)
                var targetOwner = target.GetComponentInParent<Level6Object>();
                if (targetOwner != null)
                {
                    targetOwner.OnBeeTargeted();
                    targetOwner.manager?.NotifyObjectTargeted(targetOwner);

                    targetOwner.manager?.ReevaluateCuesImmediately();
                }

                if (activeBeeCoroutines.ContainsKey(bee))
                    StopCoroutine(activeBeeCoroutines[bee]);

                Coroutine c = StartCoroutine(MoveBeeTo(bee, target));
                activeBeeCoroutines[bee] = c;
                beesMovedThisCycle++;
                
                DebugLog($"[BeeFlight] Started moving bee {i} to {target.name}");
            }

            DebugLog($"[BeeFlight] Cycle complete - moved {beesMovedThisCycle} bees");
            yield return new WaitForSeconds(beeMoveDelay);
        }
    }

    IEnumerator MoveBeeTo(Transform bee, Transform target)
    {
        if (bee == null || target == null) 
        {
            DebugLog("[BeeFlight] MoveBeeTo - null bee or target, aborting");
            yield break;
        }

        DebugLog($"[BeeFlight] Moving bee from {bee.position} to {target.position}");

        // AFTER - store lastActiveObject so manager can inspect it if needed
Transform prevParent = bee.parent;
if (prevParent != null)
{
    var prevL6 = prevParent.GetComponentInParent<Level6Object>();
    if (prevL6 != null)
    {
        // store per-flight "last active" for external use (simple shared field)
        lastActiveObject = prevL6;

                prevL6.OnBeeReleased();
        prevL6.manager?.NotifyObjectReleased(prevL6);

        prevL6.manager?.ReevaluateCuesImmediately();
    }
    else
    {
        lastActiveObject = null;
    }
}
else
{
    lastActiveObject = null;
}


        // start movement
        beesInFlight.Add(bee);
        bee.SetParent(null);

        Vector3 startPos = bee.position;
        Quaternion startRot = bee.rotation;
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (bee == null || target == null)
            {
                DebugLog("[BeeFlight] Bee or target destroyed during movement");
                break;
            }
            
            elapsed += Time.deltaTime;
            float t = moveDuration > 0f ? (elapsed / moveDuration) : 1f;
            bee.position = Vector3.Lerp(startPos, endPos, t);
            bee.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        if (bee == null || target == null)
        {
            beesInFlight.Remove(bee);
            if (bee != null) activeBeeCoroutines.Remove(bee);
            yield break;
        }

        // finalize arrival
        bee.position = endPos;
        bee.rotation = endRot;
        bee.SetParent(target);

        // AFTER arrival: notify the Level6Object at target that the bee assigned itself
        var newL6 = target.GetComponentInParent<Level6Object>();
        if (newL6 != null)
        {
            newL6.OnBeeAssigned();
            
newL6.manager?.NotifyObjectAssigned(newL6);

            newL6.manager?.ReevaluateCuesImmediately();
        }

        beesInFlight.Remove(bee);
        activeBeeCoroutines.Remove(bee);
        
        DebugLog($"[BeeFlight] Bee arrived at {target.name}");
    }

    public void ForceMoveBee(Transform bee)
    {
        if (bee == null) return;
        
        DebugLog($"[BeeFlight] ForceMoveBee called for bee");
        
        if (activeBeeCoroutines.ContainsKey(bee))
            StopCoroutine(activeBeeCoroutines[bee]);

        Coroutine c = StartCoroutine(FindAndMoveBee(bee));
        activeBeeCoroutines[bee] = c;
    }

    IEnumerator FindAndMoveBee(Transform bee)
    {
        yield return new WaitForSeconds(0.1f);

        List<Transform> validTargets = GetValidSitPoints();
        if (validTargets.Count > 0)
        {
            ShuffleList(validTargets);

            // pick first unoccupied
            Transform target = null;
            foreach (Transform t in validTargets)
            {
                if (t != null && t.childCount == 0) 
                { 
                    target = t; 
                    break; 
                }
            }
            
            if (target != null)
            {
                var targetOwner = target.GetComponentInParent<Level6Object>();
                if (targetOwner != null)
                {
                    targetOwner.OnBeeTargeted();
                    targetOwner.manager?.ReevaluateCuesImmediately();
                }

                Coroutine c = StartCoroutine(MoveBeeTo(bee, target));
                activeBeeCoroutines[bee] = c;
            }
            else
            {
                DebugLog("[BeeFlight] FindAndMoveBee - no unoccupied target found");
            }
        }
        else
        {
            DebugLog("[BeeFlight] FindAndMoveBee - no valid targets");
        }
    }

    /// <summary>
    /// Collect valid sit points belonging to Level6Objects.
    /// If Level6Object.ActiveObjectTypes is empty, treat all Level6 objects as valid (fallback).
    /// </summary>
    List<Transform> GetValidSitPoints()
    {
        List<Transform> valid = new List<Transform>();
        if (sitPoints == null) 
        {
            DebugLog("[BeeFlight] GetValidSitPoints - sitPoints array is null");
            return valid;
        }

        bool allowAllIfEmpty = (Level6Object.ActiveObjectTypes == null || Level6Object.ActiveObjectTypes.Count == 0);

        foreach (Transform sit in sitPoints)
        {
            if (sit == null) continue;
            if (!sit.gameObject.activeInHierarchy) continue;
            if (sit.parent == null || !sit.parent.gameObject.activeInHierarchy) continue;

            var owner = sit.GetComponentInParent<Level6Object>();
            if (owner == null) continue;

            if (allowAllIfEmpty || Level6Object.ActiveObjectTypes.Contains(owner.objectType))
            {
                valid.Add(sit);
            }
        }

        return valid;
    }

    public bool AllSitPointsHaveBees()
    {
        if (sitPoints == null) return true;

        foreach (Transform sit in sitPoints)
        {
            if (sit == null || !sit.gameObject.activeInHierarchy || sit.parent == null || !sit.parent.gameObject.activeInHierarchy)
                continue;

            if (sit.childCount == 0) return false;

            Transform bee = sit.GetChild(0);
            if (beesInFlight.Contains(bee)) return false;
        }

        return true;
    }

    void ShuffleList(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }

    public bool IsBeeInFlight(Transform obj)
    {
        foreach (Transform bee in beesInFlight)
        {
            if (bee != null && obj != null && bee.parent != obj)
            {
                if (bee.position == obj.position)
                    return true;
            }
        }
        return false;
    }

    // Debug logging helper
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
}