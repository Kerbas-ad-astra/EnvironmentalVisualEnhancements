﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Utils;

namespace EVEManager
{
    public delegate void SceneChangeEvent(GameScenes scene);

    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class GlobalEVEManager : MonoBehaviour
    {
        private static List<EVEManagerBase> Managers = null;

        protected bool guiLoad { get { return HighLogic.LoadedScene == GameScenes.MAINMENU || HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION; } }

        private static bool useEditor = false;
        

        private Texture2D ToolTipBackground;

        private void Awake()
        {
            useEditor = false;
            ToolTipBackground = new Texture2D(4, 4);
            List<Color> colors = new List<Color>();
            for(int x = 0; x < ToolTipBackground.width* ToolTipBackground.height; x++)
            {
                colors.Add(Color.white);
            }
            ToolTipBackground.SetPixels(colors.ToArray());

            if(HighLogic.LoadedScene == GameScenes.LOADING && Managers == null)
            {
                Managers = EVEManagerBase.GetManagers();
            }
            

            Setup(false);
            StartCoroutine(SetupDelay());
        }


        IEnumerator SetupDelay()
        {
            yield return new WaitForFixedUpdate();
            Setup(true);
        }

        private void Setup(bool late)
        {
            List<EVEManagerBase> managers = Managers.Where(m => m.SceneLoad == HighLogic.LoadedScene && m.DelayedLoad == late).OrderBy(m => m.LoadOrder).ToList();
            foreach(EVEManagerBase manager in managers)
            {
                try
                {
                    manager.Setup();
                }
                catch(Exception e)
                {
                    Log("Issue loading " + manager.GetType().Name + "! Error:\n" + e.ToString());
                }
            }
        }

        private void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION ||
                HighLogic.LoadedScene == GameScenes.MAINMENU || HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                bool alt = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
                if (alt && Input.GetKeyDown(KeyCode.Alpha0) && guiLoad)
                {
                    useEditor = !useEditor;
                }
            }
        }

        #pragma warning disable 0649
        private GUISkin _mySkin;
        private Rect _mainWindowRect = new Rect(0, 0, 400, 720);

        protected static int selectedManagerIndex = 0;
        
        private void OnGUI()
        {
            GUI.skin = _mySkin;
            if (useEditor)
            {
                _mainWindowRect.width = 400;
                _mainWindowRect.height = 720;
                _mainWindowRect = GUI.Window(0x8100, _mainWindowRect, DrawMainWindow, "EVE Manager");
            }
        }

        private void DrawMainWindow(int windowID)
        {
            CelestialBody[] celestialBodies = FlightGlobals.Bodies.ToArray();
            EVEManagerBase currentManager;

            Rect placement = new Rect(0, 0, 0, 1);
            float width = _mainWindowRect.width - 10;
            float height = _mainWindowRect.height - 10;
            Rect placementBase = new Rect(10, 25, width, height);

            currentManager = GUIHelper.DrawSelector<EVEManagerBase>(Managers, ref selectedManagerIndex, 4, placementBase, ref placement);

            if (currentManager != null)
            {
                currentManager.DrawGUI(placementBase, placement);
            }

            if (GUI.tooltip != "")
            {
                GUIStyle toolStyle = new GUIStyle(GUI.skin.textField);
                toolStyle.normal.background = ToolTipBackground;
                toolStyle.normal.textColor = Color.black;
                Vector2 size = toolStyle.CalcSize(new GUIContent(GUI.tooltip));
                GUI.backgroundColor = Color.white;
                GUI.color = Color.white;
                GUI.color = Color.white;
                GUI.contentColor = Color.black;
                
                GUI.Label(new Rect(Mouse.screenPos.x + 15 - _mainWindowRect.x, Mouse.screenPos.y + 15- _mainWindowRect.y, size.x, size.y), new GUIContent( GUI.tooltip, ToolTipBackground), toolStyle);
                
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
        

        public static void Log(String message)
        {
            UnityEngine.Debug.Log("EVEManager: " + message);
        }

    }
}
