# SealedFGA

Contains the .NET OpenFGA framework.

- [SealedFGA](./SealedFga): The .NET OpenFGA framework. This is the project that should be included by the projects that want to use SealedFGA. It contains the source generator, analyzers and utility functions.
- [SealedFga.Sample](./SealedFga.Sample): Contains a sample project that uses SealedFGA. Used for testing and later to showcase the library.
- [SealedFga.Tests](./SealedFga.Tests): Will contain the unit tests for SealedFGA. Currently still holds the example tests and as such should be ignored.

The projects in here currently depend on the `master-thesis` repository structure, e.g. the `openfga-language` folder gets referenced relative to the `sealed_fga` folder. As such, when using this, make sure to clone the whole repository and keep its structure intact.