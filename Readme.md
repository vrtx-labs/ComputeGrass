# ComputeGrass - Readme

<!--TOC-->
  - [Overview](#overview)
  - [System Requirements](#system-requirements)
    - [Editor & Runtime](#editor-runtime)
  - [Supported Platforms](#supported-platforms)
    - [iOS](#ios)
    - [Windows](#windows)
    - [Virtual Reality](#virtual-reality)
  - [The Compute Grass System](#the-compute-grass-system)
    - [Definition](#definition)
    - [Parameters](#parameters)
    - [Batch](#batch)
    - [Generator](#generator)
    - [Processor](#processor)
    - [Grass](#grass)
    - [Shaders](#shaders)
    - [Samples](#samples)
- [License](#license)
- [Contribute](#contribute)
- [Attributions](#attributions)
<!--/TOC-->

## Overview

The ComputeGrass package is a package for the Unity Package Manager to create grass and similar foliage for your project. It is intended to support a broad range of platforms and especially mobile platforms.


## System Requirements
The ComputeGrass package was developed in Unity3D 2020.3.x (2020 LTS) and tested and used in 2021.3.x. 
This defines the known requirements for you to use this package in your project.
Please make sure you use Unity3d 2020.3 or 2021.3 on your projects or check whether the package works with the version you are using.  

ComputeGrass is written for the built-in render pipeline and does not include support for SRP (URP and HDRP).
The package uses the ComputeShader feature of Unity and is constrained to hardware platforms and graphics APIs with the respective support for ComputeShaders. See [Unity 2020.3 - Manual - Compute Shaders](https://docs.unity3d.com/2020.3/Documentation/Manual/class-ComputeShader.html) for specific information about Compute Shader support in Unity.

### Editor & Runtime
* Unity Editor 2020.3 or newer
* Target Platform with [Unity Compute Shaders](https://docs.unity3d.com/2020.3/Documentation/Manual/class-ComputeShader.html) support 

## Supported Platforms
The ComputeGrass package was developed for a specific project and therefore should work on all platforms the application should run on.
This includes support on iOS, Android, Windows and Virtual Reality on Windows.  
  
It should work on nearly all platforms Unity supports, though it is not possible for us to test and validate it on all possible platforms.

### iOS
The package was tested on iOS/iPadOS version 15 and 16.  
It was tested on iPad 5th gen and iPhone 7 and/or newer devices (e.g. iPhone 12 Pro, iPad Pro 4th gen).  

### Windows
The package was tested on Windows 10 21H2 and newer (excluding Windows 11).  
It was tested on desktop and notebook devices with the following integrated and dedicated graphics cards:
* Intel HD Graphics 530
* Nvidia GTX 1060
* Nvidia GTX 1080
* Nvidia GTX 1080
* Nvidia Quadro M5000M


### Virtual Reality
The package was tested on Windows 10 21H2 using OpenXR.  
Standalone devices (e.g. Oculus Quest, Vive Focus) have not been tested.  
   
## The Compute Grass System
The ComputeGrass package contains multiple different components, types and assets which lay the foundation of a compute shader based grass system.
The current system is heavily influenced by the project it was primarily intended to be used in. 
The system uses the idea that there is polygonal geometry on which some sort of grass or foliage should be distributed without manually modelling or foliage painting.
This polygonal geometry is processed by a [generator](#generator), which requires a set of definition and parameters to build the source data for the [compute Shader](#shaders).

The [definition](#definition) describes the different variants of grass blades, which includes height, aspect-ratio, random height range, tapered root width, blade spread radius and curvature. 
One definition can be used to configure up to four different variants which are later rendered by the system. The definition is also the place where you can set a density map which is applied during the compute shader execution.  
The [parameters](#parameters) describes different values and conditions taken into account during the generation process, the compute shader execution and runtime rendering of ComputeGrass.
The parameters include the subdivision behaviour of the generator, material and compute shader references, polygon & blade limits, wind values, level of detail, shadow behaviour and update behaviour.

The aforementioned source data is a list of point data on the surface of the original polygonal geometry from which the compute shader will create the geometry of the grass for rendering.
The source data is grouped into a [batch](#batch) using the the parameter and definition, where the batch is the necessary asset to compute and render grass at runtime.

The [compute shader](#shaders) receives the batch's data blocks and is configured with the parameters and definition to build polygonal geometry. This geometry is pushed down the rendering pipeline with a material instance with the [graphics shader](#shaders).

This is in general the workflow and structure of the ComputeGrass system. The following sections provide you with detailed descriptions of different parts of the system, their properties, intended use and other remarks.

### Definition
The definition type can be found in `DefinitionAsset.cs` and is a leightweight wrapper for the blade arguments and and optional density map.
A definition contains at least one blade arguments and an optional blade texture, but can contain arguments for up to four blades. 

<details><summary><b>Members</b></summary>

* Density Map
    The density map is a texture, which is used to influence the distribution density of each type of blade. 
    In our compute shader the density map is sampled for each position in the source data and each color channel is applied as a density between 0-100%.
    This allows the reduction of of specific grass blade density in some areas and do some basic landscaping.
* Height
    >The height in world units of a grass blade. The height is the base value for the effective width of a blade and the random variance.
* Aspect Ratio
    >The aspect ratio between height and final width of a grass blade. Smaller values result in tall and thin blades, larger values in short and thick blades.
* Random Height
    >The random factor each individual blade may be scaled by (as a value from 0 - 100% scaling). A zero value will not apply random scale at all, all others influence the scale by random chance.
* Radius
    >The radius describes the circular area single grass blades will be randomly placed in. \
	The value range is limited as the radius may result in blades placed outside original mesh surface bounds.
* Forward
    >The distance the tip of any blade is displaced from the up-vector axis of each blade.
	The value only applies in combination with the `Curvature` value. A zero curvature will prevent any displacement
* Curvature
    >The amount of 'bend' applied to the grass blade along the segments vertices. 
    The value is used in an exponential function and allows an inward bend (0 < value < 1) or an outward bend (1 < value).
    You will not see any 'bend' when your grass blades only use one segment, though the blade tip will be displaced by the `forward` value.
* Root Width
    >The width of a grass blade's root in relation to it's effective width.
	This only applies to tapered grass blades and has no effect on rectangular grass blades.
* Texture
    >The per-variant texture which is applied with standard mapping to the full grass blade.
    The texture defaults to a white texture and is multiplied tint colors and light/shadow values.
	
</details>


### Parameters
The parameters type can be found in `Parameters.cs` which is also wrapped into an scriptable object in the `ParametersAsset.cs`. 
The parameters contain different values and options to configure the generation, computation and rendering of the grass. 
All options are grouped into six groups.\
In **Material & Shader** the reference material and compute shader asset are defined.\
In **Subdivision** the subdivision behaviour of the generator is defined, by different modes, subdiv iterations, randomness seed and offset options.\
In **Geometry** the amount and shape of the polygon computed per source data point can be controlled.\
In **LOD** the level of detail behaviour can be enabled and adjusted. It requires per-frame update.\
In **Wind** the wind impact on computed grass can be enabled and adjusten. It requires per-frame update.\
In **Other** the shadow behaviour and update behaviour can be configured.\

<details><summary><b>Members</b></summary>

* Material
    >The graphic material used to render the computed grass geometry. \
	The material must use the `ComputeGrass/Grass` graphics shader or a shader which includes the same or similar vertex stage.
* Compute Shader
    >The compute shader should always be the `Grass` compute shader 

* Seed
    >The seed set at the beginning of the generation process, use -1 to use random seed every generation, use fixed value to reproduce previous generation results.
* Subdivision Level
    >The number of levels the subdivision routine should go through.
	This is zero by default and every subdivision level will increase the amount of generated data (and in consequence the computed grass geometry).
	You may want to increase the level to get more dense source data.
* Subdivision Method
    >The method used in subdivision. Available methods are\
	>**Center of Mass**\
	This subdivision splits each triangle into three new triangles using the center of mass and corner points of the original triangle.
	It results in a triplication of data for each subdivision level.\
	>**Center of Hypothenuse to Opposing Corner**\
	This subdivision splits each triangle into two new triangles using the center of the hypothenuse and the opposite corner of the original triangle.
	It results in a duplication of data for each subdivision level.
    
* Subdivision Options
    >The options are additional behaviours applied during subdivision.\
	It allows you to define random offsets applied during subdivision, either on every level or only on the very last one.
	This option is used to add randomness and noise into the generated data and break patterns which may occure on well-structured input geometry.
* Triangle Area Cutoff
    >The area cutoff defines the area size of a triangle at which the triangle is no longer processed by subdivision and dropped from the data.\
	You get a log info when triangles are skipped from the `BatchGenerator` component.

* Blade Shape
    >Sets the shape of the grass blades to either rectangular or tapered.\
	The tapered blade shape always has a pointy tip and can have an adjusted root width. Use the tapered shape for more stylized blades.
* Blades per Vertex
    >The number of individual blades computed per position generated by the `BatchGenerator`.\
    Individual blades are randomly rotated and placed close around the source position and can be increased for a more dense visual impression
    This value applies to the computation and does not require regeneration.
    This value directly affects the amount of data computed and polygons rendered (See 'Segments Per Blade' below).
* Segments Per Blade
    >The number of segments an individual blade is constructed from where each segment consists of two triangles.
    A value of 1 or greater will allow the application of the `Curvature` value.
    Increase the number of segments allows more fidelity using other parameters at the cost of more data computed and polygons rendered.
    The amount of triangles per blade equals the amount of segments * 2. 
    To get the total number of triangles per source data position multiplies with the 'Blades Per Vertex' value to get a rough estimate.\
    Check the `BatchAsset` output from your generator for more complete memory estimate for your setup.

* Enable Level Of Detail
    >Enables the computation of level of detail geometry modifications.
    Support for Level of Detail requires the `Update` value set for `Update On` as it requires constant re-computation.
* Ignore in Editor (Deprecated)
    >Ignore Level of Detail during edit-time.
    This option will most likely be removed in the future as it is more of a burden to maintain and the LoD feature can be easily checked in play mode.
* Min Fade Distance
    >The minimum distance from the viewing camera to begin LoD fading on individual grass blades.
* Max Fade Distance
    >The maximum distance from the viewing camera the LoD fading effect should be at 100%.
    
* Wind Speed (Deprecated)
    >The speed of fake wind applied to each vertex in the grass blade geometry.\
    Wind is a fake/simulated effect and does not use any of the Unity wind features.\
    Support for Wind requires the `Update` value set for `Update On` as it requires constant re-computation.
* Wind Strength
    >A scaling factor applied to the computed wind direction offset
	
* Cast Shadow
    >Set the shadow casting mode for the rendering of the computed grass geometry.\
    Turn off shadow casting to reduce render load. This will still allow the grass to receive shadows but exempt it from shadow pass rendering.
* Update On
    >Set the re-computation trigger event on which the system dispatches the computation process to the GPU.\
	>**Enable**\
	Dispatches the computation only when the `Grass` component is enabled, e.g.: when OnEnable() is called. 
    This causes the computation to be done only once on enable, while the generated geometry is reused every frame for rendering.
    This trigger results in static geometry. You need to trigger re-computation yourself from code.\
	>**Update**\
	Dispatches the computation on every frame, e.g.: when Update() is called.
    This causes the computation to be done on every frame and causes the load on your target device GPU.\
    This trigger results in dynamic geometry which allows LoD and Wind or other changes you apply to take effect immediately.
	
</details>



### Batch
The batch type is implemented in `BatchAsset.cs` and is the consolidated data necessary and used at runtime to compute and render grass.
Each batch holds reference to one or more source data asset(s), and references to a definition and a parameters asset.
The references to source data assets are created during the generation process, while the definition and parameters references are passed over during generation.

You can change the definition and parameters references to adopt to different grass you want to use.
**Considerations**
>Please be aware that no parameters affecting generation will have any effect on the batch and the already generated source data.

>You should not alter the list of source data aside from the generation process, as this may leave unused and orphaned assets in your project.

The batch is also the place where you get an estimation of the required memory at runtime the grass will consume in your current configuration.
Selecting multiple batch assets will give you the summed up memory consumption of all selected batches.

<details><summary><b>Members</b></summary>

Not yet documented.
	
</details>

### Generator
**Remark**
>The generator is implemented as MonoBehaviour though it is not supported at runtime and auto-destroys itself in play mode.

The batch generator is implemented in `BatchGenerator.cs` and is the starting point for the generation process.
The generator utilizes the definition and parameters assets amd a list of mesh renderers (or a mesh renderer hierarchy root) to generate a batch asset.
This includes all source data required for runtime computation and rendering. 
The generator is capable of creating new batch assets at a selected location in your project, it will override the existing batch and source data assets.

<details><summary><b>Members</b></summary>

Not yet documented.
	
</details>

### Processor
The processor is implemented in `BatchProcessor.cs` and is the starting point for the compute and rendering process.
It itself does not handle any immediate rendering but holds a batch reference and distributes it to the actual rendering component [Grass](#grass).
The processor is responsible for creating nested objects at startup or activation and manage their lifecycle in edit and play mode.

### Grass
The grass is implemented in `Grass.cs` and is the core component of this system.
It handles the setup of grass data and buffers, invokation of the compute shader program and pushing the required draw calls to the rendering pipeline.

The generated source data, in form of a batch with it's associated definition and parameters, is processed by the grass component and allocated on the GPU.
It configures and handles all buffers and data used by the compute shader or in the graphics shader. Afterwards it calls the GPU to render the computed geometry from the compute shader which results in rendered grass.
The computation is invoked either only on activation or every frame, based on the `UpdateOn` parameter value.
\
The grass component is also responsible for the culling mechanism for the grass, this works identical to renderer culling within Unity (and utilizes an active MeshRenderer component) which allows for load and memory reduction for grass not visible or otherwise out of camera view.

Though the grass component is the core component you will not see it in edit-time or interact with it directly.
The lifecycle management is encapsulated in other components and hidden from the scene hierarchy.

### Shaders
The package comes with two shaders and a set of shared shader code files which are used in the different shaders.
The first shader is the compute shader. It is doing the heavy lifting and most of the 'complicated' things at runtime.
The second shader is the graphics shader. It is doing the final steps of drawing the computed geometry to the actual screen.

### Samples
The package contains a sample scene which demonstrates the usage of the ComputeGrass system. 
The sample is fairly simple and provides a grass configuration with four variants, distributed over multiple meshes (e.g. planes and spheres).

The sample scene can be found at `Assets/ComputeGrass/Samples/Sample.unity`.

# License

The license for the ComputeGrass package is not decided upon yet.

# Contribute

Feel free and encouraged to fork this project and open a pull request for features and fixes you come up with.


# Attributions
We specifically shout out to the following people. They provided the base for this project and shared their projects, ideas and code on which we have build upon.
In their approach on sharing and benefitting others from your own work, we at VRTX Labs follow in their steps with this package.

* @Minionsart and the AstroCat project for the BIRP compute shader implementation\
 https://www.patreon.com/posts/compute-grass-in-63162723 & https://www.patreon.com/minionsart/posts

* @forkerCat for shader interaction features on universal render pipeline\
 https://gist.github.com/forkercat/fb6c030c17fe1e109a34f1c92571943f

* @NedMakesGames for base compute shader for grass blade generation\
 https://gist.github.com/NedMakesGames/3e67fabe49e2e3363a657ef8a6a09838
