# nothrow's code-annotations

This tool serves as a generator for HTML+(vanilla) JS searchable code documentation.

It's main purpose is to annotate existing (maybe even 3rd party) code with documentation for code porting purposes. It is able to annotate namespaces, and any comment added to namespace shows up in subsequent namespaces / types. The database is designed to be git-friendly, and output is git pages friendly. 

## Installation

The tool is shipped as dotnet-tool. Use `dotnet tool install -g nothrow.annotate`.

## Usage

The tool allows two operations - `scaffold`, and `generate`. 

### scaffold

Scaffolding generates "database" from given (compiled) assembly, creating directory-structure for namespaces, and empty .md files for every class.

```
λ dotnet annotation scaffold -i assembly.dll -s ~/db
```

Reads content of `assembly.dll`, and creates .md files for it in directory ~/db/assembly, along with JSON file describing the metadata (don't edit that.)

You may update the .md files in any way you need - every namespace has `_namespace.md` file used for the namespace, and bunch of `T_*.md` files for every class.

```
λ dotnet annotation generate -s ~/db -o ~/db.generated
```

Compiles the entire database to `db.generated` directory, and adds HTML+JS files for browsing it. 
