using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TundraExploration.Utils;
using UnityEngine;

namespace StarshipExplorationProject.Modules
{
    public class ModuleSEPRaptor : PartModule
    {
        [KSPField]
        public bool enableActuateOut = true;

        [KSPField]
        public float gimbalOutRange = 10f;

        [KSPField]
        public float gimbalOutSpeed = 5f;

        [KSPField(guiActive = true, guiActiveUnfocused = true, guiName = "Actuate Out", isPersistant = false, unfocusedRange = 25f)]
        [UI_Toggle(affectSymCounterparts = UI_Scene.All, disabledText = "On", enabledText = "Off", scene = UI_Scene.All)]
        public bool actuateOut;

        [KSPAction(guiName = "Toggle Actuate Out")]
        public void ToggleActuateOutAction(KSPActionParam param)
        {
            actuateOut = !actuateOut;
            CheckGimbal();
        }

        [KSPAction(guiName = "Enable Actuate Out")]
        public void EnableActuateOutAction(KSPActionParam param)
        {
            actuateOut = true;
            CheckGimbal();
        }

        [KSPAction(guiName = "Disable Actuate Out")]
        public void DisableActuateOutAction(KSPActionParam param)
        {
            actuateOut = false;
            CheckGimbal();
        }

        private bool initialized;
        private bool gimbalLock;
        private ModuleGimbal gimbalModule;

        private List<Transform> transforms;
        private List<Quaternion> origGimbalRots;
        private float[] oldActuation;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                gimbalModule = part.Modules.GetModule<ModuleGimbal>();
                List<ModuleGimbal> modules = vessel.FindPartModulesImplementing<ModuleGimbal>().ToList();
                List<ModuleGimbal> sortedmodules = new List<ModuleGimbal>();
                for (int i = 0; i < modules.Count; i++)
                {
                    if (modules[i].part.Modules.Contains("ModuleSEPRaptor"))
                    {
                        sortedmodules.Add(modules[i]);
                    }

                }
                transforms = sortedmodules.Select(m => m.part.transform).ToList();

                if (gimbalModule == null || sortedmodules.Count <= 1)
                    enableActuateOut = false;
            }
            else
            {
                Fields["actuateOut"].guiActive = false;
                Fields["actuateOut"].guiActiveEditor = false;
            }

            if (!enableActuateOut)
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
                origGimbalRots = new List<Quaternion>();
                for (int i = 0; i < gimbalModule.initRots.Count; i++)
                {
                    origGimbalRots.Add(gimbalModule.initRots[i]);
                }

                oldActuation = new float[gimbalModule.initRots.Count];
                for (int i = 0; i < oldActuation.Length; i++)
                {
                    oldActuation[i] = 0f;
                }

                if (gimbalModule.Fields.TryGetFieldUIControl("gimbalLock", out UI_Toggle gimbalLockField))
                {
                    gimbalLockField.onFieldChanged += OnGimbalLockField;
                }
            }

            if (Fields.TryGetFieldUIControl("actuateOut", out UI_Toggle actuateOutField))
            {
                actuateOutField.onFieldChanged += OnActuateOutField;
            }

            initialized = true;
        }

        private void OnGimbalLockField(BaseField arg1, object arg2)
        {
            if (!gimbalLock && actuateOut)
            {
                actuateOut = false;
            }
        }

        private void OnActuateOutField(BaseField field, object sender)
        {
            CheckGimbal();
        }

        public void CheckGimbal()
        {
            if (!enableActuateOut)
                return;

            if (actuateOut)
            {
                gimbalLock = gimbalModule.gimbalLock;
                gimbalModule.Fields["gimbalLock"].guiInteractable = false;
                gimbalModule.gimbalLock = true;
            }
            else
            {
                gimbalModule.Fields["gimbalLock"].guiInteractable = true;
                gimbalModule.gimbalLock = gimbalLock;
            }
        }

        public void FixedUpdate()
        {
            if (!enableActuateOut || !initialized)
                return;

            Vector3 centroidPosition = GetCentroidPoint(transforms);
            Vector3 gimbalOut = part.transform.position - centroidPosition;
            Vector3 engineUp = part.transform.up;
            Vector3 axis = Vector3.Cross(gimbalOut, engineUp);

            if (actuateOut)
            {
                for (int i = 0; i < gimbalModule.initRots.Count; i++)
                {
                    Vector3 localAxis = gimbalModule.gimbalTransforms[i].InverseTransformDirection(axis);
                    float lerped = Mathf.Lerp(oldActuation[i], gimbalOutRange, gimbalOutSpeed * TimeWarp.fixedDeltaTime);
                    oldActuation[i] = lerped;

                    gimbalModule.initRots[i] = origGimbalRots[i] * Quaternion.AngleAxis(lerped, localAxis);
                }
            }
            else
            {
                for (int i = 0; i < gimbalModule.initRots.Count; i++)
                {
                    Vector3 localAxis = gimbalModule.gimbalTransforms[i].InverseTransformDirection(axis);
                    float lerped = Mathf.Lerp(oldActuation[i], 0f, gimbalModule.gimbalResponseSpeed * TimeWarp.fixedDeltaTime);
                    oldActuation[i] = lerped;

                    gimbalModule.initRots[i] = origGimbalRots[i] * Quaternion.AngleAxis(lerped, localAxis);
                }
            }
        }

        // https://answers.unity.com/questions/511841
        private Vector3 GetCentroidPoint(List<Transform> points)
        {
            Vector3 centroid;
            Vector3 minPoint = points[0].position;
            Vector3 maxPoint = points[0].position;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 pos = points[i].position;
                if (pos.x < minPoint.x)
                    minPoint.x = pos.x;
                if (pos.x > maxPoint.x)
                    maxPoint.x = pos.x;
                if (pos.y < minPoint.y)
                    minPoint.y = pos.y;
                if (pos.y > maxPoint.y)
                    maxPoint.y = pos.y;
                if (pos.z < minPoint.z)
                    minPoint.z = pos.z;
                if (pos.z > maxPoint.z)
                    maxPoint.z = pos.z;
            }

            centroid = minPoint + 0.5f * (maxPoint - minPoint);

            return centroid;
        }
    }
}