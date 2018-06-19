using UnityEngine;
using System.Collections;
using com.migenius.rs4.core;
using com.migenius.rs4.math;
using com.migenius.rs4.viewport;
using Logger = com.migenius.rs4.core.Logger;

namespace com.migenius.rs4.unity
{
    public class IrayBackground : MonoBehaviour
    {
        protected UnityViewport Viewport;
        
        // Iray background colour
        public Color Colour = new Color(1, 1, 1, 0);
        private Color OldColour = new Color(1, 1, 1, 1);
        // Local copy of enabled for threading 
        private bool Enabled = false;

        void Start()
        {
            Viewport = GetComponent<UnityViewport>();
            if (Viewport == null)
            {
                Logger.Log("error", "Iray background colour component needs a UnityViewport component to work with.");
                this.enabled = false;
                return;
            }
            if (Viewport.enabled == false)
            {
                Logger.Log("error", "Iray background colour component needs a UnityViewport that is enabled to work with.");
                this.enabled = false;
                return;
            }
            Enabled = this.enabled;
            Viewport.OnAppIniting += new ApplicationInitialisingCallback(OnAppIniting);
            
            Color temp = Colour;
            temp.a = temp.a > 0.0f ? 0.0f : 1.0f;
            OldColour = temp;
        }

        protected void UpdateIrayBackground(IAddCommand seq)
        {
            if (Enabled)
            {
                Hashtable colour = new Hashtable() {
                    {"r", Colour.r},
                    {"g", Colour.g},
                    {"b", Colour.b},
                    {"a", 1.0f}
                };
                seq.AddCommand(new RSCommand("element_set_attribute",
                        "element_name", Viewport.Scene.OptionsName,
                        "attribute_name", "iray_background_color",
                        "attribute_type", "Color",
                        "attribute_value", colour,
                        "create", true
                ));
            }
            else
            {
                seq.AddCommand(new RSCommand("element_remove_attribute",
                        "element_name", Viewport.Scene.OptionsName,
                        "attribute_name", "iray_background_color"
                ));
            }
        }

        protected void OnAppIniting(RSCommandSequence seq)
        {
            UpdateIrayBackground(seq);
        }

        void OnEnable()
        {
            Enabled = true;
            if (Viewport != null)
            {
                Viewport.Service.AddCallback(UpdateIrayBackground);
            }
        }
        void OnDisable()
        {
            Enabled = false;
            if (Viewport != null && Viewport.Service != null)
            {
                Viewport.Service.AddCallback(UpdateIrayBackground);
            }
        }

        void Update()
        {
            if (Viewport == null || !Viewport.enabled || !Viewport.Scene.Ready)
            {
                return;
            }

            if (Viewport.Scene.Ready && OldColour != Colour)
            {
                OldColour = Colour;

                Viewport.Service.AddCallback(UpdateIrayBackground);
            }
        }
    }
}
