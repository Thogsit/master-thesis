# Master Thesis

Contains everything related to my Master Thesis. Currently, this means

- [SealedFGA](./sealed_fga): The .NET OpenFGA framework created within the thesis
- [openfga-language](./openfga-language): An adaption of the [openfga-language](https://github.com/openfga/language) repository that has been extended to include a .NET FGA language parser
- A patched version of the `Microsoft.CodeAnalysis.AnalyzerUtilities` package which has the `GlobalFlowStateAnalysis` modifiers changed to public so that it can be used by SealedFGA
