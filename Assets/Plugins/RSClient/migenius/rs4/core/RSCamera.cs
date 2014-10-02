using System;
using System.Collections;
using System.Collections.Generic;
using com.migenius.rs4.core;
using com.migenius.rs4.math;

namespace com.migenius.rs4.core
{
    /**
     * This class represents a camera in RealityServer.
     *
     * This can be used to change the camera's transform, resolution, field of view and if it's orthographic.
     * Only values that have changed will be sent to RealityServer.
     */
    public class RSCamera
    {
        protected Dictionary<string, bool> changedValues = new Dictionary<string, bool>();
        protected Matrix3D transformMatrix = new Matrix3D();
        public Matrix3D TransformMatrix 
        {
            get
            {
                return transformMatrix;
            }
            set
            {
                if (!transformMatrix.Compare(value))
                {
                    transformMatrix = value;
                    Changed("matrix");
                }
            }
        }
        protected int resolutionX = 320;
        public int ResolutionX
        {
            get
            {
                return resolutionX;
            }
            set
            {
                if (resolutionX != value)
                {
                    resolutionX = value;
                    aspect = resolutionY < 1 ? 0.0f : (float)resolutionX / (float)resolutionY;
                    Changed("resX");
                }
            }
        }
        protected int resolutionY = 240;
        public int ResolutionY
        {
            get
            {
                return resolutionY;
            }
            set
            {
                if (resolutionY != value)
                {
                    resolutionY = value;
                    aspect = resolutionY < 1 ? 0.0f : (float)resolutionX / (float)resolutionY;
                    Changed("resY");
                }
            }
        }
        protected float aspect = (4.0f / 3.0f);
        public float Aspect
        {
            get
            {
                return aspect;
            }
        }

        protected float fieldOfView = 0.5f;
        public float FieldOfView
        {
            get
            {
                return fieldOfView;
            }
            set
            {
                if (fieldOfView != value)
                {
                    fieldOfView = value;
                    if (!Orthographic)
                    {
                        Changed("fov");
                    }
                }
            }
        }
        protected bool orthographic = false;
        public bool Orthographic
        {
            get
            {
                return orthographic;
            }
            set
            {
                if (orthographic != value)
                {
                    orthographic = value;
                    Changed("ortho");
                }
            }
        }
        protected float orthographicSize = 5;
        public float OrthographicSize
        {
            get
            {
                return orthographicSize;
            }
            set
            {
                if (orthographicSize != value)
                {
                    orthographicSize = value;
                    if (Orthographic)
                    {
                        Changed("orthoSize");
                    }
                }
            }
        }

        public bool HasChanges
        {
            get
            {
                return changedValues.Count > 0;
            }
        }

        protected void Changed(string name)
        {
            changedValues[name] = true;
            _hasNewChanges = true;
        }
        protected bool HasChanged(string name)
        {
            if (changedValues.ContainsKey(name))
            {
                return changedValues[name];
            }
            return false;
        }

        public string CameraName;
        public string CameraInstanceName;
        private bool _hasNewChanges = false;
        public bool HasNewChanges
        {
            get
            {
                if (_hasNewChanges)
                {
                    _hasNewChanges = false;
                    return true;
                }
                return false;
            }
        }

        public RSCamera()
        {

        }
        public RSCamera(string name, string instName)
        {
            CameraName = name;
            CameraInstanceName = instName;
        }

        /**
         * If any changes have been made to the camera since the last time this function was called,
         * then commands for updating those values on the server will be added to the given command sequence.
         */
        public void UpdateCamera(IAddCommand seq)
        {
            if (CameraName == null || CameraName.Length == 0 ||
                CameraInstanceName == null || CameraInstanceName.Length == 0)
            {
                Logger.Log("error", "Cannot update camera until names have been set.");
                return;
            }

            if (changedValues.Count == 0)
            {
                return;
            }

            if (HasChanged("matrix"))
            {
                seq.AddCommand(new RSCommand("instance_set_world_to_obj",
                        "instance_name", CameraInstanceName,
                        "transform", TransformMatrix.GetMatrixForRS()
                ));
            }

            if (HasChanged("resX") || HasChanged("resY"))
            {
                seq.AddCommand(new RSCommand("camera_set_aspect",
                        "camera_name", CameraName,
                        "aspect", Aspect
                ));

                seq.AddCommand(new RSCommand("camera_set_resolution",
                        "camera_name", CameraName,
                        "resolution", new Hashtable() {
                            { "x", ResolutionX },
                            { "y", ResolutionY }
                        }
                ));
            }

            if (HasChanged("ortho"))
            {
                seq.AddCommand(new RSCommand("camera_set_orthographic",
                        "camera_name", CameraName,
                        "orthographic", Orthographic
                ));
            }
            
            // As the aperture is used by both orthographic and perspective cameras
            // got slightly different things, we need to update the aperture if 
            // the orthographic value has changed.
            if ((HasChanged("ortho") && Orthographic) || HasChanged("orthoSize"))
            {
                seq.AddCommand(new RSCommand("camera_set_aperture",
                        "camera_name", CameraName,
                        "aperture", OrthographicSize * 2.0f * Aspect
                ));
            }
            else if ((HasChanged("ortho") && !Orthographic) || HasChanged("fov"))
            {
                // As we currently do not load any camera values from the 
                // server, we need to choose an aperture to work with.
                double aperture = 1.417;
                double deg2rad = Math.PI / 180.0;
                double focal = ((aperture / Aspect) / 2.0f) 
                    / Math.Tan(FieldOfView / 2.0f * deg2rad);
                seq.AddCommand(new RSCommand("camera_set_aperture",
                        "camera_name", CameraName,
                        "aperture", aperture
                ));
                seq.AddCommand(new RSCommand("camera_set_focal",
                        "camera_name", CameraName,
                        "focal", focal
                ));
            }

            changedValues.Clear();
            _hasNewChanges = false;
        }
    }
}
