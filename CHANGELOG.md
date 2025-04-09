# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2025-04-10
### Optimized
- 重构代码结构为完全基于路径的实现，移除了对GUID的依赖
- 移除多余的GUID到路径转换缓存，减少内存使用
- 将Guid属性改为计算属性，保证实时准确性
- 删除了未使用和冗余的方法，简化了API
- 优化了文件夹状态检查逻辑，提高性能
- 优化了新资产标记与移除的逻辑，改进内部API结构

### Fixed
- 修复了由GUID和路径并行使用导致的潜在不一致问题
- 解决了ProjectBrowserExtender中的方法重载冲突

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