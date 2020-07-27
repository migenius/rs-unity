# rs-unity

## Description

This is the RealityServer Client Library for the Unity Game Engine. This project contains tools for exporting Unity scenes in a format suitable for use in RealityServer as well as tools to communicate with RealityServer to manipulate the scene at runtime. The following is needed to get started.

1. Open the folder containing 'Assets', 'ProjectSettings' and 'Library' in Unity from 'Open Project...' in the file menu.
2. Go to the 'RealityServer' object in the Hierachy
3. Under the 'Unity Viewport' group edit the attributes for 'Host' (and 'Port' if required) to point to your running instance of RealityServer.
4. Hit play and after a brief delay you should see a message that says 'Navigate to Render' in the viewport.
5. Navigate the viewport and when you stop navigating you should see a RealityServer rendering.
6. You can also drag the 3 scene elements in the viewport and the RealityServer objects will move along with them.

## MDL Material

If using the provided exporter, you wil need to install the _Assets/mdl/migenius/unity.mdl_ file into your RealityServer installation. Simply copy the file from this repository into the _mdl/migenius_ folder of your RealityServer installation.

## Exporter

The project includes an Editor script for exporting scene data from Unity to RealityServer. You can access this functionality from _Window → RealityServer_ in the Unity Editor. Populate the server, port and filename fields and hit the Convert button.

If your scene is large enough the requests to RealityServer during conversion may exceed the default POST limits (since they will include the mesh and texture information). You can increase this limit in your _realityserver.conf_ file using this directive:

```
# Increase POST body limit to 100MB
http_post_body_limit 104857600
```

If you have a large scene or many textures you may need to increase this limit significantly. Currently the converter supports relatively complete translation of the following:

- Mesh geometry
- Scene graph hierarchy
- Camera

Partial support has been implemented for the following:

- Basic material parameters (Color, roughness, metallic)
- Albedo texture map
- Texture transformations
- Sub-materials (multiple materials per mesh)
- Material emission (units currently incorrect, exponent incorrect)
- Spot and infinite lights (units currently incorrect)

The _Assets/Editor/RealityServer.cs_ editor script can be used as a reference for further development and customisation. The current exporter is not designed to exactly replicate the Unity results, rather it provides a starting point for the scene to be manually refined. With further development matching Unity more closely should be possible.

Exported element names follow predictable patterns. This allows you to find RealityServer elements that correspond to their Unity counterparts using only information available through the Unity scripting tools. The Naming convention for objects (meshs), instances, groups and group instances respectively is:

- name_id_obj
- name_id_obj_inst
- name_id_grp
- name_id_grp_inst

The use of the id ensures unique names, since Unity does not enforce this in its scene graph but RealityServer requires it for its own.

## Notes

- Requires RealityServer 5.3 build 2593.402 or later to provide the server side scene.

- Requires Unity 2019.4.5f1 or later. This was recently updated. If you require support for Unity 5.x please clone the appropriate branch of the repository. Support for older versions will not be maintained.

- If there is no scene open when you open the project, go to _File → Open_ Scene and select _tableScene.unity_ to get the default scene with RealityServer setup.

- For some reason the Unity Editor does not allow mouse navigavtion when viewed over Chrome Remote Desktop. The wheel will work but not mouse navigation. Other remote desktop tools may be affected in a similar way.

- For the camera to be translated you must have set the _MainCamera_ tag on one of the cameras in your Unity scene. If you do not a warning will be emitted during translation.

- If your RealityServer installation resides on a different machine to your where you are running Unity or serving pages with Unity content you may need to enable CORS hanlding in ```realityserver.conf```. You can find the relevant commented out section in the default file.

- Additionally. the Unity player expects a ```crossdomain.xml``` file to be at the root of the site to which requests are made by the player. The following default will allow access from all machines (you can of course be more specific), This file should be placed in your content_root folder on your RealityServer:

```xml
<?xml version="1.0"?>
<cross-domain-policy>
<allow-access-from domain="*"/>
</cross-domain-policy>
```
