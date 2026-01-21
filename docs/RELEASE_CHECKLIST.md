# Release Checklist

Pre-release verification checklist for FormFinch.JsonSchemaValidation.

Use this checklist before each release to ensure quality and completeness.

---

## Pre-Release Checklist

### Legal & Licensing

- [ ] LICENSE file present at repository root
- [ ] License headers in all source files
- [ ] .csproj license metadata matches LICENSE file
- [ ] Third-party licenses documented (if any dependencies)
- [ ] Commercial licensing terms published (if applicable)

### Version & Changelog

- [ ] Version number updated in .csproj
- [ ] Version follows semantic versioning
- [ ] CHANGELOG.md updated with release notes
- [ ] Breaking changes clearly documented
- [ ] Migration guide provided (if breaking changes)

### Code Quality

- [ ] All tests passing
- [ ] No compiler warnings
- [ ] No analyzer warnings
- [ ] Code coverage meets threshold (define: __%)
- [ ] Security scan completed (no vulnerabilities)
- [ ] Performance regression tests pass

### Documentation

- [ ] README.md up to date
- [ ] XML documentation complete (no CS1591 warnings)
- [ ] API changes documented
- [ ] Getting started guide accurate
- [ ] Code samples compile and run

### Package Quality

- [ ] Package builds successfully
- [ ] Package metadata complete:
  - [ ] PackageId
  - [ ] Version
  - [ ] Authors
  - [ ] Description
  - [ ] License
  - [ ] ProjectUrl
  - [ ] RepositoryUrl
  - [ ] PackageIcon
  - [ ] PackageTags
  - [ ] PackageReadmeFile
  - [ ] PackageReleaseNotes
- [ ] XML documentation included in package
- [ ] Source Link configured
- [ ] Symbol package generated
- [ ] Package installs correctly (test in new project)
- [ ] IntelliSense works in consuming project

### Repository & CI

- [ ] All changes committed
- [ ] Branch up to date with main
- [ ] CI pipeline passing
- [ ] Version tag created
- [ ] GitHub release drafted

### Post-Release Verification

- [ ] Package visible on NuGet.org
- [ ] Package page displays correctly:
  - [ ] Description
  - [ ] README
  - [ ] License
  - [ ] Icon
  - [ ] Dependencies
- [ ] Package installs from NuGet.org
- [ ] GitHub release published
- [ ] Announcement posted (if applicable)

---

## Release Process

### 1. Prepare Release

```bash
# Ensure you're on main and up to date
git checkout main
git pull

# Run full test suite
dotnet test

# Build release configuration
dotnet build -c Release

# Verify package
dotnet pack -c Release
```

### 2. Update Version

Edit `JsonSchemaValidation.csproj`:
```xml
<Version>X.Y.Z</Version>
```

### 3. Update CHANGELOG

Add release section to CHANGELOG.md:
```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- ...

### Changed
- ...

### Fixed
- ...
```

### 4. Commit & Tag

```bash
git add -A
git commit -m "chore: prepare release vX.Y.Z"
git tag vX.Y.Z
git push origin main --tags
```

### 5. Monitor Release Pipeline

- Watch GitHub Actions release workflow
- Verify package appears on NuGet.org
- Create GitHub release from tag

### 6. Post-Release

- [ ] Verify package on NuGet.org
- [ ] Test installation in fresh project
- [ ] Announce release (if applicable)
- [ ] Update documentation site (if applicable)
- [ ] Close related GitHub milestone

---

## Rollback Procedure

If a release has critical issues:

### NuGet.org

1. Go to NuGet.org package page
2. Click "Manage" → "Listing"
3. Unlist the problematic version
4. Note: Cannot delete, only unlist

### GitHub

1. Delete the release (not the tag)
2. Optionally delete the tag:
   ```bash
   git tag -d vX.Y.Z
   git push origin :refs/tags/vX.Y.Z
   ```

### Communication

1. Post issue/announcement about known problems
2. Recommend users use previous version
3. Fast-track fix release

---

## Version History

| Version | Date | Released By | Notes |
|---------|------|-------------|-------|
| 1.0.0   | TBD  | -           | Initial public release |

---

*Template version: 1.0*
