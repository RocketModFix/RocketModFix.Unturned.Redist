# RocketModFix.Unturned.Redist

## Why Use This?

Do you find it annoying to copy libraries from the Managed directory every time Unturned updates? With this NuGet package, you get all the necessary libraries for Unturned, and they update automatically. You don't need to wait for us to update them manually. This package is like a "redistributable" that stays current.

You get:

- **Assembly-CSharp.dll**
- **com.rlabrecque.steamworks.net.dll**
- **UnturnedDat.dll**
- and many more, including XML docs for the Unturned API and more.

## How to Install

These libraries (or "redists") update by themselves, so you don’t have to worry about manual updates.

Choose the package that fits your need:

- **RocketModFix.Unturned.Redist.Client:** For tools that run on the Unturned client.
- **RocketModFix.Unturned.Redist.Server:** For tools that run on the Unturned server.
- **RocketModFix.Unturned.Redist.Client-Preview:** For early versions of the client-side libraries.
- **RocketModFix.Unturned.Redist.Server-Preview:** For early versions of the server-side libraries.

### Installation Links

Click the links below to get the package you need:

[![RocketModFix.Unturned.Redist.Client](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Client?label=RocketModFix.Unturned.Redist.Client&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRocketModFix.Unturned.Redist.Client)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Client)  
[![RocketModFix.Unturned.Redist.Server](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Server?label=RocketModFix.Unturned.Redist.Server&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRocketModFix.Unturned.Redist.Server)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Server)  
[![RocketModFix.Unturned.Redist.Client-Preview](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Client-Preview?label=RocketModFix.Unturned.Redist.Client-Preview&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRocketModFix.Unturned.Redist.Client-Preview)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Client-Preview)  
[![RocketModFix.Unturned.Redist.Server-Preview](https://img.shields.io/nuget/v/RocketModFix.Unturned.Redist.Server-Preview?label=RocketModFix.Unturned.Redist.Server-Preview&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRocketModFix.Unturned.Redist.Server-Preview)](https://www.nuget.org/packages/RocketModFix.Unturned.Redist.Server-Preview)

> **Note:**  
> The main `RocketModFix.Unturned.Redist.Client` and `RocketModFix.Unturned.Redist.Server` packages now also include **preview (prerelease)** versions. These are the same as `RocketModFix.Unturned.Redist.Client-Preview` and `Server-Preview`, just under the main package name.  
>  
> This is mainly to simplify things going forward. The older `*-Preview` packages are still supported for backward compatibility, so you don't need to switch if you're already using them.

## Architecture

All updates and automation are handled entirely through GitHub Actions.  
We don't run any of this on external servers — everything happens directly on GitHub.

![Architecture](https://raw.githubusercontent.com/RocketModFix/RocketModFix.Unturned.Redist/master/architecture.jpg)

## Credits

Special thanks to these projects that helped us build this auto-updating tool:

- [Unturned-Datamining](https://github.com/Unturned-Datamining)

- [setup-steamcmd](https://github.com/CyberAndrii/setup-steamcmd)