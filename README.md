# MSBuild References

This repository contains a currated list of MSBuild targets which would add package dependencies to your project for various .NET libraries.  

## Foreword

As .NET changes and many commonly used libraries may become obsolete, or switch to different packages for different target framework. It becomes a pain to maintain multiple projects with similar setup that require complex package resolution. A good example is `Microsoft.AspNetCore` which has accomodated significant changes and package deprecations across its versions.

At VDimensions, we identified this maintainability struggle and came up with a solution, which is de-facto __this project__. 

## How to use

1. Add this project as a git submodule to your project
2. Add a reference to the `VDimensions.MSBuild.References.targets` target (which is at the root of this project) to your `.csproj` file, (or preferrably, in `Directory.Build.targets`)
3. For CI builds, make sure that `--recursive` flag is passed to the git checkout command

## The Problem

Normally, one would use the `<PackageReference Include="SomePackage" Version="Version" />` instruction to add a nuget package to a project. Looks simple, and it should not be any harder.

However, if you develop libraries, or in your attempt to keep up with the latest developments of `dotnet`, you may need to multi-target your projects. This is where you may end up in scenarios where the `PackageReference` is inapropriate (e.g. if you need to target `netstandard1.0` and `net6.0`, and `SomePackage` does not support the former at all). Or, you may need to use different `Version`, again depending on the different target framework.

The solution would be to increase your knowledge of how MSBuild works, and use MSBuild conditional syntax to smartly reference whatever you need. To some people, this may already

When you work on multiple projects with this problem, it can easily become a pain copy-pasting similar msbuild code across projects, and fighting it to make it work.

## How it works

We wanted to make referencing a package to your project simple and inthuitive. So, you still need to add a `PackageReference` item in your .csproj as always, but this time omit the version:

        <PackageReference Include="SomePackage" />

If you followed step 2 from [How to use](#how-to-use), you will have the `VDimensions.MSBuild.References.targets` in your project. As long as `SomePackage` is a dependency that is managed by `VDimensions.MSBuild.References`, our target will "decide" if you need this dependency based on your TFM, and will automatically pick the latest compatible version per TFM. For TFMs that do no support the package you requested, the `PackageReference` will be removed without needing your intervention.
