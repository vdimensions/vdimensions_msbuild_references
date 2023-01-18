# Packages, supported by VDimensions.MSBuild.References

|Dependency|Supported TFMs (DBD for some packages)|
|-|-|
|Kajabity.Tools.Java|![net20](https://img.shields.io/badge/net20-darkgreen.svg) ![netstandard1.0](https://img.shields.io/badge/netstandard1.0-darkgreen.svg)|
|log4net||
|MySqlConnector||
|NpgSql||
|NUnit||
|NUnit3TestAdapter||
|LinqBridge|![net20](https://img.shields.io/badge/net20-darkgreen.svg)|
|NetLegacySupport.Tuple|![net20](https://img.shields.io/badge/net20-darkgreen.svg)|
|Portable.ConcurrentDictionary <sup>1</sup>|![netstandard1.0](https://img.shields.io/badge/netstandard1.0-darkgreen.svg)|
|System.Threading.Tasks.Unofficial|![net35](https://img.shields.io/badge/net35-darkgreen.svg)|
|VDimensions.NETStandard.Shim||
|System.Collections.Immutable||
|System.Diagnostics.TraceSource||
|System.Runtime.CompilerServices.Unsafe||
|System.Runtime.Serialization.Primitives||
|System.Threading.Thread||
|System.Xml.XmlSerializer||
|YamlDotNet||
|&nbsp;|&nbsp;|


## Notes

<sup>1</sup> The maintainers for `Portable.ConcurrentDictionary` have enabled the package to install on netstandard1.1 and netstandard1.2 as well. However,
the `System.Collections.Concurrent.ConcurrentDictionary` class is also available from the standard library, and such reference will cause a resolution conflict. Therefore we have limited the package to install only for netstandard1.0.  
