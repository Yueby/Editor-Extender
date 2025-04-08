# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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