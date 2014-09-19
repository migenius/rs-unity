using UnityEngine;
using System.IO;
using com.migenius.rs4.core;

namespace com.migenius.rs4.unity
{
    public class UnityTextureRenderTarget : MonoBehaviour, IRSRenderTarget
    {
        public byte [] CurrentData { get; protected set; }
        public bool HasNewData { get; protected set; }

        private object DataLock = new object();

        protected int newWidth = 0;
        protected int newHeight = 0;
        protected bool newResolution = true;

        public bool OnLoad(RSRenderCommand command, RSService service, byte[] data)
        {
            // Updating the texture has to be done in the game loop and not from a different thread.
            lock (DataLock)
            {
                CurrentData = data;
                HasNewData = true;
            }

            return true;
        }
        public void OnError(string error)
        {
            Logger.Log("error", "Error loading render target: " + error);
        }

        public void UpdateResolution(int width, int height)
        {
            lock (DataLock)
            {
                if (newWidth != width || newHeight != height)
                {
                    newWidth = width;
                    newHeight = height;
                    newResolution = true;
                }
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
            lock (DataLock)
            {
                if (guiTexture != null)
                {
                    Texture2D t2d = null;

                    if (guiTexture.texture == null || (newResolution && newWidth > 0 && newHeight > 0))
                    {
                        newResolution = false;
                        t2d = new Texture2D(newWidth, newHeight);
                        guiTexture.texture = t2d;
                    }
                    else
                    {
                        t2d = guiTexture.texture as Texture2D;
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
            }
        }
    }
}
