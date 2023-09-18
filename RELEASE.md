# Release Process

## Pre-Releases
on the repo root folder run:
```sh
dotnet execute bump
```

this will output something like this:
```text
V bumping version from 1.0.0 to 1.0.1-alpha.0 in projects

---
<a name="1.0.1-alpha.0"></a>
## [1.0.1-alpha.0](https://www.github.com/dotnet-trx-merge/releases/tag/v1.0.1-alpha.0) (2023-8-16)
---

V updated CHANGELOG.md
```

on github, go to [Releases](https://github.com/ricardofslp/dotnet-trx-merge/releases) and follow these steps:
1. Click on **Draft a new Release**
2. Select the new pushed tag: **1.0.1-alpha.0** (example)
3. Click on **Generate release notes**
4. **IMPORTANT** Click on Set as a pre-release
5. Click on **Publish release**

## Releases
on the repo root folder run:
```sh
dotnet execute bump:live
```

this will output something like this:
```text
V bumping version from 1.0.1-alpha.0 to 1.0.1 in projects

---
<a name="1.0.0"></a>
## [1.0.0](https://github.com/ricardofslp/dotnet-trx-merge/releases/tag/v1.0.0) (2023-8-16)
---

V updated CHANGELOG.md
```

on github, go to [Releases](https://github.com/ricardofslp/dotnet-trx-merge/releases) and follow these steps:
1. Click on **Draft a new Release**
2. Select the new pushed tag: **v1.4.2** (example)
3. Click on **Generate release notes**
4. Click on **Publish release**