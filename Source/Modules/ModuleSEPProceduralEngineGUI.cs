using System;
using UnityEngine;
using System.Collections.Generic;
using KSPCommunityFixes;
using System.Linq;
using StarshipExpansionProject.Utils.DataTypes;
using KSP.Localization;
// TODO: Remove unused items from State dict, probably in OnLoad? 
//       Throw error if State dict already contains a key at load time
//       enable relationships
//       Symmetry
//       Actually Disable tranforms
//       Actually toggle nodes

namespace StarshipExpansionProject.Modules
{
    // Part Module
    public class ModuleSEPProceduralEngineGUI : PartModule
    {
        #region Static strings
        // Static strings
        public const string MODULENAME = nameof(ModuleSEPProceduralEngineGUI);

        public const string CustomSelectableNodeName = "CUSTOMSELECTABLE";
        public const string EngineSetNodeName = "ENGINESET";
        public const string EngineTypeNodeName = "ENGINETYPE";
        #endregion

        #region Serialized Fields
        // Serialized Fields
        [SerializeField]
        public List<CustomSelectable> customSelectables;

        [SerializeField]
        public List<EngineSet> engineSets;

        [SerializeField]
        public List<EngineType> engineTypes;

        [SerializeField]
        public List<Transform> transforms;
        #endregion

        #region KSP Fields
        // KSP Fields
        [KSPField]
        public string transformNameSeperator = string.Empty;

        [KSPField]
        public float engineSize = 20;

        [KSPField(isPersistant = true)]
        public PersistentDictionaryValueTypeKey<string, PersistentDictionaryValueTypes<string, bool>> State = new PersistentDictionaryValueTypeKey<string, PersistentDictionaryValueTypes<string, bool>>();

        [KSPField(isPersistant = true)]
        public int activeEngineType = 0;

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "#LOC_SEP_SelectEnginesGUIName")]
        public void OpenGui()
        {
            isVisible = true;
        }
        #endregion

        #region Private UI Fields
        // Private UI Fields
        private Rect windowRect = new Rect(250, 200, 250, 376);
        private bool isDragging = false;
        private bool isVisible = false;
        private Vector2 customSelectablesScrollPosition = Vector2.zero;
        private string uiName = Localizer.GetStringByTag("#LOC_SEP_GUILabelName");
        private float uiWidth = 236;
        private static int highestuiIndex = -1;
        private int? _uiIndex;
        private int uiIndex
        {
            get
            {
                if (_uiIndex is null)
                {
                    highestuiIndex++;
                    _uiIndex = highestuiIndex;
                }
                return (int) _uiIndex;
            }
        }
        private float? _uiScale;
        private float uiScale
        {
            get
            {
                if (_uiScale == null)
                {
                    var tmpPositions = EnginePositionsDict.SelectMany(ep => ep.Value);
                    var maxExtent = tmpPositions.Max(t => t.magnitude);
                    _uiScale = (uiWidth / 2 - engineSize / 2 - 3) / maxExtent;
                }

                return (float) _uiScale;
            }
        }

        #endregion

        #region Private Fields
        // Private Fields
        private Dictionary<int, List<CustomSelectable>> _selectablesDict;
        private Dictionary<int, List<CustomSelectable>> SelectablesDict
        {
            get
            {
                if (_selectablesDict == null)
                {
                    _selectablesDict = new Dictionary<int, List<CustomSelectable>>();
                    for (int i = 0; i < engineTypes.Count; i++)
                    {
                        _selectablesDict.Add(i, customSelectables.Where(s => engineTypes[i].CustomSelectableNames.Contains(s.name)).ToList());
                    }
                }
                return _selectablesDict;
            }
        }

        private Dictionary<int, List<EngineSet>> _engineSetDict;
        private Dictionary<int, List<EngineSet>> EngineSetDict
        {
            get
            {
                if (_engineSetDict == null)
                {
                    _engineSetDict = new Dictionary<int, List<EngineSet>>();
                    for (int i = 0; i < engineTypes.Count; i++)
                    {
                        _engineSetDict.Add(i, engineSets.Where(s => engineTypes[i].EngineSetNames.Contains(s.name)).ToList());
                    }
                }
                return _engineSetDict;
            }
        }

        private Dictionary<string, List<Vector2>> _enginePositionsDict;
        private Dictionary<string, List<Vector2>> EnginePositionsDict
        {
            get
            {
                if (_enginePositionsDict == null)
                {
                    _enginePositionsDict = new Dictionary<string, List<Vector2>>();
                    for (int i = 0; i < engineSets.Count(); i++)
                    {
                        var tmpTransformPositions = engineSets[i]
                                                   .selectableItems
                                                   .Select(t => part.transform
                                                               .InverseTransformVector(transforms[t.transformIndexes[0]].position));
                        _enginePositionsDict.Add(engineSets[i].name
                                               , tmpTransformPositions.Select(p => new Vector2(p.x, p.z)).ToList());
                    }
                }
                return _enginePositionsDict;
            }
        }
        #endregion

        #region KSP Methods
        // KSP Methods
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (customSelectables == null) customSelectables = new List<CustomSelectable>();
            if (engineSets == null) engineSets = new List<EngineSet>();
            if (engineTypes == null) engineTypes = new List<EngineType>();
            if (transforms == null
             || node.HasNode(CustomSelectableNodeName)
             || node.HasNode(EngineTypeNodeName))
            {
                transforms = new List<Transform>(); // this assumes b9ps sends a complete confnode, just modified
                _selectablesDict = null;
                _engineSetDict = null;
                _enginePositionsDict = null;
                _uiScale = null;
            }

            foreach (var tmpNode in node.GetNodes())
            {
                var nodeType = tmpNode.name;
                switch (nodeType)
                {
                    case CustomSelectableNodeName:
                        if (!ParseCustomSelectable(node: tmpNode))
                            Debug.LogError($"[{MODULENAME}] Failed to parse {nodeType} found in config for part {part.partInfo?.title}\n\n{tmpNode}");
                        break;
                    case EngineSetNodeName:
                        if (!ParseEngineSet(node: tmpNode))
                            Debug.LogError($"[{MODULENAME}] Failed to parse {nodeType} found in config for part {part.partInfo?.title}\n\n{tmpNode}");
                        break;
                    case EngineTypeNodeName:
                        if (!ParseEngineType(node: tmpNode))
                            Debug.LogError($"[{MODULENAME}] Failed to parse {nodeType} found in config for part {part.partInfo?.title}\n\n{tmpNode}");
                        break;
                }
            }
        }

        public void OnGUI()
        {
            if (isVisible) windowRect = GUI.Window(uiIndex, windowRect, DrawUI, uiName);
        }

        public void FixedUpdate()
        {
        }
        public void Start()
        {
        }
        #endregion

        #region Custom Methods
        // Custom Methods
        private bool ParseCustomSelectable(ConfigNode node)
        {
            List<(VmlSeverity, string)> vml = new List<(VmlSeverity, string)>();
            var tmpCustomSelectable = ScriptableObject.CreateInstance<CustomSelectable>();

            // Fill toplevel object
            if (node.HasValue(nameof(tmpCustomSelectable.name)))
                tmpCustomSelectable.name = node.GetValue(nameof(tmpCustomSelectable.name));
            else vml.Add((VmlSeverity.Error, $"{CustomSelectableNodeName} \"{tmpCustomSelectable.name}\" has no value {nameof(tmpCustomSelectable.name)}"));

            if (node.HasValue(nameof(tmpCustomSelectable.guiName)))
                tmpCustomSelectable.guiName = node.GetValue(nameof(tmpCustomSelectable.guiName));
            else
            {
                tmpCustomSelectable.guiName = tmpCustomSelectable.name;
                vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpCustomSelectable.name}\" has no value {nameof(tmpCustomSelectable.guiName)}, using default: {tmpCustomSelectable.name}"));
            }

            // This one is a little special because we don't need to store it in this object
            bool tmpDefaultState = true;
            if (node.HasValue("defaultState"))
                tmpDefaultState = bool.Parse(node.GetValue("defaultState"));
            else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpCustomSelectable.name}\" has no value defaultState, using default: {tmpDefaultState}"));

            if (!State.ContainsKey(tmpCustomSelectable.name)) State.Add(tmpCustomSelectable.name, new PersistentDictionaryValueTypes<string, bool>());
            if (!State[tmpCustomSelectable.name].ContainsKey(tmpCustomSelectable.name))
                State[tmpCustomSelectable.name].Add(tmpCustomSelectable.name, tmpDefaultState);

            // Fill Items
            if (node.HasNode("ITEM"))
            {
                foreach (var tmpNode in node.GetNodes("ITEM"))
                {
                    var tmpItem = ScriptableObject.CreateInstance<SelectableItem>();
                    if (tmpNode.HasValue("transformName"))
                        tmpItem.name = tmpNode.GetValue("transformName");
                    else vml.Add((VmlSeverity.Error, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value transformName"));

                    if (tmpNode.HasValue(nameof(tmpItem.invertTransformState)))
                        tmpItem.invertTransformState = bool.Parse(tmpNode.GetValue(nameof(tmpItem.invertTransformState)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value {nameof(tmpItem.invertTransformState)}, using default: {tmpItem.invertTransformState}"));

                    if (tmpNode.HasValue("nodeTransformName"))
                        tmpItem.nodeId = tmpNode.GetValue("nodeTransformName");
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value nodeTransformName, no node will be created"));

                    if (tmpNode.HasValue(nameof(tmpItem.invertNodeState)))
                        tmpItem.invertNodeState = bool.Parse(tmpNode.GetValue(nameof(tmpItem.invertNodeState)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value {nameof(tmpItem.invertNodeState)}, using default: {tmpItem.invertNodeState}"));

                    int tmpNodeSize = 2;
                    if (tmpNode.HasValue("nodeSize"))
                        tmpNodeSize = int.Parse(tmpNode.GetValue("nodeSize"));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value nodeSize, using default: {tmpNodeSize}"));

                    if (tmpNode.HasValue(nameof(tmpItem.mass)))
                        tmpItem.mass = float.Parse(tmpNode.GetValue(nameof(tmpItem.mass)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpItem.name}\" has no value {nameof(tmpItem.mass)}, using default: {tmpItem.mass}"));

                    if (tmpItem.name == string.Empty) continue;

                    var tmpTransforms = part.FindModelTransforms(tmpItem.name);
                    Transform tmpNodeTransform = null;
                    if (tmpItem.nodeId != null && tmpItem.nodeId != string.Empty)
                        tmpNodeTransform = part.FindModelTransform(tmpItem.nodeId);
                    if (tmpTransforms.Length == 0)
                    {
                        continue;
                    }
                    if (tmpItem.nodeId != null && tmpItem.nodeId != string.Empty && tmpNodeTransform == null)
                    {
                        Debug.LogError($"[{MODULENAME}] {CustomSelectableNodeName} no transform found with name \"{tmpItem.nodeId}\"");
                    }
                    tmpItem.transformIndexes.AddRange(Enumerable.Range(transforms.Count, tmpTransforms.Length));
                    transforms.AddRange(tmpTransforms);
                    if (tmpItem.nodeId != string.Empty && tmpNodeTransform != null)
                        CreateAttachNodeIfNotExists(NodeId: tmpItem.nodeId, NodeSize: tmpNodeSize);

                    tmpCustomSelectable.selectableItems.Add(tmpItem);
                }
            }

            // Fill Relationships
            if (node.HasNode("RELATIONSHIP"))
            {
                foreach (var tmpNode in node.GetNodes("RELATIONSHIP"))
                {
                    var tmpRelationship = ScriptableObject.CreateInstance<Relationship>();
                    tmpRelationship.name = tmpCustomSelectable.name + "Relationship" + transformNameSeperator + tmpCustomSelectable.relationships.Count;
                    if (tmpNode.HasValue(nameof(tmpRelationship.referencedGroupName)))
                        tmpRelationship.referencedGroupName = tmpNode.GetValue(nameof(tmpRelationship.referencedGroupName));
                    else vml.Add((VmlSeverity.Error, $"{CustomSelectableNodeName} \"{tmpRelationship.name}\" has no value {nameof(tmpRelationship.referencedGroupName)}"));

                    if (tmpNode.HasValue(nameof(tmpRelationship.invertRelationship)))
                        tmpRelationship.invertRelationship = bool.Parse(tmpNode.GetValue(nameof(tmpRelationship.invertRelationship)));
                    else vml.Add((VmlSeverity.Information, $"{CustomSelectableNodeName} \"{tmpRelationship.name}\" has no value {nameof(tmpRelationship.invertRelationship)}, using default: {tmpRelationship.invertRelationship}"));

                    if (tmpRelationship.name == string.Empty) continue;

                    tmpCustomSelectable.relationships.Add(tmpRelationship);
                }
            }

            customSelectables.Add(tmpCustomSelectable);

            var vmlResult = LogVml(Vml: vml);
            return vmlResult;
        }
        private bool ParseEngineSet(ConfigNode node)
        {
            List<(VmlSeverity, string)> vml = new List<(VmlSeverity, string)>();
            var tmpEngineSet = ScriptableObject.CreateInstance<EngineSet>();
            
            // Fill toplevel object
            if (node.HasValue(nameof(tmpEngineSet.name))) 
                tmpEngineSet.name = node.GetValue(nameof(tmpEngineSet.name));
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.name)}"));

            if (node.HasValue(nameof(tmpEngineSet.guiName)))
                tmpEngineSet.guiName = node.GetValue(nameof(tmpEngineSet.guiName));
            else
            {
                tmpEngineSet.guiName = tmpEngineSet.name;
                vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.guiName)}, using default: {tmpEngineSet.name}"));
            }

            if (node.HasValue(nameof(tmpEngineSet.engineId)))
            {
                tmpEngineSet.engineId = node.GetValue(nameof(tmpEngineSet.engineId));
                if (part.FindModulesImplementingReadOnly<ModuleEngines>().FirstOrDefault(m => (m as ModuleEngines).engineID == tmpEngineSet.engineId) == null)
                    vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} part \"{part.partInfo?.title}\" has no module implementing ModuleEngines with engineId \"{tmpEngineSet.engineId}\""));
            }
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.engineId)}"));

            if (node.HasValue(nameof(tmpEngineSet.singleEngineThrust)))
                tmpEngineSet.singleEngineThrust = float.Parse(node.GetValue(nameof(tmpEngineSet.singleEngineThrust)));
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.singleEngineThrust)}"));

            if (node.HasValue(nameof(tmpEngineSet.singleEngineMinThrust)))
                tmpEngineSet.singleEngineMinThrust = float.Parse(node.GetValue(nameof(tmpEngineSet.singleEngineMinThrust)));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value {nameof(tmpEngineSet.singleEngineMinThrust)}, using default: {tmpEngineSet.singleEngineMinThrust}"));

            // These ones are a little special because we don't need to store them in this object
            bool tmpDefaultState = true;
            if (node.HasValue("defaultState"))
                tmpDefaultState = bool.Parse(node.GetValue("defaultState"));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value defaultState, using default: {tmpDefaultState}"));

            float tmpMass = 0f;
            if (node.HasValue("mass"))
                tmpMass = float.Parse(node.GetValue("mass"));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value mass, using default: {tmpMass}"));

            string tmpTransformName = string.Empty;
            if (node.HasValue("transformName"))
                tmpTransformName = node.GetValue("transformName");
            else vml.Add((VmlSeverity.Error, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value transformName"));

            string tmpNodeTransformName = string.Empty;
            if (node.HasValue("nodeTransformName"))
                tmpNodeTransformName = node.GetValue("nodeTransformName");
            else vml.Add((VmlSeverity.Warning, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value nodeTransformName, no nodes will be created"));

            bool tmpInvertNodeState = true;
            if (node.HasValue("invertNodeState"))
                tmpInvertNodeState = bool.Parse(node.GetValue("invertNodeState"));
            else vml.Add((VmlSeverity.Information, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value invertNodeState, using default: {tmpInvertNodeState}"));

            int tmpNodeSize = 2;
            if (node.HasValue("nodeSize"))
                tmpNodeSize = int.Parse(node.GetValue("nodeSize"));
            else vml.Add((VmlSeverity.Warning, $"{EngineSetNodeName} \"{tmpEngineSet.name}\" has no value nodeSize, using default: {tmpNodeSize}"));

            if (!State.ContainsKey(tmpEngineSet.name)) State.Add(tmpEngineSet.name, new PersistentDictionaryValueTypes<string, bool>());

            int i = 0;
            var vmlResult = LogVml(Vml: vml);
            while (vmlResult)
            {
                if (tmpTransformName == string.Empty) break;
                var tmpFullTransformName = tmpTransformName + transformNameSeperator + i;
                var tmpFullNodeTransformName = tmpNodeTransformName + transformNameSeperator + i;

                var tmpTransforms = part.FindModelTransforms(tmpFullTransformName);
                Transform tmpNodeTransform = null;
                if (tmpNodeTransformName != string.Empty) 
                    tmpNodeTransform = part.FindModelTransform(tmpFullNodeTransformName);
                if (tmpTransforms.Length == 0) break;
                if (tmpNodeTransformName != string.Empty && tmpNodeTransform == null)
                {
                    Debug.LogError($"[{MODULENAME}] {EngineSetNodeName} no transform found with name \"{tmpNodeTransformName}\"");
                }

                // Fill the object
                var tmpSelectableItem = ScriptableObject.CreateInstance<SelectableItem>();
                tmpSelectableItem.name = tmpFullTransformName;
                tmpSelectableItem.mass = tmpMass;
                if (!State[tmpEngineSet.name].ContainsKey(tmpSelectableItem.name)) 
                    State[tmpEngineSet.name].Add(tmpSelectableItem.name, tmpDefaultState);
                tmpSelectableItem.transformIndexes.AddRange(Enumerable.Range(transforms.Count, tmpTransforms.Length));
                transforms.AddRange(tmpTransforms);
                if (tmpNodeTransformName != string.Empty && tmpNodeTransform != null)
                {
                    tmpSelectableItem.nodeId = tmpFullNodeTransformName;
                    tmpSelectableItem.invertNodeState = tmpInvertNodeState;
                    CreateAttachNodeIfNotExists(NodeId: tmpSelectableItem.nodeId, NodeSize: tmpNodeSize);
                }
                tmpEngineSet.selectableItems.Add(tmpSelectableItem);
                i++;
            }
            Debug.Log($"[{MODULENAME}] {EngineSetNodeName} \"{tmpEngineSet.name}\" found {i} matching transforms");

            // Replace the engineset if it already exists, this should only happen when B9PS calls OnLoad.
            var tmpExistingEngineSetIndex = engineSets.FindIndex(e => e.name == tmpEngineSet.name);
            if (tmpExistingEngineSetIndex != -1) engineSets[tmpExistingEngineSetIndex] = tmpEngineSet;
            else engineSets.Add(tmpEngineSet);

            return vmlResult;
        }
        private bool ParseEngineType(ConfigNode node)
        {
            List<(VmlSeverity, string)> vml = new List<(VmlSeverity, string)>();
            var tmpEngineType = ScriptableObject.CreateInstance<EngineType>();

            // Fill toplevel object
            if (node.HasValue(nameof(tmpEngineType.name)))
                tmpEngineType.name = node.GetValue(nameof(tmpEngineType.name));
            else vml.Add((VmlSeverity.Error, $"{EngineTypeNodeName} \"{tmpEngineType.name}\" has no value {nameof(tmpEngineType.name)}"));

            if (node.HasValue("engineSetName"))
                tmpEngineType.EngineSetNames = node.GetValues("engineSetName").ToList();
            else vml.Add((VmlSeverity.Error, $"{EngineTypeNodeName} \"{tmpEngineType.name}\" has no value(s) for engineSetName"));

            if (node.HasValue("customSelectableName"))
                tmpEngineType.CustomSelectableNames = node.GetValues("customSelectableName").ToList();
            else vml.Add((VmlSeverity.Information, $"{EngineTypeNodeName} \"{tmpEngineType.name}\" has no value(s) for customSelectableName"));

            var vmlResult = LogVml(Vml: vml);
            engineTypes.Add(tmpEngineType);

            return vmlResult;
        }

        private bool LogVml(List<(VmlSeverity type, string msg)> Vml)
        {
            var errors = false;
            foreach (var msg in Vml)
            {
                switch (msg.type)
                {
                    case VmlSeverity.Error:
                        errors = true;
                        Debug.LogError($"[{MODULENAME}] {msg.msg}");
                        break;
                    case VmlSeverity.Warning:
                        Debug.LogWarning($"[{MODULENAME}] {msg.msg}");
                        break;
                    default:
                        Debug.Log($"[{MODULENAME}] {msg.msg}");
                        break;
                }
            }
            return errors == false;
        }
        private void CreateAttachNodeIfNotExists(string NodeId, int NodeSize)
        {
            if (part.FindAttachNode(NodeId) == null)
            {
                ConfigNode node = new ConfigNode();
                node.AddValue("name", NodeId);
                node.AddValue("transform", NodeId);
                node.AddValue("size", NodeSize.ToString());
                node.AddValue("method", "FIXED_JOINT");
                part.AddAttachNode(node);
            }
        }

        private void DrawUI(int windowIndex)
        {
            // Setup
            GUI.DragWindow(new Rect(0, 0, windowRect.width - 20, 20));
            float barButtonWidth = 60;
            float barButtonHeight = 35;
            
            Rect closeButtonRect = new Rect(windowRect.width - 22, 2, 20, 20);
            GUIStyle buttonStyle = GUI.skin.button;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fontSize = 15;
            GUIStyle labelStyle = GUI.skin.label;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUIStyle toggleStyle = GUI.skin.toggle;
            toggleStyle.wordWrap = true;

            GUI.skin.window.padding = new RectOffset(0, 0, 20, 0);

            if (GUI.Button(closeButtonRect, "X", buttonStyle))
            {
                isVisible = false;
            }

            // Main Window
            GUILayout.BeginVertical();
            GUILayout.Space(5);

            // Top Bar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<"
                               , buttonStyle
                               , GUILayout.Width(barButtonWidth)
                               , GUILayout.Height(barButtonHeight)))
            {
                UpdateActiveEngineType(-1);
            }

            GUILayout.Label(engineTypes[activeEngineType].name
                          , labelStyle
                          , GUILayout.Width(uiWidth - 2 * barButtonWidth)
                          , GUILayout.Height(barButtonHeight));

            if (GUILayout.Button(">"
                               , buttonStyle
                               , GUILayout.Width(barButtonWidth)
                               , GUILayout.Height(barButtonHeight)))
            {
                UpdateActiveEngineType(1);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            // Custom Selectables
            customSelectablesScrollPosition = GUILayout.BeginScrollView(customSelectablesScrollPosition
                                                                      , GUI.skin.box
                                                                      , GUILayout.Height(60));
            GUILayout.BeginVertical();
            foreach(var selectable in SelectablesDict[activeEngineType])
            {
                // Worry about disabling based on relationships.
                if (GUILayout.Toggle(true, selectable.guiName, toggleStyle))
                {
                    //State[selectable.name][selectable.name] = !State[selectable.name][selectable.name];
                    Debug.Log("Mock Toggle " + selectable.name);
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.Space(3);

            // Engines
            //Rect canvasRect = GUILayoutUtility.GetRect(windowWidth - GUI.skin.window.border.horizontal*16, windowWidth - GUI.skin.window.border.horizontal*6);
            //GUI.Box(canvasRect, GUIContent.none, GUI.skin.box);
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(uiWidth));
            GUILayout.Label(GUIContent.none);
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                Rect canvasRect = GUILayoutUtility.GetLastRect();
                foreach (var set in EngineSetDict[activeEngineType])
                {
                    for (int i = 0; i < set.selectableItems.Count; i++)
                    {
                        float tmpX = canvasRect.x + canvasRect.width / 2 + EnginePositionsDict[set.name][i].x * uiScale - engineSize / 2;
                        float tmpY = canvasRect.y + canvasRect.height / 2 + EnginePositionsDict[set.name][i].y * uiScale - engineSize / 2;
                        var tmpPosition = new Rect(tmpX, tmpY, engineSize, engineSize);
                        if(GUI.Toggle(tmpPosition, true, GUIContent.none))
                        {
                            Debug.Log($"Mock Toggle {set.name} : {set.selectableItems[i].name}");
                        }
                    }
                }
            }            
            GUILayout.Space(3);

            GUILayout.EndVertical();
        }

        public void UpdateActiveEngineType(int dir)
        {
            activeEngineType = (activeEngineType + dir + engineTypes.Count) % engineTypes.Count;
        }
        #endregion
    }

    #region Custom Data Classes
    // Custom Data Classes
    [Serializable]
    public class CustomSelectable : ScriptableObject
    {
        public string guiName;
        public List<Relationship> relationships = new List<Relationship>();
        public List<SelectableItem> selectableItems = new List<SelectableItem>();
    }

    [Serializable]
    public class EngineSet : ScriptableObject
    {
        public string guiName;
        public string engineId;
        public float singleEngineThrust;
        public float singleEngineMinThrust = 0;

        public List<SelectableItem> selectableItems = new List<SelectableItem>();
    }

    [Serializable]
    public class EngineType : ScriptableObject
    {
        public List<string> EngineSetNames = new List<string>();
        public List<string> CustomSelectableNames = new List<string>();
    }

    [Serializable]
    public class SelectableItem : ScriptableObject
    {
        public float mass = 0f;
        public string nodeId;
        public List<int> transformIndexes = new List<int>();
        public bool invertNodeState = true;
        public bool invertTransformState = false;
    }

    [Serializable]
    public class Relationship : ScriptableObject
    {
        public string referencedGroupName;
        public bool invertRelationship = false;
    }

    public enum VmlSeverity
    {
        Error,
        Warning,
        Information,
    }
    #endregion
}
