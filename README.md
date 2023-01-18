# MSBuild References

This repository contains a currated list of MSBuild targets which would manage package dependencies for various .NET libraries that you may need in your project.  

## Foreword

As .NET changes and many commonly used libraries may become obsolete, or switch to different packages for different target frameworks. It becomes a pain to maintain multiple projects with similar setup that require complex package resolution. A good example is `Microsoft.AspNetCore` which has accomodated significant changes and package deprecations across its versions.

At VDimensions, we identified this maintainability struggle and came up with a solution, which is de-facto __this project__. 

## How to Use

1. Add this project as a git submodule to your project
2. Add a reference to the `VDimensions.MSBuild.References.targets` target (which is at the root of this project) at the end of your `.csproj` file, (or preferrably, in `Directory.Build.targets` if you use it)
3. For CI builds, make sure that `--recursive` flag is passed to the git checkout command
4. Remove the `Version` property of your `PackageReference` tags in the `.csproj` file(s) for depedencies that are managed by `VDimensions.MSBuild.References`. Here is a [full list](supported_packages.md) of the packages that this project supports.  

## The Problem

Normally, one would use the `<PackageReference Include="SomePackage" Version="Version" />` instruction to add a nuget package to a project. Looks simple, and it should not be any harder.

However, if you develop libraries, or in your attempt to keep up with the latest developments of `dotnet`, you may need to multi-target your projects. This is where you may end up in scenarios where the `PackageReference` is inapropriate (e.g. if you need to target `netstandard1.0` and `net6.0`, and `SomePackage` does not support the former at all). Or, you may need to use different `Version`, again depending on the different target framework.

The solution would be to increase your knowledge of how MSBuild works, and use MSBuild conditional syntax to smartly reference whatever you need. To some people, this may seem too much struggle for making a single dependency work. With more dependencies requiring attention, each could end up requiring its own MSBuild "hacks".

When you work on multiple projects with this problem, it can easily become a pain copy-pasting similar msbuild code across projects, and fighting it to make it work.

## This Project's Solution

We wanted to make referencing a package to a project simple and inthuitive. So, you still need to add a `PackageReference` item in your .csproj as always, but this time omit the version:

        <PackageReference Include="SomePackage" />

If you followed step 2 from [How to Use](#how-to-use), you will have the `VDimensions.MSBuild.References.targets` in your project. As long as `SomePackage` is a dependency that is [managed by `VDimensions.MSBuild.References`](supported_packages.md), our target will _decide_ if you need this dependency based on your TFM, and will automatically pick the _appropriate_ version per TFM. For TFMs that do no support the package you requested, the `PackageReference` will be removed without requiring your intervention.

For the curious, the "approriateness" of the dependencies version is determined as follows:

* if the project is a class library and needs a dependency `A`, the lowest compatible version of `A` is selected. This gives the most flexibility scenarios for a library, as it ensures that the library will play nice with other dependencies that may require a higher-version of `A`, and allows the referencing project of your library to override the version of `A` with any compatible higher version number. (see how [dependency resolution works](https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution) for nuget packages for more info.)
* if the project is not a class library, it is assumed that it is an application, and then the highest compatible version of `A` is selected. Again, this should play nice with other libraries depending on `A` in your project, as they normally should request lower versions of `A`. Eventually, you may (and probably should) specify a more recent version of `A` as an explicit dependency (this time you supply the `Version` attribute yourself). All this ensures that in the final build, `A` can be provided with the latest patches and security vulnerability fixes.
