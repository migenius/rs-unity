using UnityEngine;
using UnityEngine.UI;
using System.IO;
using com.migenius.rs4.core;

namespace com.migenius.rs4.unity
{
    public class UnityTextureRenderTarget : MonoBehaviour, IRSRenderTarget
    {
        protected int newWidth = 0;
        protected int newHeight = 0;
        protected bool newResolution = true;
        private bool isDestoryed = false;

        public bool OnLoad(RSRenderCommand command, RSService service, byte[] data)
        {
            if (GetComponent<RawImage>() != null)
            {
                Texture2D t2d = null;

                if (GetComponent<RawImage>() == null || (newResolution && newWidth > 0 && newHeight > 0))
                {
                    newResolution = false;
                    t2d = new Texture2D(newWidth, newHeight);
                    GetComponent<RawImage>().texture = t2d;
                }
                else
                {
                    t2d = GetComponent<RawImage>().texture as Texture2D;
                }

                if (t2d != null && data != null)
                {
                    t2d.LoadImage(data);
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
            newWidth = Screen.width;
            newHeight = Screen.height;
            newResolution = true;
        }

        void Update()
        {
        }

        void OnDestroy()
        {
            isDestoryed = true;
        }

        public bool Valid()
        {
            return !isDestoryed;
        }
    }
}
