using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using TundraExploration.Modules;

namespace StarshipExpansionProject.Modules
{

    public class ModuleSEPEngineGUI : PartModule, IPartMassModifier
    {
        // Inherited Functions
        public float GetModuleMass(float baseMass, ModifierStagingSituation situation)
        {
            return TotalMass;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
        // KSP Settable Variables
        [KSPField]
        public string RBTransformName = "";

        [KSPField]
        public string RBNodeName = "";

        [KSPField]
        public string RC9TransformName = "";

        [KSPField]
        public string RC9NodeName = "";

        [KSPField]
        public string RC13TransformName = "";

        [KSPField]
        public string RC13NodeName = "";

        [KSPField]
        public string RC9HeatshieldTransform = "";

        [KSPField]
        public string RC13HeatshieldTransform = "";

        [KSPField]
        public string RBCover0Transform = "";

        [KSPField]
        public string RBCover1Transform = "";

        [KSPField]
        public string LabelGUIName = "SEP Engine Selection";
        
        [KSPField]
        public string RCHeatshieldGUIName = "RC Heatshield";

        [KSPField]
        public string RBCoversGUIName = "RB Covers";

        [KSPField]
        public float SingleEngineThrust = 1;

        [KSPField]
        public float SingleMinEngineThrust = 0;

		[KSPField]
		public float CenterSingleEngineThrust = -1;

		[KSPField]
		public float CenterSingleMinEngineThrust = -1;

		[KSPField]
		public float MiddleSingleEngineThrust = -1;

		[KSPField]
		public float MiddleSingleMinEngineThrust = -1;

		[KSPField]
        public float SingleEngineMass = 0;

        [KSPField]
        public float RCHeatshieldMass = 0;

        [KSPField]
        public float RBCoverMass = 0;

        [KSPField]
        public string AllEngineID = "";

        [KSPField]
        public string MiddleEngineID = "";

        [KSPField]
        public string CenterEngineID = "";

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "Select Engines")]
        public void Button1()
        {
            GUIenabled = true;
        }

        //}

        // Plugin Variables

        [KSPField(guiActive = false, isPersistant = true)]
        private string EngString29 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX|";

        [KSPField(guiActive = false, isPersistant = true)]
        private string EngString33 = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX|";

        [KSPField(guiActive = false, isPersistant = true)]
        public string GUIMode = "33 Engines";

        private Vector2 GUIPosition = new Vector2(250, 200);

        private Vector2 GUISize = new Vector2(250, 340);

        private Vector2 EngineButtonSize = new Vector2(20, 20);

        public const string MODULENAME = "ModuleSEPEngineGUI";

        private const string ButtonOnString = "X";

        private const string ButtonOffString = "ㅤ";

        private bool DrawOuterRing = true;
        private bool Draw9Ring = false;
        private bool Draw13Ring = true;

        private bool GUIenabled = false;
        private Rect GUIWindowPosition;
        private float TotalMass;

        private string CurrentString = "";

        private GUIStyle styleforbuttons = GUIStyle.none;
        private GUIStyle styleforlabels = GUIStyle.none;

        public List<string> RBoostTransforms;
        public List<string> RBoostNodeTransforms;
        public List<string> RBoostCoverTransforms;
        public List<string> RCTransforms33;
        public List<string> RCNodeTransforms33;
        public List<string> RCTransforms29;
        public List<string> RCNodeTransforms29;

        private bool SwitchActive;
        private int SwitchActiveID;

        public void OnGUI()
        {
            if (GUIenabled)
            {
                //GUI.Window(0, new Rect(GUIPosition.x, GUIPosition.y, GUISize.x, GUISize.y), DrawEngineGUI, LabelGUIName); //rect(horoffset, veroffset, width, height)
                //DrawEngineGUI();
                GUIWindowPosition = GUI.Window(1, GUIWindowPosition, DrawEngineGUI, LabelGUIName);
            }


        }
        public void DrawEngineGUI(int index)
        {
            if (GUIMode == "33 Engines")
            {
                CurrentString = EngString33;
                DrawOuterRing = true;
                Draw9Ring = false;
                Draw13Ring = true;
            }
            else if (GUIMode == "29 Engines")
            {
                CurrentString = EngString29;
                DrawOuterRing = true;
                Draw9Ring = true;
                Draw13Ring = false;
            }

            // Make a background box
            //GUI.Box(new Rect(GUIPosition.x, GUIPosition.y, GUISize.x, GUISize.y), LabelGUIName); //rect(horoffset, veroffset, width, height)

            // Set styles
            styleforbuttons = GUI.skin.button;
            styleforbuttons.alignment = TextAnchor.MiddleCenter;
            styleforbuttons.fontSize = 15;
            styleforlabels = GUI.skin.label;
            styleforlabels.alignment = TextAnchor.MiddleCenter;



            // Make the Exit Button
            if (GUI.Button(new Rect((GUISize.x - EngineButtonSize.x), 0, EngineButtonSize.x, EngineButtonSize.y), "X", styleforbuttons))
            {
                GUIenabled = false;
            }

            // Edit style for buttons
            styleforbuttons.fontSize = 30;

            // Make the 33 - 29 swap button
            if (GUI.Button(new Rect(15, 30, 60, 35), "<"))
            {
                UpdateGUIMode("Left");
            }

            if (GUI.Button(new Rect(-15 - 60 + GUISize.x, 30, 60, 35), ">"))
            {
                UpdateGUIMode("Right");
            }

            GUI.Label(new Rect((GUISize.x / 2) - (150 / 2), 30, 150, 35), GUIMode, styleforlabels);


            // Edit style for buttons
            styleforbuttons.fontSize = 15;

            // Make RBoost Cover switch & RC Heatshield Switch
            if (CurrentString.Substring(0, 20).Contains("X"))
            {
                //Debug.Log($"One or more RBoost's are active");
                GUI.enabled = false;
                if(CurrentString[33].ToString() == ButtonOffString)
                {
                    Debug.Log($"[{MODULENAME}] Forced Re-Enable of the RB Covers due to RB Activation");
                    string tempstring = "";
                    for (int i = 0; i < CurrentString.Count(); i++)
                    {
                        if (i != 33)
                        {
                            tempstring = tempstring + CurrentString[i].ToString();
                        }
                        else
                        {
                            tempstring = tempstring + ButtonOnString;
                        }
                    }
                    CurrentString = tempstring;
                    if (GUIMode == "33 Engines")
                    {
                        EngString33 = CurrentString;
                    }
                    else if (GUIMode == "29 Engines")
                    {
                        EngString29 = CurrentString;
                    }
                    CheckTransformStates();
                }
                
            }

            if (GUI.Button(new Rect(15, 70, EngineButtonSize.x, EngineButtonSize.y), CurrentString[33].ToString()))
            {
                //Button 33
                UpdatedButton(33);
            }
            GUI.enabled = true;
            GUI.Label(new Rect(10 + EngineButtonSize.x, 70, (GUISize.x / 2) - EngineButtonSize.x - 15 , EngineButtonSize.y), RBCoversGUIName , styleforlabels);

            if (GUI.Button(new Rect((GUISize.x / 2) - 5 , 70, EngineButtonSize.x, EngineButtonSize.y), CurrentString[34].ToString()))
            {
                //Button 34
                UpdatedButton(34);
            }
            GUI.Label(new Rect(EngineButtonSize.x + (GUISize.x / 2) + 5, 70, (GUISize.x / 2) - EngineButtonSize.x - 15, EngineButtonSize.y), RCHeatshieldGUIName, styleforlabels);


            // Make the outer ring
            if (DrawOuterRing)
            {
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 16, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 99, EngineButtonSize.x, EngineButtonSize.y), CurrentString[0].ToString()))
                {
                    //Button 0
                    UpdatedButton(0);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 45, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 89, EngineButtonSize.x, EngineButtonSize.y), CurrentString[1].ToString()))
                {
                    //Button 1
                    UpdatedButton(1);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 71, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 71, EngineButtonSize.x, EngineButtonSize.y), CurrentString[2].ToString()))
                {
                    //Button 2
                    UpdatedButton(2);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 89, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 45, EngineButtonSize.x, EngineButtonSize.y), CurrentString[3].ToString()))
                {
                    //Button 3
                    UpdatedButton(3);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 99, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 16, EngineButtonSize.x, EngineButtonSize.y), CurrentString[4].ToString()))
                {
                    //Button 4
                    UpdatedButton(4);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 99, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 16, EngineButtonSize.x, EngineButtonSize.y), CurrentString[5].ToString()))
                {
                    //Button 5
                    UpdatedButton(5);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 89, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 45, EngineButtonSize.x, EngineButtonSize.y), CurrentString[6].ToString()))
                {
                    //Button 6
                    UpdatedButton(6);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 71, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 71, EngineButtonSize.x, EngineButtonSize.y), CurrentString[7].ToString()))
                {
                    //Button 7
                    UpdatedButton(7);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 45, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 89, EngineButtonSize.x, EngineButtonSize.y), CurrentString[8].ToString()))
                {
                    //Button 8
                    UpdatedButton(8);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 16, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 99, EngineButtonSize.x, EngineButtonSize.y), CurrentString[9].ToString()))
                {
                    //Button 9
                    UpdatedButton(9);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 16, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 99, EngineButtonSize.x, EngineButtonSize.y), CurrentString[10].ToString()))
                {
                    //Button 10
                    UpdatedButton(10);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 45, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 89, EngineButtonSize.x, EngineButtonSize.y), CurrentString[11].ToString()))
                {
                    //Button 11
                    UpdatedButton(11);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 71, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 71, EngineButtonSize.x, EngineButtonSize.y), CurrentString[12].ToString()))
                {
                    //Button 12
                    UpdatedButton(12);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 89, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 45, EngineButtonSize.x, EngineButtonSize.y), CurrentString[13].ToString()))
                {
                    //Button 13
                    UpdatedButton(13);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 99, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 16, EngineButtonSize.x, EngineButtonSize.y), CurrentString[14].ToString()))
                {
                    //Button 14
                    UpdatedButton(14);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 99, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 16, EngineButtonSize.x, EngineButtonSize.y), CurrentString[15].ToString()))
                {
                    //Button 15
                    UpdatedButton(15);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 89, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 45, EngineButtonSize.x, EngineButtonSize.y), CurrentString[16].ToString()))
                {
                    //Button 16
                    UpdatedButton(16);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 71, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 71, EngineButtonSize.x, EngineButtonSize.y), CurrentString[17].ToString()))
                {
                    //Button 17
                    UpdatedButton(17);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 45, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 89, EngineButtonSize.x, EngineButtonSize.y), CurrentString[18].ToString()))
                {
                    //Button 18
                    UpdatedButton(18);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 16, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 99, EngineButtonSize.x, EngineButtonSize.y), CurrentString[19].ToString()))
                {
                    //Button 19
                    UpdatedButton(19);
                }
            }

            // Make the 29 Version ring & center
            if (Draw9Ring)
            {
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 20, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 47, EngineButtonSize.x, EngineButtonSize.y), CurrentString[20].ToString()))
                {
                    //Button 20
                    UpdatedButton(20);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 47, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 20, EngineButtonSize.x, EngineButtonSize.y), CurrentString[21].ToString()))
                {
                    //Button 21
                    UpdatedButton(21);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 47, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 20, EngineButtonSize.x, EngineButtonSize.y), CurrentString[22].ToString()))
                {
                    //Button 22
                    UpdatedButton(22);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 20, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 47, EngineButtonSize.x, EngineButtonSize.y), CurrentString[23].ToString()))
                {
                    //Button 23
                    UpdatedButton(23);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 20, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 47, EngineButtonSize.x, EngineButtonSize.y), CurrentString[24].ToString()))
                {
                    //Button 24
                    UpdatedButton(24);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 47, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 20, EngineButtonSize.x, EngineButtonSize.y), CurrentString[25].ToString()))
                {
                    //Button 25
                    UpdatedButton(25);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 47, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 20, EngineButtonSize.x, EngineButtonSize.y), CurrentString[26].ToString()))
                {
                    //Button 26
                    UpdatedButton(26);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 20, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 47, EngineButtonSize.x, EngineButtonSize.y), CurrentString[27].ToString()))
                {
                    //Button 27
                    UpdatedButton(27);
                }




                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 0, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 0, EngineButtonSize.x, EngineButtonSize.y), CurrentString[28].ToString()))
                {
                    //Button 28
                    UpdatedButton(28);
                }
            }

            // Make the 33 Version ring & center
            if (Draw13Ring)
            {
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 0, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 61, EngineButtonSize.x, EngineButtonSize.y), CurrentString[20].ToString()))
                {
                    //Button 20
                    UpdatedButton(20);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 36, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 49, EngineButtonSize.x, EngineButtonSize.y), CurrentString[21].ToString()))
                {
                    //Button 21
                    UpdatedButton(21);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 58, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 19, EngineButtonSize.x, EngineButtonSize.y), CurrentString[22].ToString()))
                {
                    //Button 22
                    UpdatedButton(22);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 58, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 19, EngineButtonSize.x, EngineButtonSize.y), CurrentString[23].ToString()))
                {
                    //Button 23
                    UpdatedButton(23);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 36, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 49, EngineButtonSize.x, EngineButtonSize.y), CurrentString[24].ToString()))
                {
                    //Button 24
                    UpdatedButton(24);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 0, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 61, EngineButtonSize.x, EngineButtonSize.y), CurrentString[25].ToString()))
                {
                    //Button 25
                    UpdatedButton(25);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 36, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 49, EngineButtonSize.x, EngineButtonSize.y), CurrentString[26].ToString()))
                {
                    //Button 26
                    UpdatedButton(26);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 58, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 19, EngineButtonSize.x, EngineButtonSize.y), CurrentString[27].ToString()))
                {
                    //Button 27
                    UpdatedButton(27);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 58, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 19, EngineButtonSize.x, EngineButtonSize.y), CurrentString[28].ToString()))
                {
                    //Button 28
                    UpdatedButton(28);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 36, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 49, EngineButtonSize.x, EngineButtonSize.y), CurrentString[29].ToString()))
                {
                    //Button 29
                    UpdatedButton(29);
                }




                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 0, (GUISize.y - 125) - (EngineButtonSize.y / 2) - 22, EngineButtonSize.x, EngineButtonSize.y), CurrentString[30].ToString()))
                {
                    //Button 30
                    UpdatedButton(30);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) + 19, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 11, EngineButtonSize.x, EngineButtonSize.y), CurrentString[31].ToString()))
                {
                    //Button 31
                    UpdatedButton(31);
                }
                if (GUI.Button(new Rect((GUISize.x / 2) - (EngineButtonSize.x / 2) - 19, (GUISize.y - 125) - (EngineButtonSize.y / 2) + 11, EngineButtonSize.x, EngineButtonSize.y), CurrentString[32].ToString()))
                {
                    //Button 32
                    UpdatedButton(32);
                }
            }
            // Make Window Draggable
            GUI.DragWindow();
        }
        public void UpdatedButton(int ButtonNumber)
        {
            Debug.Log($"[{MODULENAME}] Received Button: {ButtonNumber}");         
            string tempstring = "";
            for (int i = 0; i < CurrentString.Length; i++)
            {
                if (i != ButtonNumber)
                {
                    tempstring = tempstring + CurrentString[i].ToString();
                }
                else
                {
                    if (CurrentString[ButtonNumber].ToString() == ButtonOnString)
                    {
                        tempstring = tempstring + ButtonOffString;
                    }
                    else if (CurrentString[ButtonNumber].ToString() == ButtonOffString)
                    {
                        tempstring = tempstring + ButtonOnString;
                    }
                    
                }
                
                
                //Debug.Log($"[{MODULENAME}] Appended: {CurrentString[i]} to make {tempstring}");
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                tempstring = "";
                if (ButtonNumber >= 0 && ButtonNumber <= 19)
                {
                    if (CurrentString[ButtonNumber].ToString() == ButtonOnString)
                    {
                        for (int i = 0; i <= 19; i++)
                        {
                            tempstring = tempstring + ButtonOffString;
                        }

                    }
                    else if (CurrentString[ButtonNumber].ToString() == ButtonOffString)
                    {
                        for (int i = 0; i <= 19; i++)
                        {
                            tempstring = tempstring + ButtonOnString;
                        }
                    }
                    for (int i = 20; i < CurrentString.Length; i++)
                    {
                        tempstring = tempstring + CurrentString[i].ToString();
                    }

                }
                else {
                    for (int i = 0; i < RBoostTransforms.Count; i++)
                    {
                        tempstring = tempstring + CurrentString[i].ToString();
                    }
                    if (GUIMode == "33 Engines")
                    {
                        if (ButtonNumber >= 20 && ButtonNumber <= 29)
                        {
                            if (CurrentString[ButtonNumber].ToString() == ButtonOnString)
                            {
                                for (int i = 20; i <= 29; i++)
                                {
                                    tempstring = tempstring + ButtonOffString;
                                }

                            }
                            else if (CurrentString[ButtonNumber].ToString() == ButtonOffString)
                            {
                                for (int i = 20; i <= 29; i++)
                                {
                                    tempstring = tempstring + ButtonOnString;
                                }
                            }
                            for (int i = 30; i < CurrentString.Count(); i++)
                            {
                                tempstring = tempstring + CurrentString[i].ToString();
                            }
                        }
                        else if (ButtonNumber >= 30 && ButtonNumber <= 32)
                        {
                            for (int i = 20; i < 30; i++)
                            {
                                tempstring = tempstring + CurrentString[i].ToString();
                            }
                            if (CurrentString[ButtonNumber].ToString() == ButtonOnString)
                            {
                                for (int i = 30; i <= 32; i++)
                                {
                                    tempstring = tempstring + ButtonOffString;
                                }

                            }
                            else if (CurrentString[ButtonNumber].ToString() == ButtonOffString)
                            {
                                for (int i = 30; i <= 32; i++)
                                {
                                    tempstring = tempstring + ButtonOnString;
                                }
                            }
                            for (int i = 33; i < CurrentString.Count(); i++)
                            {
                                tempstring = tempstring + CurrentString[i].ToString();
                            }
                        }
                        else
                        {

                            for (int i = 20; i < CurrentString.Count(); i++)
                            {
                                if (i != ButtonNumber)
                                {
                                    tempstring = tempstring + CurrentString[i].ToString();
                                }
                                else
                                {
                                    if (CurrentString[ButtonNumber].ToString() == ButtonOnString)
                                    {
                                        tempstring = tempstring + ButtonOffString;
                                    }
                                    else if (CurrentString[ButtonNumber].ToString() == ButtonOffString)
                                    {
                                        tempstring = tempstring + ButtonOnString;
                                    }

                                }
                                //tempstring = tempstring + CurrentString[i].ToString();
                            }
                        }
                    }
                    else if (GUIMode == "29 Engines")
                    {
                        if (ButtonNumber >= 20 && ButtonNumber <= 27)
                        {
                            if (CurrentString[ButtonNumber].ToString() == ButtonOnString)
                            {
                                for (int i = 20; i <= 27; i++)
                                {
                                    tempstring = tempstring + ButtonOffString;
                                }

                            }
                            else if (CurrentString[ButtonNumber].ToString() == ButtonOffString)
                            {
                                for (int i = 20; i <= 27; i++)
                                {
                                    tempstring = tempstring + ButtonOnString;
                                }
                            }
                            for (int i = 28; i < CurrentString.Count(); i++)
                            {
                                tempstring = tempstring + CurrentString[i].ToString();
                            }
                        }
                        else
                        {

                            for (int i = 20; i < CurrentString.Count(); i++)
                            {
                                if (i != ButtonNumber)
                                {
                                    tempstring = tempstring + CurrentString[i].ToString();
                                }
                                else
                                {
                                    if (CurrentString[ButtonNumber].ToString() == ButtonOnString)
                                    {
                                        tempstring = tempstring + ButtonOffString;
                                    }
                                    else if (CurrentString[ButtonNumber].ToString() == ButtonOffString)
                                    {
                                        tempstring = tempstring + ButtonOnString;
                                    }

                                }
                                //tempstring = tempstring + CurrentString[i].ToString();
                            }
                        }
                    }
                }
            }

            //Debug.Log($"[{MODULENAME}] Stored String: {tempstring}");
            CurrentString = tempstring;
            if (GUIMode == "33 Engines")
            {
                EngString33 = CurrentString;
            }
            else if (GUIMode == "29 Engines")
            {
                EngString29 = CurrentString;
            }
            //InitialiseLists();
            //Debug.Log(RBoostTransforms[0]);
            CheckTransformStates();
        }

        public void UpdateGUIMode(string inputstring)
        {
            if (GUIMode == "33 Engines" && inputstring == "Right")
            {
                GUIMode = "29 Engines";
            }
            else if (GUIMode == "33 Engines" && inputstring == "Left")
            {
                GUIMode = "29 Engines";
            }
            else if (GUIMode == "29 Engines" && inputstring == "Right")
            {
                GUIMode = "33 Engines";
            }
            else if (GUIMode == "29 Engines" && inputstring == "Left")
            {
                GUIMode = "33 Engines";
            }

            if (GUIMode == "33 Engines")
            {
                CurrentString = EngString33;
            }
            else if (GUIMode == "29 Engines")
            {
                CurrentString = EngString29;
            }
            CheckTransformStates();
        }
        public void CheckTransformStates()
        {
            // RBoost Section
            for (int i = 0; i < RBoostTransforms.Count; i++)
            {
                if (CurrentString[i].ToString() == ButtonOnString)
                {
                    UpdateTransformState(RBoostTransforms[i], true, false);
                    UpdateTransformState(RBoostNodeTransforms[i], false, true);
                }
                else if (CurrentString[i].ToString() == ButtonOffString)
                {
                    UpdateTransformState(RBoostTransforms[i], false, false);
                    UpdateTransformState(RBoostNodeTransforms[i], true, true);
                }
            }
            if (CurrentString[33].ToString() == ButtonOnString)
            {
                for (int i = 0; i < RBoostCoverTransforms.Count; i++)
                {
                    UpdateTransformState(RBoostCoverTransforms[i], true, false);
                }
            }
            else if (CurrentString[33].ToString() == ButtonOffString)
            {
                for (int i = 0; i < RBoostCoverTransforms.Count; i++)
                {
                    UpdateTransformState(RBoostCoverTransforms[i], false, false);
                }
            }
            // RC Section
            if (GUIMode == "33 Engines")
            {
                for (int i = 0; i < RCTransforms29.Count; i++)
                {
                    UpdateTransformState(RCTransforms29[i], false, false);
                    UpdateTransformState(RCNodeTransforms29[i], false, true);
                }
                UpdateTransformState(RC9HeatshieldTransform, false, false);
                if (CurrentString[34].ToString() == ButtonOnString)
                {
                    UpdateTransformState(RC13HeatshieldTransform, true, false);
                }
                else if (CurrentString[34].ToString() == ButtonOffString)
                {
                    UpdateTransformState(RC13HeatshieldTransform, false, false);
                }
                for (int i = 20; i < RBoostTransforms.Count + RCTransforms33.Count; i++)
                {
                    if (CurrentString[i].ToString() == ButtonOnString)
                    {
                        UpdateTransformState(RCTransforms33[i - RBoostTransforms.Count], true, false);
                        UpdateTransformState(RCNodeTransforms33[i - RBoostTransforms.Count], false, true);
                    }
                    else if (CurrentString[i].ToString() == ButtonOffString)
                    {
                        UpdateTransformState(RCTransforms33[i - RBoostTransforms.Count], false, false);
                        UpdateTransformState(RCNodeTransforms33[i - RBoostTransforms.Count], true, true);
                    }
                }
            }
            else if (GUIMode == "29 Engines")
            {
                for (int i = 0; i < RCTransforms33.Count; i++)
                {
                    UpdateTransformState(RCTransforms33[i], false, false);
                    UpdateTransformState(RCNodeTransforms33[i], false, true);
                }
                UpdateTransformState(RC13HeatshieldTransform, false, false);
                if (CurrentString[34].ToString() == ButtonOnString)
                {
                    UpdateTransformState(RC9HeatshieldTransform, true, false);
                }
                else if (CurrentString[34].ToString() == ButtonOffString)
                {
                    UpdateTransformState(RC9HeatshieldTransform, false, false);
                }
                for (int i = 20; i < RBoostTransforms.Count + RCTransforms29.Count; i++)
                {
                    if (CurrentString[i].ToString() == ButtonOnString)
                    {
                        UpdateTransformState(RCTransforms29[i - RBoostTransforms.Count], true, false);
                        UpdateTransformState(RCNodeTransforms29[i - RBoostTransforms.Count], false, true);
                    }
                    else if (CurrentString[i].ToString() == ButtonOffString)
                    {
                        UpdateTransformState(RCTransforms29[i - RBoostTransforms.Count], false, false);
                        UpdateTransformState(RCNodeTransforms29[i - RBoostTransforms.Count], true, true);
                    }
                }
            }
            CheckEngineCount();
        }

        public void CheckEngineCount()
        {
            int RBCount = 0;
            int MiddleRCCount = 0;
            int CenterRCCount = 0;
            //RBoost Section
            for (int i = 0; i < RBoostTransforms.Count; i++)
            {
                if (CurrentString[i].ToString() == ButtonOnString)
                {
                    RBCount++;
                }
            }
            if (GUIMode == "33 Engines")
            {
                //Inner RC Section
                for (int i = RBoostTransforms.Count + 10; i < RBoostTransforms.Count + 13; i++)
                {
                    if (CurrentString[i].ToString() == ButtonOnString)
                    {
                        CenterRCCount++;
                    }
                }
                //Middle RC Section
                for (int i = RBoostTransforms.Count; i < RBoostTransforms.Count + 10; i++)
                {
                    if (CurrentString[i].ToString() == ButtonOnString)
                    {
                        MiddleRCCount++;
                    }
                }

            }
            else if (GUIMode == "29 Engines")
            {
                //Inner RC Section
                if (CurrentString[RBoostTransforms.Count + 0].ToString() == ButtonOnString)
                {
                    CenterRCCount++;
                }
                if (CurrentString[RBoostTransforms.Count + 4].ToString() == ButtonOnString)
                {
                    CenterRCCount++;
                }
                if (CurrentString[RBoostTransforms.Count + 8].ToString() == ButtonOnString)
                {
                    CenterRCCount++;
                }


                //Middle RC Section
                if (CurrentString[RBoostTransforms.Count + 1].ToString() == ButtonOnString)
                {
                    MiddleRCCount++;
                }
                if (CurrentString[RBoostTransforms.Count + 2].ToString() == ButtonOnString)
                {
                    MiddleRCCount++;
                }
                if (CurrentString[RBoostTransforms.Count + 3].ToString() == ButtonOnString)
                {
                    MiddleRCCount++;
                }
                if (CurrentString[RBoostTransforms.Count + 5].ToString() == ButtonOnString)
                {
                    MiddleRCCount++;
                }
                if (CurrentString[RBoostTransforms.Count + 6].ToString() == ButtonOnString)
                {
                    MiddleRCCount++;
                }
                if (CurrentString[RBoostTransforms.Count + 7].ToString() == ButtonOnString)
                {
                    MiddleRCCount++;
                }
            }
            //Debug.Log($"[{MODULENAME}] Found {RBCount} Raptor Boosts, Found {MiddleRCCount} Middle RCs, Found {CenterRCCount} Center RCs");

            UpdateEngineThrust(RBCount, MiddleRCCount, CenterRCCount);
            UpdateEngineMass(RBCount, MiddleRCCount, CenterRCCount);
            
        }
        private void TundraPresent(int RBCount, int MiddleRCCount, int CenterRCCount)
        {
            List<ModuleSEPEngineSwitch> EngineSwitchModules = part.Modules.GetModules<ModuleSEPEngineSwitch>();
            if (EngineSwitchModules.Count != 0)
            {
                for (int i = 0; i < EngineSwitchModules.Count; i++)
                {
                    Debug.Log($"[{MODULENAME}] SEP State: {EngineSwitchModules[i].selectedIndex}");
                    if (RBCount + MiddleRCCount + CenterRCCount  == 0 && EngineSwitchModules[i].isEnabled)
                    {
                        EngineSwitchModules[i].isEnabled = false;
                        EngineSwitchModules[i].SetStagingState(false);
                    }
                    else if (RBCount + MiddleRCCount + CenterRCCount != 0 && !EngineSwitchModules[i].isEnabled)
                    {
                        EngineSwitchModules[i].isEnabled = true;
                        EngineSwitchModules[i].SetStagingState(true);
                    }
                    SwitchActive = EngineSwitchModules[i].isEnabled;
                    SwitchActiveID = EngineSwitchModules[i].selectedIndex;
                }

            }
        }

        private void TundraNotPresent(int RBCount, int MiddleRCCount, int CenterRCCount)
        {
            List<MultiModeEngine> EngineSwitchModules = part.Modules.GetModules<MultiModeEngine>();
            if (EngineSwitchModules.Count != 0)
            {
                for (int i = 0; i < EngineSwitchModules.Count; i++)
                {
                    if (RBCount + MiddleRCCount + CenterRCCount == 0 && EngineSwitchModules[i].isEnabled)
                    {
                        EngineSwitchModules[i].isEnabled = false;
                    }
                    else if (RBCount + MiddleRCCount + CenterRCCount != 0 && !EngineSwitchModules[i].isEnabled)
                    {
                        EngineSwitchModules[i].isEnabled = true;

                    }
                    SwitchActive = EngineSwitchModules[i].isEnabled;
                    if (EngineSwitchModules[i].mode == EngineSwitchModules[0].primaryEngineID)
                    {
                        SwitchActiveID = 0;
                    }
                    else if (EngineSwitchModules[i].mode == EngineSwitchModules[0].secondaryEngineID)
                    {
                        SwitchActiveID = 1;
                    }
                }

            }
        }

        public void UpdateEngineThrust(int RBCount, int MiddleRCCount, int CenterRCCount)
        {            
            try
            {
                TundraPresent(RBCount, MiddleRCCount, CenterRCCount);                
            }
            catch (Exception ex)
            {
                TundraNotPresent(RBCount, MiddleRCCount, CenterRCCount);
            }
            List<ModuleEnginesFX> EngineModules = part.Modules.GetModules<ModuleEnginesFX>();
			float CenterEngineThrust = (float)(CenterRCCount * (CenterSingleEngineThrust > 0 ? CenterSingleEngineThrust : SingleEngineThrust));
			float MiddleEngineThrust = (float)(MiddleRCCount * (MiddleSingleEngineThrust > 0 ? MiddleSingleEngineThrust : SingleEngineThrust));
			float RBEngineThrust = (float)(RBCount * SingleEngineThrust);

			float CenterEngineMinThrust = (float)(CenterRCCount * (CenterSingleMinEngineThrust > 0 ? CenterSingleMinEngineThrust : SingleMinEngineThrust));
			float MiddleEngineMinThrust = (float)(MiddleRCCount * (MiddleSingleMinEngineThrust > 0 ? MiddleSingleMinEngineThrust : SingleMinEngineThrust));
			float RBEngineMinThrust = (float)(RBCount * SingleEngineThrust);
            for (int i = 0; i < EngineModules.Count; i++)
            {
                Debug.Log($"[{MODULENAME}] Updating Thrust for engine with ID: {EngineModules[i].engineID}");
                if (EngineModules[i].engineID == CenterEngineID)
                {
                    EngineModules[i].maxThrust = CenterEngineThrust; //Set maxthrust for mechjeb PVG
                    EngineModules[i].maxFuelFlow = (float)(CenterEngineThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);
                    if (CenterSingleMinEngineThrust > 0) EngineModules[i].minFuelFlow = (float)(CenterEngineMinThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);
				}
                else if (EngineModules[i].engineID == MiddleEngineID)
                {
					EngineModules[i].maxThrust = MiddleEngineThrust; //Set maxthrust for mechjeb PVG
					EngineModules[i].maxFuelFlow = (float)(MiddleEngineThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);
                    if (MiddleSingleMinEngineThrust > 0) EngineModules[i].minFuelFlow = (float)(MiddleEngineMinThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);
				}
				else if (EngineModules[i].engineID == AllEngineID)
				{
					EngineModules[i].maxThrust = RBEngineThrust; //Set maxthrust for mechjeb PVG
					EngineModules[i].maxFuelFlow = (float)(RBEngineThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);
                    if (SingleMinEngineThrust > 0) EngineModules[i].minFuelFlow = (float)(RBEngineMinThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);
				}

				//if (EngineModules[i].engineID == CenterEngineID || EngineModules[i].engineID == MiddleEngineID) { RBEngineThrust = 0; RBEngineMinThrust = 0; }
    //            if (EngineModules[i].engineID == CenterEngineID) { MiddleEngineThrust = 0; MiddleEngineMinThrust = 0; }

				//float TotalThrust = CenterEngineThrust + MiddleEngineThrust + RBEngineThrust;
    //            float TotalMinThrust = CenterEngineMinThrust + MiddleEngineMinThrust + RBEngineMinThrust;

				//EngineModules[i].maxThrust = TotalThrust; //Set maxthrust for mechjeb PVG

    //            EngineModules[i].maxFuelFlow = (float) (TotalThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);

    //            if (SingleMinEngineThrust > 0 || MiddleSingleMinEngineThrust > 0 || CenterSingleMinEngineThrust > 0)
				//	EngineModules[i].minFuelFlow = (float)(TotalMinThrust / EngineModules[i].atmosphereCurve.Curve.keys[0].value / 9.80665);

				if (RBCount + MiddleRCCount + CenterRCCount == 0)
				{
					EngineModules[i].isEnabled = false;
					EngineModules[i].SetStagingState(false);
				}
				else if (!EngineModules[i].isEnabled && SwitchActive && SwitchActiveID == i)
				{
					EngineModules[i].isEnabled = true;
					EngineModules[i].SetStagingState(true);
				}
            }
        }

        public void UpdateEngineMass(int RBCount, int MiddleRCCount, int CenterRCCount)
        {
            TotalMass = 0;
            TotalMass = TotalMass + (RBCount + MiddleRCCount + CenterRCCount) * SingleEngineMass;
            if (CurrentString[33].ToString() == ButtonOnString) // RB Cover
            {
                TotalMass = TotalMass + RBCoverMass;
            }
            if (CurrentString[34].ToString() == ButtonOnString) // RC Cover
            {
                TotalMass = TotalMass + RCHeatshieldMass;
            }
            Debug.Log($"[{MODULENAME}] Updating mass to: {TotalMass}");
            part.UpdateMass();
        }

        public void UpdateTransformState(string transformname, bool TransformState, bool IsNode)
        {
            if (transformname != "")
            {
                if (part.FindModelTransforms(transformname).Length > 0)
                {
                    foreach (Transform t in part.FindModelTransforms(transformname))
                    {
                        if (TransformState)
                        {
                            t.gameObject.SetActive(true);
                        }
                        else if (!TransformState)
                        {
                            t.gameObject.SetActive(false);
                        }
                    }
                    if (IsNode)
                    {
                        foreach (AttachNode t in part.attachNodes)
                        {
                            if (TransformState && t.nodeTransform.name == transformname)
                            {
                                //Debug.Log($"Found the node");
                                t.nodeType = AttachNode.NodeType.Stack;
                                t.radius = 0.4f;
                            }
                            else if (!TransformState && t.nodeTransform.name == transformname)
                            {
                                //Debug.Log($"Found the node");
                                t.nodeType = AttachNode.NodeType.Dock;
                                t.radius = 1f / 1000f;
                            }
                        }
                    }

                }
                else
                {
                    Debug.LogError($"[{MODULENAME}] Transform: {transformname} not found on {part}!");
                }
            }
            else
            {
                Debug.LogError($"[{MODULENAME}] transformname or nodetransformname not set!");
            }
        }

        public void InitialiseLists()
        {
            if (true) // RBoost Section
            {
                RBoostTransforms = new List<string>();
                for (int i = 0; i >= 0; i++)
                {
                    if (part.FindModelTransforms(RBTransformName + i.ToString()).Length > 0)
                    {
                        //Debug.Log($"Engine found name is: {RBTransformName + i.ToString()}");
                        RBoostTransforms.Add(RBTransformName + i.ToString());
                    }
                    else
                    {
                        //Debug.Log($"No more engines found last one was: {i-1}");
                        break;
                    }
                }
                //Debug.Log($"Out of loop");

                RBoostNodeTransforms = new List<string>();
                for (int i = 0; i >= 0; i++)
                {
                    if (part.FindModelTransforms(RBNodeName + i.ToString()).Length > 0)
                    {
                        //Debug.Log($"Node found name is: {RBNodeName + i.ToString()}");
                        RBoostNodeTransforms.Add(RBNodeName + i.ToString());
                    }
                    else
                    {
                        //Debug.Log($"No more nodes found last one was: {i - 1}");
                        break;
                    }
                }
                //Debug.Log($"Out of loop");
                

                RBoostCoverTransforms = new List<string>();
                //for (int i = 0; i < 15; i++)
                //{
                //    RBoostCoverTransforms.Add("");
                //}
                RBoostCoverTransforms.Add(RBCover0Transform);
                RBoostCoverTransforms.Add(RBCover1Transform);

            }
            if (true) // RC 33 Section
            {
                RCTransforms33 = new List<string>();
                for (int i = 0; i >= 0; i++)
                {
                    if (part.FindModelTransforms(RC13TransformName + i.ToString()).Length > 0)
                    {
                        //Debug.Log($"Engine found name is: {RBTransformName + i.ToString()}");
                        RCTransforms33.Add(RC13TransformName + i.ToString());
                    }
                    else
                    {
                        //Debug.Log($"No more engines found last one was: {i-1}");
                        break;
                    }
                }

                RCNodeTransforms33 = new List<string>();
                for (int i = 0; i >= 0; i++)
                {
                    if (part.FindModelTransforms(RC13NodeName + i.ToString()).Length > 0)
                    {
                        //Debug.Log($"Engine found name is: {RBTransformName + i.ToString()}");
                        RCNodeTransforms33.Add(RC13NodeName + i.ToString());
                    }
                    else
                    {
                        //Debug.Log($"No more engines found last one was: {i-1}");
                        break;
                    }
                }

            }
            if (true) // RC 29 Section
            {
                RCTransforms29 = new List<string>();
                for (int i = 0; i >= 0; i++)
                {
                    if (part.FindModelTransforms(RC9TransformName + i.ToString()).Length > 0)
                    {
                        //Debug.Log($"Engine found name is: {RBTransformName + i.ToString()}");
                        RCTransforms29.Add(RC9TransformName + i.ToString());
                    }
                    else
                    {
                        //Debug.Log($"No more engines found last one was: {i-1}");
                        break;
                    }
                }

                RCNodeTransforms29 = new List<string>();
                for (int i = 0; i >= 0; i++)
                {
                    if (part.FindModelTransforms(RC9NodeName + i.ToString()).Length > 0)
                    {
                        //Debug.Log($"Engine found name is: {RBTransformName + i.ToString()}");
                        RCNodeTransforms29.Add(RC9NodeName + i.ToString());
                    }
                    else
                    {
                        //Debug.Log($"No more engines found last one was: {i-1}");
                        break;
                    }
                }
            }
            Debug.Log($"[{MODULENAME}] Loaded Transform Names into their respective lists");
            if (CurrentString == "")
            {
                if (GUIMode == "33 Engines")
                {
                    CurrentString = EngString33;
                    Debug.Log($"[{MODULENAME}] Updated CurrentString from: EngString33");
                }
                else if (GUIMode == "29 Engines")
                {
                    CurrentString = EngString29;
                    Debug.Log($"[{MODULENAME}] Updated CurrentString from: EngString29");
                }
            }
            
        }
        public void CreateNodes(int NodeSize)
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING)
            {
                foreach (String Tname in RBoostNodeTransforms)
                {
                    bool NodeExists = false;
                    foreach (AttachNode n in part.attachNodes)
                    {
                        if (n.nodeTransform.name == Tname)
                        {
                            NodeExists = true;
                        }
                    }
                    if (NodeExists)
                    {
                        //Debug.Log($"{MODULENAME} Node {Tname} Already Exists, Skipping");
                    }
                    else
                    {
                        ConfigNode TemporaryNode = new ConfigNode();
                        TemporaryNode.AddValue("name", Tname);
                        TemporaryNode.AddValue("transform", Tname);
                        TemporaryNode.AddValue("size", "2");
                        TemporaryNode.AddValue("method", "FIXED_JOINT");
                        part.AddAttachNode(TemporaryNode);
                        //Debug.Log($"{MODULENAME} Adding Node {Tname}");
                    }
                }

                foreach (String Tname in RCNodeTransforms29)
                {
                    bool NodeExists = false;
                    foreach (AttachNode n in part.attachNodes)
                    {
                        if (n.nodeTransform.name == Tname)
                        {
                            NodeExists = true;
                        }
                    }
                    if (NodeExists)
                    {
                        //Debug.Log($"{MODULENAME} Node {Tname} Already Exists, Skipping");
                    }
                    else
                    {
                        ConfigNode TemporaryNode = new ConfigNode();
                        TemporaryNode.AddValue("name", Tname);
                        TemporaryNode.AddValue("transform", Tname);
                        TemporaryNode.AddValue("size", NodeSize.ToString());
                        TemporaryNode.AddValue("method", "FIXED_JOINT");
                        part.AddAttachNode(TemporaryNode);
                        //Debug.Log($"{MODULENAME} Adding Node {Tname}");
                    }
                }

                foreach (String Tname in RCNodeTransforms33)
                {
                    bool NodeExists = false;
                    foreach (AttachNode n in part.attachNodes)
                    {
                        if (n.nodeTransform.name == Tname)
                        {
                            NodeExists = true;
                        }
                    }
                    if (NodeExists)
                    {
                        //Debug.Log($"{MODULENAME} Node {Tname} Already Exists, Skipping");
                    }
                    else
                    {
                        ConfigNode TemporaryNode = new ConfigNode();
                        TemporaryNode.AddValue("name", Tname);
                        TemporaryNode.AddValue("transform", Tname);
                        TemporaryNode.AddValue("size", NodeSize.ToString());
                        TemporaryNode.AddValue("method", "FIXED_JOINT");
                        part.AddAttachNode(TemporaryNode);
                        //Debug.Log($"{MODULENAME} Adding Node {Tname}");
                    }
                }
                Debug.Log($"[{MODULENAME}] Added Nodes to their respective transforms");
            }

        }
        public void Start()
        {
            InitialiseLists();
            CheckTransformStates();
            GUIWindowPosition = new Rect(GUIPosition.x, GUIPosition.y, GUISize.x, GUISize.y);
            GUIenabled = false;
        }
        public override void OnLoad(ConfigNode val)
        {
            InitialiseLists();
            CreateNodes(2);
            CheckTransformStates();
            GUIenabled = false;
        }
        
        // StackSymmetry changes have to run not when the engine gui is changed but regularly, so I must use Update unfortunately.
        public void Update()
        {
            // Making sure the player is within the editor scene to reduce unnecessary calls to this function
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                // Only run if necessary, don't go changing the part symmetry to what it already is each frame!
                if (part.stackSymmetry == EditorLogic.fetch.symmetryMode) return;
                part.stackSymmetry = EditorLogic.fetch.symmetryMode;
            }
        }
    }
}