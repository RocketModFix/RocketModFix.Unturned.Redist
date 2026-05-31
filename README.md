# RocketModFix.Unturned.Redist

Unturned's managed assemblies (`Assembly-CSharp.dll`, `com.rlabrecque.steamworks.net.dll`, `UnturnedDat.dll`, the SDG/Unity dependencies, and the API XML docs), packaged for NuGet and refreshed automatically each time Unturned ships a new build. Reference a package instead of hand-copying DLLs out of the game's `Managed` folder after every patch.

> **Redistributed with permission.** Nelson Sexton (Smartly Dressed Games) gave explicit written approval to distribute Unturned's libraries — see [issue #8](https://github.com/RocketModFix/RocketModFix.Unturned.Redist/issues/8).

## Install

```bash
dotnet add package RocketModFix.Unturned.Redist.Server
```

or in your `.csproj`:

```xml
<PackageReference Include="RocketModFix.Unturned.Redist.Server" Version="x.y.z" />
```

## Which package do I want?

Choose by **side** (client or server). Use a **`.Publicized`** variant when your plugin needs to touch private/internal members — their visibility is rewritten to public.

| Package | For |
| --- | --- |
| [![Client](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Client?label=Client)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Client) | Client-side tools and mods |
| [![Server](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Server?label=Server)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Server) | Server-side plugins and tools |
| [![Client.Publicized](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Client.Publicized?label=Client.Publicized)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Client.Publicized) | Client mods that need non-public members |
| [![Server.Publicized](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Server.Publicized?label=Server.Publicized)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Server.Publicized) | Server plugins that need non-public members |

### Stable vs. preview builds

`Client` and `Server` carry two streams under the **same package id**:

- **Stable** — Unturned's default branch, versioned `x.y.z.n`.
- **Preview** — Unturned's `preview` branch, published as a prerelease `x.y.z.n-preview<build>`. Enable "include prerelease" in your NuGet client to pull it.

The standalone [`…Client-Preview`](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Client-Preview) and [`…Server-Preview`](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Server-Preview) packages are **legacy**. They still update for projects that already reference them, but new code should use the prerelease stream above.

## How it works

Everything runs on GitHub Actions, with no external servers. A scheduled job watches Steam for new Unturned builds; when one lands it downloads the build, repackages the managed DLLs, and opens a pull request. The PR is validated (files present, hashes match, version not a downgrade) and auto-merged, which publishes the affected package to NuGet.

📖 **[ARCHITECTURE.md](ARCHITECTURE.md)** has the full picture: the workflows, the variant matrix in [`.github/variants.json`](.github/variants.json), how the 4 Steam sources map to 10 directories and 6 packages, and how to add a variant.

![Architecture](architecture.svg)

## Credits

- [Unturned-Datamining](https://github.com/Unturned-Datamining)
- [setup-steamcmd](https://github.com/CyberAndrii/setup-steamcmd)
