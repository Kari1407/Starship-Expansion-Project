using UnityEngine;
using Random = System.Random;
using Waterfall;
using System.Linq;

namespace ModuleVaporCone
{
    public class ModuleSEPVaporCone : PartModule
    {
        public const string MODULENAME = "ModuleSEPVaporCone";

        [KSPField] public string waterfallEffectController = "thrust";

        [KSPField] public string vaporTransform;

        [KSPField] public string engineID = "SEPVaporEngine";

        [KSPField] public int chance = 1; 

        private ModuleEnginesFX engineModule;

        private Random random = new Random();

        private bool isShutdown = false; // Used only for override purposes
        private bool hasRolledChance = false;


        public void OnStart()
        {
            // Find engine module and set variable
            foreach (ModuleEnginesFX moduleEnginesFx in part.FindModulesImplementing<ModuleEnginesFX>())
            {
                if (moduleEnginesFx.thrustVectorTransformName.Equals(vaporTransform))
                {
                    Debug.Log($"[{MODULENAME}] Found Vapor Cone EngineFX");

                    moduleEnginesFx.Events["Shutdown"].guiActive = false;
                    moduleEnginesFx.Events["Activate"].guiActive = false;
                    moduleEnginesFx.Actions["OnAction"].guiName = "#LOC_SEP_ToggleVapor";
                    moduleEnginesFx.Actions["ShutdownAction"].guiName = "#LOC_SEP_ShutdownVapor";
                    moduleEnginesFx.Actions["ActivateAction"].guiName = "#LOC_SEP_Activate";
                    engineModule = moduleEnginesFx;
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            // Find engine module and set variable OnLoad to ensure they are stored
            foreach (ModuleEnginesFX moduleEnginesFx in part.FindModulesImplementing<ModuleEnginesFX>())
            {
                if (moduleEnginesFx.thrustVectorTransformName.Equals(vaporTransform))
                {
                    Debug.Log($"[{MODULENAME}] Found Vapor Cone EngineFX");

                    moduleEnginesFx.Events["Shutdown"].guiActive = false;
                    moduleEnginesFx.Events["Activate"].guiActive = false;
                    moduleEnginesFx.Actions["OnAction"].guiName = "#LOC_SEP_ToggleVapor";
                    moduleEnginesFx.Actions["ShutdownAction"].guiName = "#LOC_SEP_ShutdownVapor";
                    moduleEnginesFx.Actions["ActivateAction"].guiName = "#LOC_SEP_Activate";
                    engineModule = moduleEnginesFx;
                }
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT) return;
            if (vessel.verticalSpeed > 1 && !hasRolledChance && engineModule)
            {
                if (random.Next(1, chance) == 1)
                {
                    Debug.Log($"[{MODULENAME}] Activating Vapor Cone Engine | Vertical Speed > 1 [Chance]");
                    foreach (ModuleWaterfallFX module in part.FindModulesImplementing<ModuleWaterfallFX>().Where(m => m.engineID == engineID))
                    {
                        Debug.Log($"Waterfall engineID Found: {module.engineID}");
                        foreach (WaterfallController controller in module.Controllers.Where(c => c.name == waterfallEffectController))
                        {
                            Debug.Log($"Waterfall Controller Found: {name}");
                            controller.overridden = true;
                            controller.overrideValue = 1f;
                        }
                    }
                }
                isShutdown = false; // Allows for future rolls, seems redundant but we have manual overrides
                hasRolledChance = true; // 1 or not, they already rolled
            }
            else if (vessel.verticalSpeed < -1 && engineModule && !isShutdown)
            {
                Debug.Log($"[{MODULENAME}] Shutting Down Vapor Cone Engine | Vertical Speed < -1");
                foreach (ModuleWaterfallFX module in part.FindModulesImplementing<ModuleWaterfallFX>().Where(m => m.engineID == engineID))
                {
                    Debug.Log($"Waterfall engineID Found: {module.engineID}");
                    foreach (WaterfallController controller in module.Controllers.Where(c => c.name == waterfallEffectController))
                    {
                        Debug.Log($"Waterfall Controller Found: {name}");
                        controller.overridden = true;
                        controller.overrideValue = 0f;
                    }
                } // Shuts down no matter what if you are falling
                isShutdown = true; // Allows for future rolls, seems redundant but we have manual overrides
                hasRolledChance = false; // Another chance to roll when they go up again
            }
        }
    }
}