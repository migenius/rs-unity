# rs-unity

## Description

RealityServer Client Library for the Unity Game Engine

1. Open the folder containing 'Assets', 'ProjectSettings' and 'Library' in Unity from 'Open Project...' in the file menu.
2. Go to the 'RealityServer' object in the Hierachy
3. Under the 'Unity Viewport' group edit the attributes for 'Host' (and 'Port' if required) to point to your running instance of RealityServer.
4. Hit play and after a brief delay you should see a message that says 'Navigate to Render' in the viewport.
5. Navigate the viewport and when you stop navigating you should see a RealityServer rendering.
6. You can also drag the 3 scene elements in the viewport and the RealityServer objects will move along with them.

## Notes

- Requires RealityServer 4.3 build 1346.112 or later to provide the server side scene.

- Requires Unity 5.3.4f1 or later. This was recently updated. If you require support for Unity 4.x please clone an earlier version of the repository. Support for older versions will not be maintained.

- If there is no scene open when you open the project, go to _File → Open_ Scene and select _tableScene.unity_ to get the default scene with RealityServer setup.

- For some reason the Unity Editor does not allow mouse navigavtion when viewed over Chrome Remote Desktop. The wheel will work but not mouse navigation. Other remote desktop tools may be affected in a similar way.

- If your RealityServer installation resides on a different machine to your where you are running Unity or serving pages with Unity content you may need to enable CORS hanlding in ```realityserver.conf```. You can find the relevant commented out section in the default file.

- Additionally. the Unity player expects a ```crossdomain.xml``` file to be at the root of the site to which requests are made by the player. The following default will allow access from all machines (you can of course be more specific), This file should be placed in your content_root folder on your RealityServer:

```xml
<?xml version="1.0"?>
<cross-domain-policy>
<allow-access-from domain="*"/>
</cross-domain-policy>
```
