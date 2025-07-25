// Copyright 2020 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;

namespace TiltBrush
{
    public class BrushStrokeCommand : BaseCommand
    {
        public Stroke m_Stroke;
        private float m_LineLength_CS; // Only valid if m_Widget != null

        private Vector3 CommandAudioPosition
        {
            get { return GetPositionForCommand(m_Stroke); }
        }

        public BrushStrokeCommand(Stroke stroke,
                                  float lineLength = -1, BaseCommand parent = null) : base(parent)
        {
            m_Stroke = stroke;
            m_Stroke.Command = this;
            m_LineLength_CS = lineLength;
        }

        // New constructor that accepts an existing Guid
        public BrushStrokeCommand(Stroke stroke, Guid existingGuid, int timestamp,
                                  float lineLength = -1, BaseCommand parent = null)
            : base(existingGuid, timestamp, parent)
        {
            m_Stroke = stroke;
            m_Stroke.Command = this;
            m_LineLength_CS = lineLength;
        }

        public override string Serialize()
        {
            //var data = Newtonsoft.Json.JsonConvert.SerializeObject(m_Stroke);
            //UnityEngine.Debug.Log($"test: {data}");
            return JsonUtility.ToJson(m_Stroke);
        }

        public override bool NeedsSave { get { return true; } }

        protected override void OnDispose()
        {
            //SketchMemoryScript.m_Instance.RemoveMemoryObject(m_Stroke);
            m_Stroke.DestroyStroke();
        }

        protected override void OnRedo()
        {
            switch (m_Stroke.m_Type)
            {
                case Stroke.Type.BrushStroke:
                    {
                        GameObject gameObj = m_Stroke.m_Object;
                        if (gameObj)
                        {
                            BaseBrushScript rBrushScript = gameObj.GetComponent<BaseBrushScript>();
                            if (rBrushScript)
                            {
                                rBrushScript.HideBrush(false);
                            }
                        }
                        break;
                    }
                case Stroke.Type.BatchedBrushStroke:
                    {
                        var batch = m_Stroke.m_BatchSubset.m_ParentBatch;
                        batch.EnableSubset(m_Stroke.m_BatchSubset);
                        break;
                    }
                case Stroke.Type.NotCreated:
                    Debug.LogError("Unexpected: redo NotCreated stroke");
                    //m_Stroke.Recreate();
                    break;
            }

            TiltMeterScript.m_Instance.AdjustMeter(m_Stroke, up: true);
        }

        protected override void OnUndo()
        {
            switch (m_Stroke.m_Type)
            {
                case Stroke.Type.BrushStroke:
                    {
                        GameObject gameObj = m_Stroke.m_Object;
                        if (gameObj)
                        {
                            BaseBrushScript rBrushScript = gameObj.GetComponent<BaseBrushScript>();
                            if (rBrushScript)
                            {
                                rBrushScript.HideBrush(true);
                            }
                        }
                        break;
                    }
                case Stroke.Type.BatchedBrushStroke:
                    {
                        var batch = m_Stroke.m_BatchSubset.m_ParentBatch;
                        batch.DisableSubset(m_Stroke.m_BatchSubset);
                        break;
                    }
                case Stroke.Type.NotCreated:
                    Debug.LogError("Unexpected: undo NotCreated stroke");
                    break;
            }

            TiltMeterScript.m_Instance.AdjustMeter(m_Stroke, up: false);
        }

        public override bool Merge(BaseCommand other)
        {
            if (base.Merge(other)) { return true; }
            BrushStrokeCommand stroke = other as BrushStrokeCommand;
            if (stroke != null)
            {
                m_Children.Add(stroke);
                return true;
            }
            return false;
        }
    }
} // namespace TiltBrush
