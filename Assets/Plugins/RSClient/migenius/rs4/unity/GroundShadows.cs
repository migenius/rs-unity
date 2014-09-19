using UnityEngine;
using com.migenius.rs4.core;
using com.migenius.rs4.math;
using com.migenius.rs4.viewport;

namespace com.migenius.rs4.unity
{
    public class GroundShadows : MonoBehaviour
    {
        protected UnityViewport Viewport;

        // Ground Shadow settings
        private bool Enabled = false;
        public bool AutoCalculateGroundHeight = true;
        public float GroundHeight = 0.0f;
        private bool OldEnableGroundShadow = false;
        private bool OldAutoCalculateGroundHeight = false;
        private float OldGroundHeight = -1.0f;
        private bool OldSceneYUp = false;

        void Start()
        {
            Viewport = GetComponent<UnityViewport>();
            if (Viewport == null)
            {
                Debug.Log("- Ground shadows component needs a UnityViewport component to work with.");
                this.enabled = false;
                return;
            }
            if (Viewport.enabled == false)
            {
                Debug.Log("- Ground shadows component needs a UnityViewport that is enabled to work with.");
                this.enabled = false;
                return;
            }
            Enabled = true;
            Viewport.OnAppIniting += new ApplicationInitialisingCallback(OnAppIniting);

            OldGroundHeight = GroundHeight + 1.0f;
            OldAutoCalculateGroundHeight = !AutoCalculateGroundHeight;
            OldSceneYUp = !Viewport.SceneYUp;
        }

        protected void OnAppIniting(RSCommandSequence seq)
        {
            UpdateGroundShadows(seq);
        }
        
        protected void UpdateGroundShadows(IAddCommand seq)
        {
            seq.AddCommand(new RSCommand("element_set_attribute",
                    "element_name", Viewport.Scene.OptionsName,
                    "attribute_name", "environment_dome_ground",
                    "attribute_type", "Boolean",
                    "attribute_value", Enabled,
                    "create", true
            ));
        
            if (Enabled)
            {
                Vector3D position = new Vector3D(0, Viewport.Scene.BBoxMin.Y);
                if (!AutoCalculateGroundHeight)
                {
                    position.Y = GroundHeight;
                }

                seq.AddCommand(new RSCommand("element_set_attribute",
                        "element_name", Viewport.Scene.OptionsName,
                        "attribute_name", "environment_dome_ground_position",
                        "attribute_type", "Float32<3>",
                        "attribute_value", position.GetVectorForRS(),
                        "create", true
                ));
                
                Vector3D axis = new Vector3D(0, 0, 1);
                if (Viewport.SceneYUp)
                {
                    axis.Y = 1;
                    axis.Z = 0;
                }

                seq.AddCommand(new RSCommand("element_set_attribute",
                        "element_name", Viewport.Scene.OptionsName,
                        "attribute_name", "environment_dome_rotation_axis",
                        "attribute_type", "Float32<3>",
                        "attribute_value", axis.GetVectorForRS(),
                        "create", true
                ));
            }
        }

        void OnEnable()
        {
            Enabled = true;
            if (Viewport != null)
            {
                Viewport.Service.AddCallback(UpdateGroundShadows);
            }
        }
        void OnDisable()
        {
            Enabled = false;
            if (Viewport != null)
            {
                Viewport.Service.AddCallback(UpdateGroundShadows);
            }
        }

        void Update()
        {
            if (Viewport != null && Viewport.Scene.Ready && 
                    (OldGroundHeight != GroundHeight ||
                    OldAutoCalculateGroundHeight != AutoCalculateGroundHeight ||
                    OldSceneYUp != Viewport.SceneYUp))
            {
                OldGroundHeight = GroundHeight;
                OldAutoCalculateGroundHeight = AutoCalculateGroundHeight;
                OldSceneYUp = Viewport.SceneYUp;

                Viewport.Service.AddCallback(UpdateGroundShadows);
            }
        }
    }
}
