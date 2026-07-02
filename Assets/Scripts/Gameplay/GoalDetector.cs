using UnityEngine;
using System;

/// <summary>
/// Kale içine yerleştirilen görünmez bir Trigger Collider'a eklenir.
/// Topun bu alana girdiğini tespit ederek "Gol" olayını (event) fırlatır.
/// Tespit tag yerine component ile yapılır (sahne kurulumunda tag tanımı gerekmez).
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalDetector : MonoBehaviour
{
    /// <summary>
    /// Gol atıldığında tetiklenen event. MatchReferee bunu dinler.
    /// </summary>
    public event Action OnGoalScored;

    private void OnTriggerEnter(Collider other)
    {
        // Giren obje top mu? (Top, BallLauncher taşıyan tek objedir.)
        if (other.GetComponentInParent<BallLauncher>() != null)
        {
            Debug.Log("[GoalDetector] GOOOAAAL!");
            OnGoalScored?.Invoke();
        }
    }
}
