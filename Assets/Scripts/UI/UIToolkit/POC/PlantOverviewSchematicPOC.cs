// ============================================================================
// CRITICAL: Master the Atom - UI Toolkit Proof of Concept
// PlantOverviewSchematicPOC.cs - Full-Plant Overview Schematic
// ============================================================================
//
// PURPOSE:
//   Primary-tab overview visual showing plant-wide energy and mass flow:
//   Reactor -> Pressurizer -> Steam Generator -> Turbine -> Generator
//   plus condenser/feedwater return loop.
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    public class PlantOverviewSchematicPOC : VisualElement
    {
        // Live inputs (set by dashboard controller)
        public float reactorTempF = 120f;
        public float reactorPressurePsia = 115f;
        public float pressurizerLevelPct = 55f;
        public float sgPressurePsia = 100f;
        public float sgHeatTransferMw = 0f;
        public float condenserVacuumInHg = 0f;
        public float hotwellLevelPct = 50f;
        public float chargingFlowGpm = 0f;
        public float letdownFlowGpm = 0f;
        public int rcpCount;
        public bool rhrActive;
        public bool steamDumpActive;
        public bool sprayActive;

        private float _flowPhase;

        private readonly Label _reactorLabel = NewNodeLabel("REACTOR");
        private readonly Label _pzrLabel = NewNodeLabel("PRESSURIZER");
        private readonly Label _sgLabel = NewNodeLabel("STEAM GENERATOR");
        private readonly Label _turbineLabel = NewNodeLabel("TURBINE");
        private readonly Label _generatorLabel = NewNodeLabel("GENERATOR");
        private readonly Label _condenserLabel = NewNodeLabel("CONDENSER");
        private readonly Label _feedLabel = NewNodeLabel("FEEDWATER");

        private static Label NewNodeLabel(string text)
        {
            var label = new Label(text);
            label.style.position = Position.Absolute;
            label.style.fontSize = 10f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.75f, 0.82f, 0.93f, 1f);
            label.style.backgroundColor = new Color(0.05f, 0.07f, 0.11f, 0.75f);
            label.style.paddingLeft = 4f;
            label.style.paddingRight = 4f;
            label.style.paddingTop = 1f;
            label.style.paddingBottom = 1f;
            label.style.borderTopLeftRadius = 3f;
            label.style.borderTopRightRadius = 3f;
            label.style.borderBottomLeftRadius = 3f;
            label.style.borderBottomRightRadius = 3f;
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        public PlantOverviewSchematicPOC()
        {
            generateVisualContent += OnGenerateVisualContent;

            Add(_reactorLabel);
            Add(_pzrLabel);
            Add(_sgLabel);
            Add(_turbineLabel);
            Add(_generatorLabel);
            Add(_condenserLabel);
            Add(_feedLabel);

            RegisterCallback<GeometryChangedEvent>(_ => LayoutLabels());
        }

        public void UpdateAnimation(float deltaTime)
        {
            bool activeFlow = rcpCount > 0 || Mathf.Abs(chargingFlowGpm - letdownFlowGpm) > 0.5f || sgHeatTransferMw > 0.05f;
            float speed = activeFlow ? 0.65f : 0.15f;
            _flowPhase += deltaTime * speed;
            if (_flowPhase > 1f) _flowPhase -= 1f;
            MarkDirtyRepaint();
        }

        private void LayoutLabels()
        {
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            if (w < 10f || h < 10f) return;

            PositionLabel(_reactorLabel, w * 0.07f, h * 0.13f);
            PositionLabel(_pzrLabel, w * 0.225f, h * 0.22f);
            PositionLabel(_sgLabel, w * 0.35f, h * 0.15f);
            PositionLabel(_turbineLabel, w * 0.575f, h * 0.15f);
            PositionLabel(_generatorLabel, w * 0.80f, h * 0.17f);
            PositionLabel(_condenserLabel, w * 0.60f, h * 0.59f);
            PositionLabel(_feedLabel, w * 0.17f, h * 0.63f);
        }

        private static void PositionLabel(Label label, float x, float y)
        {
            label.style.left = x;
            label.style.top = y;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 40f || h < 40f) return;

            var p = mgc.painter2D;

            Rect reactor = NR(w, h, 0.05f, 0.19f, 0.13f, 0.46f);
            Rect pzr = NR(w, h, 0.23f, 0.28f, 0.055f, 0.30f);
            Rect sg = NR(w, h, 0.33f, 0.20f, 0.16f, 0.36f);
            Rect turbine = NR(w, h, 0.54f, 0.20f, 0.18f, 0.30f);
            Rect generator = NR(w, h, 0.77f, 0.23f, 0.17f, 0.25f);
            Rect condenser = NR(w, h, 0.56f, 0.64f, 0.20f, 0.20f);
            Rect feedPumps = NR(w, h, 0.08f, 0.68f, 0.14f, 0.16f);
            Rect feedHeaters = NR(w, h, 0.24f, 0.68f, 0.20f, 0.16f);

            Color panelStroke = new Color(0.36f, 0.45f, 0.58f, 1f);
            Color panelFill = new Color(0.08f, 0.11f, 0.17f, 1f);

            DrawNode(p, reactor, panelFill, panelStroke, 7f);
            DrawNode(p, sg, panelFill, panelStroke, 7f);
            DrawNode(p, turbine, panelFill, panelStroke, 7f);
            DrawNode(p, generator, panelFill, panelStroke, 7f);
            DrawNode(p, condenser, panelFill, panelStroke, 7f);
            DrawNode(p, feedPumps, panelFill, panelStroke, 7f);
            DrawNode(p, feedHeaters, panelFill, panelStroke, 7f);
            DrawCapsuleNode(p, pzr, panelFill, panelStroke);

            DrawReactorCore(p, reactor);
            DrawPressurizerLevel(p, pzr);
            DrawSteamGeneratorInternals(p, sg);
            DrawTurbineRotor(p, turbine);
            DrawGeneratorRotor(p, generator);
            DrawCondenserCoils(p, condenser);
            DrawFeedPumps(p, feedPumps);
            DrawHeaterBanks(p, feedHeaters);

            Vector2 reactorHotOut = new Vector2(reactor.xMax, reactor.y + reactor.height * 0.42f);
            Vector2 pzrIn = new Vector2(pzr.xMin, pzr.y + pzr.height * 0.52f);
            Vector2 pzrOut = new Vector2(pzr.xMax, pzr.y + pzr.height * 0.52f);
            Vector2 sgPriIn = new Vector2(sg.xMin, sg.y + sg.height * 0.45f);
            Vector2 sgPriOut = new Vector2(sg.xMin, sg.y + sg.height * 0.68f);
            Vector2 reactorReturn = new Vector2(reactor.xMax, reactor.y + reactor.height * 0.72f);

            Vector2 sgSteamOut = new Vector2(sg.xMax, sg.y + sg.height * 0.40f);
            Vector2 turbineIn = new Vector2(turbine.xMin, turbine.y + turbine.height * 0.42f);
            Vector2 turbineOut = new Vector2(turbine.xMax, turbine.y + turbine.height * 0.42f);
            Vector2 generatorIn = new Vector2(generator.xMin, generator.y + generator.height * 0.45f);

            Vector2 condenserIn = new Vector2(condenser.x + condenser.width * 0.50f, condenser.yMin);
            Vector2 condenserOut = new Vector2(condenser.xMin, condenser.y + condenser.height * 0.70f);
            Vector2 heatersIn = new Vector2(feedHeaters.xMax, feedHeaters.y + feedHeaters.height * 0.60f);
            Vector2 heatersOut = new Vector2(feedHeaters.xMin, feedHeaters.y + feedHeaters.height * 0.60f);
            Vector2 pumpsIn = new Vector2(feedPumps.xMax, feedPumps.y + feedPumps.height * 0.60f);
            Vector2 pumpsOut = new Vector2(feedPumps.xMin, feedPumps.y + feedPumps.height * 0.60f);
            Vector2 sgFeedIn = new Vector2(sg.x + sg.width * 0.45f, sg.yMax);

            Color hotPrimary = Color.Lerp(new Color(1f, 0.64f, 0.18f, 1f), new Color(1f, 0.24f, 0.12f, 1f), Mathf.InverseLerp(120f, 620f, reactorTempF));
            Color coldPrimary = Color.Lerp(new Color(0.25f, 0.63f, 1f, 1f), new Color(0.16f, 0.43f, 0.82f, 1f), Mathf.InverseLerp(0f, 2600f, reactorPressurePsia));
            Color steamSecondary = Color.Lerp(new Color(0.95f, 0.70f, 0.18f, 1f), new Color(1f, 0.32f, 0.12f, 1f), Mathf.InverseLerp(0f, 90f, sgHeatTransferMw));
            Color waterSecondary = Color.Lerp(new Color(0.15f, 0.65f, 1f, 1f), new Color(0.10f, 0.47f, 0.84f, 1f), Mathf.InverseLerp(0f, 30f, condenserVacuumInHg));

            DrawPipe(p, reactorHotOut, pzrIn, hotPrimary, 4f);
            DrawPipe(p, pzrOut, sgPriIn, hotPrimary, 4f);
            DrawPipe(p, sgPriOut, reactorReturn, coldPrimary, 4f);

            DrawPipe(p, sgSteamOut, turbineIn, steamSecondary, 4f);
            DrawPipe(p, turbineOut, generatorIn, steamSecondary, 4f);

            DrawPipe(p, turbine.xMin + turbine.width * 0.50f, turbine.yMax, condenserIn.x, condenserIn.y, waterSecondary, 3.5f);
            DrawPipe(p, condenserOut, heatersIn, waterSecondary, 3.5f);
            DrawPipe(p, heatersOut, pumpsIn, waterSecondary, 3.5f);
            DrawPipe(p, pumpsOut, sgFeedIn, waterSecondary, 3.5f);

            bool primaryFlow = rcpCount > 0;
            bool secondaryFlow = sgHeatTransferMw > 0.05f;

            if (primaryFlow)
            {
                DrawFlowArrows(p, reactorHotOut, pzrIn, hotPrimary, 3, _flowPhase);
                DrawFlowArrows(p, pzrOut, sgPriIn, hotPrimary, 3, _flowPhase + 0.17f);
                DrawFlowArrows(p, sgPriOut, reactorReturn, coldPrimary, 3, _flowPhase + 0.35f);
            }

            if (secondaryFlow)
            {
                DrawFlowArrows(p, sgSteamOut, turbineIn, steamSecondary, 3, _flowPhase);
                DrawFlowArrows(p, turbineOut, generatorIn, steamSecondary, 2, _flowPhase + 0.2f);
                DrawFlowArrows(p, condenserOut, heatersIn, waterSecondary, 3, _flowPhase + 0.35f);
                DrawFlowArrows(p, heatersOut, pumpsIn, waterSecondary, 2, _flowPhase + 0.5f);
                DrawFlowArrows(p, pumpsOut, sgFeedIn, waterSecondary, 3, _flowPhase + 0.65f);
            }

            if (rhrActive)
            {
                Vector2 rhrStart = new Vector2(reactor.x + reactor.width * 0.22f, reactor.yMax);
                Vector2 rhrEnd = new Vector2(feedPumps.x + feedPumps.width * 0.15f, feedPumps.yMin);
                DrawPipe(p, rhrStart, rhrEnd, new Color(0.96f, 0.55f, 0.18f, 1f), 3f);
                DrawFlowArrows(p, rhrStart, rhrEnd, new Color(0.96f, 0.55f, 0.18f, 1f), 2, _flowPhase + 0.42f);
            }

            if (sprayActive)
            {
                Vector2 sprayA = new Vector2(pzr.center.x, pzr.yMin);
                Vector2 sprayB = new Vector2(sg.x + sg.width * 0.12f, sg.y + sg.height * 0.18f);
                DrawPipe(p, sprayA, sprayB, new Color(0.4f, 0.8f, 1f, 0.9f), 2.2f);
                DrawFlowArrows(p, sprayA, sprayB, new Color(0.4f, 0.8f, 1f, 0.9f), 2, _flowPhase + 0.1f);
            }

            if (steamDumpActive)
            {
                Vector2 dumpA = new Vector2(sg.x + sg.width * 0.92f, sg.y + sg.height * 0.22f);
                Vector2 dumpB = new Vector2(sg.x + sg.width * 0.98f, sg.y - sg.height * 0.12f);
                DrawPipe(p, dumpA, dumpB, new Color(1f, 0.56f, 0.24f, 0.95f), 2.4f);
                DrawFlowArrows(p, dumpA, dumpB, new Color(1f, 0.56f, 0.24f, 0.95f), 2, _flowPhase + 0.6f);
            }
        }

        private static Rect NR(float w, float h, float nx, float ny, float nw, float nh)
        {
            return new Rect(w * nx, h * ny, w * nw, h * nh);
        }

        private static void DrawNode(Painter2D p, Rect rect, Color fill, Color stroke, float radius)
        {
            p.fillColor = fill;
            p.BeginPath();
            DrawRoundedRect(p, rect, radius);
            p.Fill();

            p.strokeColor = stroke;
            p.lineWidth = 1.6f;
            p.BeginPath();
            DrawRoundedRect(p, rect, radius);
            p.Stroke();
        }

        private static void DrawCapsuleNode(Painter2D p, Rect rect, Color fill, Color stroke)
        {
            float r = rect.width * 0.5f;

            p.fillColor = fill;
            p.BeginPath();
            p.Arc(new Vector2(rect.center.x, rect.yMin + r), r, 180f, 360f);
            p.LineTo(new Vector2(rect.xMax, rect.yMax - r));
            p.Arc(new Vector2(rect.center.x, rect.yMax - r), r, 0f, 180f);
            p.LineTo(new Vector2(rect.xMin, rect.yMin + r));
            p.ClosePath();
            p.Fill();

            p.strokeColor = stroke;
            p.lineWidth = 1.4f;
            p.BeginPath();
            p.Arc(new Vector2(rect.center.x, rect.yMin + r), r, 180f, 360f);
            p.LineTo(new Vector2(rect.xMax, rect.yMax - r));
            p.Arc(new Vector2(rect.center.x, rect.yMax - r), r, 0f, 180f);
            p.LineTo(new Vector2(rect.xMin, rect.yMin + r));
            p.ClosePath();
            p.Stroke();
        }

        private void DrawReactorCore(Painter2D p, Rect reactor)
        {
            float barW = reactor.width * 0.10f;
            float gap = reactor.width * 0.045f;
            float x = reactor.x + reactor.width * 0.20f;
            float baseY = reactor.y + reactor.height * 0.86f;
            float hotNorm = Mathf.InverseLerp(70f, 620f, reactorTempF);
            Color barColor = Color.Lerp(new Color(1f, 0.74f, 0.2f, 1f), new Color(1f, 0.18f, 0.14f, 1f), hotNorm);

            p.fillColor = barColor;
            for (int i = 0; i < 5; i++)
            {
                float h = reactor.height * (0.20f + i * 0.10f);
                p.BeginPath();
                DrawRoundedRect(p, new Rect(x + i * (barW + gap), baseY - h, barW, h), 1.8f);
                p.Fill();
            }
        }

        private void DrawPressurizerLevel(Painter2D p, Rect pzr)
        {
            float r = pzr.width * 0.5f;
            float innerX = pzr.xMin + pzr.width * 0.14f;
            float innerW = pzr.width * 0.72f;
            float innerTop = pzr.yMin + r * 0.35f;
            float innerBottom = pzr.yMax - r * 0.35f;
            float usableH = Mathf.Max(1f, innerBottom - innerTop);
            float level = Mathf.Clamp01(pressurizerLevelPct / 100f);
            float waterY = innerBottom - usableH * level;

            p.fillColor = new Color(0.24f, 0.56f, 0.98f, 0.95f);
            p.BeginPath();
            DrawRoundedRect(p, new Rect(innerX, waterY, innerW, innerBottom - waterY), 2f);
            p.Fill();

            p.fillColor = new Color(0.75f, 0.80f, 0.86f, 0.35f);
            p.BeginPath();
            DrawRoundedRect(p, new Rect(innerX, innerTop, innerW, waterY - innerTop), 2f);
            p.Fill();
        }

        private static void DrawSteamGeneratorInternals(Painter2D p, Rect sg)
        {
            p.strokeColor = new Color(0.78f, 0.85f, 0.94f, 0.55f);
            p.lineWidth = 1.2f;
            for (int i = 0; i < 5; i++)
            {
                float x = sg.x + sg.width * (0.14f + i * 0.16f);
                float y1 = sg.y + sg.height * 0.32f;
                float y2 = sg.y + sg.height * 0.80f;
                p.BeginPath();
                p.MoveTo(new Vector2(x, y2));
                p.LineTo(new Vector2(x, y1));
                p.Stroke();
            }
        }

        private static void DrawTurbineRotor(Painter2D p, Rect turbine)
        {
            p.fillColor = new Color(0.62f, 0.68f, 0.77f, 0.95f);
            p.BeginPath();
            p.MoveTo(new Vector2(turbine.x + turbine.width * 0.08f, turbine.center.y));
            p.LineTo(new Vector2(turbine.x + turbine.width * 0.30f, turbine.y + turbine.height * 0.28f));
            p.LineTo(new Vector2(turbine.x + turbine.width * 0.78f, turbine.y + turbine.height * 0.36f));
            p.LineTo(new Vector2(turbine.x + turbine.width * 0.78f, turbine.y + turbine.height * 0.64f));
            p.LineTo(new Vector2(turbine.x + turbine.width * 0.30f, turbine.y + turbine.height * 0.72f));
            p.ClosePath();
            p.Fill();
        }

        private static void DrawGeneratorRotor(Painter2D p, Rect generator)
        {
            p.fillColor = new Color(0.56f, 0.64f, 0.75f, 0.95f);
            p.BeginPath();
            DrawRoundedRect(p, new Rect(generator.x + generator.width * 0.20f, generator.y + generator.height * 0.26f, generator.width * 0.62f, generator.height * 0.48f), 5f);
            p.Fill();
        }

        private void DrawCondenserCoils(Painter2D p, Rect condenser)
        {
            float vacNorm = Mathf.InverseLerp(0f, 30f, condenserVacuumInHg);
            Color coilColor = Color.Lerp(new Color(0.22f, 0.72f, 1f, 0.85f), new Color(0.12f, 0.94f, 0.86f, 0.95f), vacNorm);
            p.strokeColor = coilColor;
            p.lineWidth = 2.2f;

            for (int i = 0; i < 3; i++)
            {
                float y = condenser.y + condenser.height * (0.30f + i * 0.22f);
                p.BeginPath();
                p.MoveTo(new Vector2(condenser.x + condenser.width * 0.12f, y));
                p.LineTo(new Vector2(condenser.x + condenser.width * 0.88f, y));
                p.Stroke();
            }
        }

        private static void DrawFeedPumps(Painter2D p, Rect feedPumps)
        {
            p.fillColor = new Color(0.60f, 0.68f, 0.77f, 0.95f);
            float r = feedPumps.height * 0.16f;
            float y = feedPumps.center.y;
            float x1 = feedPumps.x + feedPumps.width * 0.34f;
            float x2 = feedPumps.x + feedPumps.width * 0.66f;

            p.BeginPath();
            p.Arc(new Vector2(x1, y), r, 0f, 360f);
            p.Fill();
            p.BeginPath();
            p.Arc(new Vector2(x2, y), r, 0f, 360f);
            p.Fill();
        }

        private static void DrawHeaterBanks(Painter2D p, Rect heaters)
        {
            p.fillColor = new Color(0.62f, 0.68f, 0.77f, 0.9f);
            float y = heaters.y + heaters.height * 0.56f;
            float h = heaters.height * 0.24f;
            float w = heaters.width * 0.30f;
            float x1 = heaters.x + heaters.width * 0.16f;
            float x2 = heaters.x + heaters.width * 0.56f;

            p.BeginPath();
            DrawRoundedRect(p, new Rect(x1, y - h * 0.5f, w, h), 4f);
            p.Fill();
            p.BeginPath();
            DrawRoundedRect(p, new Rect(x2, y - h * 0.5f, w, h), 4f);
            p.Fill();
        }

        private static void DrawPipe(Painter2D p, Vector2 a, Vector2 b, Color color, float width)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.lineCap = LineCap.Round;
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(b);
            p.Stroke();
        }

        private static void DrawPipe(Painter2D p, float ax, float ay, float bx, float by, Color color, float width)
        {
            DrawPipe(p, new Vector2(ax, ay), new Vector2(bx, by), color, width);
        }

        private static void DrawFlowArrows(Painter2D p, Vector2 a, Vector2 b, Color color, int count, float phase)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float size = 4f;

            p.strokeColor = color;
            p.lineWidth = 1.7f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Repeat(phase + i / (float)count, 1f);
                Vector2 pos = Vector2.Lerp(a, b, t);
                Vector2 tip = pos + dir * size;
                Vector2 left = pos - dir * size * 0.5f + perp * size * 0.52f;
                Vector2 right = pos - dir * size * 0.5f - perp * size * 0.52f;

                p.BeginPath();
                p.MoveTo(left);
                p.LineTo(tip);
                p.LineTo(right);
                p.Stroke();
            }
        }

        private static void DrawRoundedRect(Painter2D p, Rect rect, float radius)
        {
            float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.45f);
            p.MoveTo(new Vector2(rect.xMin + r, rect.yMin));
            p.LineTo(new Vector2(rect.xMax - r, rect.yMin));
            p.ArcTo(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMin + r), r);
            p.LineTo(new Vector2(rect.xMax, rect.yMax - r));
            p.ArcTo(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax - r, rect.yMax), r);
            p.LineTo(new Vector2(rect.xMin + r, rect.yMax));
            p.ArcTo(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMax - r), r);
            p.LineTo(new Vector2(rect.xMin, rect.yMin + r));
            p.ArcTo(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin + r, rect.yMin), r);
            p.ClosePath();
        }
    }
}
