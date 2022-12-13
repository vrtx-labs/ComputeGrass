# ComputeGrass - Changes

<!--TOC-->
  - [Releases](#releases)
    - [0.4.3](#0.4.3)
    - [0.4.2](#0.4.2)
    - [0.4.1](#0.4.1)
    - [0.4.0](#0.4.0)
<!--/TOC-->

## Releases
### 0.4.3
Removed MeshRenderer-based enabled state on all platforms (caused erratic behaviour on desktop platforms).

### 0.4.2
Added support for random color tint picked from color range (per blade, for top and bottom tint).
Reduced draw buffer size by approx. 25% by removing unused per blade color value.

### 0.4.1
Disabled MeshRenderer-based enabled state on mobile (iOS/Android) platforms.

### 0.4.0
Initial release of the ComputeGrass UPM package.
