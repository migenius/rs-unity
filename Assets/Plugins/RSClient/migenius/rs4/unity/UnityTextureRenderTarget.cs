using UnityEngine;
using System.IO;
using com.migenius.rs4.core;

namespace com.migenius.rs4.unity
{
    public class UnityTextureRenderTarget : MonoBehaviour, IRSRenderTarget
    {
        public byte [] CurrentData { get; protected set; }
        public bool HasNewData { get; protected set; }
        
        protected int newWidth = 0;
        protected int newHeight = 0;
        protected bool newResolution = true;

        public bool OnLoad(RSRenderCommand command, RSService service, byte[] data)
        {
            CurrentData = data;
            HasNewData = true;



            if (GetComponent<GUITexture>() != null)
            {
                Texture2D t2d = null;

                if (GetComponent<GUITexture>().texture == null || (newResolution && newWidth > 0 && newHeight > 0))
                {
                    newResolution = false;
                    t2d = new Texture2D(newWidth, newHeight);
                    GetComponent<GUITexture>().texture = t2d;
                }
                else
                {
                    t2d = GetComponent<GUITexture>().texture as Texture2D;
                }

                if (t2d != null)
                {

                    if (CurrentData != null && HasNewData)
                    {
                        t2d.LoadImage(CurrentData);
                        HasNewData = false;
                    }
                }
            }




            return true;
        }
        public void OnError(string error)
        {
            com.migenius.rs4.core.Logger.Log("error", "Error loading render target: " + error);
        }

        public void UpdateResolution(int width, int height)
        {
            if (newWidth != width || newHeight != height)
            {
                newWidth = width;
                newHeight = height;
                newResolution = true;
            }
        }

        void Start()
        {
            CurrentData = null;
            HasNewData = false;
            
            newWidth = Screen.width;
            newHeight = Screen.height;
            newResolution = true;
        }

        void Update()
        {
        }
    }
}
