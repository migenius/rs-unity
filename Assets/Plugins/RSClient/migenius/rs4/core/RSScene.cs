using System;
using System.Collections;
using System.Collections.Generic;
using com.migenius.rs4.core;
using com.migenius.rs4.math;

namespace com.migenius.rs4.core
{
    public class RSScene
    {
        public event ApplicationInitialisingCallback OnAppIniting;
        public event ApplicationInitialisedCallback OnAppInited;
        
        public event StatusUpdateCallback OnStatus;
        public event SceneImportedCallback OnSceneImported;

        public event ShutdownCallback OnShutdown;

        public string Filename = @"scenes\meyemII\main.mi";
        public bool Ready { get; protected set; }
        private string _sceneName = null;
        public string Name 
        {
            get
            {
                return _sceneName;
            }
            set
            {
                if (!Ready)
                {
                    _sceneName = value;
                }
                // else error?
            }
        }

        public string CameraName = null;
        public string CameraInstanceName = null;
        public string OptionsName = null;
        public string RootGroupName = null;
        public Vector3D BBoxMax = new Vector3D();
        public Vector3D BBoxMin = new Vector3D();
        
        public RSService Service { get; protected set; }
        private string _host = "localhost";
        private int _port = 8080;
        public bool IsShutdown { get; protected set; }

        public string Host 
        {
            get
            {
                if (Service != null)
                {
                    return Service.Host;
                }
                return _host;
            }
            set
            {
                if (Service == null)
                {
                    _host = value;
                }
                // else error?
            }
        }
        public int Port 
        {
            get
            {
                if (Service != null)
                {
                    return Service.Port;
                }
                return _port;
            }
            set
            {
                if (Service == null)
                {
                    _port = value;
                }
                // else error?
            }
        }

        public string UserScope { get; set; }
        public string ApplicationScope = "unity_app";

        public RSScene()
        {

        }
        public RSScene(RSService service)
        {
            Service = service;
            Ready = false;
        }
        public RSScene(string host, int port)
        {
            Host = host;
            Port = port;
        }
        
        public void Status(string type, string message)
        {
            if (OnStatus != null)
            {
                OnStatus(type, message);
            }
        }

        public void InitValues()
        {
            if (Service == null)
            {
                Service = new RSService(Host, Port);
            }
            
            if (Name == null)
            {
                Name = Filename;
            }
            
            string rand = RSUtils.RandomString();
            if (UserScope == null || UserScope.Length == 0)
            {
                UserScope = "user_" + rand;
            }
        }
        
        public void ImportScene()
        {
            InitValues();

            Status("info", "Importing scene");
            Service.AddCallback((RSCommandSequence seq) => {
                    seq.AddCommand(new RSCommand("create_scope",
                            "scope_name", "app_scope"
                    ));
                    seq.AddCommand(new RSCommand("create_scope",
                            "scope_name", UserScope,
                            "parent_scope", "app_scope"
                    ));
                    seq.AddCommand(new RSCommand("use_scope",
                            "scope_name", UserScope
                    ));
                    seq.AddCommand(new RSCommand("import_scene",
                            "block", true,
                            "scene_name", Filename,
                            "filename", Filename
                    ), OnSceneImport);
                    seq.AddCommand(new RSCommand("scene_get_bounding_box",
                            "scene_name", Filename
                    ), OnSceneGetBoundingBox);

                    seq.AddCommand(new RSCommand("echo",
                            "input", "pre_app_init"
                    ), (RSResponse resp) => {
                        OnApplicationInit(); 
                    });
            });
        }
        
        protected void OnApplicationInit()
        {
            if (CameraName == null)
            {
                return;
            }

            Status("info", "Application initialising");

            Service.AddCallback((RSCommandSequence seq) => {
                seq.AddCommand(new RSCommand("localize_element", "element_name", CameraName ));
                seq.AddCommand(new RSCommand("localize_element", "element_name", CameraInstanceName ));
                seq.AddCommand(new RSCommand("localize_element", "element_name", OptionsName ));

                if (OnAppIniting != null)
                {
                    OnAppIniting(seq);
                }

                seq.AddCommand(new RSCommand("echo", "input", "app_init"), (RSResponse resp) => {
                        SceneReady();
                });
            });
        }
        protected void SceneReady()
        {
            Ready = true;
            if (OnAppInited != null)
            {
                OnAppInited();
            }
        }

        protected void OnSceneImport(RSResponse resp)
        {
            if (resp.IsErrorResponse)
            {
                Logger.Log("error", "Error importing scene: " + resp.ErrorMessage);
                Status("error", "Error importing scene");
                if (OnSceneImported != null)
                {
                    OnSceneImported(resp.ErrorMessage);
                }
            }
            else
            {
                RSStateData defaultState = new RSStateData();
                defaultState.StateCommands.Add(new RSCommand("use_scope",
                        "scope_name", UserScope
                ));

                Service.DefaultStateData = defaultState;
                Hashtable sceneData = resp.Result as Hashtable;
                if (sceneData == null)
                {
                    Logger.Log("error", "Error receiving scene data from import!");
                    Status("error", "Error importing scene");
                    if (OnSceneImported != null)
                    {
                        OnSceneImported("No scene data returned from import.");
                    }
                    return;
                }
                CameraName = (string)sceneData["camera"];
                CameraInstanceName = (string)sceneData["camera_instance"];
                OptionsName = (string)sceneData["options"];
                RootGroupName = (string)sceneData["rootgroup"];

                if (OnSceneImported != null)
                {
                    OnSceneImported(null);
                }
            }
        }
        
        protected void OnSceneGetBoundingBox(RSResponse resp)
        {
            if (resp.IsErrorResponse)
            {
                Logger.Log("error", "Error getting scene bounding box: " + resp.ErrorMessage);
                Status("error", "Error importing scene");
                return;
            }

            Hashtable bbox = resp.Result as Hashtable;
            if (bbox == null)
            {
                Logger.Log("error", "Scene bounding box result was not an object: " + resp.Result.GetType());
                Status("error", "Error importing scene");
                return;
            }
            BBoxMax.X = Convert.ToDouble(bbox["max_x"]); 
            BBoxMax.Y = Convert.ToDouble(bbox["max_y"]); 
            BBoxMax.Z = Convert.ToDouble(bbox["max_z"]); 

            BBoxMin.X = Convert.ToDouble(bbox["min_x"]); 
            BBoxMin.Y = Convert.ToDouble(bbox["min_y"]); 
            BBoxMin.Z = Convert.ToDouble(bbox["min_z"]); 
        }

        public void Shutdown()
        {
            if (IsShutdown)
            {
                return;
            }
            IsShutdown = true;
            if (OnShutdown != null)
            {
                OnShutdown();
            }
        }

    }
}

