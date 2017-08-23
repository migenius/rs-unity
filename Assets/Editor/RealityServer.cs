using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using com.migenius.rs4.core;
using com.migenius.rs4.viewport;
using com.migenius.rs4.unity;
using com.migenius.rs4.math;
using com.migenius.util;

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

        // Add menu named "RealityServer" to the Window menu
        [MenuItem("Window/RealityServer")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            RSWindow window = (RSWindow)EditorWindow.GetWindow(typeof(RSWindow));
            window.titleContent.text = "RealityServer";
            window.Show();
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
                Debug.LogError(resp.ErrorMessage.ToString() + "\n");
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
                //    Debug.Log("RSWS: " + cmd.Command.ToJSON(1) + "\n");
                //}
            });
        }

        // Build the mesh data in correct format for RealityServer
        private Hashtable BuildMeshes()
        {
            // Convert all meshes first since hierarchy is not relevant for them
            MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
            Hashtable rsMeshes = new Hashtable();

            // Iterate over ach mesh and build the appropriate mesh data structure
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
                for (int i = 0; i < mf.sharedMesh.triangles.Length; i += 3)
                {
                    ArrayList triangle = new ArrayList();
                    triangle.Add(mf.sharedMesh.triangles[i]);
                    triangle.Add(mf.sharedMesh.triangles[i + 1]);
                    triangle.Add(mf.sharedMesh.triangles[i + 2]);
                    polygons.Add(triangle);
                }

                // Mesh
                Hashtable rsMesh = new Hashtable() {
                    { "vectors", vectors },
                    { "vertices", vertices },
                    { "polygons", polygons }
                };

                // Add the mesh to our list of meshes
                rsMeshes.Add(mf.gameObject.name + "_" + mf.gameObject.GetInstanceID() + "_obj", rsMesh);

                // Debugging output
                /*
                Debug.Log("RSWS: Converted Mesh - Name: " + mf.name
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
            // No mesh in the object or its children, bail out
            if (gameObject.GetComponentsInChildren<MeshFilter>().Length == 0 && gameObject.GetComponent<MeshFilter>() == null)
            {
                return;
            }

            // Check if our current object contains a mesh
            bool hasMesh = (gameObject.GetComponent<MeshFilter>() != null);

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

            // If we are a leaf we can just add and finish, if not we need to traverse further
            if (gameObject.transform.childCount == 0 && hasMesh)
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
                if (hasMesh)
                {
                    Hashtable meshItem = new Hashtable() {
                        { "name", gameObject.name },
                        { "type", "instance" },
                        { "id", gameObject.GetInstanceID() }
                    };
                    items.Add(meshItem);
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
                "filename", "${shader}/material_examples/architectural.mdl"
            ), CheckResponse);
            seq.AddCommand(new RSCommand("create_material_instance_from_definition",
                "arguments", new Hashtable() {
                    { "reflectivity", 0.0 }
                },
                "material_definition_name", "mdl::material_examples::architectural::architectural",
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
                Debug.LogError("Camera not defined.\n");
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
                //Debug.Log("RSWS: Start Group Commands");
                //foreach (RSOutgoingCommand cmd in groupCommands.Commands)
                //{
                //    Debug.Log("RSWS: " + cmd.ToJSON());
                //}

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
                    //Debug.Log("RSWS: Attaching " + groupItemName + " to " + itemName);
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

            // If a real instance then add the default material
            if (item["type"].ToString() == "instance")
            {
                seq.AddCommand(new RSCommand("element_set_attribute",
                    "element_name", instanceName,
                    "attribute_name", "material",
                    "attribute_type", "Ref",
                    "attribute_value", "default_mat",
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
