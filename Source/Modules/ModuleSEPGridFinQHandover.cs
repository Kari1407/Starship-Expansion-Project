// ============================================================================
//  ModuleSEPGridFinQHandover
//
//  Dynamic-pressure based handover of roll authority between the Super Heavy
//  grid fins and the Raptor TVC.
//
//  Problem (see README "Known Issues"):
//      "Grid Fins can cause big roll oscillations if actuating with the engines,
//       make sure to disable them on ignition"
//
//  Logic:
//      q = 0.5 * rho * v^2                     (kPa)
//      w = clamp01((q - qLow) / (qHigh - qLow))
//
//      finAuthority    = lerp(finLow,  finHigh, w)
//      engineAuthority = lerp(engHigh, engLow,  w)   // optional
//
//      w = 0 : low q  -> fins off, roll comes from the Raptor TVC
//      w = 1 : high q -> fins take the roll axis, engines backed off
//
//  Notes:
//  - Attached to the grid fin parts by Patches/GridFinHandover.cfg.
//  - `rollOnly = false` disables the module, because the stock grid fin config
//    sets ignoreRoll = true and would otherwise never provide roll authority.
//  - Scaling the engines uses ModuleGimbal.gimbalLimiter, which is not per-axis
//    (it also reduces pitch/yaw), hence `scaleEngineGimbal` defaults to false.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace StarshipExpansionProject.Modules
{
    public class ModuleSEPGridFinQHandover : PartModule
    {
        // ---- tuning -------------------------------------------------------

        [KSPField]
        public float qLow = 10f;                 // kPa, fins start taking over

        [KSPField]
        public float qHigh = 40f;                // kPa, fins fully in charge

        [KSPField]
        public float finLow = 0f;                // fin authority at low q  (%)

        [KSPField]
        public float finHigh = 100f;             // fin authority at high q (%)

        [KSPField]
        public bool scaleEngineGimbal = false;   // also scale ModuleGimbal

        [KSPField]
        public float engHigh = 100f;             // engine authority at low q  (%)

        [KSPField]
        public float engLow = 30f;               // engine authority at high q (%)

        [KSPField]
        public bool rollOnly = true;             // force fins to roll-only control

        [KSPField]
        public float smoothing = 0.15f;          // 0 = instant, 1 = very sluggish

        // ---- debug readout -------------------------------------------------

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_SEP_gridfin_q")]
        public string qDisplay = "-";

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_SEP_gridfin_handover")]
        public string wDisplay = "-";

        // ---- runtime ------------------------------------------------------

        private ModuleControlSurface ctrl;
        private readonly List<ModuleGimbal> gimbals = new List<ModuleGimbal>();
        private bool initialised;
        private float currentW;
        private int refreshCounter;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            ctrl = part.FindModuleImplementing<ModuleControlSurface>();

            if (ctrl != null && rollOnly)
            {
                ctrl.ignorePitch = true;
                ctrl.ignoreYaw = true;
                ctrl.ignoreRoll = false;
            }

            if (scaleEngineGimbal && vessel != null)
            {
                gimbals.Clear();
                gimbals.AddRange(vessel.FindPartModulesImplementing<ModuleGimbal>());
            }

            currentW = 0f;
            initialised = true;
        }

        public override void OnFixedUpdate()
        {
            if (!initialised || !HighLogic.LoadedSceneIsFlight) return;

            float q = 0f;

            if (vessel != null && vessel.atmDensity > 0.0)
            {
                double v = vessel.srfSpeed;
                q = (float)(0.5 * vessel.atmDensity * v * v / 1000.0);
                if (q < 0f) q = 0f;
            }

            float targetW = Mathf.Clamp01((q - qLow) / Mathf.Max(0.001f, qHigh - qLow));

            // Low-pass the weight so a noisy q cannot make the authority jump.
            if (smoothing > 0f && smoothing < 1f)
                currentW = Mathf.Lerp(currentW, targetW, 1f - smoothing);
            else
                currentW = targetW;

            if (ctrl != null)
                ctrl.authorityLimiter = Mathf.Lerp(finLow, finHigh, currentW);

            if (scaleEngineGimbal)
            {
                float target = Mathf.Lerp(engHigh, engLow, currentW);
                for (int i = 0; i < gimbals.Count; i++)
                {
                    var g = gimbals[i];
                    if (g != null && g.part != null && g.part.vessel == vessel)
                        g.gimbalLimiter = target;
                }
            }

            // Refresh the readout a few times per second instead of every tick.
            if (++refreshCounter >= 10)
            {
                refreshCounter = 0;
                qDisplay = q.ToString("F1") + " kPa";
                wDisplay = (currentW * 100f).ToString("F0") + " %";
            }
        }
    }
}
