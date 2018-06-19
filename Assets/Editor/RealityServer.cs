using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using com.migenius.rs4.core;
using com.migenius.rs4.viewport;
using com.migenius.rs4.unity;
using com.migenius.rs4.math;
using com.migenius.util;
using Logger = com.migenius.rs4.core.Logger;

namespace com.migenius.rs4.unity
{
    public class RSWindow : EditorWindow
    {
        string status = "Idle";
        string host = "localhost";
        int port = 8080;
        int timeout = 100;
        string filename = "unity.mi";
        string convertButton = "Convert";

        bool converting = false;
        bool error = false;

        // List of log message categories to exclude and our logger
        static List<string> ExcludeLogMessageCategories = new List<string> { "debug" };

        // Simple logger
        protected static void onLog(string category, params object[] values)
        {
            if (ExcludeLogMessageCategories.Contains(category))
            {
                return;
            }

            StringBuilder str = new StringBuilder();
            str.Append("RSEXP: ");
            foreach (object v in values)
            {
                str.Append(v.ToString());
                str.Append(' ');
            }
            str.Append("\n"); // supress Unity two line logs

            switch (category) {
                case "debug":
                    Debug.Log(str.ToString());
                    break;
                case "warn":
                    Debug.LogWarning(str.ToString());
                    break;
                case "error":
                    Debug.LogError(str.ToString());
                    break;
                case "info":
                    Debug.Log(str.ToString());
                    break;
                default:
                    Debug.Log(str.ToString());
                    break;
            }
        }

        // Add menu named "RealityServer" to the Window menu
        [MenuItem("Window/RealityServer")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            RSWindow window = (RSWindow)EditorWindow.GetWindow(typeof(RSWindow));
            window.titleContent.text = "RealityServer";
            window.Show();

            // Set up our simple logger
            Logger.OnLog += new Logger.LogHandler(onLog);
        }

        // Layout our UI
        void OnGUI()
        {
            GUI.enabled = !converting;
            GUILayout.Label("Server", EditorStyles.boldLabel);
            host = EditorGUILayout.TextField("Host", host);
            port = EditorGUILayout.IntField("Port", port);
            timeout = EditorGUILayout.IntField("Timeout", timeout);
            filename = EditorGUILayout.TextField("Filename", filename);
 
            if (GUILayout.Button(convertButton))
            {
                if(!converting)
                {
                    convertButton = "Converting...";
                    converting = true;
                    Convert();
                }
            }

            GUI.enabled = true;
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Status: ", status);
        }

        // Check response of commands for errors
        private void CheckResponse(RSResponse resp)
        {
            if (resp.IsErrorResponse)
            {
                error = true;
                Logger.Log("error", resp.ErrorMessage.ToString() + " when running command " + resp.Command.ToString());
            }

            if (resp.IsBatchResponse && resp.HasSubErrorResponse)
            {
                error = true;
                for (int i = 0; i < resp.SubResponses.Count; i++)
                {
                    if (resp.SubResponses[i].IsErrorResponse)
                    {
                        Logger.Log("error", resp.ErrorMessage.ToString() + " when running command " + resp.Command.ToString());
                    }
                }
            }
        }

        // Performs conversion of scene data to RealityServer
        private void Convert()
        {
            status = "Starting Conversion...";
            error = false;
            RSService Service = new RSService(host, port, timeout);
            string sceneName = RSUtils.RandomString();
            string sceneRootGroupName = "root";
            string scopeName = RSUtils.RandomString();

            // Generate the mesh data
            status = "Building Mesh Data...";
            Hashtable rsMeshes = BuildMeshes();
            Hashtable rsLights = BuildLights();

            // Get all of the top level game objects
            // Note: GetRootGameObjects() only available in Unity 5.3.2 or later
            GameObject[] gameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            // Create a root group and traverse all game objects
            status = "Building Scene Graph...";
            ArrayList rootGroup = new ArrayList();
            foreach (GameObject gameObject in gameObjects)
            {
                Traverse(gameObject, ref rootGroup);
            }

            // Send all of the commands to build the scene geometry to the server
            status = "Sending Commands...";
            Service.AddCallback((RSCommandSequence seq) => {
                // Setup the scope to isolate the conversion
                seq.AddCommand(new RSCommand("create_scope",
                        "scope_name", scopeName
                ), CheckResponse);
                seq.AddCommand(new RSCommand("use_scope",
                        "scope_name", scopeName
                ), CheckResponse);

                // Build the commands to send to the server
                GenerateSceneCommands(ref seq, sceneName, sceneRootGroupName);
                GenerateCameraCommands(ref seq);
                GenerateMeshCommands(ref seq, rsMeshes);
                GenerateLightCommands(ref seq, rsLights);
                GenerateRootgroupAndItemCommands(ref seq, rootGroup, sceneRootGroupName);

                // Export the scene data for testing
                seq.AddCommand(new RSCommand("export_scene",
                    "scene_name", sceneName,
                    "filename", "scenes/" + filename
                ), CheckResponse);

                // Remove the scope to clean up the database
                seq.AddCommand(new RSCommand("delete_scope",
                        "scope_name", scopeName
                ), (RSResponse resp) => { // Handler for completion
                    CheckResponse(resp);
                    if (!error)
                    {
                        status = "Done";
                    }
                    else
                    {
                        status = "Errors, check console.";
                    }
                    converting = false;
                    convertButton = "Convert";
                });
                // Log out all of the commands
                //foreach (RSOutgoingCommand cmd in seq.Commands) {
                //    Logger.Log("debug", cmd.Command.ToJSON(1) + "\n");
                //}
            });
        }

        // Build the light data in correct format for RealityServer
        private Hashtable BuildLights()
        {
            // Convert all meshes first since hierarchy is not relevant for them
            Light[] lights = FindObjectsOfType<Light>();
            Hashtable rsLights = new Hashtable();

            foreach (Light lgt in lights)
            {
                // Barf if we see a cookie texture
                if (lgt.cookie != null)
                {
                    Logger.Log("warn", "Light " + lgt.name + " has a cookie texture. These are not supported");
                }
                
                // Store data needed to recreate the light fully
                Hashtable rsLight = new Hashtable() {
                    { "intensity", lgt.intensity },
                    { "color", lgt.color },
                    { "type", lgt.type.ToString() },
                    { "spot_angle", lgt.spotAngle },
                    { "area_size", lgt.areaSize }
                };

                // Add the light to our list of lights
                rsLights.Add(lgt.gameObject.name + "_" + lgt.gameObject.GetInstanceID() + "_obj", rsLight);

                // Debugging output
                /*
                Logger.Log("debug", "Converted Light - Name: " + lgt.name
                    + ", ID: " + lgt.GetInstanceID()
                    + ", GO ID: " + lgt.gameObject.GetInstanceID()
                    + ", Hash: " + lgt.GetHashCode()
                    + ", Intensity: " + lgt.intensity
                    + ", Color: " + lgt.color.ToString()
                    + ", Type: " + lgt.type.ToString() + "\n");
                */
            }

            return (rsLights);
        }

        // Build the mesh data in correct format for RealityServer
        private Hashtable BuildMeshes()
        {
            // Convert all meshes first since hierarchy is not relevant for them
            MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
            Hashtable rsMeshes = new Hashtable();

            // Iterate over each mesh and build the appropriate mesh data structure
            foreach (MeshFilter mf in meshFilters)
            {
                // Vectors
                Hashtable vectors = new Hashtable();
                ArrayList points = new ArrayList();
                ArrayList normals = new ArrayList();
                ArrayList uvs = new ArrayList(); // TODO: Multiple UVs
                // Unity states vert, norm and uv array lengths always equal
                // negate x value of points for left to right handed conversion
                for (int i = 0; i < mf.sharedMesh.vertices.Length; i++)
                {
                    points.Add(new Hashtable() {
                        { "x", -mf.sharedMesh.vertices[i].x },
                        { "y", mf.sharedMesh.vertices[i].y },
                        { "z", mf.sharedMesh.vertices[i].z }
                    });
                    normals.Add(new Hashtable() {
                        { "x", -mf.sharedMesh.normals[i].x },
                        { "y", mf.sharedMesh.normals[i].y },
                        { "z", mf.sharedMesh.normals[i].z }
                    });
                    uvs.Add(new Hashtable() {
                        { "x", 1.0 - mf.sharedMesh.uv[i].x },
                        { "y", mf.sharedMesh.uv[i].y }
                    });
                }
                vectors["points"] = points;
                vectors["normals"] = normals;
                vectors["uvs"] = uvs;

                // Vertices
                ArrayList vertices = new ArrayList();
                for (int i = 0; i < mf.sharedMesh.vertices.Length; i++)
                {
                    vertices.Add(new Hashtable() {
                        { "v", i }, { "n", i }, { "t", i }
                    });
                }

                // Polygons
                ArrayList polygons = new ArrayList();
                for (int i = 0; i < mf.sharedMesh.subMeshCount; i++)
                {
                    int[] triangles = mf.sharedMesh.GetTriangles(i);
                    for (int v = 0; v < triangles.Length; v += 3)
                    {
                        ArrayList triangle = new ArrayList();
                        triangle.Add(i); // material index
                        triangle.Add(triangles[v]);
                        triangle.Add(triangles[v + 1]);
                        triangle.Add(triangles[v + 2]);
                        polygons.Add(triangle);
                    }
                }

                // Mesh
                Hashtable rsMesh = new Hashtable() {
                    { "tagged", true },
                    { "vectors", vectors },
                    { "vertices", vertices },
                    { "polygons", polygons }
                };

                // Add the mesh to our list of meshes
                rsMeshes.Add(mf.gameObject.name + "_" + mf.gameObject.GetInstanceID() + "_obj", rsMesh);

                // Debugging output
                /*
                Logger.Log("debug", "Converted Mesh - Name: " + mf.name
                    + ", ID: " + mf.GetInstanceID()
                    + ", GO ID: " + mf.gameObject.GetInstanceID()
                    + ", Hash: " + mf.GetHashCode()
                    + ", Vertices: " + mf.sharedMesh.vertices.Length
                    + ", Normals: " + mf.sharedMesh.normals.Length
                    + ", UVs: " + mf.sharedMesh.uv.Length
                    + ", Tricount: " + mf.sharedMesh.triangles.Length + "\n");
                */
            }
            return(rsMeshes);
        }

        // Traverse GameObject and its children
        private void Traverse(GameObject gameObject, ref ArrayList parentGroup)
        {
            // Check if our current object contains a mesh or a light
            bool hasMesh = (gameObject.GetComponent<MeshFilter>() != null);
            bool hasLight = (gameObject.GetComponent<Light>() != null);

            // Check if children have meshes or lights
            bool hasLightsInChildren = (gameObject.GetComponentsInChildren<Light>().Length > 0);
            bool hasMeshesInChildren = (gameObject.GetComponentsInChildren<MeshFilter>().Length > 0);

            // No mesh or lights in the object or its children, bail out
            if (!hasLightsInChildren && !hasMeshesInChildren && !hasMesh && !hasLight)
            {
                return;
            }

            // Is inactive so mesh will not have been generated
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            // Everything we need to make an instance later
            Hashtable item = new Hashtable() {
                { "name", gameObject.name },
                { "type", "group" },
                { "id", gameObject.GetInstanceID() }
            };

            // Add the transformation matrix (convert left handed to right handed)
            GameObject go = new GameObject(); // Needed to make Transform
            Transform tt = go.transform;
            tt.localScale = gameObject.transform.localScale;
            tt.localRotation = gameObject.transform.localRotation;
            tt.localPosition = gameObject.transform.localPosition;
            Matrix4x4 t0 = tt.worldToLocalMatrix.transpose;
            Matrix4x4 t1 = new Matrix4x4();
            t1 = Matrix4x4.identity;
            t1[0, 0] = -1; // convert rotation handedness
            Matrix4x4 t = t1 * t0 * t1;
            item.Add("transform", new Hashtable() {
                { "xx", t.m00 }, { "xy", t.m01 }, { "xz", t.m02 }, { "xw", t.m03 },
                { "yx", t.m10 }, { "yy", t.m11 }, { "yz", t.m12 }, { "yw", t.m13 },
                { "zx", t.m20 }, { "zy", t.m21 }, { "zz", t.m22 }, { "zw", t.m23 },
                { "wx", t.m30 }, { "wy", t.m31 }, { "wz", t.m32 }, { "ww", t.m33 }
            });
            DestroyImmediate(go); // Don't clutter the workspace

            // Fetch and store the material information if present
            if (gameObject.GetComponent<MeshRenderer>() != null)
            {
                Material[] srcMats = gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                ArrayList mats = new ArrayList();
                for (int i = 0; i < srcMats.Length; i++)
                {
                    // Name and albedo color
                    Hashtable mat = new Hashtable() {
                        { "name", srcMats[i].name },
                        { "color", srcMats[i].color.linear }
                    };

                    // Check if shader type is supported before reading more properties
                    if (srcMats[i].shader.name != "Standard")
                    {
                        Logger.Log("warn", "Material " + srcMats[i].name + " has unsupported shader type " + srcMats[i].shader.name);

                        // Just take the basic properties and continue
                        mats.Add(mat);
                        continue;
                    }

                    // Albedo map
                    if (srcMats[i].mainTexture != null) {
                        // Retreive the texture filename and settings
                        String path = Application.dataPath;
                        int idx = path.LastIndexOf("/Assets");
                        path = path.Remove(idx, path.Length - idx);
                        path += "/" + AssetDatabase.GetAssetPath(srcMats[i].mainTexture);
                        mat.Add("texture", new Hashtable() {
                            { "filename",  path }
                        });
                        mat.Add("texture_u_scale", srcMats[i].mainTextureScale.x);
                        mat.Add("texture_v_scale", srcMats[i].mainTextureScale.y);
                        mat.Add("texture_u_offset", srcMats[i].mainTextureOffset.x);
                        mat.Add("texture_v_offset", srcMats[i].mainTextureOffset.y);
                    }

                    // Handle rendering modes
                    if (srcMats[i].HasProperty("_Mode"))
                    {
                        switch((int)srcMats[i].GetFloat("_Mode"))
                        {
                            case 0: // Opaque
                                // Nothing needed for Opaque (yet)
                                break;
                            case 1: // Cutout
                                // TODO: Handle cutout materials
                                Logger.Log("warn", "Cutout material mode used in " + srcMats[i].name + " not implemented.");
                                break;
                            case 2: // Fade
                                // It is unlikely that fade materials can be supported in Iray
                                Logger.Log("warn", "Fade material mode used in " + srcMats[i].name + " not implemented.");
                                break;
                            case 3: // Transparent
                                // TODO: Handle transparent materials
                                Logger.Log("warn", "Transparent material mode used in " + srcMats[i].name + " not implemented.");
                                break;
                        }
                    }

                    // Handle emission
                    if (srcMats[i].HasProperty("_EmissionColor") && srcMats[i].IsKeywordEnabled("_EMISSION"))
                    {
                        // TODO: Rework conversion to tint and intensity, this is just a hack
                        Vector4 emission = (Vector4)srcMats[i].GetColor("_EmissionColor");
                        float intensity = emission.magnitude;
                        float invMagnitude = 1.0f / intensity;
                        emission.Scale(new Vector4(invMagnitude, invMagnitude, invMagnitude, invMagnitude));
                        mat.Add("emission_color", emission);
                        mat.Add("emission_intensity", intensity);
                    }

                    // Handle metalic
                    if (srcMats[i].HasProperty("_Metallic"))
                    {
                        mat.Add("metallic", srcMats[i].GetFloat("_Metallic"));
                    }

                    // Handle smoothness
                    if (srcMats[i].HasProperty("_Glossiness"))
                    {
                        // Invert glossiness to obtain roughness
                        mat.Add("roughness", (1.0 - srcMats[i].GetFloat("_Glossiness")));
                    }

                    mats.Add(mat);
                }
                item.Add("materials", mats);
            }

            // If we are a leaf we can just add and finish, if not we need to traverse further
            if (gameObject.transform.childCount == 0 && (hasMesh || hasLight))
            {
                // leaf mesh node
                item["type"] = "instance";
                parentGroup.Add(item);
            }
            else
            {
                // group
                ArrayList items = new ArrayList();
                item.Add("items", items);
                parentGroup.Add(item);
                // if transform has a mesh move it to the items array with identity transform
                if (hasMesh || hasLight)
                {
                    Hashtable instItem = new Hashtable() {
                        { "name", gameObject.name },
                        { "type", "instance" },
                        { "id", gameObject.GetInstanceID() }
                    };
                    items.Add(instItem);
                }
                // Process the children
                foreach (Transform child in gameObject.transform)
                {
                    Traverse(child.gameObject, ref items);
                }
            }
        }

        // Generate the RealityServer commands to build the empty scene
        private void GenerateSceneCommands(ref RSCommandSequence seq, string sceneName, string sceneRootGroupName)
        {
            seq.AddCommand(new RSCommand("log",
                "log_entry", "Generating Unity Scene Data"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_scene",
                "scene_name", sceneName
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_element",
                "element_type", "Options",
                "element_name", "opt"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("scene_set_options",
                "scene_name", sceneName,
                "options", "opt"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_element",
                "element_type", "Group",
                "element_name", sceneRootGroupName
            ), CheckResponse);
            seq.AddCommand(new RSCommand("scene_set_rootgroup",
                "scene_name", sceneName,
                "group", sceneRootGroupName
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_element",
                "element_type", "Camera",
                "element_name", "cam"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("camera_set_resolution",
                "camera_name", "cam",
                "resolution", new Hashtable() {
                    { "x", 640 },
                    { "y", 480 }
                }
            ), CheckResponse);
            seq.AddCommand(new RSCommand("camera_set_aspect",
                "camera_name", "cam",
                "aspect", 1.3333333
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_element",
                "element_type", "Instance",
                "element_name", "cam_inst"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("instance_attach",
                "instance_name", "cam_inst",
                "item_name", "cam"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("group_attach",
                "group_name", sceneRootGroupName,
                "item_name", "cam_inst"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("scene_set_camera_instance",
                "scene_name", sceneName,
                "camera_instance", "cam_inst"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("import_scene_elements",
                "filename", "${shader}/base.mdl"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("import_scene_elements",
                "filename", "${shader}/migenius/unity.mdl"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_material_instance_from_definition",
                "arguments", new Hashtable() {
                    { "diffuse_color", new Hashtable() { { "r", 0.7 }, { "g", 0.7 }, { "b", 0.7 } } }
                },
                "material_definition_name", "mdl::migenius::unity::diffuse",
                "material_name", "default_mat"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_function_call_from_definition",
                "function_definition_name", "mdl::base::sun_and_sky(bool,float,color,float,float,float,float,float,color,color,float3,float,float,float,bool,int,bool)",
                "function_name", "sunsky",
                "arguments", new Hashtable() {
                    { "multiplier", 0.10132 },
                    { "rgb_unit_conversion", new Hashtable() { { "r", 1.0 }, { "g", 1.0 }, { "b", 1.0 } } },
                    { "sun_disk_intensity", 1.0 },
                    { "physically_scaled_sun", true },
                    { "sun_direction", new Hashtable() { { "x", 0.0 }, { "y", 0.5 }, { "z", -1.0 } } }
                }
            ), CheckResponse);
            seq.AddCommand(new RSCommand("element_set_attribute",
                "element_name", "opt",
                "attribute_type", "Ref",
                "attribute_name", "environment_function",
                "attribute_value", "sunsky",
                "create", true
            ), CheckResponse);
            seq.AddCommand(new RSCommand("element_set_attributes",
                "create", true,
                "element_name", "cam",
                "attributes", new Hashtable()
                {
                    {
                        "tm_tonemapper", new Hashtable()
                        {
                            { "type", "String" },
                            { "value", "mia_exposure_photographic" }
                        }
                    },
                    {
                        "mip_cm2_factor", new Hashtable()
                        {
                            { "type", "Float32" },
                            { "value", 1.0 }
                        }
                    },
                    {
                        "mip_film_iso", new Hashtable()
                        {
                            { "type", "Float32" },
                            { "value", 100.0 }
                        }
                    },
                    {
                        "mip_camera_shutter", new Hashtable()
                        {
                            { "type", "Float32" },
                            { "value", 250.0 }
                        }
                    },
                    {
                        "mip_f_number", new Hashtable()
                        {
                            { "type", "Float32" },
                            { "value", 8.0 }
                        }
                    },
                    {
                        "mip_gamma", new Hashtable()
                        {
                            { "type", "Float32" },
                            { "value", 2.2 }
                        }
                    }
                }
            ), CheckResponse);
        }

        // Generate the RealityServer commands to set the camera properties
        private void GenerateCameraCommands(ref RSCommandSequence seq)
        {
            // Fetch the current camera data
            Camera cam = Camera.main;
            if (!cam)
            {
                Logger.Log("error", "Camera not defined. Have you set the MainCamera tag on a camera?\n");
                return;
            }
            Transform camTrans = cam.transform;
            Vector3 camPos = camTrans.position;
            Vector3 camForward = camTrans.forward;

            // Negate the x position and direction as Unity is left handed while RS is right handed.
            Vector3D pos = new Vector3D(-camPos.x, camPos.y, camPos.z);
            Vector3D forward = new Vector3D(-camForward.x, camForward.y, camForward.z);
            Vector3D up = new Vector3D(0, 1, 0);
            Transform3D trans = new Transform3D();
            trans.SetLookAt(pos, forward, up);

            // Add the command to set the camera matrix
            seq.AddCommand(new RSCommand("instance_set_world_to_obj",
                "instance_name", "cam_inst",
                "transform", trans.world_to_object.GetMatrixForRS()
            ), CheckResponse);

            // Add the field of view properties
            // TODO: Support orthographic projection
            double aspect = 1.33333;
            double aperture = 1.417;
            double deg2rad = Math.PI / 180.0;
            double focal = ((aperture / aspect) / 2.0f) / Math.Tan(cam.fieldOfView / 2.0f * deg2rad);
            seq.AddCommand(new RSCommand("camera_set_aperture",
                    "camera_name", "cam",
                    "aperture", aperture
            ), CheckResponse);
            seq.AddCommand(new RSCommand("camera_set_focal",
                    "camera_name", "cam",
                    "focal", focal
            ), CheckResponse);
        }

        // Generate the RealityServer commands to build the root group
        private void GenerateRootgroupAndItemCommands(ref RSCommandSequence seq, ArrayList rootGroup, string sceneRootGroupName)
        {
            // Output the commands for the instances and groups
            ArrayList rootGroupInstances = new ArrayList();
            foreach (Hashtable item in rootGroup)
            {
                rootGroupInstances.Add(GenerateItemCommands(ref seq, item));
            }
            // Output the commands to add the rootGroup items to
            // the actual root group. We do this after the previous
            // commands to ensure the elements have been made
            foreach (string itemName in rootGroupInstances)
            {
                seq.AddCommand(new RSCommand("group_attach",
                    "group_name", sceneRootGroupName,
                    "item_name", itemName
                ), CheckResponse);
            }
        }

        // Generate the RealityServer commands for all of the light data
        private void GenerateLightCommands(ref RSCommandSequence seq, Hashtable rsLights)
        {
            // Create each light
            foreach (string key in rsLights.Keys)
            {
                // Grab our light varialbes
                Hashtable light = (Hashtable)rsLights[key];
                String mdlInst = key + "_lgt_mat";
                float intensity = (float)light["intensity"];
                Color color = (Color)light["color"];
                String type = (String)light["type"];
                Vector2 areaSize = (Vector2)light["area_size"];
                float spotAngle = (float)light["spot_angle"];
                String mdlDef = "mdl::migenius::unity::light_spot";

                // Create the actual light (blank for now)
                seq.AddCommand(new RSCommand("create_element",
                    "element_name", key,
                    "element_type", "Light"
                ), CheckResponse);

                // Setup the light according to the type
                switch (type)
                {
                    case "Spot":
                        mdlDef = "mdl::migenius::unity::light_spot";
                        seq.AddCommand(new RSCommand("light_set_type",
                            "light_name", key,
                            "type", "point"
                        ), CheckResponse);
                        break;
                    case "Directional":
                        mdlDef = "mdl::migenius::unity::light_omni";
                        seq.AddCommand(new RSCommand("light_set_type",
                            "light_name", key,
                            "type", "spot" // bug in RealityServer, infinite does not work but spot creates infinite anyway so use it
                        ), CheckResponse);
                        break;
                    case "Point":
                        mdlDef = "mdl::migenius::unity::light_omni";
                        seq.AddCommand(new RSCommand("light_set_type",
                            "light_name", key,
                            "type", "point"
                        ), CheckResponse);
                        break;
                    case "Area":
                        mdlDef = "mdl::migenius::unity::light_omni";
                        seq.AddCommand(new RSCommand("light_set_type",
                            "light_name", key,
                            "type", "point"
                        ), CheckResponse);
                        seq.AddCommand(new RSCommand("light_set_area_shape",
                            "light_name", key,
                            "area_shape", "rectangle" // Unity only supports rectangle for now
                        ), CheckResponse);
                        seq.AddCommand(new RSCommand("light_set_area_size",
                            "light_name", key,
                            "area_size", new Hashtable() { {"x", areaSize.x }, { "y", areaSize.y} }
                        ), CheckResponse);
                        break;
                    default:
                        Logger.Log("warn", "Light " + key + " has unsupported type " + type + ".");
                        break;
                }

                // Create the MDL instance and attach to the light
                seq.AddCommand(new RSCommand("create_material_instance_from_definition",
                    "arguments", new Hashtable() {
                        { "tint",  new Hashtable() { { "r", color.r }, { "g", color.g }, { "b", color.b } } },
                        { "intensity", intensity }
                    },
                    "material_definition_name", mdlDef,
                    "material_name", mdlInst
                ), CheckResponse);
                seq.AddCommand(new RSCommand("element_set_attribute",
                    "element_name", key,
                    "attribute_name", "material",
                    "attribute_type", "Ref",
                    "attribute_value", mdlInst,
                    "create", true
                ), CheckResponse);

                // Set the spotlight angle
                if (type == "Spot")
                {
                    // TODO: fix exponent calculation, this is not correct
                    float exponent = 100.0f - spotAngle;
                    seq.AddCommand(new RSCommand("mdl_set_argument",
                        "element_name", mdlInst,
                        "argument_name", "spot_exponent",
                        "value", exponent
                    ), CheckResponse);
                }

                // Reverse emission of area lights
                if (type == "Area")
                {
                    seq.AddCommand(new RSCommand("mdl_set_argument",
                        "element_name", mdlInst,
                        "argument_name", "direction",
                        "value", "back"
                    ), CheckResponse);
                }
            }
        }

        // Generate the RealityServer commands for all of the mesh data
        private void GenerateMeshCommands(ref RSCommandSequence seq, Hashtable rsMeshes)
        {
            // Output the commands for the objects (meshes)
            foreach (string key in rsMeshes.Keys)
            {
                seq.AddCommand(new RSCommand("generate_mesh",
                    "name", key,
                    "mesh", rsMeshes[key]
                ), CheckResponse);
                seq.AddCommand(new RSCommand("element_set_attribute",
                    "element_name", key,
                    "attribute_name", "visible",
                    "attribute_type", "Boolean",
                    "attribute_value", true,
                    "create", true
                ), CheckResponse);
            }
        }

        // Generate the RealityServer commands for an individual item
        private string GenerateItemCommands(ref RSCommandSequence seq, Hashtable item)
        {
            RSCommandSequence groupCommands = new RSCommandSequence(seq.Service, seq.StateData);

            // Process children if present
            if (item["items"] != null)
            {
                // Recurse for each child
                foreach (Hashtable nextItem in (ArrayList)item["items"])
                {
                    GenerateItemCommands(ref seq, nextItem);
                }
            }

            string baseName = item["name"] + "_" + item["id"];
            string itemName = baseName;
            string instanceName = baseName;

            // Are we making an instance of a mesh or a group
            if (item["type"].ToString() == "group")
            {
                // Create both group and instance of it
                itemName += "_grp";
                seq.AddCommand(new RSCommand("create_element",
                    "element_name", itemName,
                    "element_type", "Group"
                ), CheckResponse);
                // Create commands to attach item to parent group
                foreach (Hashtable groupItem in (ArrayList)item["items"])
                {
                    string groupItemName = groupItem["name"] + "_" + groupItem["id"];
                    if (groupItem["type"].ToString() == "group")
                    {
                        groupItemName += "_grp_inst";
                    }
                    else
                    {
                        groupItemName += "_obj_inst";
                    }
                    groupCommands.AddCommand(new RSCommand("group_attach",
                        "group_name", itemName,
                        "item_name", groupItemName
                    ), CheckResponse);
                    //Logger.Log("debug", "Attaching " + groupItemName + " to " + itemName);
                }
            }
            else if (item["type"].ToString() == "instance")
            {
                // Mesh already exists so just set the instance name
                itemName += "_obj";
            }

            // Make the instance and attach the group or mesh
            instanceName = itemName + "_inst";
            seq.AddCommand(new RSCommand("create_element",
                "element_name", instanceName,
                "element_type", "Instance"
                ), CheckResponse);
            seq.AddCommand(new RSCommand("instance_attach",
                "instance_name", instanceName,
                "item_name", itemName
            ), CheckResponse);
            seq.AddCommand(new RSCommand("instance_set_world_to_obj",
                "instance_name", instanceName,
                "transform", item["transform"]
            ), CheckResponse);

            // List of materials names
            ArrayList materialNames = new ArrayList();

            // If materials is defined then use
            if (item.ContainsKey("materials"))
            {
                // Object can have multiple materials
                ArrayList mats = (ArrayList)item["materials"];

                // Process them all
                for (int i = 0; i < mats.Count; i++)
                {
                    Hashtable mat = (Hashtable)mats[i];
                    Color color = (Color)mat["color"];
                    float metallic = 0.0f;
                    float roughness = 0.5f;

                    // Update metallic
                    if (mat.Contains("metallic"))
                    {
                        metallic = (float)mat["metallic"];
                    }

                    // Update roughness
                    if (mat.Contains("roughness"))
                    {
                        double r = (double)mat["roughness"];
                        roughness = (float)r;
                    }

                    // Keep track of materials we made
                    materialNames.Add((String)mat["name"]);

                    // Create the material using the found parameters
                    if (mat.Contains("emission_color") && mat.Contains("emission_intensity"))
                    {
                        Vector4 emissionColor = (Vector4)mat["emission_color"];
                        float emissionIntensity = (float)mat["emission_intensity"];
                        seq.AddCommand(new RSCommand("create_material_instance_from_definition",
                            "arguments", new Hashtable() {
                                { "tint",  new Hashtable() { { "r", emissionColor.x }, { "g", emissionColor.y }, { "b", emissionColor.z } } },
                                { "intensity", emissionIntensity }
                            },
                            "material_definition_name", "mdl::migenius::unity::light_omni",
                            "material_name", (String)mat["name"]
                        ), CheckResponse);
                    }
                    else if (metallic < 0.001 && roughness > 0.99) // replace with diffuse material
                    {
                        seq.AddCommand(new RSCommand("create_material_instance_from_definition",
                            "arguments", new Hashtable() {
                                { "diffuse_color",  new Hashtable() { { "r", color.r }, { "g", color.g }, { "b", color.b } } }
                            },
                            "material_definition_name", "mdl::migenius::unity::diffuse",
                            "material_name", (String)mat["name"]
                        ), CheckResponse);
                    }
                    else
                    { // normal PBR material
                        seq.AddCommand(new RSCommand("create_material_instance_from_definition",
                            "arguments", new Hashtable() {
                                { "base_color",  new Hashtable() { { "r", color.r }, { "g", color.g }, { "b", color.b } } },
                                { "metallic", metallic },
                                { "roughness", roughness }
                            },
                            "material_definition_name", "mdl::migenius::unity::metallic_roughness",
                            "material_name", (String)mat["name"]
                        ), CheckResponse);
                    }

                    // Attach texture if present
                    if (mat.Contains("texture"))
                    {
                        string textureArgument = (metallic < 0.001 && roughness > 0.99) ? "diffuse_color" : "base_color";
                        Hashtable tex = (Hashtable)mat["texture"];

                        // Build texture options for tiling and offset
                        Hashtable texOptions = new Hashtable() {
                            { "scaling",  new Hashtable() {
                                { "x", (mat.Contains("texture_u_scale")) ? mat["texture_u_scale"] : 1.0 },
                                { "y", (mat.Contains("texture_v_scale")) ? mat["texture_v_scale"] : 1.0 },
                                { "z", 1.0 } }
                            },
                            { "translation",  new Hashtable() {
                                { "x", (mat.Contains("texture_u_offset")) ? mat["texture_u_offset"] : 0.0 },
                                { "y", (mat.Contains("texture_v_offset")) ? mat["texture_v_offset"] : 0.0 },
                                { "z", 0.0 } }
                            }
                        };

                        // Read and base64 encode the texture data
                        String texFilename = (String)tex["filename"];
                        String texBaseFilename = Path.GetFileName(texFilename);
                        String texFormat = Path.GetExtension(texFilename).Replace(".", "");
                        byte[] texBytes = File.ReadAllBytes(texFilename);
                        String texString = System.Convert.ToBase64String(texBytes);

                        // Create the image and texture elements
                        seq.AddCommand(new RSCommand("create_element",
                            "element_name", "img_" + texFilename,
                            "element_type", "Image"
                        ), CheckResponse);
                        seq.AddCommand(new RSCommand("image_reset_from_base64",
                            "image_name", "img_" + texFilename,
                            "data", texString,
                            "image_format", texFormat
                        ), CheckResponse);
                        seq.AddCommand(new RSCommand("create_element",
                            "element_name", "tex_" + texFilename,
                            "element_type", "Texture"
                        ), CheckResponse);
                        seq.AddCommand(new RSCommand("texture_set_image",
                            "texture_name", "tex_" + texFilename,
                            "image_name", "img_" + texFilename
                        ), CheckResponse);

                        // Save the in-memory texture to disk and reload them
                        // this greatly reduces the size of the resulting .mi file
                        ArrayList commands = new ArrayList();
                        String imageCmdId = Guid.NewGuid().ToString();
                        String typeCmdId = Guid.NewGuid().ToString();
                        commands.Add(new Hashtable() {
                            {  "name", "texture_get_image" },
                            {
                                "params", new Hashtable() {
                                    { "texture_name", "tex_" + texFilename }
                                }
                            },
                            { "id", imageCmdId }
                        });
                        commands.Add(new Hashtable() {
                            {  "name", "image_get_type" },
                            {
                                "params", new Hashtable() {
                                    { "image_name", "${" + imageCmdId + "}" }
                                }
                            },
                            { "id", typeCmdId }
                        });
                        commands.Add(new Hashtable() {
                            {  "name", "image_save_to_disk" },
                            {
                                "params", new Hashtable() {
                                    { "filename", "scenes/" + filename.Substring(0, filename.Length - 3) + "_tex_" + texBaseFilename },
                                    { "format", texFormat },
                                    { "image_name", "${" + imageCmdId + "}" },
                                    { "pixel_type", "${" + typeCmdId + "}" }
                                }
                            }
                        });
                        commands.Add(new Hashtable() {
                            {  "name", "image_reset_file" },
                            {
                                "params", new Hashtable() {
                                    { "filename", "scenes/" + filename.Substring(0, filename.Length - 3) + "_tex_" + texBaseFilename },
                                    { "image_name", "${" + imageCmdId + "}" }
                                }
                            }
                        });
                        seq.AddCommand(new RSCommand("smart_batch",
                            "commands", commands
                        ), CheckResponse);

                        // Attach our new texture to the material
                        seq.AddCommand(new RSCommand("material_attach_texture_to_argument",
                            "argument_name", textureArgument,
                            "create_from_file", false,
                            "material_name", (String)mat["name"],
                            "texture_name", "tex_" + texFilename,
                            "texture_options", texOptions
                        ), CheckResponse);
                    }
                }
            }

            // If we didn't get a material then add the default one
            if (materialNames.Count == 0)
            {
                materialNames.Add("default_mat");
            }

            // If a real instance then add the default material
            if (item["type"].ToString() == "instance")
            {
                seq.AddCommand(new RSCommand("element_set_attribute",
                    "element_name", instanceName,
                    "attribute_name", "material",
                    "attribute_type", "Ref[]",
                    "attribute_value", materialNames,
                    "create", true
                ), CheckResponse);
            }

            // Add any commands needed to attach items to the groups
            groupCommands.AddToSequence(seq);

            // Give back the computed instance name
            return instanceName;
        }
    }
}
