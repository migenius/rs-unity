namespace com.migenius.rs4.core
{
    public interface IRSRenderTarget
    {
        bool OnLoad(RSRenderCommand command, RSService service, byte[] data);
        void OnError(string error);
        void UpdateResolution(int width, int height);
    }
}
