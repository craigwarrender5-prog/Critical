// ============================================================================
// CRITICAL: Validation Dashboard Test Setup
// ValidationDashboardTestSetup.cs - Minimal test to verify compilation
// ============================================================================

using UnityEngine;

using Critical.Validation;
/// <summary>
/// Minimal test component to verify ValidationDashboard compilation.
/// If this component can be added, the namespace/class resolution is working.
/// </summary>
public class ValidationDashboardTestSetup : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool testMode = true;
    
    void Start()
    {
        Debug.Log("[ValidationDashboardTestSetup] Component loaded successfully!");
        
        // Try to find the engine
        var engine = FindObjectOfType<HeatupSimEngine>();
        if (engine != null)
        {
            Debug.Log("[ValidationDashboardTestSetup] Found HeatupSimEngine");
        }
        else
        {
            Debug.LogWarning("[ValidationDashboardTestSetup] No HeatupSimEngine found");
        }
    }
}

