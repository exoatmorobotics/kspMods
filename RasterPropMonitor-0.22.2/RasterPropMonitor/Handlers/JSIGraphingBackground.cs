﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace JSI
{
    // JSIGraphingBackground provides an editable / configurable way to render
    // one or more data in a graphical manner.
    class JSIGraphingBackground : InternalModule
    {
        [KSPField]
        public string layout = null;

        private Color32 backgroundColorValue;
        private List<DataSet> dataSets = new List<DataSet>();
        private PersistenceAccessor persistence;
        private bool startupComplete = false;
        private Material lineMaterial = JUtil.DrawLineMaterial();
        private Material graphMaterial;


        public bool RenderBackground(RenderTexture screen, float cameraAspect)
        {
            if (!enabled)
            {
                return false;
            }

            GL.Clear(true, true, backgroundColorValue);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0.0f, screen.width, screen.height, 0.0f);
            GL.Viewport(new Rect(0, 0, screen.width, screen.height));
            lineMaterial.SetPass(0);

            try
            {
                RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
                // Render background - eventually, squirrel this away onto a render tex
                for (int i = 0; i < dataSets.Count; ++i)
                {
                    dataSets[i].RenderBackground(screen);
                }

                // Render data
                for (int i = 0; i < dataSets.Count; ++i)
                {
                    dataSets[i].RenderData(screen, comp, persistence);
                }
            }
            catch
            {
                // Something went wrong ....
                GL.Color(Color.white);
                GL.PopMatrix();
                GL.Viewport(new Rect(0, 0, screen.width, screen.height));

                return false;
            }

            GL.Color(Color.white);
            GL.PopMatrix();
            GL.Viewport(new Rect(0, 0, screen.width, screen.height));

            return true;
        }

        public void LateUpdate()
        {
            if (vessel != null && JUtil.VesselIsInIVA(vessel) && !startupComplete)
            {
                JUtil.AnnoyUser(this);
                enabled = false;
            }
        }

        public void Start()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                return;
            }
            try
            {
                if (string.IsNullOrEmpty(layout))
                {
                    throw new ArgumentNullException("layout");
                }

                foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("RPM_GRAPHING_BACKGROUND"))
                {
                    if (node.GetValue("layout") == layout)
                    {
                        if (!node.HasValue("backgroundColor"))
                        {
                            JUtil.LogErrorMessage(this, "?!? no backgroundColor");
                        }
                        string s = node.GetValue("backgroundColor");
                        if (string.IsNullOrEmpty(s))
                        {
                            JUtil.LogErrorMessage(this, "backgroundColor is missing?");
                        }
                        backgroundColorValue = ConfigNode.ParseColor32(node.GetValue("backgroundColor"));

                        ConfigNode[] dataNodes = node.GetNodes("DATA_SET");

                        for (int i = 0; i < dataNodes.Length; i++)
                        {
                            try
                            {
                                dataSets.Add(new DataSet(dataNodes[i]));
                            }
                            catch (ArgumentException e)
                            {
                                JUtil.LogErrorMessage(this, "Error in building prop number {1} - {0}", e.Message, internalProp.propID);
                                throw;
                            }
                        }
                        break;
                    }
                }

                graphMaterial = new Material(Shader.Find("KSP/Alpha/Unlit Transparent"));
                persistence = new PersistenceAccessor(internalProp);
                startupComplete = true;
            }

            catch
            {
                JUtil.AnnoyUser(this);
                throw;
            }
        }

        public void OnDestroy()
        {
            //JUtil.LogMessage(this, "OnDestroy()");
            persistence = null;
        }
    }

    class DataSet
    {
        //--- Static data
        private readonly Vector2 position;
        private readonly Vector2 size;
        private readonly Color32 color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        private readonly int lineWidth = 0;

        private readonly Vector2 fillTopLeftCorner;
        private readonly Vector2 fillSize;

        //--- Graphing data
        private readonly GraphType graphType;
        private readonly string variableName;
        private readonly Color32 passiveColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        private readonly Color32 activeColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        private readonly VariableOrNumber[] scale = new VariableOrNumber[2];
        private readonly Vector2 threshold;
        private double lastStateChange = 0.0;
        private float lastState = 0.0f;
        private readonly float flashingDelay;
        private readonly bool thresholdMode;
        private readonly bool reverse;

        private enum GraphType
        {
            VerticalUp,
            VerticalDown,
            VerticalSplit,
            HorizontalRight,
            HorizontalLeft,
            HorizontalSplit,
            Lamp,
        };

        public DataSet(ConfigNode node)
        {
            Vector4 packedPosition = ConfigNode.ParseVector4(node.GetValue("borderPosition"));
            position.x = packedPosition.x;
            position.y = packedPosition.y;
            size.x = packedPosition.z;
            size.y = packedPosition.w;

            if (node.HasValue("borderColor"))
            {
                color = ConfigNode.ParseColor32(node.GetValue("borderColor"));
            }

            if (node.HasValue("borderWidth"))
            {
                lineWidth = int.Parse(node.GetValue("borderWidth"));
            }

            string graphTypeStr = node.GetValue("graphType").Trim();
            if (graphTypeStr == GraphType.VerticalUp.ToString())
            {
                graphType = GraphType.VerticalUp;
            }
            else if (graphTypeStr == GraphType.VerticalDown.ToString())
            {
                graphType = GraphType.VerticalDown;
            }
            else if (graphTypeStr == GraphType.VerticalSplit.ToString())
            {
                graphType = GraphType.VerticalSplit;
            }
            else if (graphTypeStr == GraphType.HorizontalRight.ToString())
            {
                graphType = GraphType.HorizontalRight;
            }
            else if (graphTypeStr == GraphType.HorizontalLeft.ToString())
            {
                graphType = GraphType.HorizontalLeft;
            }
            else if (graphTypeStr == GraphType.HorizontalSplit.ToString())
            {
                graphType = GraphType.HorizontalSplit;
            }
            else if (graphTypeStr == GraphType.Lamp.ToString())
            {
                graphType = GraphType.Lamp;
            }
            else
            {
                throw new ArgumentException("Unknown 'graphType' in DATA_SET");
            }

            if (node.HasValue("passiveColor"))
            {
                passiveColor = ConfigNode.ParseColor32(node.GetValue("passiveColor"));
            }
            if (node.HasValue("activeColor"))
            {
                activeColor = ConfigNode.ParseColor32(node.GetValue("activeColor"));
            }
            string[] token = node.GetValue("scale").Split(',');
            scale[0] = new VariableOrNumber(token[0].Trim(), this);
            scale[1] = new VariableOrNumber(token[1].Trim(), this);
            variableName = node.GetValue("variableName").Trim();

            if (node.HasValue("reverse"))
            {
                if (!bool.TryParse(node.GetValue("reverse"), out reverse))
                {
                    throw new ArgumentException("So is 'reverse' true or false?");
                }
            }

            if (node.HasValue("threshold"))
            {
                threshold = ConfigNode.ParseVector2(node.GetValue("threshold"));
            }
            if (threshold != Vector2.zero)
            {
                thresholdMode = true;

                float min = Mathf.Min(threshold.x, threshold.y);
                float max = Mathf.Max(threshold.x, threshold.y);
                threshold.x = min;
                threshold.y = max;

                if (node.HasValue("flashingDelay"))
                {
                    flashingDelay = float.Parse(node.GetValue("flashingDelay"));
                    flashingDelay = Mathf.Max(flashingDelay, 0.0f);
                }
            }

            fillTopLeftCorner = position + new Vector2((float)lineWidth, (float)lineWidth);
            fillSize = (size - new Vector2((float)(2 * lineWidth), (float)(2 * lineWidth)));
        }

        public void RenderBackground(RenderTexture screen)
        {
            if (lineWidth > 0)
            {
                DrawBorder(screen);
            }
        }

        public void RenderData(RenderTexture screen, RPMVesselComputer comp, PersistenceAccessor persistence)
        {
            float leftVal, rightVal;
            if (!scale[0].Get(out leftVal, comp, persistence) || !scale[1].Get(out rightVal, comp, persistence))
            {
                return; // bad values - can't render
            }

            float eval = comp.ProcessVariable(variableName, persistence).MassageToFloat();
            if (float.IsInfinity(eval) || float.IsNaN(eval))
            {
                return; // bad value - can't render
            }

            float ratio = Mathf.InverseLerp(leftVal, rightVal, eval);

            if (thresholdMode)
            {
                if (ratio >= threshold.x && ratio <= threshold.y)
                {
                    if (flashingDelay > 0.0f)
                    {
                        if (lastStateChange + flashingDelay < Planetarium.GetUniversalTime())
                        {
                            lastStateChange = Planetarium.GetUniversalTime();
                            lastState = 1.0f - lastState;
                        }

                        ratio = lastState;
                    }
                    else
                    {
                        ratio = 1.0f;
                    }
                }
                else
                {
                    ratio = 0.0f;
                }
            }

            if (reverse)
            {
                ratio = 1.0f - ratio;
            }

            switch (graphType)
            {
                case GraphType.VerticalDown:
                    DrawVerticalDown(ratio);
                    break;
                case GraphType.VerticalUp:
                    DrawVerticalUp(ratio);
                    break;
                case GraphType.VerticalSplit:
                    DrawVerticalSplit(ratio);
                    break;
                case GraphType.HorizontalLeft:
                    DrawHorizontalLeft(ratio);
                    break;
                case GraphType.HorizontalRight:
                    DrawHorizontalRight(ratio);
                    break;
                case GraphType.HorizontalSplit:
                    DrawHorizontalSplit(ratio);
                    break;
                case GraphType.Lamp:
                    DrawLamp(ratio);
                    break;
                default:
                    throw new NotImplementedException("Unimplemented graphType " + graphType.ToString());
            }
        }

        private void DrawBorder(RenderTexture screen)
        {
            GL.Color(color);
            GL.Begin(GL.LINES);
            for (int i = 0; i < lineWidth; ++i)
            {
                float offset = (float)i;
                GL.Vertex3(position.x + offset, position.y + offset, 0.0f);
                GL.Vertex3(position.x + offset, position.y + size.y - offset, 0.0f);
                GL.Vertex3(position.x + offset, position.y + size.y - offset, 0.0f);
                GL.Vertex3(position.x + size.x - offset, position.y + size.y - offset, 0.0f);
                GL.Vertex3(position.x + size.x - offset, position.y + size.y - offset, 0.0f);
                GL.Vertex3(position.x + size.x - offset, position.y + offset, 0.0f);
                GL.Vertex3(position.x + size.x - offset, position.y + offset, 0.0f);
                GL.Vertex3(position.x + offset, position.y + offset, 0.0f);
            }
            GL.End();
        }

        private void DrawHorizontalLeft(float fillRatio)
        {
            if (fillRatio <= 0.0f)
            {
                return; // early return - empty graph
            }

            Color fillColor = Color.Lerp(passiveColor, activeColor, fillRatio);

            GL.Color(fillColor);
            GL.Begin(GL.QUADS);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x * (1.0f - fillRatio), fillTopLeftCorner.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x * (1.0f - fillRatio), fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.End();
        }

        private void DrawHorizontalRight(float fillRatio)
        {
            if (fillRatio <= 0.0f)
            {
                return; // early return - empty graph
            }

            Color fillColor = Color.Lerp(passiveColor, activeColor, fillRatio);

            GL.Color(fillColor);
            GL.Begin(GL.QUADS);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x * fillRatio, fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x * fillRatio, fillTopLeftCorner.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y, 0.0f);
            GL.End();
        }

        private void DrawHorizontalSplit(float fillRatio)
        {
            Color fillColor = Color.Lerp(passiveColor, activeColor, fillRatio);

            GL.Color(fillColor);

            if (fillRatio < 0.5f || fillRatio > 0.5f)
            {
                // MOARdV: It doesn't look like back face culling is enabled,
                // so I don't need to have separate cases for < 0.5 and > 0.5
                GL.Begin(GL.QUADS);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x * fillRatio, fillTopLeftCorner.y, 0.0f);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x * fillRatio, fillTopLeftCorner.y + fillSize.y, 0.0f);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x * 0.5f, fillTopLeftCorner.y + fillSize.y, 0.0f);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x * 0.5f, fillTopLeftCorner.y, 0.0f);
                GL.End();
            }
            else
            {
                GL.Begin(GL.LINES);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x * 0.5f, fillTopLeftCorner.y, 0.0f);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x * 0.5f, fillTopLeftCorner.y + fillSize.y, 0.0f);
                GL.End();
            }

        }

        private void DrawLamp(float fillRatio)
        {
            Color fillColor = Color.Lerp(passiveColor, activeColor, fillRatio);

            GL.Color(fillColor);
            GL.Begin(GL.QUADS);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y, 0.0f);
            GL.End();
        }

        private void DrawVerticalDown(float fillRatio)
        {
            if (fillRatio <= 0.0f)
            {
                return; // early return - empty graph
            }

            Color fillColor = Color.Lerp(passiveColor, activeColor, fillRatio);

            GL.Color(fillColor);
            GL.Begin(GL.QUADS);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillRatio * fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillRatio * fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y, 0.0f);
            GL.End();
        }

        private void DrawVerticalSplit(float fillRatio)
        {
            Color fillColor = Color.Lerp(passiveColor, activeColor, fillRatio);

            GL.Color(fillColor);

            if (fillRatio < 0.5f || fillRatio > 0.5f)
            {
                // MOARdV: It doesn't look like back face culling is enabled,
                // so I don't need to have separate cases for < 0.5 and > 0.5
                GL.Begin(GL.QUADS);
                GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillSize.y * (1.0f - fillRatio), 0.0f);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillSize.y * (1.0f - fillRatio), 0.0f);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillSize.y * 0.5f, 0.0f);
                GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillSize.y * 0.5f, 0.0f);
                GL.End();
            }
            else
            {
                GL.Begin(GL.LINES);
                GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillSize.y * 0.5f, 0.0f);
                GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillSize.y * 0.5f, 0.0f);
                GL.End();
            }

        }

        private void DrawVerticalUp(float fillRatio)
        {
            if (fillRatio <= 0.0f)
            {
                return; // early return - empty graph
            }

            Color fillColor = Color.Lerp(passiveColor, activeColor, fillRatio);

            GL.Color(fillColor);
            GL.Begin(GL.QUADS);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillSize.y, 0.0f);
            GL.Vertex3(fillTopLeftCorner.x + fillSize.x, fillTopLeftCorner.y + fillSize.y * (1.0f - fillRatio), 0.0f);
            GL.Vertex3(fillTopLeftCorner.x, fillTopLeftCorner.y + fillSize.y * (1.0f - fillRatio), 0.0f);
            GL.End();
        }
    }
}
