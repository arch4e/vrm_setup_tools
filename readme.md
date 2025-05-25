# VRM Setup Tools

## Installation

### Use .unitypackage

1. Download the unitypackage file from [releases](https://github.com/arch4e/vrm_setup_tools/releases)
1. Import unitypackage file to your project
1. Open tool window `(Menu) >> VRM0 >> VST >> <categoly> >> <tool>`

### Manually Install

1. Download this project as a zip
1. Extract zip file
1. Move folder to `Assets` in your project
1. Open tool window `(Menu) >> VRM0 >> VST >> <categoly> >> <tool>`

## Usage

### VRM0/VST/BlendShape/BlendShapeClip

`YAML (optional)` is a optional field.  
Generate blend shape clips from YAML configuration if specified,
otherwise use all available blend shapes.

```yaml
# In the case of containing a single blend shape
<blend shape name>: <blend shape name>

# In the case of containing multiple blend shapes
<blend shape name>:
  - <blend shape name>
  - <blend shape name>
  - <blend shape name>
```

## Development Environment

* Unity 2021.3.40f1
* [UniVRM-0.128.0_264a_vrm0](https://github.com/vrm-c/UniVRM/releases)

## License

GPLv3

