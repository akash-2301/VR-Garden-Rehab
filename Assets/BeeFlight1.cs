// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// [System.Serializable]
// public class BeeData1
// {
//     public Transform bee;
//     public Vector3 initialPosition;
// }

// public class BeeFlight1 : MonoBehaviour
// {
//     [Header("Bee Settings")]
//     public BeeData1[] bees;
//     public Transform[] sitPoints;
//     public float beeMoveDelay = 5f;
//     public float moveDuration = 2f;

//     [Header("Runtime")]
//     public HashSet<Transform> beesInFlight = new HashSet<Transform>();
//     private Dictionary<Transform, Coroutine> activeBeeCoroutines = new Dictionary<Transform, Coroutine>();

//     private int maxBees => bees.Length;
//     public bool beeMovementPaused = false;

//     void OnEnable()
//     {
//         ResetAllBees();                  // 🟡 Ensure all bees reset even on enable
//         beesInFlight.Clear();
//         StopAllCoroutines();
//         StartCoroutine(BeeMovementCycle());
//     }

//     // ✅ Call this at start of each stage
//     public void ResetAllBees()
//     {
//         beesInFlight.Clear();

//         foreach (var beeData in bees)
//         {
//             if (beeData.bee == null) continue;

//             beeData.bee.gameObject.SetActive(true);      // Enable the bee
//             beeData.bee.SetParent(null);                 // Detach from any sit point
//             beeData.bee.position = beeData.initialPosition; // Move to start position
//         }
//     }

//     public IEnumerator BeeMovementCycle()
//     {
//         while (true)
//         {
//             if (beeMovementPaused || Time.timeScale == 0f)
//             {
//                 yield return null;
//                 continue;
//             }

//             List<Transform> availablePoints = GetValidSitPoints();
//             if (availablePoints.Count < maxBees)
//             {
//                 yield return null;
//                 continue;
//             }

//             ShuffleList(availablePoints);
//             HashSet<Transform> usedPoints = new HashSet<Transform>();

//             for (int i = 0; i < Mathf.Min(maxBees, bees.Length); i++)
//             {
//                 Transform bee = bees[i].bee;
//                 if (bee == null || beesInFlight.Contains(bee)) continue;

//                 Transform target = null;
//                 foreach (Transform p in availablePoints)
//                 {
//                     if (!usedPoints.Contains(p))
//                     {
//                         target = p;
//                         usedPoints.Add(p);
//                         break;
//                     }
//                 }

//                 if (target != null)
//                 {
//                     if (activeBeeCoroutines.ContainsKey(bee))
//                         StopCoroutine(activeBeeCoroutines[bee]);

//                     Coroutine c = StartCoroutine(MoveBeeTo(bee, target));
//                     activeBeeCoroutines[bee] = c;
//                 }
//             }

//             yield return new WaitForSeconds(beeMoveDelay);
//         }
//     }

//     IEnumerator MoveBeeTo(Transform bee, Transform target)
//     {
//         if (bee == null || target == null) yield break;

//         beesInFlight.Add(bee);
//         bee.SetParent(null); // Detach for movement

//         Vector3 startPos = bee.position;
//         Quaternion startRot = bee.rotation;
//         Vector3 endPos = target.position;
//         Quaternion endRot = target.rotation;

//         float elapsed = 0f;
//         while (elapsed < moveDuration)
//         {
//             elapsed += Time.deltaTime;
//             float t = elapsed / moveDuration;
//             bee.position = Vector3.Lerp(startPos, endPos, t);
//             bee.rotation = Quaternion.Slerp(startRot, endRot, t);
//             yield return null;
//         }

//         bee.position = endPos;
//         bee.rotation = endRot;
//         bee.SetParent(target);

//         beesInFlight.Remove(bee);
//         activeBeeCoroutines.Remove(bee);
//     }

//     public void ForceMoveBee(Transform bee)
//     {
//         if (activeBeeCoroutines.ContainsKey(bee))
//             StopCoroutine(activeBeeCoroutines[bee]);

//         Coroutine c = StartCoroutine(FindAndMoveBee(bee));
//         activeBeeCoroutines[bee] = c;
//     }

//     IEnumerator FindAndMoveBee(Transform bee)
//     {
//         yield return new WaitForSeconds(0.1f);

//         List<Transform> validTargets = GetValidSitPoints();
//         if (validTargets.Count > 0)
//         {
//             Transform target = validTargets[Random.Range(0, validTargets.Count)];
//             Coroutine c = StartCoroutine(MoveBeeTo(bee, target));
//             activeBeeCoroutines[bee] = c;
//         }
//     }

//     List<Transform> GetValidSitPoints()
//     {
//         List<Transform> valid = new List<Transform>();

//         foreach (Transform sit in sitPoints)
//         {
//             if (sit == null || !sit.gameObject.activeInHierarchy || !sit.parent.gameObject.activeInHierarchy)
//                 continue;

//             Level5Object obj = sit.GetComponentInParent<Level5Object>();
//             if (obj != null && Level5Object.ActiveObjectTypes.Contains(obj.objectType))
//             {
//                 valid.Add(sit);
//             }
//             Level6Object objj = sit.GetComponentInParent<Level6Object>();
//             if (objj != null && Level6Object.ActiveObjectTypes.Contains(objj.objectType))
//             {
//                 valid.Add(sit);
//             }
//         }

//         return valid;
//     }

//     public bool AllSitPointsHaveBees()
//     {
//         foreach (Transform sit in sitPoints)
//         {
//             if (sit == null || !sit.gameObject.activeInHierarchy || !sit.parent.gameObject.activeInHierarchy)
//                 continue;

//             if (sit.childCount == 0) return false;

//             Transform bee = sit.GetChild(0);
//             if (beesInFlight.Contains(bee)) return false;
//         }

//         return true;
//     }

//     void ShuffleList(List<Transform> list)
//     {
//         for (int i = 0; i < list.Count; i++)
//         {
//             int rand = Random.Range(i, list.Count);
//             (list[i], list[rand]) = (list[rand], list[i]);
//         }
//     }

//     public bool IsBeeInFlight(Transform obj)
//     {
//         foreach (Transform bee in beesInFlight)
//         {
//             if (bee != null && obj != null && bee.parent != obj)
//             {
//                 if (bee.position == obj.position)
//                     return true;
//             }
//         }
//         return false;
//     }
// }

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeeData
{
    public Transform bee;
}

public class BeeFlight1 : MonoBehaviour
{
    [Header("Bee Settings")]
    public BeeData[] bees;         // 🟡 Drag bee Transforms
    public Transform[] sitPoints;  // 🟡 Drag all sitPoints from Level5Object
    [Range(0.3f, 1.5f)]
public float beeScale = 0.8f;

    // ✅ Call this at the start of each stage
    // Reset all bees and place on random sit points
    public void ResetAllBees()
    {
        List<Transform> shuffledSitPoints = new List<Transform>(sitPoints);
        ShuffleList(shuffledSitPoints);

        foreach (var beeData in bees)
        {
            if (beeData.bee == null) continue;

            beeData.bee.SetParent(null);
            beeData.bee.gameObject.SetActive(true);   // 🔄 Reactivate here
        }

        for (int i = 0; i < Mathf.Min(bees.Length, shuffledSitPoints.Count); i++)
        {
            var bee = bees[i].bee;
            var sitPoint = shuffledSitPoints[i];

            if (bee == null || sitPoint == null) continue;

            bee.SetParent(sitPoint);
            bee.localPosition = Vector3.zero;
            bee.localRotation = Quaternion.identity;
            bee.localScale = Vector3.one * beeScale;

        }
    }


    void ShuffleList(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
}




