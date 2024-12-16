using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarshipExpansionProject.Modules
{

    public class ModuleSEPRaptor : PartModule
    {
        public const string MODULENAME = "ModuleSEPRaptor";

        public bool enableActuateOut = true;

        [KSPField(guiActive = false, isPersistant = true)]
        private bool IsActuated;

        [KSPField]
        public float gimbalOutRange = 10f;

        [KSPField]
        public float gimbalOutSpeed = 5f;

        [KSPField(guiActive = true, guiActiveUnfocused = true, guiName = "#LOC_SEP_actuateOut", isPersistant = false, unfocusedRange = 25f)]
        [UI_Toggle(affectSymCounterparts = UI_Scene.All, disabledText = "Off", enabledText = "On", scene = UI_Scene.All)]
        public bool actuateOut;

        [KSPField(guiActive = true, guiActiveUnfocused = true, guiName = "#LOC_SEP_actuateLimiter", isPersistant = false, unfocusedRange = 25f)]
        [UI_FloatRange(minValue = 0f, stepIncrement = 1f, maxValue = 100f, affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All)]
        public float actuateLimiter = 100f;

        [KSPAction(guiName = "#LOC_SEP_ToggleActuateOutAction")]
        public void ToggleActuateOutAction(KSPActionParam param)
        {
            actuateOut = !actuateOut;
            ActuateOut();
        }

        [KSPAction(guiName = "#LOC_SEP_EnableActuateOutAction")]
        public void EnableActuateOutAction(KSPActionParam param)
        {
            actuateOut = true;
            ActuateOut();
        }

        [KSPAction(guiName = "#LOC_SEP_DisableActuateOutAction")]
        public void DisableActuateOutAction(KSPActionParam param)
        {
            actuateOut = false;
            ActuateOut();
        }

        private bool initialized;
        private ModuleGimbal gimbalModule;
        private List<Quaternion> origGimbalsRots;
        private float gimbalXN, gimbalXP, gimbalYN, gimbalYP;
        private float[] oldActuation;
        private bool instantLerp = false;

        /*//Debug:
        Vector3Renderer gimbalOutLine;
        Vector3Renderer axisLine;*/


        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                gimbalModule = part.Modules.GetModule<ModuleGimbal>();
                gimbalXN = gimbalModule.gimbalRangeXN;
                gimbalXP = gimbalModule.gimbalRangeXP;
                gimbalYN = gimbalModule.gimbalRangeYN;
                gimbalYP = gimbalModule.gimbalRangeYP;

                if (gimbalModule == null) { enableActuateOut = false; }

                /*// Debug
                gimbalOutLine = new Vector3Renderer(part, "Gimbal Out", "Gimbal Out Direction", Color.red);
                axisLine = new Vector3Renderer(part, "Axis Line", "Cross Axis", Color.blue);*/

            }
            else // If in editor disable
            {
                Fields["actuateOut"].guiActive = false;
                Fields["actuateOut"].guiActiveEditor = false;
            }



            if (!enableActuateOut) // Module is disabled via config
            {
                Fields["actuateOut"].guiActive = false;
                Fields["actuateOut"].guiActiveEditor = false;

                Actions["ToggleActuateOutAction"].activeEditor = false;
                Actions["ToggleActuateOutAction"].active = false;

                Actions["EnableActuateOutAction"].activeEditor = false;
                Actions["EnableActuateOutAction"].active = false;

                Actions["DisableActuateOutAction"].activeEditor = false;
                Actions["DisableActuateOutAction"].active = false;

                actuateOut = false;
            }
            else
            {
                origGimbalsRots = gimbalModule.initRots.Select(o => o).ToList();
                oldActuation = gimbalModule.initRots.Select(t => 0f).ToArray();
            }

            if (Fields.TryGetFieldUIControl("actuateOut", out UI_Toggle actuateOutField))
            {
                actuateOutField.onFieldChanged += OnActuateOutField;
            }

            initialized = true;
        }

        public override void OnLoad(ConfigNode node)
        {
            actuateOut = IsActuated;
        }

        private void OnActuateOutField(BaseField field, object sender)
        {
            ActuateOut();
        }


        public void ActuateOut()
        {
            if (!enableActuateOut) // Plugin is disabled
                return;

            if (actuateOut)
            {
                IsActuated = true;
                if (!gimbalModule.gimbalActive)
                {
                    // Auto Lerp to 0 ranges as it is already at 0 movement, and we don't want someone holding WASD to jump randomly when the gimbal becomes active
                    // Go ahead and turn that bad boy on
                    gimbalModule.gimbalActive = true;
                    instantLerp = true;
                }
            }
            else
            {
                IsActuated = false;
            }
        }

        public void FixedUpdate()
        {
            if (!enableActuateOut || !initialized)
                return;

            // Determine the direction to gimbal the engine outwards
            Vector3 gimbalOutDir = part.transform.position - part.parent.transform.position;

            // Cross product of the upwards axis and the gimbalOutDir axis
            Vector3 axis = Vector3.Cross(gimbalOutDir, part.transform.up);

            /*// Debug:
            gimbalOutLine.DrawLine(part.transform, gimbalOutDir, 2f, true);
            axisLine.DrawLine(part.transform, axis, 2f, true);*/

            if (actuateOut)
            {
                // Lerp the gimbal ranges forwards
                LerpRanges(true);

                for (int i = 0; i < gimbalModule.initRots.Count; i++)
                {
                    // Get the local cross axis direction
                    Vector3 localAxis = gimbalModule.gimbalTransforms[i].InverseTransformDirection(axis);

                    // Lerp a value based on the previous value to the final value by the amount determined by gOS * TW.dT
                    float lerped = Mathf.Lerp(oldActuation[i], gimbalOutRange * (actuateLimiter * 0.01f), gimbalOutSpeed * TimeWarp.deltaTime);

                    // Set the previous value to the new value so next lerped value is this + gOS * TW.dT
                    oldActuation[i] = lerped;

                    // Rotate the initPos about the local axis
                    gimbalModule.initRots[i] = origGimbalsRots[i] * Quaternion.AngleAxis(lerped, localAxis);
                }

            }
            else
            {
                LerpRanges(false);

                for (int i = 0; i < gimbalModule.initRots.Count; i++)
                {
                    // This is the same thing as above, but in reverse order back to default
                    Vector3 localAxis = gimbalModule.gimbalTransforms[i].InverseTransformDirection(axis);
                    float lerped = Mathf.Lerp(oldActuation[i], 0f, gimbalOutSpeed * TimeWarp.deltaTime);
                    oldActuation[i] = lerped;

                    gimbalModule.initRots[i] = origGimbalsRots[i] * Quaternion.AngleAxis(lerped, localAxis);

                }

            }

            if (actuateOut != IsActuated)
            {
                ActuateOut();
            }

        }

        public void LerpRanges(bool forward)
        {
            if (forward)
            {
                if (instantLerp) // Used if the gimbalModule wasn't already active (such as engine is off). Upon activating it via this plugin the engine might be seen jumping to a random spot based on player control
                {
                    gimbalModule.gimbalRangeXN = 0;
                    gimbalModule.gimbalRangeXP = 0;
                    gimbalModule.gimbalRangeYN = 0;
                    gimbalModule.gimbalRangeYP = 0;
                    instantLerp = false; // Next time this won't be needed as the GimbalModule should now be active
                }
                else
                {
                    // Ramp down
                    gimbalModule.gimbalRangeXN = Mathf.Lerp(gimbalModule.gimbalRangeXN, 0, gimbalOutSpeed * TimeWarp.deltaTime);
                    gimbalModule.gimbalRangeXP = Mathf.Lerp(gimbalModule.gimbalRangeXP, 0, gimbalOutSpeed * TimeWarp.deltaTime);
                    gimbalModule.gimbalRangeYN = Mathf.Lerp(gimbalModule.gimbalRangeYN, 0, gimbalOutSpeed * TimeWarp.deltaTime);
                    gimbalModule.gimbalRangeYP = Mathf.Lerp(gimbalModule.gimbalRangeYP, 0, gimbalOutSpeed * TimeWarp.deltaTime);
                }
            }
            else
            {
                // Ramp up
                gimbalModule.gimbalRangeXN = Mathf.Lerp(gimbalModule.gimbalRangeXN, gimbalXN, gimbalOutSpeed * TimeWarp.deltaTime);
                gimbalModule.gimbalRangeXP = Mathf.Lerp(gimbalModule.gimbalRangeXP, gimbalXP, gimbalOutSpeed * TimeWarp.deltaTime);
                gimbalModule.gimbalRangeYN = Mathf.Lerp(gimbalModule.gimbalRangeYN, gimbalYN, gimbalOutSpeed * TimeWarp.deltaTime);
                gimbalModule.gimbalRangeYP = Mathf.Lerp(gimbalModule.gimbalRangeYP, gimbalYP, gimbalOutSpeed * TimeWarp.deltaTime);

            }
        }

    }
}
