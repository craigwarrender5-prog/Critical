// ============================================================================
// CRITICAL: Master the Atom — UI Toolkit Proof of Concept
// RCSLoopDiagramPOC.cs — 4-Loop RCS Schematic Visualization
// ============================================================================
//
// PURPOSE:
//   Renders a stylized 4-loop PWR primary system schematic showing:
//   - Central reactor vessel
//   - 4 hot legs (red/orange based on temperature)
//   - 4 steam generators (simplified U-tube representation)
//   - 4 RCPs with status indication
//   - 4 cold legs (blue based on temperature)
//   - Pressurizer on Loop 2 hot leg
//   - RHR connection indication
//   - Animated flow chevrons when RCPs running
//
// ============================================================================

using UnityEngine;
using UnityEngine.UIElements;

namespace Critical.UI.POC
{
    public class RCSLoopDiagramPOC : VisualElement
    {
        // ====================================================================
        // PUBLIC PROPERTIES
        // ====================================================================
        
        public float T_hot = 550f;
        public float T_cold = 530f;
        public float T_avg = 540f;
        public float pressure = 2235f;
        public int rcpCount = 0;
        public bool rhrActive = false;
        
        private bool[] _rcpRunning = new bool[4];
        private float[] _sgTemps = new float[4] { 400f, 400f, 400f, 400f };
        
        // Animation
        private float _flowPhase = 0f;
        
        // ====================================================================
        // COLORS
        // ====================================================================
        
        private static readonly Color COLOR_VESSEL = new Color(0.3f, 0.3f, 0.35f, 1f);
        private static readonly Color COLOR_VESSEL_OUTLINE = new Color(0.5f, 0.5f, 0.55f, 1f);
        private static readonly Color COLOR_HOT_LEG = new Color(1f, 0.4f, 0.2f, 1f);
        private static readonly Color COLOR_COLD_LEG = new Color(0.2f, 0.5f, 0.9f, 1f);
        private static readonly Color COLOR_SG = new Color(0.25f, 0.25f, 0.3f, 1f);
        private static readonly Color COLOR_SG_TUBES = new Color(0.6f, 0.6f, 0.65f, 0.5f);
        private static readonly Color COLOR_RCP_OFF = new Color(0.2f, 0.2f, 0.25f, 1f);
        private static readonly Color COLOR_RCP_ON = new Color(0f, 0.8f, 0.4f, 1f);
        private static readonly Color COLOR_PZR_WATER = new Color(0.2f, 0.4f, 0.7f, 1f);
        private static readonly Color COLOR_PZR_STEAM = new Color(0.5f, 0.5f, 0.55f, 1f);
        private static readonly Color COLOR_RHR = new Color(0.8f, 0.5f, 0.2f, 1f);
        private static readonly Color COLOR_FLOW_CHEVRON = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color COLOR_TEXT = new Color(0.7f, 0.7f, 0.75f, 1f);
        private static readonly Color COLOR_ACCENT = new Color(0f, 1f, 0.533f, 1f);
        
        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================
        
        public RCSLoopDiagramPOC()
        {
            generateVisualContent += OnGenerateVisualContent;
        }
        
        // ====================================================================
        // PUBLIC METHODS
        // ====================================================================
        
        public void SetRCPRunning(int index, bool running)
        {
            if (index >= 0 && index < 4)
            {
                _rcpRunning[index] = running;
            }
        }
        
        public void SetSGTemp(int index, float temp)
        {
            if (index >= 0 && index < 4)
            {
                _sgTemps[index] = temp;
            }
        }
        
        public void UpdateAnimation(float deltaTime)
        {
            if (rcpCount > 0)
            {
                _flowPhase += deltaTime * 2f;
                if (_flowPhase > 1f) _flowPhase -= 1f;
                MarkDirtyRepaint();
            }
        }
        
        // ====================================================================
        // RENDERING
        // ====================================================================
        
        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            float w = contentRect.width;
            float h = contentRect.height;
            
            if (w < 10 || h < 10) return;
            
            float cx = w / 2f;
            float cy = h / 2f;
            float scale = Mathf.Min(w, h) / 400f;
            
            // Draw components in order (back to front)
            DrawRHRConnections(painter, cx, cy, scale);
            DrawLoopPiping(painter, cx, cy, scale);
            DrawSteamGenerators(painter, cx, cy, scale);
            DrawRCPs(painter, cx, cy, scale);
            DrawReactorVessel(painter, cx, cy, scale);
            DrawPressurizer(painter, cx, cy, scale);
            
            if (rcpCount > 0)
            {
                DrawFlowChevrons(painter, cx, cy, scale);
            }
            
            DrawLabels(painter, cx, cy, scale);
        }
        
        private void DrawReactorVessel(Painter2D p, float cx, float cy, float scale)
        {
            float vw = 60 * scale;
            float vh = 80 * scale;
            
            // Vessel body
            p.fillColor = COLOR_VESSEL;
            p.BeginPath();
            
            // Rounded rectangle vessel
            float r = 15 * scale;
            p.MoveTo(new Vector2(cx - vw/2 + r, cy - vh/2));
            p.LineTo(new Vector2(cx + vw/2 - r, cy - vh/2));
            p.ArcTo(new Vector2(cx + vw/2, cy - vh/2), new Vector2(cx + vw/2, cy - vh/2 + r), r);
            p.LineTo(new Vector2(cx + vw/2, cy + vh/2 - r));
            p.ArcTo(new Vector2(cx + vw/2, cy + vh/2), new Vector2(cx + vw/2 - r, cy + vh/2), r);
            p.LineTo(new Vector2(cx - vw/2 + r, cy + vh/2));
            p.ArcTo(new Vector2(cx - vw/2, cy + vh/2), new Vector2(cx - vw/2, cy + vh/2 - r), r);
            p.LineTo(new Vector2(cx - vw/2, cy - vh/2 + r));
            p.ArcTo(new Vector2(cx - vw/2, cy - vh/2), new Vector2(cx - vw/2 + r, cy - vh/2), r);
            p.ClosePath();
            p.Fill();
            
            // Vessel outline
            p.strokeColor = COLOR_VESSEL_OUTLINE;
            p.lineWidth = 2 * scale;
            p.Stroke();
            
            // Vessel head (dome)
            p.fillColor = COLOR_VESSEL;
            p.BeginPath();
            p.Arc(new Vector2(cx, cy - vh/2), vw/2 - 5*scale, 180f, 360f);
            p.ClosePath();
            p.Fill();
            p.strokeColor = COLOR_VESSEL_OUTLINE;
            p.Stroke();
            
            // Core indication (inner rectangle)
            p.fillColor = new Color(0.15f, 0.15f, 0.2f, 1f);
            float coreW = 30 * scale;
            float coreH = 40 * scale;
            p.BeginPath();
            p.MoveTo(new Vector2(cx - coreW/2, cy - coreH/2));
            p.LineTo(new Vector2(cx + coreW/2, cy - coreH/2));
            p.LineTo(new Vector2(cx + coreW/2, cy + coreH/2));
            p.LineTo(new Vector2(cx - coreW/2, cy + coreH/2));
            p.ClosePath();
            p.Fill();
        }
        
        private void DrawLoopPiping(Painter2D p, float cx, float cy, float scale)
        {
            float vw = 60 * scale;
            float vh = 80 * scale;
            float loopRadius = 120 * scale;
            float pipeWidth = 8 * scale;
            
            // Loop positions (0=top-right, 1=bottom-right, 2=bottom-left, 3=top-left)
            float[] angles = { 45f, 135f, 225f, 315f };
            
            for (int i = 0; i < 4; i++)
            {
                float angle = angles[i] * Mathf.Deg2Rad;
                float sgX = cx + Mathf.Cos(angle) * loopRadius;
                float sgY = cy + Mathf.Sin(angle) * loopRadius;
                
                // Hot leg (from vessel to SG)
                Color hotColor = GetTemperatureColor(T_hot, true);
                p.strokeColor = hotColor;
                p.lineWidth = pipeWidth;
                p.lineCap = LineCap.Round;
                
                // Hot leg path
                float hotStartX = cx + Mathf.Cos(angle) * (vw/2 + 5*scale);
                float hotStartY = cy + Mathf.Sin(angle) * (vh/2 - 10*scale);
                float hotEndX = sgX - Mathf.Cos(angle) * 25 * scale;
                float hotEndY = sgY - Mathf.Sin(angle) * 35 * scale;
                
                p.BeginPath();
                p.MoveTo(new Vector2(hotStartX, hotStartY));
                p.LineTo(new Vector2(hotEndX, hotEndY));
                p.Stroke();
                
                // Cold leg (from RCP to vessel)
                Color coldColor = GetTemperatureColor(T_cold, false);
                p.strokeColor = coldColor;
                
                float coldStartX = sgX + Mathf.Cos(angle + Mathf.PI/6) * 30 * scale;
                float coldStartY = sgY + Mathf.Sin(angle + Mathf.PI/6) * 40 * scale;
                float coldEndX = cx + Mathf.Cos(angle + Mathf.PI/8) * (vw/2 + 5*scale);
                float coldEndY = cy + Mathf.Sin(angle + Mathf.PI/8) * (vh/2);
                
                p.BeginPath();
                p.MoveTo(new Vector2(coldStartX, coldStartY));
                p.LineTo(new Vector2(coldEndX, coldEndY));
                p.Stroke();
            }
        }
        
        private void DrawSteamGenerators(Painter2D p, float cx, float cy, float scale)
        {
            float loopRadius = 120 * scale;
            float sgW = 35 * scale;
            float sgH = 50 * scale;
            
            float[] angles = { 45f, 135f, 225f, 315f };
            
            for (int i = 0; i < 4; i++)
            {
                float angle = angles[i] * Mathf.Deg2Rad;
                float sgX = cx + Mathf.Cos(angle) * loopRadius;
                float sgY = cy + Mathf.Sin(angle) * loopRadius;
                
                // SG body
                p.fillColor = COLOR_SG;
                p.BeginPath();
                
                // Rounded rectangle
                float r = 8 * scale;
                p.MoveTo(new Vector2(sgX - sgW/2 + r, sgY - sgH/2));
                p.LineTo(new Vector2(sgX + sgW/2 - r, sgY - sgH/2));
                p.ArcTo(new Vector2(sgX + sgW/2, sgY - sgH/2), new Vector2(sgX + sgW/2, sgY - sgH/2 + r), r);
                p.LineTo(new Vector2(sgX + sgW/2, sgY + sgH/2 - r));
                p.ArcTo(new Vector2(sgX + sgW/2, sgY + sgH/2), new Vector2(sgX + sgW/2 - r, sgY + sgH/2), r);
                p.LineTo(new Vector2(sgX - sgW/2 + r, sgY + sgH/2));
                p.ArcTo(new Vector2(sgX - sgW/2, sgY + sgH/2), new Vector2(sgX - sgW/2, sgY + sgH/2 - r), r);
                p.LineTo(new Vector2(sgX - sgW/2, sgY - sgH/2 + r));
                p.ArcTo(new Vector2(sgX - sgW/2, sgY - sgH/2), new Vector2(sgX - sgW/2 + r, sgY - sgH/2), r);
                p.ClosePath();
                p.Fill();
                
                // SG outline
                p.strokeColor = COLOR_VESSEL_OUTLINE;
                p.lineWidth = 1.5f * scale;
                p.Stroke();
                
                // U-tubes indication
                p.strokeColor = COLOR_SG_TUBES;
                p.lineWidth = 1 * scale;
                for (int t = 0; t < 3; t++)
                {
                    float tubeX = sgX - 8*scale + t * 8*scale;
                    p.BeginPath();
                    p.MoveTo(new Vector2(tubeX, sgY + sgH/2 - 5*scale));
                    p.LineTo(new Vector2(tubeX, sgY - sgH/4));
                    p.Arc(new Vector2(tubeX + 4*scale, sgY - sgH/4), 4*scale, 180f, 0f);
                    p.LineTo(new Vector2(tubeX + 8*scale, sgY + sgH/2 - 5*scale));
                    p.Stroke();
                }
            }
        }
        
        private void DrawRCPs(Painter2D p, float cx, float cy, float scale)
        {
            float loopRadius = 120 * scale;
            float rcpRadius = 12 * scale;
            
            float[] angles = { 45f, 135f, 225f, 315f };
            
            for (int i = 0; i < 4; i++)
            {
                float angle = angles[i] * Mathf.Deg2Rad;
                float baseX = cx + Mathf.Cos(angle) * loopRadius;
                float baseY = cy + Mathf.Sin(angle) * loopRadius;
                
                // RCP positioned below/beside SG
                float rcpX = baseX + Mathf.Cos(angle + Mathf.PI/4) * 35 * scale;
                float rcpY = baseY + Mathf.Sin(angle + Mathf.PI/4) * 35 * scale;
                
                // RCP body (circle)
                p.fillColor = _rcpRunning[i] ? COLOR_RCP_ON : COLOR_RCP_OFF;
                p.BeginPath();
                p.Arc(new Vector2(rcpX, rcpY), rcpRadius, 0f, 360f);
                p.ClosePath();
                p.Fill();
                
                // RCP outline
                p.strokeColor = COLOR_VESSEL_OUTLINE;
                p.lineWidth = 1.5f * scale;
                p.Stroke();
                
                // Impeller indication (if running)
                if (_rcpRunning[i])
                {
                    p.strokeColor = new Color(1f, 1f, 1f, 0.6f);
                    p.lineWidth = 2 * scale;
                    float impellerAngle = _flowPhase * 360f * Mathf.Deg2Rad;
                    for (int blade = 0; blade < 4; blade++)
                    {
                        float bladeAngle = impellerAngle + blade * Mathf.PI / 2;
                        p.BeginPath();
                        p.MoveTo(new Vector2(rcpX, rcpY));
                        p.LineTo(new Vector2(
                            rcpX + Mathf.Cos(bladeAngle) * rcpRadius * 0.7f,
                            rcpY + Mathf.Sin(bladeAngle) * rcpRadius * 0.7f));
                        p.Stroke();
                    }
                }
            }
        }
        
        private void DrawPressurizer(Painter2D p, float cx, float cy, float scale)
        {
            // Pressurizer on Loop 2 hot leg (bottom-right)
            float angle = 135f * Mathf.Deg2Rad;
            float loopRadius = 120 * scale;
            float hotLegX = cx + Mathf.Cos(angle) * (loopRadius * 0.5f);
            float hotLegY = cy + Mathf.Sin(angle) * (loopRadius * 0.5f);
            
            // Offset pressurizer to side
            float pzrX = hotLegX + 30 * scale;
            float pzrY = hotLegY;
            float pzrW = 18 * scale;
            float pzrH = 40 * scale;
            
            // Surge line
            p.strokeColor = COLOR_HOT_LEG;
            p.lineWidth = 4 * scale;
            p.BeginPath();
            p.MoveTo(new Vector2(hotLegX, hotLegY));
            p.LineTo(new Vector2(pzrX - pzrW/2, pzrY + pzrH/2 - 5*scale));
            p.Stroke();
            
            // PZR body
            p.fillColor = COLOR_VESSEL;
            p.BeginPath();
            
            // Rounded rectangle
            float r = 5 * scale;
            p.MoveTo(new Vector2(pzrX - pzrW/2 + r, pzrY - pzrH/2));
            p.LineTo(new Vector2(pzrX + pzrW/2 - r, pzrY - pzrH/2));
            p.ArcTo(new Vector2(pzrX + pzrW/2, pzrY - pzrH/2), new Vector2(pzrX + pzrW/2, pzrY - pzrH/2 + r), r);
            p.LineTo(new Vector2(pzrX + pzrW/2, pzrY + pzrH/2 - r));
            p.ArcTo(new Vector2(pzrX + pzrW/2, pzrY + pzrH/2), new Vector2(pzrX + pzrW/2 - r, pzrY + pzrH/2), r);
            p.LineTo(new Vector2(pzrX - pzrW/2 + r, pzrY + pzrH/2));
            p.ArcTo(new Vector2(pzrX - pzrW/2, pzrY + pzrH/2), new Vector2(pzrX - pzrW/2, pzrY + pzrH/2 - r), r);
            p.LineTo(new Vector2(pzrX - pzrW/2, pzrY - pzrH/2 + r));
            p.ArcTo(new Vector2(pzrX - pzrW/2, pzrY - pzrH/2), new Vector2(pzrX - pzrW/2 + r, pzrY - pzrH/2), r);
            p.ClosePath();
            p.Fill();
            
            // Water level (60% full)
            float waterLevel = 0.6f;
            float waterTop = pzrY + pzrH/2 - pzrH * waterLevel;
            
            p.fillColor = COLOR_PZR_WATER;
            p.BeginPath();
            p.MoveTo(new Vector2(pzrX - pzrW/2 + 2*scale, waterTop));
            p.LineTo(new Vector2(pzrX + pzrW/2 - 2*scale, waterTop));
            p.LineTo(new Vector2(pzrX + pzrW/2 - 2*scale, pzrY + pzrH/2 - 3*scale));
            p.LineTo(new Vector2(pzrX - pzrW/2 + 2*scale, pzrY + pzrH/2 - 3*scale));
            p.ClosePath();
            p.Fill();
            
            // Steam space
            p.fillColor = COLOR_PZR_STEAM;
            p.BeginPath();
            p.MoveTo(new Vector2(pzrX - pzrW/2 + 2*scale, pzrY - pzrH/2 + 3*scale));
            p.LineTo(new Vector2(pzrX + pzrW/2 - 2*scale, pzrY - pzrH/2 + 3*scale));
            p.LineTo(new Vector2(pzrX + pzrW/2 - 2*scale, waterTop));
            p.LineTo(new Vector2(pzrX - pzrW/2 + 2*scale, waterTop));
            p.ClosePath();
            p.Fill();
            
            // PZR outline
            p.strokeColor = COLOR_VESSEL_OUTLINE;
            p.lineWidth = 1.5f * scale;
            p.BeginPath();
            p.MoveTo(new Vector2(pzrX - pzrW/2 + r, pzrY - pzrH/2));
            p.LineTo(new Vector2(pzrX + pzrW/2 - r, pzrY - pzrH/2));
            p.ArcTo(new Vector2(pzrX + pzrW/2, pzrY - pzrH/2), new Vector2(pzrX + pzrW/2, pzrY - pzrH/2 + r), r);
            p.LineTo(new Vector2(pzrX + pzrW/2, pzrY + pzrH/2 - r));
            p.ArcTo(new Vector2(pzrX + pzrW/2, pzrY + pzrH/2), new Vector2(pzrX + pzrW/2 - r, pzrY + pzrH/2), r);
            p.LineTo(new Vector2(pzrX - pzrW/2 + r, pzrY + pzrH/2));
            p.ArcTo(new Vector2(pzrX - pzrW/2, pzrY + pzrH/2), new Vector2(pzrX - pzrW/2, pzrY + pzrH/2 - r), r);
            p.LineTo(new Vector2(pzrX - pzrW/2, pzrY - pzrH/2 + r));
            p.ArcTo(new Vector2(pzrX - pzrW/2, pzrY - pzrH/2), new Vector2(pzrX - pzrW/2 + r, pzrY - pzrH/2), r);
            p.ClosePath();
            p.Stroke();
        }
        
        private void DrawRHRConnections(Painter2D p, float cx, float cy, float scale)
        {
            if (!rhrActive) return;
            
            // RHR connects to Loop 4 (top-left area)
            float angle = 315f * Mathf.Deg2Rad;
            float loopRadius = 120 * scale;
            float rhrX = cx + Mathf.Cos(angle) * (loopRadius * 0.6f) - 40*scale;
            float rhrY = cy + Mathf.Sin(angle) * (loopRadius * 0.6f);
            
            // RHR piping
            p.strokeColor = COLOR_RHR;
            p.lineWidth = 6 * scale;
            p.lineCap = LineCap.Round;
            
            // Simplified RHR indication
            p.BeginPath();
            p.MoveTo(new Vector2(rhrX, rhrY));
            p.LineTo(new Vector2(rhrX - 25*scale, rhrY));
            p.LineTo(new Vector2(rhrX - 25*scale, rhrY + 30*scale));
            p.Stroke();
            
            // RHR pump symbol
            p.fillColor = COLOR_RHR;
            p.BeginPath();
            p.Arc(new Vector2(rhrX - 25*scale, rhrY + 40*scale), 8*scale, 0f, 360f);
            p.Fill();
            p.strokeColor = COLOR_VESSEL_OUTLINE;
            p.lineWidth = 1.5f * scale;
            p.Stroke();
        }
        
        private void DrawFlowChevrons(Painter2D p, float cx, float cy, float scale)
        {
            float loopRadius = 120 * scale;
            float[] angles = { 45f, 135f, 225f, 315f };
            
            p.strokeColor = COLOR_FLOW_CHEVRON;
            p.lineWidth = 2 * scale;
            p.lineCap = LineCap.Round;
            
            for (int i = 0; i < 4; i++)
            {
                if (!_rcpRunning[i]) continue;
                
                float angle = angles[i] * Mathf.Deg2Rad;
                float chevSize = 5 * scale;
                
                // Hot leg flow chevrons (OUTWARD from vessel toward SG)
                // Chevrons point in direction of flow (away from reactor)
                for (int c = 0; c < 3; c++)
                {
                    float t = ((_flowPhase + c * 0.33f) % 1f);
                    float chevX = cx + Mathf.Cos(angle) * (40 + t * 60) * scale;
                    float chevY = cy + Mathf.Sin(angle) * (40 + t * 60) * scale;
                    
                    float perpAngle = angle + Mathf.PI / 2;
                    
                    // Chevron points OUTWARD (in direction of angle, away from center)
                    p.BeginPath();
                    p.MoveTo(new Vector2(
                        chevX + Mathf.Cos(perpAngle) * chevSize - Mathf.Cos(angle) * chevSize,
                        chevY + Mathf.Sin(perpAngle) * chevSize - Mathf.Sin(angle) * chevSize));
                    p.LineTo(new Vector2(chevX, chevY));
                    p.LineTo(new Vector2(
                        chevX - Mathf.Cos(perpAngle) * chevSize - Mathf.Cos(angle) * chevSize,
                        chevY - Mathf.Sin(perpAngle) * chevSize - Mathf.Sin(angle) * chevSize));
                    p.Stroke();
                }
                
                // Cold leg flow chevrons (INWARD from RCP toward vessel)
                // Chevrons point in direction of flow (toward reactor)
                float coldAngle = angle + Mathf.PI / 8;  // Offset for cold leg position
                for (int c = 0; c < 3; c++)
                {
                    // Flow moves from outer (RCP) toward center (reactor)
                    // So we animate from far to near, chevrons point inward
                    float t = ((_flowPhase + c * 0.33f) % 1f);
                    float dist = (100 - t * 60) * scale;  // Start far, move toward center
                    float chevX = cx + Mathf.Cos(coldAngle) * dist;
                    float chevY = cy + Mathf.Sin(coldAngle) * dist;
                    
                    float perpAngle = coldAngle + Mathf.PI / 2;
                    
                    // Chevron points INWARD (opposite of coldAngle, toward center)
                    // Note the + instead of - on the chevron arms to flip direction
                    p.BeginPath();
                    p.MoveTo(new Vector2(
                        chevX + Mathf.Cos(perpAngle) * chevSize + Mathf.Cos(coldAngle) * chevSize,
                        chevY + Mathf.Sin(perpAngle) * chevSize + Mathf.Sin(coldAngle) * chevSize));
                    p.LineTo(new Vector2(chevX, chevY));
                    p.LineTo(new Vector2(
                        chevX - Mathf.Cos(perpAngle) * chevSize + Mathf.Cos(coldAngle) * chevSize,
                        chevY - Mathf.Sin(perpAngle) * chevSize + Mathf.Sin(coldAngle) * chevSize));
                    p.Stroke();
                }
            }
        }
        
        private void DrawLabels(Painter2D p, float cx, float cy, float scale)
        {
            // Labels are drawn using child Label elements, not Painter2D
            // This method is a placeholder for future text rendering if needed
        }
        
        private Color GetTemperatureColor(float temp, bool isHot)
        {
            // Map temperature to color intensity
            float t = Mathf.Clamp01((temp - 100f) / 500f);
            
            if (isHot)
            {
                // Red/orange gradient for hot leg
                return new Color(
                    0.8f + t * 0.2f,
                    0.3f + t * 0.2f,
                    0.1f + t * 0.1f,
                    1f);
            }
            else
            {
                // Blue gradient for cold leg
                return new Color(
                    0.1f + t * 0.2f,
                    0.3f + t * 0.3f,
                    0.7f + t * 0.3f,
                    1f);
            }
        }
    }
}
