using System;
using System.Collections;
using System.Collections.Generic;
using com.migenius.rs4.core;
using com.migenius.rs4.math;

namespace com.migenius.rs4.core
{
    /**
     * This has handles importing a specific scene on RealityServer and then loading some
     * information about the scene, such as the bounding box.
     *
     * Events at each stage of the importing process are triggered.
     */
    public class RSScene
    {
        /**
         * Triggered once the scene has imported but before the rest of the data
         * has been loaded. Will contain an error if an error occured during import.
         */
        public event SceneImportedCallback OnSceneImported;
        /**
         * Triggered after the scene has loaded and the first set
         * of commands for loading further importing about the scene
         * are being sent.
         */
        public event ApplicationInitialisingCallback OnAppIniting;
        /**
         * Triggered after the initial set of commands have completed.
         * At this point all data should be loaded.
         */
        public event ApplicationInitialisedCallback OnAppInited;
        
        /**
         * Triggered everytime there is a status update. These updates
         * are designed to be something that can be displayed to the user.
         */
        public event StatusUpdateCallback OnStatus;

        /**
         * Triggered when the scene needs to shutdown.
         */
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
        
        // These define the bounding box of the scene. 
        // These will be null if the data has not yet loaded.
        public Vector3D BBoxMax { get; protected set; }
        public Vector3D BBoxMin { get; protected set; }
        
        public RSService Service { get; protected set; }
        private string _host = "localhost";
        private int _port = 8080;
        private int _timeout = 100;
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
        public int Timeout 
        {
            get
            {
                if (Service != null)
                {
                    return Service.Timeout;
                }
                return _timeout;
            }
            set
            {
                if (Service == null)
                {
                    _timeout = value;
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
        public RSScene(string host, int port, int timeout = 100)
        {
            Host = host;
            Port = port;
            Timeout = timeout;
        }
        
        public void Status(string type, string message)
        {
            if (OnStatus != null)
            {
                OnStatus(type, message);
            }
        }

        /**
         * Sets up any values that are still null.
         */
        public void InitValues()
        {
            // Create a service if one wasn't given.
            if (Service == null)
            {
                Service = new RSService(Host, Port, Timeout);
            }
            
            // Default the scene name to be the same as the filename.
            if (Name == null)
            {
                Name = Filename;
            }
            
            // Create a user scope if one wasn't given.
            string rand = RSUtils.RandomString();
            if (UserScope == null || UserScope.Length == 0)
            {
                UserScope = "user_" + rand;
            }
        }
        
        /**
         * Creates the application and user scopes and imports the scene into the user scope.
         * Also gets the bounding box.
         */
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
                    
                    // All commands after this point will be done in the user scope.
                    RSStateData defaultState = new RSStateData();
                    defaultState.StateCommands.Add(new RSCommand("use_scope",
                                "scope_name", UserScope
                                ));

                    Service.DefaultStateData = defaultState;
            });
        }
        
        /**
         * The scene has loaded so we now have the basic scene data, such as the name of the camera and options.
         *
         * This will go through and localise those elements into the user scope.
         */
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

                // Let anyone listening to add more commands at this point.
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

        /**
         * This handles the import_scene response
         */
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
        
        /**
         * Handle the bounding box response.
         */
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
            
            BBoxMax = new Vector3D(
                Convert.ToDouble(bbox["max_x"]),
                Convert.ToDouble(bbox["max_y"]),
                Convert.ToDouble(bbox["max_z"]));
        
            BBoxMin = new Vector3D(
                Convert.ToDouble(bbox["min_x"]),
                Convert.ToDouble(bbox["min_y"]),
                Convert.ToDouble(bbox["min_z"]));
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

