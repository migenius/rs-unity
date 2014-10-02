namespace com.migenius.rs4.core
{
    /**
     * This interface defines how image renders are handled.
     */
    public interface IRSRenderTarget
    {
        // When the image data has loaded successfully, this method is called
        // with the original command that was used to create this render,
        // the service that was used and the raw byte array of data that was returned.
        bool OnLoad(RSRenderCommand command, RSService service, byte[] data);

        // When an error occured, with a string giving some information about what happened.
        void OnError(string error);

        // Should be called when a render of a different size is being returned.
        // Not all render targets are going to care about this.
        void UpdateResolution(int width, int height);
    }
}
