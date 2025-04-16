# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.4] - 2025-04-16
### Optimized
- Completely redesigned asset tracking system to only monitor new assets
- Removed unnecessary asset tree management for better performance
- Significantly reduced memory usage by eliminating asset caching
- Optimized drawing logic with static caching for UI elements
- Enhanced visual performance with path status caching
- Improved handling of asset changes without unnecessary refreshes
- Eliminated redundant .meta file filtering for cleaner code

### Fixed
- Fixed issue where saving scenes would incorrectly mark assets as new
- Addressed potential stack overflow with non-recursive asset traversal

## [1.0.3] - 2025-04-10
### Optimized
- Refactored code structure to be completely path-based, removing GUID dependencies
- Removed redundant GUID to path conversion caching, reducing memory usage
- Changed Guid property to a computed property for real-time accuracy
- Deleted unused and redundant methods, simplifying the API
- Optimized folder status checking logic for better performance
- Improved new asset marking and removal logic with better internal API structure

### Fixed
- Fixed potential inconsistencies caused by parallel use of GUIDs and paths
- Resolved method overload conflicts in ProjectBrowserExtender

## [1.0.2] - 2025-04-08
### Added
- Added support for marking empty folders as new assets
- Improved asset finding with more robust lazy loading mechanism

### Optimized
- Removed excessive logging statements to reduce console spam
- Simplified folder status cache mechanism for better performance
- Optimized point marker positioning for different view types
- Enhanced asset processing with proper meta file filtering
- Replaced Thread.Sleep with proper async/await pattern for initialization

### Fixed
- Fixed issue where assets with special characters weren't being properly marked
- Fixed inconsistency between marked assets and visual indicators

## [1.0.1] - 2025-04-08
### Optimized
- Optimized asset tree construction speed in ProjectBrowserExtends
- Added caching mechanism to reduce per-frame queries, improving performance
- Enhanced asset import and processing logic

### Fixed
- Fixed dictionary key error in RemoveByGuid method
- Fixed folder asset marking logic to ensure only files are marked as new assets

## [1.0.0] - 2025-01-31
### Added
- Initial release of the package