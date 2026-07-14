# TEEP Avatar Spawn Effect

This package provides a simple AI NPC Avatar spawn effect for Unity.

The core effect keeps one visual idea:

- The avatar is hidden first.
- A scan line moves from the bottom of the avatar to the top.
- The visible part of the avatar appears gradually.
- When the effect finishes, the original avatar materials are restored.

You can optionally assign an external particle prefab. The script does not create particles by itself; it only instantiates and controls the prefab you provide.

## Usage

1. Select the avatar root object, for example `MainPage_Avatar` or `model`.
2. Add Component: `TEEPAvatarSpawnEffect`.
3. Keep `Play On Enable` enabled if you want the effect to play when the avatar appears.
4. Set `Effect Duration` to about `1.0` to `1.5`.
5. Keep `Hide Before Effect` enabled if the avatar should appear from invisible to fully visible.
6. Optional: add an `AudioSource` to the avatar and assign a scan sound clip to `Scan Sound`.
7. Optional: assign your external particle prefab to `Particle Prefab`.
8. Optional: assign an intro voice clip and a Canvas to show after the avatar finishes speaking.

## Inspector Fields

- `Play On Enable`: Plays the reveal effect when the GameObject is enabled.
- `Effect Duration`: Controls how long the scan reveal takes.
- `Hide Before Effect`: Hides the avatar before the scan starts.
- `Visual Warmup Frames`: Waits a few frames after applying scan materials before the scan begins. Use this if audio starts before the visual effect appears.
- `Target Renderers`: Optional. Leave empty to automatically use child renderers.
- `Shader Name`: Fallback shader lookup name.
- `Resources Shader Name`: Shader name loaded from the `Resources` folder for Android builds.
- `Lower Edge Color`: Color for the lower scan glow.
- `Upper Edge Color`: Color for the upper scan glow.
- `Lower Edge Size`: Thickness of the lower scan edge.
- `Upper Edge Size`: Thickness of the upper scan edge.
- `Noise Strength`: Adds electronic distortion to the reveal edge.
- `Noise Scale`: Controls the scan noise size.
- `Scan Audio Source`: AudioSource used to play the scan sound. Leave empty to use an AudioSource on the same GameObject.
- `Scan Sound`: Sound clip played when the scan line starts.
- `Match Sound To Effect Duration`: Adjusts pitch so the sound length matches `Effect Duration`.
- `Scan Sound Volume`: Volume used for the scan sound.
- `Scan Sound Delay`: Delays only the scan sound. Increase this if the sound still starts earlier than the visual scan.
- `Scan Sound Target Duration`: Manual sound duration in seconds. Leave `0` to use `Effect Duration`; set a value like `1.4` or `1.8` for fine tuning.
- `Particle Prefab`: External particle effect prefab to instantiate when the scan starts.
- `Particle Local Position`: Local offset from the avatar root.
- `Particle Local Rotation`: Local rotation offset from the avatar root.
- `Particle Scale`: Size multiplier for the particle effect.
- `Parent Particle To Avatar`: Parents the generated particle effect under the avatar.
- `Match Particle To Effect Duration`: Adjusts particle simulation speed so the prefab plays during `Effect Duration`.
- `Particle Start Delay`: Delays only the particle effect.
- `Intro Delay After Effect`: Wait time after the scan effect finishes before the avatar starts speaking.
- `Intro Audio Source`: AudioSource used to play the recorded app introduction.
- `Intro Voice Clip`: Recorded voice clip for the app introduction.
- `Avatar Animator`: Animator that contains the `Talk` bool parameter.
- `Talk Bool Name`: Animator bool parameter used to switch between Idle and Talk.
- `Canvas To Show After Intro`: Canvas GameObject enabled after the intro voice finishes.
- `Hide Canvas On Play`: Disables the Canvas when the spawn effect starts.

## Android Build Note

The scan shader is stored under `Resources`, so Android builds can include it even though the script creates temporary materials at runtime.
