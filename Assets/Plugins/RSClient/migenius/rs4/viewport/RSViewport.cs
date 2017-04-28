using System;
using System.Collections;
using com.migenius.rs4.core;
using com.migenius.rs4.math;


namespace com.migenius.rs4.viewport
{
    public class RSViewport
    {
        public delegate void RestartRenderCallback();
        public event RestartRenderCallback OnRestartRender;
        public event ResponseHandler OnRender;

        public RSCamera Camera { get; set; }
        public string Renderer = "default";

        public bool UseRenderLoop = false;
        public string RenderLoopHandler = "default";
        public string RenderLoopName = null;
        public float RenderLoopTimeout = 30;
        public Hashtable RenderLoopContextOptions = new Hashtable();
        public float RenderLoopInterval = 0.2f;
        public IRSRenderTarget RenderTarget = null;
        public bool ReceivedFirstRender { get; protected set; }
        public bool RenderLoopRunning { get; protected set; }
        public bool IsConverged { get; protected set; }

        public RSScene Scene { get; protected set; }
        protected RSService KeepAliveService = null;
        public RSService Service
        {
            get
            {
                return Scene.Service;
            }
        }

        protected bool UpdateNextRender = false;
        protected bool UpdatePollResult = false;
        protected float nextRenderTimer = 0;
        protected float pollResultTimer = 0;
        protected DateTime NextRenderTime = DateTime.Now; 
        protected DateTime LastRenderTime = DateTime.Now;
        protected int LastRenderResultCounter = 0;

        public RSViewport(RSScene scene)
        {
            Scene = scene;
            Camera = new RSCamera();

            Scene.OnSceneImported += new SceneImportedCallback(OnSceneImport);
        }

        protected void InitValues()
        {
            ReceivedFirstRender = false;

            string rand = RSUtils.RandomString();
            if (RenderLoopName == null || RenderLoopName.Length == 0)
            {
                RenderLoopName = "loop_" + rand;
            }
            
            KeepAliveService = new RSService(Scene.Host, Scene.Port);
        }

        public void Status(string type, string message)
        {
            if (Scene != null)
            {
                Scene.Status(type, message);
            }
        }

        protected void OnSceneImport(string errorMessage)
        {
            if (errorMessage != null)
            {
                return;
            }
            InitValues();
            
            if (Camera.CameraName == null || Camera.CameraName.Length == 0)
            {
                Camera.CameraName = Scene.CameraName;
            }
            if (Camera.CameraInstanceName == null || Camera.CameraInstanceName.Length == 0)
            {
                Camera.CameraInstanceName = Scene.CameraInstanceName;
            }

            StartRendering();
        }

        protected void StartRendering()
        {
            if (RenderTarget == null)
            {
                Logger.Log("error", "Unable to start rendering without a render target.");
                Status("error", "Error starting render loop");
                return;
            }

            if (UseRenderLoop)
            {
                StartRenderLoop();
            }
            else
            {
                StartLocalLoop();
            }
        }

        protected void StartRenderLoop()
        {
            Status("info", "Starting render loop");
            Service.AddCommand(new RSCommand("render_loop_start",
                    "render_loop_name", RenderLoopName,
                    "render_loop_handler_name", RenderLoopHandler,
                    "scene_name", Scene.Name,
                    "render_loop_handler_parameters", new ArrayList() { "renderer", Renderer },
                    "timeout", RenderLoopTimeout,
                    "render_context_options", RenderLoopContextOptions
            ), OnStartRenderLoop);
        }

        public void Update(float elapsed)
        {
            nextRenderTimer += elapsed;
            pollResultTimer += elapsed;

            if (nextRenderTimer > RenderLoopInterval)
            {
                nextRenderTimer -= RenderLoopInterval;
                if (UpdateNextRender)
                {
                    GetNextRenderLoopRender();
                }
            }
            if (pollResultTimer > RenderLoopTimeout / 3)
            {
                pollResultTimer -= RenderLoopTimeout / 3;
                if (UpdatePollResult)
                {
                    GetPollResult();
                }
            }
        }

        protected void OnStartRenderLoop(RSResponse resp)
        {
            if (resp.IsErrorResponse)
            {
                Logger.Log("error", "Error starting render loop: " + resp.ErrorMessage);
                Status("error", "Error starting render loop");

                return;
            }

            Status("info", "Waiting for first render");

            RenderLoopRunning = true;

            UpdatePollResult = true;

            RestartLoop();
        }

        protected void GetPollResult()
        {
            Logger.Log("debug", "Sending keep alive.");
            KeepAliveService.AddCommand(new RSCommand("render_loop_keep_alive",
                    "render_loop_name", RenderLoopName
            ));
        }

        public void RestartLoop()
        {
            if (Scene.IsShutdown)
            {
                return;
            }

            if (OnRestartRender != null)
            {
                OnRestartRender();
            }

            UpdateNextRender = false;

            IsConverged = false;
            LastRenderResultCounter = 0;
            NextRenderTime = DateTime.Now;
            Service.AddCallback((RSCommandSequence seq) => {

                    Camera.UpdateCamera(seq);

                    MarkDirty(seq);
                    seq.AddCommand(new RSRenderCommand(RenderTarget, "render_loop_get_next_render",
                            "render_loop_name", RenderLoopName,
                            "format", "jpg",
                            "quality", 90,
                            "pixel_type", "Color"
                    ), OnGetRenderLoopRender);
            });
        }

        protected void GetNextRenderLoopRender()
        {
            if (Scene.IsShutdown)
            {
                return;
            }

            UpdateNextRender = false;

            LastRenderTime = DateTime.Now;
            Service.AddCommand(new RSRenderCommand(RenderTarget, "render_loop_get_last_render",
                    "render_loop_name", RenderLoopName,
                    "format", "jpg",
                    "quality", 90,
                    "pixel_type", "Color"
            ), OnGetRenderLoopRender);
        }
        protected void OnGetRenderLoopRender(RSResponse resp)
        {
            if (Scene.IsShutdown)
            {
                return;
            }

            // Ignore these renders.
            if (LastRenderTime > NextRenderTime && OnRender != null)
            {
                OnRender(resp);
            }
            
            if (resp.IsErrorResponse)
            {
                if (ReceivedFirstRender)
                {
                    Logger.Log("error", "Error getting render loop render.");
                    Status("error", "Error retrieving render");
                    UpdateNextRender = false;
                }
                UpdateNextRender = true;
                return;
            }

            LastRenderResultCounter++;
            if (LastRenderResultCounter > 5)
            {
                LastRenderResultCounter = 0;
                GetLastRenderResult();
            }

            if (!ReceivedFirstRender)
            {
                Status("render", "");
            }
            ReceivedFirstRender = true;

            if (!IsConverged)
            {
                UpdateNextRender = true;
            }
        }
        protected void GetLastRenderResult()
        {
            Service.AddCommand(new RSCommand("render_loop_get_last_render_result",
                "render_loop_name", RenderLoopName
            ), (RSResponse resp) => {
                int result = Convert.ToInt32(resp.Result);
                if (result > 0)
                {
                    Logger.Log("info", "Converged!");
                    IsConverged = true;
                }
            });
        }

        protected void StartLocalLoop()
        {
            // TODO
            Logger.Log("info", "Currently only render loop is supported.");
        }

        public void Shutdown()
        {
        }

        public void MarkDirty()
        {
            MarkDirty(Service);
        }
        protected void MarkDirty(IAddCommand seq)
        {
            if (UseRenderLoop)
            {
				seq.AddCommand(new RSCommand("render_loop_cancel_render",
                        "render_loop_name", RenderLoopName
                ));
            }
        }
        public void UpdateResolution(int width, int height)
        {
            RenderTarget.UpdateResolution(width, height);
            Camera.ResolutionX = width;
            Camera.ResolutionY = height;
        }
    }
}
