# Changelog

## [1.1.6] - 2024-08-30
### Fixed
- Fixed an issue where SpriteRenderers would lose their reference if an Aseprite file's name was changed. (DANB-692)

## [1.1.5] - 2024-06-19
### Fixed
- Reduced the font size slightly in the importer headers to match other inspector headers in Unity. (DANB-644) 
- Fixed an issue where the Sort Order would not be reset in Animation Clips when making use of the Z-index in Aseprite.

## [1.1.4] - 2024-05-02
### Fixed
- Fixed an issue where Sprite Renderers would be hidden after transitioning from one Animation Clip to another.
- Fixed an issue where generated AnimationClips would be 0.01 seconds too long.

## [1.1.3] - 2024-03-25
### Fixed
- Fixed an issue where the importer would not parse palette data from the "old palette" chunks.
- Fixed an issue where the Physics Shapes would not take the Sprite Rects into account, causing the outline to be wrongly offset.
- Fixed an issue where .ase/.aseprite files containing z-index data would fail to import. (DANB-608)

## [1.1.2] - 2024-03-10
### Fixed
- Fixed an issue where the Mosaic padding did not show up in Sprite Sheet import mode.
- Fixed an issue where using Sprite Padding with individual import mode would misalign the GameObjects in the generated model prefab.
- Fixed an issue where the Aseprite package would contest with the XR subsystem package over the InternalAPIEditorBridge.005. (UUM-49338)

## [1.1.1] - 2024-01-03
### Fixed
- Fixed an issue where the Sprite Editor could be opened even though there were no valid texture to open it with.
- Fixed an issue where the importer would not generate a square power of two texture for compressions which needs it (pvrtc).
- Fixed an issue where changes to linked cells would not be taken into account when reimporting.

## [1.1.0] - 2023-11-24
### Added
- Added a mosaic padding option to the importer editor.
- Added Generate Physics Shape option to the importer editor.

### Changed
- Fixed an issue where the background importer would act on files that were not Aseprite files.

## [1.0.0] - 2023-05-17
### Added
- Added a new event to the Aseprite Importer which is fired at the last import process step.
- Made the Aseprite file property publicly available.
- Made the Aseprite file parsing API publicly available.

### Fixed
- Fixed an issue where the Animation Window would no longer detect Animation Clips on a prefab. (DANB-458)

## [1.0.0-pre.4] - 2023-04-16
### Added
- Added a property to set the padding within each generated SpriteRect.
- Added an option to select import mode for the file, either Animated Sprite or Sprite Sheet.

### Fixed
- Fixed an issue where the platform settings could not be modified. (DANB-445)
- Fixed an issue where the Animation Events would be generated with the wrong time stamp.

## [1.0.0-pre.3] - 2023-03-23
### Added
- Bursted the texture generation tasks, to speed up importation of Aseprite files. (Note: Only for Unity 2022.2 and newer).
- Layer blend modes are now supported with Import Mode: Merge Frames.
- Added ability to generate Animation Events from Cell user data.
- Added ability to export Animator Controller and/or Animation Clips.
- Added canvasSize to the Aseprite Importer's public API.

### Fixed
- Fixed an issue where the last frame in a generated Animation Clip would receive an incorrect length. (DANB-434)
- Improved the background importer, so that it only imports modified Aseprite files in the background.

## [1.0.0-pre.2] - 2023-02-27
### Added
- Added support for individual frame timings in animation clips.
- Added support for layer groups.
- Added support for Layer & Cel opacity.
- Added support for repeating/non repeating tags/clips.

### Changed
- The importer UI is now re-written in UI Toolkit.
- If a Model Prefab only contains one SpriteRenderer, all components will be placed on the root GameObject, rather than generating a single GameObject to house them.
- A Sorting Group component is added by default to Model Prefabs with more than one Sprite Renderer.

### Fixed
- Fixed an issue where renaming an Asperite file in Unity would throw a null reference exception. (DANB-384)
- Fixed an issue where the background importer would import assets even when Unity Editor has focus.
- Fixed an issue where the Pixels Per Unit value could be set to invalid values.

## [1.0.0-pre.1] - 2023-01-06
### Added
- First release of this package.
