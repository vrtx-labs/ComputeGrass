# ComputeGrass - Quick-Start

<!--TOC-->
  - [Overview](#overview)
  - [Requirements](#requirements)
  - [Included Sample](#included-sample)
  - [Parameters and Definition](#parameters-and-definition)
    - [Definition](#definition)
    - [Parameters](#parameters)
  - [Quick Start](#quick-start)
  - [Issues and Problems](#issues-and-problems)
<!--/TOC-->

## Overview

The ComputeGrass package is a package for the Unity Package Manager to create grass and similar foliage for your project. It is intended to support a broad range of platforms and especially mobile platforms.


## Requirements
The ComputeGrass package was developed in Unity3D 2020.3.x (2020 LTS) and tested and used in 2021.3.x. 
Please make sure you use one of the above versions or a newer version of the editor.
ComputeGrass is written for the built-in render pipeline and does not include support for SRP (URP and HDRP).

* Unity 2020.3.x or newer (also tested in Unity 2021.3.x)
* Use built-in render pipeline (BIRP)
* Target Platform Support for [ComputeShader](https://docs.unity3d.com/2020.3/Documentation/Manual/class-ComputeShader.html) 


## Included Sample
The package comes with a sample scene located under `Samples/Sample`. 
The scene includes the generator and processor components, a reference mesh scenery and other basic necessities like a camera and a general directional light.
The reference mesh scenery includes scaled and transformed geometry and geometry with non-planar aligned surfaces (e.g. spheres).

The definition makes use of the maximum of four variants. The variant arguments are identical except for the texture maps, which use red, green, blue and yellow solid colors.
Though this is not realistic grass at all, it provides a cleaner canvas to start from and see and understand the effect of the definition and parameters.
\
Check out the `Samples/Textures` folder for extra grass and foliage textures to play around with. Thanks to [https://opengameart.org](https://opengameart.org) for the resources.


## Parameters and Definition Overview

###  Definition
The definition addresses the differences between your variants. \
Adjust the height, aspect ratio and random height factor for your variants to make higher or wider grass blades or adjust to a textures aspect ratio.
Set or change the grass texture and tweak the computation of the grass geometry.

<details><summary><b>Members</b></summary>

* Density Map
    >The density map is a texture, which is used to influence the distribution density of each type of blade. 
    In our compute shader the density map is sampled for each position in the source data and each color channel is applied as a density between 0-100%.
    This allows the reduction of of specific grass blade density in some areas and do some basic landscaping.
    \
	The density map is optional and can be set to null. In this case the density is set to 100% for all positions.
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
    \
    The variant texture is optional and can be set to null. In this case the texture defaults to full white.
	Transparency of the texture is taken into account for the alpha cutoff, transparency blending is not supported.
	
</details>

### Parameters

The parameters are used to control the grass generation and part of the computation. \
It holds references to the compute shader and graphic material used to compute and render the grass blades.
You can configure the subdivision behaviour, geometry generation parameters, level of detail settings, wind, shadowing and update behaviour.

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

## Quick Start

This section guides you through the steps to setup and configure grass for your custom scenario. Check the technical [requirements](#requirements) to make sure the grass system works for your scenario.\

1. Make sure you have the source geometry available (or use built-in primitives like the sample scene does).
2. Arrange your geometry in a scene as you desire.
3. Create a new `Definition` asset in your project view by right-clicking and selecting `Assets > Create > ComputeGrass > Definition`.
    1. You can also duplicate the sample definition.
4. Create a new `Parameters` asset in your project view by right-clicking and selecting `Assets > Create > ComputeGrass > Parameters`.
    1. Assign a material to the parameters asset, the material needs to use the `ComputeGrass/Grass` shader.
    2. Assign the `Grass.compute` compute shader to the parameters asset.
    3. Alternatively, you can also duplicate the sample parameters.
5. Create a new GameObject and add the `BatchProcessor` component to it.
6. Create a new GameObject and add the `BatchGenerator` component to it (do not add it to the processor object!).
    1. Assign your source geometry objects to either the `Renderers Root` or the `Renderers` list. Both values are exclusive and the other one will be ignored.\
    Use `Renderers Root` to include the a hierarchy of objects to the generator.\
    Use `Renderers` to include explicit individual objects. 
    2. Assign you definition and parameters assets to the respective fields.
    3. Give the generated batch a name.\
    Use a unique name as the generator will use existing batches to override and update them with the new generated data.
    4. Select a location in your project the generated batch should be saved into.
    Be ware that existing batches at your selected location may be overridden!
    5. Click "Generate" and check your selected location for the new batch asset.
7. Assign the generated batch asset to the `BatchProcessor` component's `Batch` field.

The processor immediately renders the grass to your scene on top of your reference geometry.
Depending on your configuration you will see rectangular white grass blades on your source geometry.

You have a working grass configuration and start tweaking and adjusting it to your needs or desire. 
You can change the variant textures, change the height and aspect ratios or other variant arguments or change the number of blades and segments.

Please keep in mind that changes to values in the `Subdivision` section of the parameters asset, require the generator to process the source geometry again.
If you adjust the subdivision but don't notice any changes, you may miss the generation step. Any processors which have your batch assigned will update automatically.

## Issues and Problems
We assume you will find your way with the package and the system. In case we are wrong with this assumption and you encounter problems and difficulties we encourage you to report your problems as an issue in this repository.
Please keep in mind that this package is not maintained as a commercial product and updates or fixes will be subject to our internal capacity.\
If you are able to describe your problems in details or even provide a solution (either discriptive or as pull request) chances increase that we find the tiem to integrate them.