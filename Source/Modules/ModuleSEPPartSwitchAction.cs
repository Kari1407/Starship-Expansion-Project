using B9PartSwitch;
using System.Collections.Generic;
using UnityEngine;

namespace StarshipExpansionProject.Modules
{
    public class ModuleSEPPartSwitchAction : PartModule
    {
        public const string MODULENAME = "ModuleSEPPartSwitchAction";

        private ModuleB9PartSwitch SwitchModule; // reference to the ModuleB9PartSwitch module in the part

        private List<PartSubtype> Subtypes; // list of subtypes available in the ModuleB9PartSwitch module

        // Fields that can be set in the part's config file and displayed in the part's information window in the game
        [KSPField]
        public string SwitchID = ""; // ID of the ModuleB9PartSwitch module in the part

        [KSPField]
        public string ActionName = "Subtype"; // name of the action buttons for switching subtypes

        [KSPField]
        public string moduleID = "ModuleSEPPartSwitchAction"; // ID of this module

        [KSPField]
        public bool ShowCurrentSubtype = false; // Wether to show the current subtype in the PAW or not

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_SEP_CurrentSubtype")] // Field that returns the name of the currently selected subtype
        public string CurrentSubtype;

        // Action to switch to the next subtype in the list, wrapping around to the beginning of the list if necessary
        [KSPAction(guiName = "#LOC_SEP_NextSubtype")]
        public void NextSubtype(KSPActionParam param)
        {
            Debug.Log($"[{MODULENAME}] Cycling to next subtype on {SwitchModule.moduleID}");
            int CurrentIndex = SwitchModule.currentSubtypeIndex; // get the current index of the subtype in the ModuleB9PartSwitch module
            CurrentIndex++; // increment the index
            if (CurrentIndex >= Subtypes.Count) // if the index is out of range
            {
                CurrentIndex = 0; // wrap around to the beginning of the list
            }
            SwitchModule.SwitchSubtype(Subtypes[CurrentIndex].Name); // switch to the subtype at the new index
        }

        // Action to switch to the previous subtype in the list, wrapping around to the end of the list if necessary
        [KSPAction(guiName = "#LOC_SEP_PreviousSubtype")]
        public void PreviousSubtype(KSPActionParam param)
        {
            Debug.Log($"[{MODULENAME}] Cycling to previous subtype on {SwitchModule.moduleID}");
            int CurrentIndex = SwitchModule.currentSubtypeIndex; // get the current index of the subtype in the ModuleB9PartSwitch module
            CurrentIndex--; // decrement the index
            if (CurrentIndex < 0) // if the index is out of range
            {
                CurrentIndex = Subtypes.Count - 1; // wrap around to the end of the list
            }
            SwitchModule.SwitchSubtype(Subtypes[CurrentIndex].Name); // switch to the subtype at the new index
        }

        // Update the names of the action buttons to reflect the current value of the ActionName field
        private void UpdateActions()
        {
            Actions["NextSubtype"].guiName = "Next " + ActionName;
            Actions["PreviousSubtype"].guiName = "Previous " + ActionName;
            Fields["CurrentSubtype"].guiName = "Current " + ActionName;
            if (!ShowCurrentSubtype)
            {
                Fields["CurrentSubtype"].guiActive = false;
                Fields["CurrentSubtype"].guiActiveEditor = false;
            }
        }

        // Update the CurrentSubtype field to reflect the current subtype of the part as set in the ModuleB9PartSwitch module
        public void FixedUpdate()
        {
            if (CurrentSubtype != SwitchModule.CurrentSubtypeName)
            {
                CurrentSubtype = SwitchModule.CurrentSubtypeName;
            }
            
        }

        // Find the ModuleB9PartSwitch module in the part and store a reference to it in the SwitchModule field,
        // and store a list of subtypes available in the module in the Subtypes field.
        // If the module cannot be found, disable the action buttons for switching subtypes.
        public void Start()
        {
            List<ModuleB9PartSwitch> ModuleList = part.Modules.GetModules<ModuleB9PartSwitch>();
            for (int i = 0; i < ModuleList.Count; i++)
            {
                ModuleB9PartSwitch module = ModuleList[i];
                if (module.moduleID == SwitchID)
                {
                    SwitchModule = module;
                    Subtypes = SwitchModule.subtypes;
                }
            }
            if (SwitchModule == null)
            {
                Debug.LogError($"[{MODULENAME}] B9PS module with id {SwitchID} not found!");
                Actions["NextSubtype"].active = false;
                Actions["PreviousSubtype"].active = false;
                Actions["NextSubtype"].activeEditor = false;
                Actions["PreviousSubtype"].activeEditor = false;
                Fields["CurrentSubtype"].guiActive = false;
                Fields["CurrentSubtype"].guiActiveEditor = false;

            }
            UpdateActions();
        }
    }
}