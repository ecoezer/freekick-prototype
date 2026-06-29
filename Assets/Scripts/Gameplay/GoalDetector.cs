using UnityEngine;
using System;

/// <summary>
/// Kale içine yerleştirilen görünmez bir Trigger Collider'a eklenir.
/// Topun bu alana girdiğini tespit ederek "Gol" olayını (event) fırlatır.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalDetector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Golü atacak olan objenin etiketi (genelde 'Ball').")]
    [SerializeField] private string ballTag = "Ball";

    /// <summary>
    /// Gol atıldığında tetiklenen event. ScoreManager veya UI bunu dinleyebilir.
    /// </summary>
    public event Action OnGoalScored;

    private void OnTriggerEnter(Collider other)
    {
        // Giren obje top mu?
        if (other.CompareTag(ballTag))
        {
            Debug.Log("[GoalDetector] GOOOOAAAL!!!");
            OnGoalScored?.Invoke();
        }
    }
}
