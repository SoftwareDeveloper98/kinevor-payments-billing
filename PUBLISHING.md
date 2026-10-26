# Publishing kinevor-payments-billing

This repository is publish-ready for private GitHub Packages distribution.

## Published artifacts

| Ecosystem | Package |
| --- | --- |
| NuGet | `Kinevor.Modules.PaymentsBilling` |
| NuGet | `Kinevor.Modules.PaymentsBilling.EntityFrameworkCore` |
| npm | `@softwaredeveloper98/kinevor-payments-billing-web` |
| Flutter | `kinevor_payments_billing` (GitHub release archive) |

## Prerequisites

- The repository exists at `https://github.com/SoftwareDeveloper98/kinevor-payments-billing`.
- `kinevor-platform` `v0.1.0` is published first.
- A repository secret named `KINEVOR_PACKAGES_TOKEN` is configured. Use a
  classic PAT with `repo`, `read:packages` and `write:packages`.
- Actions are enabled with read and write workflow permissions.

The secret is required because the platform packages are private and belong to a
different repository. The workflow falls back to `GITHUB_TOKEN`, which is not
sufficient for cross-repository package restore.

## Release Steps

1. Confirm the version is `0.1.0` in `backend/Directory.Build.props`, the
   Angular package, the Flutter package and `CHANGELOG.md`.
2. Run the local verification commands in the root README.
3. Commit and push `main`.
4. Create and push the tag:

   ```powershell
   git tag v0.1.0
   git push origin main
   git push origin v0.1.0
   ```

5. Watch the Release workflow. It publishes the packages listed above and
   attaches the Flutter archive when applicable.

## After Publishing

- Confirm the package versions in the repository Packages tab.
- Re-run CI from a clean checkout to verify GitHub Packages restore.
- Do not reuse or move the `v0.1.0` tag. Publish a new patch version for fixes.

## Consumer Access

NuGet consumers need the GitHub Packages source:

```powershell
dotnet nuget add source https://nuget.pkg.github.com/SoftwareDeveloper98/index.json `
  --name github-kinevor `
  --username SoftwareDeveloper98 `
  --password $env:KINEVOR_PACKAGES_TOKEN `
  --store-password-in-clear-text
```

npm consumers need this `.npmrc`:

```ini
@softwaredeveloper98:registry=https://npm.pkg.github.com
//npm.pkg.github.com/:_authToken=${KINEVOR_PACKAGES_TOKEN}
```

Flutter consumers should download the archive attached to the GitHub release.
