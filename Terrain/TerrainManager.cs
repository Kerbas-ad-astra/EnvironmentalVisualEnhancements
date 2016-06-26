﻿using Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using EVEManager;
using ShaderLoader;

namespace Terrain
{
    public class TerrainManager : GenericEVEManager<TerrainObject>
    {
        public override ObjectType objectType { get { return ObjectType.BODY; } }
        public override String configName { get { return "EVE_TERRAIN"; } }

        //private bool camerasInitialized = false;
        private static Shader oceanShader = null;
        private static Shader planetShader = null;
        private static Shader terrainShader = null;
        private static Shader oceanBackingShader = null;

        public static Shader OceanShader
        {
            get
            {
                if (oceanShader == null)
                {
                    oceanShader = ShaderLoaderClass.FindShader("EVE/Ocean");
                } return oceanShader;
            }
        }
        public static Shader PlanetShader
        {
            get
            {
                if (planetShader == null)
                {
                    planetShader = ShaderLoaderClass.FindShader("EVE/Planet");
                } return planetShader;
            }
        }
        public static Shader TerrainShader
        {
            get
            {
                if (terrainShader == null)
                {
                    terrainShader = ShaderLoaderClass.FindShader("EVE/Terrain");
                } return terrainShader;
            }
        }
        
        public static Shader OceanBackingShader
        {
            get
            {
                if (oceanBackingShader == null)
                {
                    oceanBackingShader = ShaderLoaderClass.FindShader("EVE/OceanBack");
                } return oceanBackingShader;
            }
        }
        

    }
}
