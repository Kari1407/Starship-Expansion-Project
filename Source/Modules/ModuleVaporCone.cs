using UnityEngine;
using Random = System.Random;

namespace ModuleVaporCone
{
    public class ModuleVaporCone : PartModule
    {
        public const string MODULENAME = "ModuleVaporConeTest";

        [KSPField] public string transform;

        [KSPField] public int chance = 1; // Made nullable for those who don't fill it out! Setting it equal to 1 KSP didn't like

        private ModuleEnginesFX engineModule;

        private Random random = new Random();

        private bool isShutdown = false; // Used only for override purposes
        private bool hasRolledChance = false;


        public void OnStart()
        {
            // Find engine module and set variable
            foreach (ModuleEnginesFX moduleEnginesFx in part.FindModulesImplementing<ModuleEnginesFX>())
            {
                if (moduleEnginesFx.thrustVectorTransformName.Equals(transform))
                {
                    Debug.Log($"[{MODULENAME}] Found Vapor Cone EngineFX");

                    moduleEnginesFx.Events["Shutdown"].guiActive = false;
                    moduleEnginesFx.Events["Activate"].guiActive = false;
                    moduleEnginesFx.Actions["OnAction"].guiName = "Toggle Vapor (Manual Override)";
                    moduleEnginesFx.Actions["ShutdownAction"].guiName = "Shutdown Vapor (Manual Override)";
                    moduleEnginesFx.Actions["ActivateAction"].guiName = "Activate Vapor (Manual Override)";
                    engineModule = moduleEnginesFx;
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            // Find engine module and set variable OnLoad to ensure they are stored
            foreach (ModuleEnginesFX moduleEnginesFx in part.FindModulesImplementing<ModuleEnginesFX>())
            {
                if (moduleEnginesFx.thrustVectorTransformName.Equals(transform))
                {
                    Debug.Log($"[{MODULENAME}] Found Vapor Cone EngineFX");

                    moduleEnginesFx.Events["Shutdown"].guiActive = false;
                    moduleEnginesFx.Events["Activate"].guiActive = false;
                    moduleEnginesFx.Actions["OnAction"].guiName = "Toggle Vapor (Manual Override)";
                    moduleEnginesFx.Actions["ShutdownAction"].guiName = "Shutdown Vapor (Manual Override)";
                    moduleEnginesFx.Actions["ActivateAction"].guiName = "Activate Vapor (Manual Override)";
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
                    engineModule.Activate();
                }
                isShutdown = false; // Allows for future rolls, seems redundant but we have manual overrides
                hasRolledChance = true; // 1 or not, they already rolled
            }
            else if (vessel.verticalSpeed < -1 && engineModule && !isShutdown)
            {
                Debug.Log($"[{MODULENAME}] Shutting Down Vapor Cone Engine | Vertical Speed < -1");
                engineModule.Shutdown(); // Shuts down no matter what if you are falling
                isShutdown = true; // Allows for future rolls, seems redundant but we have manual overrides
                hasRolledChance = false; // Another chance to roll when they go up again
            }
        }
    }
}