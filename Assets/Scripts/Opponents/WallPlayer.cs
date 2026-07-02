using UnityEngine;
using System;

/// <summary>
/// Barajdaki tek bir oyuncu. Topla teması üst katmana (DefensiveWall) bildirir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WallPlayer : MonoBehaviour
{
    /// <summary>Top bu oyuncuya çarptığında tetiklenir.</summary>
    public event Action OnBallHit;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<BallLauncher>() != null)
        {
            OnBallHit?.Invoke();
        }
    }
}
