using UnityEngine;
using System;

/// <summary>
/// Direk / üst direğe eklenir. Top çarptığında event fırlatır.
/// MatchReferee "DİREK!" mesajı için dinler.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WoodworkNotifier : MonoBehaviour
{
    /// <summary>Top bu direğe çarptığında tetiklenir.</summary>
    public event Action OnBallHit;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<BallLauncher>() != null)
        {
            OnBallHit?.Invoke();
        }
    }
}
