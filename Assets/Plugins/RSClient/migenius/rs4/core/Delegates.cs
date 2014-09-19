namespace com.migenius.rs4.core
{
    public delegate void ResponseHandler(RSResponse response);
    public delegate void SequenceHandler(RSCommandSequence sequence);
    
    public delegate void ImportCompleteCallback();
    public delegate void SceneImportedCallback(string errorMessage);
    public delegate void ApplicationInitialisingCallback(RSCommandSequence seq);
    public delegate void ApplicationInitialisedCallback();
    public delegate void StatusUpdateCallback(string type, string message);
    public delegate void ShutdownCallback();
}
