﻿using Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using EveManager;

namespace Terrain
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class TerrainManager : GenericEVEManager<TerrainObject>
    {
        protected override String configName { get { return "EVE_TERRAIN"; } }

        protected void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !Initialized)
            {
                Setup();
            }
        }

        public static void StaticSetup()
        {
            TerrainManager tm = new TerrainManager();
            tm.Setup();
        }

    }
}
