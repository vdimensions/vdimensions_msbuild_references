# MSBuild References

This repository contains a currated list of MSBuild targets which would add package dependencies to your project for various .NET libraries. 

## Foreword

As .NET changes and many of those libraries may become obsolete, or switch to different packages for different target framework, it becomes a pain to maintain multiple projects with similar setup. A good example is `Microsoft.AspNetCore` which has accomodated significant changes and package deprecation across its versions.

At VDimensions, we identified this maintainability struggle and came up with a solution, which is de-facto this project. Instead of adding the desired package directly, we defined MSBuild targets that wrap certain package references, and import the respective MSBuild target instead. This provides a layer where we can use MSBuild conditions to deside whether to import the package, or if we need to import it at all.

## How to use

Using this project is simple -- just add it as a sub-module to your repository, and from your .csproj files you can refer to the package import targets in the submodule using relative paths.

This gives you a way to keep up with changes in this project as updates are simply pulling the latest master branch of this repo.
