using UnityEngine;
using Critical.Tests;

public class UnityTestRunner : MonoBehaviour
{
    public bool runTestsOnStart = true;

    void Start()
    {
        if (runTestsOnStart)
        {
            Debug.Log("=== STARTING PHASE 1 PHYSICS TESTS ===");
            var runner = new Phase1TestRunner();
            runner.RunAllTests();
            Debug.Log("=== TESTS COMPLETE ===");
        }
    }
}