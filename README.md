# rs-unity

## Description

RealityServer Client Library for the Unity Game Engine

1. Open the folder containing 'Assets', 'ProjectSettings' and 'Library' in Unity from 'Open Project...' in the file menu.
2. Go to the 'RealityServer' object in the Hierachy
3. Under the 'Unity Viewport' group edit the attributes for 'Host' (and 'Port' if required) to point to your running instance of RealityServer.
4. Hit play and after a brief delay you should see a message that says 'Navigate to Render' in the viewport.
5. Navigate the viewport and when you stop navigating you should see a RealityServer rendering.

## Notes

If your RealityServer installatio resides on a different machine to your where you are running Unity or serving pages with Unity content you may need to enable CORS hanlding in ```realityserver.conf```. You can find the relevant commented out section in the default file.

Additionally. the Unity player expects a ```crossdomain.xml``` file to be at the root of the site to which requests are made by the player. The following default will allow access from all machines (you can of course be more specific):

```xml
<?xml version="1.0"?>
<cross-domain-policy>
<allow-access-from domain="*"/>
</cross-domain-policy>
```

This file should be placed in your content_root folder on your RealityServer.

More detailed descriptions to come later!
