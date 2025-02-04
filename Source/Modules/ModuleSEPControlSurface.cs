using UnityEngine;
using StarshipExpansionProject.Utils;

namespace StarshipExpansionProject
{
    public class ModuleSEPControlSurface : ModuleLiftingSurface, IMultipleDragCube, ITorqueProvider
    {
        /*
         * Definition:
           - X, right
           - Y up/down? dorsal-vectral
           - Z forward
         */

        public const string MODULENAME = "ModuleSEPControlSurface";

        [KSPAxisField(axisMode = KSPAxisMode.Incremental, guiActive = true, guiActiveUnfocused = true, guiFormat = "0", guiName = "#autoLOC_6013041", guiUnits = "°", incrementalSpeed = 50f, isPersistant = true, maxValue = 100f, minValue = -100f, unfocusedRange = 25f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 90f, minValue = 0f, scene = UI_Scene.All, stepIncrement = 0.1f)]
        public float deployAngle = -1f;

        [KSPAxisField(axisMode = KSPAxisMode.Incremental, guiActive = true, guiActiveUnfocused = true, guiFormat = "0", guiName = "#autoLOC_6001336", guiUnits = "°", incrementalSpeed = 50f, isPersistant = true, maxValue = 100f, minValue = -100f, unfocusedRange = 25f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 40, minValue = 0f, scene = UI_Scene.All, stepIncrement = 0.25f)]
        public float controlAuthority = 40f;

        [KSPField(guiActive = false, guiActiveUnfocused = true, guiFormat = "0", guiName = "#LOC_SEP_pitchAuthority", guiUnits = "°", unfocusedRange = 25f)]
        public float pitchAuthority = 1f;

        [KSPField(guiActive = false, guiActiveUnfocused = true, guiFormat = "0", guiName = "#LOC_SEP_yawAuthority", guiUnits = "°", unfocusedRange = 25f)]
        public float yawAuthority = 1f;

        [KSPField(guiActive = false, guiActiveUnfocused = true, guiFormat = "0", guiName = "#LOC_SEP_rollAuthority", guiUnits = "°", unfocusedRange = 25f)]
        public float rollAuthority = 1f;

        [KSPField(guiActive = true, guiActiveUnfocused = true, guiName = "#autoLOC_6001333", isPersistant = true, unfocusedRange = 25f)]
        [UI_Toggle(affectSymCounterparts = UI_Scene.All, disabledText = "#autoLOC_6001081", enabledText = "#autoLOC_6001080", scene = UI_Scene.All)]
        public bool deploy;

        [KSPField(guiActive = true, guiActiveUnfocused = true, guiName = "#autoLOC_6001330", isPersistant = true, unfocusedRange = 25f)]
        [UI_Toggle(affectSymCounterparts = UI_Scene.All, disabledText = "#autoLOC_6001079", enabledText = "#autoLOC_6001078", scene = UI_Scene.All)]
        public bool ignorePitch;

        [UI_Toggle(affectSymCounterparts = UI_Scene.All, disabledText = "#autoLOC_6001079", enabledText = "#autoLOC_6001078", scene = UI_Scene.All)]
        [KSPField(guiActive = true, guiActiveUnfocused = true, guiName = "#autoLOC_6001331", isPersistant = true, unfocusedRange = 25f)]
        public bool ignoreYaw;

        [UI_Toggle(affectSymCounterparts = UI_Scene.All, disabledText = "#autoLOC_6001079", enabledText = "#autoLOC_6001078", scene = UI_Scene.All)]
        [KSPField(guiActive = true, guiActiveUnfocused = true, guiName = "#autoLOC_6001332", isPersistant = true, unfocusedRange = 25f)]
        public bool ignoreRoll;

        #region KSP Action Groups
        [KSPAction(guiName = "#autoLOC_6001337")]
        public void ToggleDeployAction(KSPActionParam param)
        {
            deploy = !deploy;
        }

        [KSPAction(guiName = "#LOC_SEP_ExtendFinsAction")]
        public void ExtendFinsAction(KSPActionParam param)
        {
            deploy = false;
        }

        [KSPAction(guiName = "#LOC_SEP_RetractFinsAction")]
        public void RetractFinsAction(KSPActionParam param)
        {
            deploy = true;
        }

        [KSPAction(guiName = "#autoLOC_6006035")]
        public void ActivePitchAction(KSPActionParam param)
        {
            ignorePitch = false;
        }

        [KSPAction(guiName = "#autoLOC_6006036")]
        public void IgnorePitchAction(KSPActionParam param)
        {
            ignorePitch = true;
        }

        [KSPAction(guiName = "#autoLOC_6006037")]
        public void TogglePitchAction(KSPActionParam param)
        {
            ignorePitch = !ignorePitch;
        }


        [KSPAction(guiName = "#autoLOC_6006038")]
        public void ActiveYawAction(KSPActionParam param)
        {
            ignoreYaw = false;
        }

        [KSPAction(guiName = "#autoLOC_6006039")]
        public void IgnoreYawAction(KSPActionParam param)
        {
            ignoreYaw = true;
        }

        [KSPAction(guiName = "#autoLOC_6006040")]
        public void ToggleYawAction(KSPActionParam param)
        {
            ignoreYaw = !ignoreYaw;
        }

        [KSPAction(guiName = "#autoLOC_6006041")]
        public void ActiveRollAction(KSPActionParam param)
        {
            ignoreRoll = false;
        }

        [KSPAction(guiName = "#autoLOC_6006042")]
        public void IgnoreRollAction(KSPActionParam param)
        {
            ignoreRoll = true;
        }

        [KSPAction(guiName = "#autoLOC_6006043")]
        public void ToggleRollAction(KSPActionParam param)
        {
            ignoreRoll = !ignoreRoll;
        }

        [KSPAction(guiName = "#autoLOC_6006044")]
        public void ActivateAllInputs(KSPActionParam param)
        {
            ignorePitch = false;
            ignoreYaw = false;
            ignoreRoll = false;
        }

        [KSPAction(guiName = "#autoLOC_6006045")]
        public void IgnoreAllInputs(KSPActionParam param)
        {
            ignorePitch = true;
            ignoreYaw = true;
            ignoreRoll = true;
        }

        [KSPAction(guiName = "#LOC_SEP_ToggleAllAction")]
        public void ToggleAllAction(KSPActionParam param)
        {
            ignorePitch = !ignorePitch;
            ignoreYaw = !ignoreYaw;
            ignoreRoll = !ignoreRoll;
        }

        [KSPAction(guiName = "#LOC_SEP_ActivateFlipAngles")]
        public void ActivateFlipAngles(KSPActionParam param)
        {
            deployAngle = ctrlSurfaceFlipDeployAngle;
            ignorePitch = true;
            ignoreYaw = true;
            ignoreRoll = true;
        }

        [KSPAction(guiName = "#LOC_SEP_ActivateLandingAngles")]
        public void ActivateLandingAngles(KSPActionParam param)
        {
            deployAngle = ctrlSurfaceLandingDeployAngle;
            ignorePitch = true;
            ignoreYaw = true;
            ignoreRoll = true;
        }


        #endregion

        /// <summary>
        /// Actuator transform name
        /// </summary>
        [KSPField]
        public new string transformName;

        /// <summary>
        /// Sets the speed of the wing, transition speed
        /// </summary>
        [KSPField]
        public float actuatorSpeed = 50f;

        /// <summary>
        /// Controls the range of the control surface (in degrees)
        /// </summary>
        [KSPField]
        public Vector2 ctrlSurfaceRange = new Vector2(0, 90);

        /// <summary>
        /// min, max, default value of the deploy range 
        /// </summary>
        [KSPField]
        public Vector3 ctrlSurfaceDeployRange = new Vector3(0, 90, 60);

        /// <summary>
        /// The deploy angle the surface will be set to on activation of "Set to Flip Angles"
        /// </summary>
        [KSPField]
        public float ctrlSurfaceFlipDeployAngle = 70f;

        /// <summary>
        /// The deploy angle the surface will be set to on activation of "Set to Landing Angles"
        /// </summary>
        [KSPField]
        public float ctrlSurfaceLandingDeployAngle = 70f;
        
        /// <summary>
        /// Controls the range of the authority limiter (in degrees)
        /// </summary>
        [KSPField]
        public Vector2 controlAuthorityRange = new Vector2(0, 40);

        /// <summary>
        /// Defines the percentage of how much of the part is the control surface
        /// Valid values ranging from 0 to 1
        /// </summary>
        //[KSPField]
        //public float ctrlSurfaceArea = 1f;

         /// <summary>
        /// Defines the multiplier of the lift force, useful for tweaking specific behaviours
        /// </summary>
        [KSPField]
        public float liftMultiplier = 1f;

         /// <summary>
        /// Defines the multiplier of the drag force, useful for tweaking specific behaviours
        /// </summary>
        [KSPField]
        public float dragMultiplier = 1f;
        
        /// <summary>
        /// Defines a deadband, a treshold - where below the value, the actuation will not be applied to the wings
        /// </summary>
        [KSPField]
        public float deadband = 0.01f;

        private Transform ctrlSurface;
        private Quaternion defaultRotation;
        private float deflection;

        private Vector3 torque = Vector3.zero;
        private Vector3 baseLiftForce = Vector3.zero;
        private float direction = 0f;

        private Vector3Renderer liftPointer;
        private Vector3Renderer dragPointer;

        public void Start()
        {
            ctrlSurface = part.FindModelTransform(transformName);
            if (ctrlSurface == null)
            {
                Debug.LogError($"[{MODULENAME}] Error! Could not find actuating transform '{transformName}' on part '{part.name}'");
            }

            if (Fields.TryGetFieldUIControl("deployAngle", out UI_FloatRange deployAngleField))
            {
                deployAngleField.minValue = ctrlSurfaceDeployRange.x;
                deployAngleField.maxValue = ctrlSurfaceDeployRange.y;
                deployAngleField.stepIncrement = (ctrlSurfaceDeployRange.y - ctrlSurfaceDeployRange.x) * 0.00999999977648258f;

                if (deployAngle == -1f)
                    deployAngle = ctrlSurfaceDeployRange.z;
            }

            if (Fields.TryGetFieldUIControl("controlAuthority", out UI_FloatRange controlAuthorityField))
            {
                controlAuthorityField.minValue = controlAuthorityRange.x;
                controlAuthorityField.maxValue = controlAuthorityRange.y;
                controlAuthorityField.stepIncrement = 0.25f;
            }

/*            if (Fields.TryGetFieldUIControl("pitchAuthority", out UI_FloatRange pitchAuthorityField))
            {
                pitchAuthorityField.minValue = controlAuthorityRange.x;
                pitchAuthorityField.maxValue = controlAuthorityRange.y;
                pitchAuthorityField.stepIncrement = 0.25f;
            }

            if (Fields.TryGetFieldUIControl("yawAuthority", out UI_FloatRange yawAuthorityField))
            {
                yawAuthorityField.minValue = controlAuthorityRange.x;
                yawAuthorityField.maxValue = controlAuthorityRange.y;
                yawAuthorityField.stepIncrement = 0.25f;
            }

            if (Fields.TryGetFieldUIControl("rollAuthority", out UI_FloatRange rollAuthorityField))
            {
                rollAuthorityField.minValue = controlAuthorityRange.x;
                rollAuthorityField.maxValue = controlAuthorityRange.y;
                rollAuthorityField.stepIncrement = 0.25f;
            }*/

            defaultRotation = ctrlSurface.localRotation;

            if (HighLogic.LoadedSceneIsFlight)
            {
                liftPointer = new Vector3Renderer(part, "Lift", "Lift Vector", Color.yellow);
                dragPointer = new Vector3Renderer(part, "Drag", "Drag Vector", Color.red);
            }
        }

        public new void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && part.Rigidbody != null && !part.ShieldedFromAirstream && HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                liftField.guiActive = PhysicsGlobals.AeroDataDisplay;
                dragField.guiActive = PhysicsGlobals.AeroDataDisplay && useInternalDragModel;

                // Velocity around the center of the craft
                Vector3 pointVelocity = Krakensbane.GetFrameVelocityV3f() + part.Rigidbody.GetPointVelocity(baseTransform.position);

                double Qlift = (part.dynamicPressurekPa * (1.0 - part.submergedPortion) + part.submergedDynamicPressurekPa * part.submergedPortion * part.submergedLiftScalar) * 1000.0;
                double Qdrag = (part.dynamicPressurekPa * (1.0 - part.submergedPortion) + part.submergedDynamicPressurekPa * part.submergedPortion * part.submergedDragScalar) * 1000.0;

                // Get Coefficients
                SetupCoefficients(pointVelocity, out Vector3 nVel, out Vector3 liftVector, out float liftDot, out float absDot);

                baseLiftForce = GetLiftVector(liftVector, liftDot, absDot, Qlift, (float)part.machNumber);
                Vector3 liftForce = GetLiftVector(liftVector, liftDot, absDot, Qlift, (float)part.machNumber) * liftMultiplier;
                Vector3 dragForce = GetDragVector(nVel, absDot, Qdrag) * dragMultiplier;

                if (ctrlSurface != null)
                {
                    CtrlSurfaceUpdate();
                }

                // If Control Surface is not in an neutral/default position (i.e. angled), update lift force according to new position 
                if (ctrlSurface.localRotation != defaultRotation)
                {
                    Quaternion wingAngled = Quaternion.AngleAxis(deflection, baseTransform.rotation * Vector3.right);
                    liftVector = wingAngled * liftVector;
                    liftDot = Vector3.Dot(nVel, liftVector);
                    absDot = Mathf.Abs(liftDot);
                }

                liftForce += GetLiftVector(liftVector, liftDot, absDot, Qlift, (float)part.machNumber) * liftMultiplier;
                dragForce += GetDragVector(nVel, absDot, Qdrag, (float)part.machNumber) * dragMultiplier;
                
                part.AddForceAtPosition(liftForce /* (1.0f - (deflection / (1.5f * ctrlSurfaceRange.y)))*/, part.partTransform.TransformPoint(part.CoLOffset));
                part.AddForceAtPosition(dragForce, part.partTransform.TransformPoint(part.CoPOffset));
                part.AddForceAtPosition((deploy ? ((controlAuthority == 0 ? Vector3.zero : torque.normalized) * direction * dragForce.magnitude * 1.1f) : Vector3.zero) * 
                                       (Mathf.Sign(direction) == 1 ? -1f : 1f), part.partTransform.TransformPoint(part.CoPOffset));

/*                if (deploy)
                {
                    if (Mathf.Sign(direction) == 1f)
                    {
                        part.AddForceAtPosition(torque.normalized * direction * dragForce.magnitude * -1f, part.partTransform.TransformPoint(part.CoPOffset));
                    }
                    else
                    {
                        part.AddForceAtPosition(torque.normalized * direction * dragForce.magnitude, part.partTransform.TransformPoint(part.CoPOffset));
                    }
                }*/

                liftPointer.DrawLine(part.transform, liftForce, PhysicsGlobals.AeroForceDisplayScale, PhysicsGlobals.AeroForceDisplay);
                dragPointer.DrawLine(part.transform, dragForce, PhysicsGlobals.AeroForceDisplayScale, PhysicsGlobals.AeroForceDisplay);
                

                if (liftScalar > 0)
                    liftScalar = liftForce.magnitude;
                
                if (dragScalar > 0)
                    dragScalar = dragForce.magnitude;
                
                float delta = 0f;
                bool sign = false;
                if (Mathf.Sign(deflection) == 1)
                {
                    delta = deflection / ctrlSurfaceRange.y;
                    sign = false;
                }
                else
                {
                    delta = deflection / ctrlSurfaceRange.x;
                    sign = true;
                }

                part.DragCubes.SetCubeWeight("defaultRotation", 1.0f - delta);
                part.DragCubes.SetCubeWeight("fullDeflectionPos", sign ? 0 : delta);
                part.DragCubes.SetCubeWeight("fullDeflectionNeg", sign ? delta : 0);
            }
            else
            {
                CtrlSurfaceUpdateEditor();
            }  
        }

        public void CtrlSurfaceUpdate()
        {
            pitchAuthority = Mathf.Clamp01(pitchAuthority);
            yawAuthority = Mathf.Clamp01(yawAuthority);
            rollAuthority = Mathf.Clamp01(rollAuthority);

            Vector3 currentCoM = vessel.CurrentCoM;
            Vector3 angularInput = vessel.ReferenceTransform.rotation * new Vector3(ignorePitch ? 0f : vessel.ctrlState.pitch * pitchAuthority, ignoreRoll ? 0f : vessel.ctrlState.roll * rollAuthority, ignoreYaw ? 0f : vessel.ctrlState.yaw * yawAuthority);
            Vector3 vector = baseTransform.position - currentCoM;

            torque = Vector3.Cross(angularInput, vector.normalized);
            direction = Vector3.Dot(-baseTransform.up, torque);

            float action = 0f;
            action += Vector3.Dot(-baseTransform.forward, torque);
            action += direction;
            action *= controlAuthority;
            action = deploy ? (action + deployAngle) : 0f;
            action = Mathf.Clamp(action, ctrlSurfaceRange.x, ctrlSurfaceRange.y);

            deflection = Mathf.MoveTowards(deflection, action, actuatorSpeed * TimeWarp.fixedDeltaTime);
            ctrlSurface.localRotation = Quaternion.AngleAxis(deflection, Vector3.right) * defaultRotation;
        }

        public void CtrlSurfaceUpdateEditor()
        {
            float action = 0f;
            action = deploy ? action + deployAngle : 0f;
            action = Mathf.Clamp(action, ctrlSurfaceRange.x, ctrlSurfaceRange.y);
            deflection = Mathf.MoveTowards(deflection, action, actuatorSpeed * TimeWarp.fixedDeltaTime);
            ctrlSurface.localRotation = Quaternion.AngleAxis(deflection, Vector3.right) * defaultRotation;
        }

        // ITorqueProvider Stuff
        public void GetPotentialTorque(out Vector3 pos, out Vector3 neg)
        {
            pos = Vector3.zero;
            neg = Vector3.zero;

            if (!deploy)
                return;

            Vector3d CoM = part.WCoM - vessel.CurrentCoM;
            Vector3 localCoM = vessel.ReferenceTransform.InverseTransformDirection(CoM);

            Vector3 fullPositiveDeflection = GetLiftFromDeflection(Quaternion.AngleAxis(ctrlSurfaceRange.y, baseTransform.rotation * Vector3.right) * baseTransform.forward);
            Vector3 fullNegativeDeflection = GetLiftFromDeflection(Quaternion.AngleAxis(ctrlSurfaceRange.x, baseTransform.rotation * Vector3.right) * baseTransform.forward);

            pos = Vector3.Scale(vessel.ReferenceTransform.InverseTransformDirection(Vector3.Cross(CoM, fullPositiveDeflection)), localCoM);
            neg = Vector3.Scale(vessel.ReferenceTransform.InverseTransformDirection(Vector3.Cross(CoM, fullNegativeDeflection)), localCoM);
            pos.y = -pos.y;
            neg.y = -neg.y;
        }

        /// <summary>
        /// Returns Lift Force by given deflection vector
        /// </summary>
        /// <param name="deflection"></param>
        /// <returns></returns>
        public Vector3 GetLiftFromDeflection(Vector3 deflection)
        {
            Vector3 liftForce = baseLiftForce * liftMultiplier;
            float dot = Vector3.Dot(nVel, deflection);
            return GetLiftVector(deflection, dot, Mathf.Abs(dot), Qlift, (float)part.machNumber) * liftMultiplier - liftForce;
        }

        /// <summary>
        /// Drag Cube Stuff
        /// </summary>
        public string[] GetDragCubeNames()
        {
            return new string[3]
            {
              "defaultRotation",
              "fullDeflectionPos",
              "fullDeflectionNeg"
            };
        }

        public bool UsesProceduralDragCubes()
        {
            return false;
        }

        public void AssumeDragCubePosition(string name)
        {
            float action = 0f;
            switch(name)
            {
                case "fullDeflectionNeg":
                    action = ctrlSurfaceRange.x;
                    break;
                case "fullDeflectionPos":
                    action = ctrlSurfaceRange.y;
                    break;
                case "defaultRotation":
                    action = 0.0f;
                    break;
            }

            if (ctrlSurface == null)
            {
                ctrlSurface = part.FindModelTransform(transformName);
            }
            
            if (ctrlSurface != null)
            {
                ctrlSurface.localRotation = defaultRotation;
                ctrlSurface.localRotation = Quaternion.AngleAxis(action, Vector3.right) * defaultRotation;
            }
        }

        public bool IsMultipleCubesActive
        {
            get
            {
                return true;
            }
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                Destroy(liftPointer.gameObject);
                Destroy(dragPointer.gameObject);
            }           
        }
    }
}
