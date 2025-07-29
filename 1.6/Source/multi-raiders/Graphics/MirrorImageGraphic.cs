using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace MultiRaiders.Graphics
{
    public class MirrorImageGraphic : Graphic_Multi
    {
        public static Lazy<FieldInfo> matsInfo = new(() => AccessTools.Field(typeof(MirrorImageGraphic), "mats"));

        public Material[] _mats;
        protected Shader _shader;
        protected int _renderQueue;
        protected List<ShaderParameter> _shaderParameters;
        protected Pawn _pawn;

        public override bool ShouldDrawRotated => false;

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
            path = req.path;
            maskPath = req.maskPath;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;
            _shader = req.shader;
            _renderQueue = req.renderQueue;
            _shaderParameters = req.shaderParameters;
            _mats = matsInfo.Value.GetValue(this) as Material[];
        }

        public void SetMaterial(Pawn pawn, Rot4 rot, bool asleep)
        {
            MaterialRequest req1 = new()
            {
                mainTex = PortraitsCache.Get(pawn, new Vector2(600f, 600f), rot, healthStateOverride: asleep ? PawnHealthState.Down : PawnHealthState.Mobile, cameraZoom: 0.5f, compensateForUIScale: false, supersample: false),
                shader = _shader,
                color = color,
                colorTwo = colorTwo,
                renderQueue = _renderQueue,
                shaderParameters = _shaderParameters
            };

            _mats[rot.AsInt] = MaterialPool.MatFrom(req1);

            _pawn = pawn;
        }
    }
}
