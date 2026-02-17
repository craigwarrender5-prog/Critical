using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Critical.Validation;
namespace Critical.Validation
{
    public static class IP0025StageDRunner
    {
        [MenuItem("Critical/Run IP-0025 Stage D Legacy Order Parity")]
        public static void RunStageDLegacyOrderParity()
        {
            string root = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string runstamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string issuePath = Path.Combine(root, "Governance", "Issues", $"IP-0025_StageD_LegacyOrderParity_{runstamp}.md");

            var sb = new StringBuilder();
            sb.AppendLine("# IP-0025 Stage D - Legacy Order Parity");
            sb.AppendLine();
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Run stamp: `{runstamp}`");
            sb.AppendLine("- Scope: legacy `StepSimulation(dt)` causal sequence mapping to coordinator module slots.");
            sb.AppendLine();
            sb.AppendLine("## Legacy Causal Sequence (from `HeatupSimEngine.StepSimulation(dt)` callsite comments)");
            sb.AppendLine("1. RCP startup sequencing and contribution update.");
            sb.AppendLine("2. Heater and spray control updates before thermal solve paths.");
            sb.AppendLine("3. Regime-based thermal/pressure update paths and CVCS/transport coupling.");
            sb.AppendLine("4. RVLIS + annunciator updates.");
            sb.AppendLine("5. HZP system updates.");
            sb.AppendLine("6. Inventory audit and primary mass ledger diagnostics.");
            sb.AppendLine();
            sb.AppendLine("## Proposed Coordinator Slot Order (Stage D provisional)");
            sb.AppendLine("1. `Reactor`");
            sb.AppendLine("2. `RCP`");
            sb.AppendLine("3. `RCS`");
            sb.AppendLine("4. `PZR`");
            sb.AppendLine("5. `CVCS`");
            sb.AppendLine("6. `RHR`");
            sb.AppendLine("7. transfer finalize");
            sb.AppendLine("8. snapshot publish");
            sb.AppendLine("9. validation hook");
            sb.AppendLine();
            sb.AppendLine("## Parity Matrix");
            sb.AppendLine("| Legacy Phase | Proposed Slot(s) | Parity Status | Decision |");
            sb.AppendLine("|---|---|---|---|");
            sb.AppendLine("| RCP sequencing | `RCP` | MATCH | Keep slot order. |");
            sb.AppendLine("| Heater + spray control | `PZR` | PARTIAL (legacy pre-solve control timing) | Preserve legacy authority path in Stage D; defer slot-level behavioral activation to Stage E packaging. |");
            sb.AppendLine("| Regime thermal + coupling | `RCS`, `CVCS`, `RHR`, `Reactor` | PARTIAL (legacy monolithic solve) | Keep stubs no-op in Stage D; no physics moved. |");
            sb.AppendLine("| RVLIS + annunciators | post-module internal legacy update | MATCH (via legacy authority path) | Keep under legacy step until extraction stage. |");
            sb.AppendLine("| Inventory + primary ledger diagnostics | transfer finalize + snapshot publish | MATCH | Keep finalize/publish order after legacy step. |");
            sb.AppendLine();
            sb.AppendLine("## Mismatch Flags");
            sb.AppendLine("- No blocking mismatch for Stage D scaffolding because authoritative mutable updates remain legacy-only.");
            sb.AppendLine("- Legacy bypass flags remain scaffold-only; subsystem bypass activation is deferred with Stage E extraction authorization.");
            sb.AppendLine();
            sb.AppendLine("## Decision Record");
            sb.AppendLine("- Stage D coordinator order is accepted as provisional parity scaffold.");
            sb.AppendLine("- Comparator remains shadow-only and side-effect free.");
            sb.AppendLine("- Any causal-order change that affects mutable-state authority requires explicit Stage E authorization.");

            Directory.CreateDirectory(Path.GetDirectoryName(issuePath) ?? ".");
            File.WriteAllText(issuePath, sb.ToString());

            Debug.Log($"[IP-0025][StageD] Artifact: {issuePath}");
        }
    }
}

