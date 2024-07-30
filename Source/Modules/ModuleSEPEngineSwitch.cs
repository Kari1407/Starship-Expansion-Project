using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace StarshipExpansionProject.Modules
{
	// Full Credit to Vali Zockt for writing this module, Initially for Tundra Exploration
	// Later adjusted for StarshipExpansionProject by Sofie Brink.
	public class ModuleSEPEngineSwitch : PartModule, IEngineStatus
	{
		public const string MODULENAME = "ModuleSEPEngineSwitch";

		[KSPField(guiName = "Mode", isPersistant = true, guiActive = true, guiActiveEditor = true)]
		public string currentEngineDisplay;

		[KSPField(isPersistant = true)]
		public int selectedIndex = 0;

		[KSPField]
		public string primaryEngineID;

		[KSPField]
		public string secondaryEngineID;

		[KSPField]
		public string tertiaryEngineID;

		[KSPEvent(guiActive = true, guiName = "Next Engine Mode")]
		public void NextEngineModeEvent() => SetNextEngine();

		[KSPEvent(guiActive = true, guiName = "Previous Engine Mode")]
		public void PreviousEngineModeEvent() => SetPreviousEngine();

		[KSPAction(guiName = "Activate Engine")]
		public void ActivateEngineAction(KSPActionParam param) => ActivateActiveEngine();

		[KSPAction(guiName = "Shutdown Engine")]
		public void ShutdownEngineAction(KSPActionParam param) => ShutdownActiveEngine();

		[KSPAction(guiName = "Toggle Engine")]
		public void ToggleEngineAction(KSPActionParam param) => ToggleActiveEngine();

		[KSPAction(guiName = "Next Engine Mode")]
		public void NextEngineModeAction(KSPActionParam param) => SetNextEngine();

		[KSPAction(guiName = "Previous Engine Mode")]
		public void PreviousEngineModeAction(KSPActionParam param) => SetPreviousEngine();

		[KSPAction(guiName = "Toggle Engine Mode")]
		public void ToggleEngineModeAction(KSPActionParam param) => SetNextEngine();

		[KSPAction(guiName = "Toggle Independent Throttle")]
		public void ToggleIndependentThrottleAction(KSPActionParam param) => ToggleIndependentThrottle(param);

		private List<ModuleEnginesFX> activeEngines;
		private List<ModuleEnginesFX> oldEngines;
		private List<ModuleEnginesFX> engines = new List<ModuleEnginesFX>();

		public override void OnStart(StartState state)
		{

			base.OnStart(state);

			List<string> engineIDs = new List<string> { primaryEngineID, secondaryEngineID, tertiaryEngineID };
			for (int i = 0; i < 3; i++)
			{
				ModuleEnginesFX moduleEnginesFX = part.Modules.GetModules<ModuleEnginesFX>()
															  .Where(eng => eng.engineID == engineIDs[i])
															  .FirstOrDefault();
				if (moduleEnginesFX == null)
				{
					Debug.LogError($"[{MODULENAME}] Could not find engine with ID '{engineIDs[i]}' on part '{part.name}'");
					continue;
				}

				for (int i2 = 0; i2 < moduleEnginesFX.Actions.Count; i2++)
				{
					moduleEnginesFX.Actions[i2].active = false;
					//moduleEnginesFX.Actions[12].independentThrottle = false;
				}

				moduleEnginesFX.manuallyOverridden = true;
				moduleEnginesFX.isEnabled = false;

				engines.Add(moduleEnginesFX);
			}
			SetActiveEngine(selectedIndex, true);
		}

		public void ShutdownActiveEngine()
		{
			activeEngines.ForEach(en => en.Shutdown());
		}

		public void ActivateActiveEngine()
		{
			activeEngines.ForEach(en => en.Activate());
		}

		public void ToggleActiveEngine()
		{
			if (activeEngines.Any(en => en.EngineIgnited))
				activeEngines.ForEach(en => en.Shutdown());
			else
				activeEngines.ForEach(en => en.Activate());
		}

		public void ToggleIndependentThrottle(KSPActionParam param)
		{
			activeEngines.ForEach(en => en.ToggleThrottle(param));
		}

		public void SetNextEngine()
		{
			int newIndex = selectedIndex + 1;

			if (newIndex >= 3)
				newIndex = 0;

			SetActiveEngine(newIndex);
		}

		public void SetPreviousEngine()
		{
			int newIndex = selectedIndex - 1;

			if (newIndex < 0)
				newIndex = 2;

			SetActiveEngine(newIndex);
		}

		public void SetActiveEngine(int index, bool setup = false)
		{
			oldEngines = activeEngines;
			
			activeEngines = new List<ModuleEnginesFX>();
			for (int i = index; i < 3; i++) activeEngines.Add(engines[i]);

			activeEngines.ForEach(en => en.manuallyOverridden = false);
			activeEngines.ForEach(en => en.isEnabled = true);

			currentEngineDisplay = Regex.Replace(activeEngines[0].engineName, "([a-z])([A-Z])", "$1 $2");
			selectedIndex = index;

			if (setup)
				return;

			if (activeEngines.Any(en => en.EngineIgnited))
			{
				activeEngines.ForEach(en => {if (!en.EngineIgnited) { en.Activate();}});
				activeEngines.ForEach(en => en.currentThrottle = oldEngines[0].currentThrottle);
				foreach (var engine in oldEngines)
				{
					if (!activeEngines.Contains(engine) && engine.EngineIgnited)
					{
						engine.Shutdown();
						engine.isEnabled = false;
					}
				}
			}

			activeEngines.ForEach(en => en.manuallyOverridden = true);
		}

		public bool isOperational
		{
			get
			{
				return activeEngines[0].isOperational;
			}
		}

		public float normalizedOutput
		{
			get
			{
				return activeEngines[0].normalizedOutput;
			}
		}

		public float throttleSetting
		{
			get
			{
				return activeEngines[0].throttleSetting;
			}
		}

		public string engineName
		{
			get
			{
				return activeEngines[0].engineName;
			}
		}
	}
}