name: Bug report
about: Create a report to help us improve
body:
- type: markdown
  attributes:
    value: |
      We welcome bug reports! This template will help us gather the information we need to start the triage process.

- type: input
  id: version
  attributes:
    label: Version of the Meziantou.Analyzer NuGet package
    placeholder: "2.0.0"
  validations:
    required: true

- type: input
  id: rule
  attributes:
    label: Rule Identifier
    placeholder: "MA0000"
  validations:
    required: true

- type: input
  id: tfm
  attributes:
    label: Target Framework
    placeholder: "net6.0, netstandard2.0, net472"
  validations:
    required: true

- type: input
  id: language
  attributes:
    label: "C# Language version"
    description: "If you are unsure you can use `#error` in your code to get the version. See [Getting Roslyn (C# compiler) and Language versions](https://www.meziantou.net/getting-roslyn-csharp-compiler-and-language-versions.htm) for more information."
    placeholder: "C# 11"
  validations:
    required: true

- type: textarea
    id: description
    attributes:
      label: Description
      description: Please share a clear and concise description of the problem.
      placeholder: Description
    validations:
      required: true
      
- type: textarea
    id: repro-steps
    attributes:
      label: Reproduction Steps
      description: |
        Please include minimal steps to reproduce the problem if possible. E.g.: the smallest possible code snippet; or a small project, with steps to run it. If possible include text as text rather than screenshots (so it shows up in searches).
      placeholder: Minimal Reproduction      
      value: |
        Minimal code:
               
        ```c#
        code to reproduce the error
        ```
    validations:
      required: true
      
- type: textarea
    id: other-info
    attributes:
      label: Other information
      description: |
        If you have an idea where the problem might lie, let us know that here. Please include any pointers to code, relevant changes, or related issues you know of.
      placeholder: Other information
    validations:
      required: false
