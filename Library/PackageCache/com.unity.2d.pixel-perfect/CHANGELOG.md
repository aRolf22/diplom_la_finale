# Changelog

## [5.0.3] - 2022-10-20
### Added
- Added editor assembly reference to Unity.RenderPipelines.Universal.2D.Runtime

### Changed
- Move internal tests to Pixel Perfect Tests package

## [5.0.2] - 2022-09-21
### Added
- Added URP Pixel Perfect Camera converter

### Fixed
- Removed AudioModule package dependency from sample project

### Changed
- Hide duplicate Pixel Perfect menus if URP Package is installed

## [5.0.1] - 2021-08-06
### Fixed
- Fixed a bug where clear buffer was executed out of order (case 129263)

### Changed
- Update compatibility warning text (case 1337165)

## [5.0.0] - 2021-03-17
### Changed
- Update version for release

## [5.0.0-pre.2] - 2021-01-16
### Changed
- Update license file

## [5.0.0-pre.1] - 2020-10-30
### Changed
- Version bump for Unity 2021.1

## [4.0.1] - 2020-06-10

### Fixed

- Fixed the broken documentation URL of the Component.

## [4.0.0] - 2020-05-11

### Changed
- Version bump for Unity 2020.2

## [3.0.2] - 2020-03-28

### Fixed

- Fixed an issue where Cinemachine Pixel Perfect Extension didn't work when CinemachineBrain Update Method is anything other than Late Update.

## [3.0.1] - 2019-11-14

### Changed
- Deploy samples as individual files.
- Made the editor class internal.
- Changed License file

## [3.0.0] - 2019-11-06

### Changed
- Update version number for Unity 2020.1

## [2.0.3] - 2019-11-06

### Changed

- Deprecated the CinemachinePixelPerfect extension. Use the one from Cinemachine v2.4 instead.

## [2.0.2] - 2019-07-13

### Changed

- Mark package to support Unity 2019.3.0a10 onwards.

## [2.0.1] - 2019-07-12

### Changed

- Deploy Samples as UnityPackage.

## [2.0.0] - 2019-07-05

### Added

- Added CinemachinePixelPerfect, a Cinemachine Virtual Camera Extension that solves some compatibility issues between Cinemachine and Pixel Perfect Camera.

### Fixed

- Fixed an issue where recompiling scripts while a Pixel Perfect Camera is running would cause null reference exeptions.

## [1.0.1-preview] - 2018-06-19

### Changed

- Disabled "Run In Edit Mode" button for presets and inactive game objects.
- "Run In Edit Mode" is now automatically disabled when you enter play mode.

### Fixed

- Fixed an issue where some UI text was missing from the preset inspector.
- Addressed a performance warning you could get when you target mobile platforms.

## [1.0.0-preview] - 2018-05-03

### This is the first preview release of *Unity Package \<2D Pixel Perfect\>*.

*This initial release contains a Pixel Perfect Camera component which ensures your pixel art remains crisp and clear at different resolutions, and stable in motion.*
