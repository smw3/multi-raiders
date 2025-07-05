using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MultiRaiders.Helpers
{
    public static class MirrorImageHelper
    {
        public static float GetTickOffsetForPawn(Pawn pawn, int idx)
        {
            float tickOffset = GenTicks.TicksGame * 0.0005f * idx + idx * 1234.0f + pawn.HashOffset() % 1000;

            return tickOffset;
        }
        public static Vector3 GetSwirlOffset(float time, float scale = 1f)
        {
            // Lissajous-like parameters for complexity
            float x = Mathf.Sin(time * 0.7f) + 0.5f * Mathf.Sin(time * 1.3f + 1.2f);
            float z = Mathf.Cos(time * 1.1f) + 0.5f * Mathf.Cos(time * 0.9f + 2.3f);
            return new Vector3(x, 0f, z) * scale;
        }

        public static Vector3 GetSwirlDirection(float time, float scale = 1f)
        {
            // Numerical derivative for direction
            float delta = 0.01f;
            Vector3 pos1 = GetSwirlOffset(time, scale);
            Vector3 pos2 = GetSwirlOffset(time + delta, scale);
            Vector3 dir = (pos2 - pos1).normalized;
            return dir;
        }

        public static Rot4 GetSwirlInfluencedRot4(Rot4 baseRot, Vector3 swirlDir, float influence = 0.5f)
        {
            // Calculate the angle in degrees (XZ plane)
            float angleSwirl = Mathf.Atan2(swirlDir.x, swirlDir.z) * Mathf.Rad2Deg;
            if (angleSwirl < 0) angleSwirl += 360f;
            float angleRot = baseRot.AsAngle;

            float combinedAngle = influence * angleSwirl + (1 - influence) * angleRot;

            Rot4 swirlRot;
            if (combinedAngle >= 315f || combinedAngle < 45f)
                swirlRot = Rot4.North;
            else if (combinedAngle >= 45f && combinedAngle < 135f)
                swirlRot = Rot4.East;
            else if (combinedAngle >= 135f && combinedAngle < 225f)
                swirlRot = Rot4.South;
            else
                swirlRot = Rot4.West;

            return swirlRot;
        }
    }
}
