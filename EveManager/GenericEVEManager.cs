﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Utils;

namespace EVEManager
{
    public class Optional : System.Attribute
    {
        public String Field;
        public object Value;
        bool nullCheck = false;
        public Optional()
        {
            Field = null;
        }
        public Optional(String field)
        {
            Field = field;
            nullCheck = true;
        }
        public Optional(String field, object value)
        {
            Field = field;
            Value = value;
        }

        public bool isActive(ConfigNode node)
        {
            if(nullCheck)
            {
                return node.HasNode(Field);
            }
            else
            {
                if (Field != null)
                {
                    return node.GetValue(Field) == Value.ToString();
                }
                else
                {
                    return true;
                }
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class GenericEVEManager<T> : EVEManagerClass where T : IEVEObject, new()
    {
        [Flags] protected enum ObjectType
        {
            NONE = 0,
            PLANET = 1,
            GLOBAL = 2,
            MULTIPLE = 4,
            STATIC = 8
        }
        protected virtual ObjectType objectType { get { return ObjectType.NONE; } }
        protected virtual String configName { get { return ""; } }
        protected virtual GameScenes SceneLoad { get { return GameScenes.MAINMENU; } }

        protected static List<T> ObjectList = new List<T>();
        protected static UrlDir.UrlConfig[] configs;

        protected static bool spaceCenterReload = true;
        protected virtual bool sceneConfigLoad { get {
            bool load = HighLogic.LoadedScene == GameScenes.MAINMENU;
            if (spaceCenterReload && HighLogic.LoadedScene == SceneLoad)
            {
                spaceCenterReload = false;
                load = true;
            }
            return load; } }
        protected ConfigNode ConfigNode { get { return configNode; } }
        protected ConfigNode configNode;
        private static List<ConfigWrapper> ConfigFiles = new List<ConfigWrapper>();
        private static int selectedConfigIndex = 0;

        protected static Vector2 objListPos = Vector2.zero;
        protected static int selectedObjIndex = -1;
        protected static String objNameEditString = "";

        public virtual void GenerateGUI(){}
        private static bool staticInitialized = false;

        internal void Awake()
        {
            KSPLog.print(configName + " " + SceneLoad);
            if (sceneLoad)
            {
                StartCoroutine(SetupDelay());
            }
        }

        IEnumerator SetupDelay()
        {
            yield return new WaitForFixedUpdate();
            Setup();
        }

        public virtual void Setup()
        {
            if ((ObjectType.STATIC & objectType) != ObjectType.STATIC)
            {
                Managers.Add(this);
                Managers.RemoveAll(item => item == null);
                LoadConfig();
            }
            else
            {
                StaticSetup(this);
            }
        }

        public static void StaticSetup(GenericEVEManager<T> instance)
        {
            if (staticInitialized == false)
            {
                Managers.Add(instance);
                Managers.RemoveAll(item => item == null);
                instance.LoadConfig();
                staticInitialized = true;
            }
        }

        protected virtual void SingleSetup()
        {

        }

        public virtual void LoadConfig()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                Log("Loading...");
                configs = GameDatabase.Instance.GetConfigs(configName);
                ConfigFiles.Clear();
                foreach (UrlDir.UrlConfig config in configs)
                {
                    ConfigFiles.Add(new ConfigWrapper(config));
                }
            }
            if (sceneConfigLoad)
            {
                Apply();
            }
        }

        public virtual void Apply()
        {
            Clean();
            SingleSetup();
            foreach (UrlDir.UrlConfig config in configs)
            {
                foreach (ConfigNode node in config.config.nodes)
                {
                    if ((objectType & ObjectType.MULTIPLE) == ObjectType.MULTIPLE)
                    {
                        foreach (ConfigNode bodyNode in node.nodes)
                        {
                            ApplyConfigNode(bodyNode, node.name);
                        }
                    }
                    else
                    {
                        ApplyConfigNode(node, node.name);
                    }
                }
            }
        }

        protected virtual void ApplyConfigNode(ConfigNode node, String body)
        {
            T newObject = new T();
            newObject.LoadConfigNode(node, body);
            ObjectList.Add(newObject);
            newObject.Apply();
        }

        protected virtual void Clean()
        {
            foreach (T obj in ObjectList)
            {
                obj.Remove();
            }
            ObjectList.Clear();
        }

        public void SaveConfig()
        {
            Log("Saving...");
            foreach (UrlDir.UrlConfig config in configs)
            {
                config.parent.SaveConfigs();
            }
        }

        private void HandleGUI(object obj, ConfigNode configNode, Rect placementBase, ref Rect placement)
        {
            /*
            var objfields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(
                    field => Attribute.IsDefined(field, typeof(Persistent)));
                    foreach (FieldInfo fi in objfields)
                        */
            /*if(field.FieldType == typeof(String))
            {
                GUI.Label(labelRect, field.Name);
                GUI.TextField(fieldRect, field.GetValue(obj).ToString());
                placement.y++;
            }
            else if (field.FieldType == typeof(Vector3))
            {
                GUI.Label(labelRect, field.Name);
                GUI.TextField(fieldRect, ((Vector3)field.GetValue(obj)).ToString("F3"));
                placement.y++;
            }
            else if (field.FieldType == typeof(Color))
            {
                GUI.Label(labelRect, field.Name);
                GUI.TextField(fieldRect, ((Color)field.GetValue(obj)).ToString("F3"));
                placement.y++;
            }
            else if (field.FieldType == typeof(float))
            {
                GUI.Label(labelRect, field.Name);
                GUI.TextField(fieldRect, ((float)field.GetValue(obj)).ToString("F3"));
                placement.y++;
            }
             */
            
            var objfields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(
                    field => Attribute.IsDefined(field, typeof(Persistent)));
            foreach (FieldInfo field in objfields)
            {
                bool isNode = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(
                    fi => Attribute.IsDefined(fi, typeof(Persistent))).Count() > 0 ? true : false;

                if(!isNode)
                {
                    if (field.Name != "body")
                    {
                        
                        GUIHelper.DrawField(placementBase, ref placement, obj, field, configNode);

                        placement.y++;
                    }
                }
                else
                {
                    bool isOptional = Attribute.IsDefined(field, typeof(Optional));
                    bool show = true;
                    if (isOptional)
                    {
                        Optional op = (Optional)Attribute.GetCustomAttribute(field, typeof(Optional));
                        show = op.isActive(configNode);
                    }
                    if (show)
                    {
                        ConfigNode node = configNode.GetNode(field.Name);
                        GUIStyle gsRight = new GUIStyle(GUI.skin.label);
                        gsRight.alignment = TextAnchor.MiddleCenter;

                        Rect boxRect = GUIHelper.GetSplitRect(placementBase, ref placement, node);
                        GUI.Box(boxRect, "");
                        placement.height = 1;

                        Rect boxPlacementBase = new Rect(placementBase);
                        boxPlacementBase.x += 10;
                        Rect boxPlacement = new Rect(placement);
                        boxPlacement.width -= 20;

                        Rect toggleRect = GUIHelper.GetSplitRect(boxPlacementBase, ref boxPlacement);
                        Rect titleRect = GUIHelper.GetSplitRect(boxPlacementBase, ref boxPlacement);
                        GUIHelper.SplitRect(ref toggleRect, ref titleRect, (1f / 16));

                        GUI.Label(titleRect, field.Name);
                        bool removeable = node == null ? false : true;
                        if (isOptional)
                        {
                            if (removeable != GUI.Toggle(toggleRect, removeable, ""))
                            {
                                if (removeable)
                                {
                                    configNode.RemoveNode(field.Name);
                                    node = null;
                                }
                                else
                                {
                                    node = configNode.AddNode(new ConfigNode(field.Name));
                                }
                            }
                        }
                        else if (node == null)
                        {

                            node = configNode.AddNode(new ConfigNode(field.Name));
                        }
                        float height = boxPlacement.y + 1;
                        if (node != null)
                        {
                            height = boxPlacement.y + GUIHelper.GetFieldCount(node) + .25f;
                            boxPlacement.y++;

                            object subObj = field.GetValue(obj);
                            if (subObj == null)
                            {
                                ConstructorInfo ctor = field.FieldType.GetConstructor(System.Type.EmptyTypes);
                                subObj = ctor.Invoke(null);
                            }

                            HandleGUI(subObj, node, boxPlacementBase, ref boxPlacement);

                        }

                        placement.y = height;
                        placement.x = boxPlacement.x;
                    }
                }
            }
        }

        private void DrawConfigManagement(Rect placementBase, ref Rect placement)
        {
            Rect applyRect = GUIHelper.GetSplitRect(placementBase, ref placement);
            Rect saveRect = GUIHelper.GetSplitRect(placementBase, ref placement);
            GUIHelper.SplitRect(ref applyRect, ref saveRect, 1f / 2);
            if(GUI.Button(applyRect, "Apply"))
            {
                this.Apply();
            }
            if (GUI.Button(saveRect, "Save"))
            {
                this.SaveConfig();
            }
            placement.y++;
        }

        private ConfigNode DrawNodeManagement(Rect placementBase, ref Rect placement, ConfigNode node, String body)
        {
            Rect applyRect = GUIHelper.GetSplitRect(placementBase, ref placement);

            ConfigNode objNode = node.GetNode(body);

            if (objNode == null && GUI.Button(applyRect, "Add"))
            {
                objNode = node.AddNode(body);
            }
            else if (objNode != null && GUI.Button(applyRect, "Remove"))
            {
                node.RemoveNode(body);
                objNode = null;
            }
            placement.y++;
            return objNode;
        }

        public override void DrawGUI(Rect placementBase, Rect placement)
        {
            string body = null;
            ConfigNode objNode = null;
            
            ConfigWrapper selectedConfig = GUIHelper.DrawSelector<ConfigWrapper>(ConfigFiles, ref selectedConfigIndex, 16, placementBase, ref placement);

            DrawConfigManagement(placementBase, ref placement);

            if ((objectType & ObjectType.PLANET) == ObjectType.PLANET)
            {
                body = GUIHelper.DrawBodySelector(placementBase, ref placement);
                
            }

            if ((objectType & ObjectType.MULTIPLE) == ObjectType.MULTIPLE)
            {
                ConfigNode bodyNode;
                if(!selectedConfig.Node.HasNode(body))
                {
                    bodyNode = selectedConfig.Node.AddNode(body);
                }
                else
                {
                    bodyNode = selectedConfig.Node.GetNode(body);
                }

                objNode = GUIHelper.DrawObjectSelector(bodyNode.nodes, ref selectedObjIndex, ref objNameEditString, ref objListPos, placementBase, ref placement);
                
            }
            else
            {
                objNode = DrawNodeManagement(placementBase, ref placement, selectedConfig.Node, body);
            }

            if (this.configNode != null)
            {
                HandleGUI(this, this.configNode, placementBase, ref placement);
            }

            if (objNode != null)
            {
                HandleGUI(new T(), objNode, placementBase, ref placement);
            }
            
        }

        

        protected void OnGUI()
        {
        }

        protected void Update()
        {
        }

        public new static void Log(String message)
        {
            UnityEngine.Debug.Log(typeof(T).Name + ": " + message);
        }
    }

}
