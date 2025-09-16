# SystemCommandLine.ConfigBinder

![CI](https://github.com/roelvandersteen/systemcommandline-configbinder/actions/workflows/ci.yml/badge.svg)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=roelvandersteen_systemcommandline-configbinder&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=roelvandersteen_systemcommandline-configbinder)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=roelvandersteen_systemcommandline-configbinder&metric=coverage)](https://sonarcloud.io/summary/new_code?id=your_org_systemcommandline-configbinder)

Reflection-based automatic configuration binder for `System.CommandLine` (v2) with automatic option generation + binding.

> SPDX-License-Identifier: MIT-0

Point a configuration/options POCO at a `RootCommand` (or any `Command`) and the binder will:

- Generate options (kebab-case from property names: `MaxRetries` → `--max-retries`)
- Add an inverse `--no-<flag>` for boolean properties whose default value is `true` (e.g. `DryRun = true` → `--no-dry-run`)
- Apply non-trivial default values via `Option<T>.DefaultValueFactory`
- Support arrays / repeated options (e.g. `string[]`, `int[]` via repeated occurrences)
- Bind parsed values back into a strongly-typed instance
- Run DataAnnotations validation (e.g. `[Required]`, `[Range]`) after parse
- Perform only one reflection pass; subsequent executions are delegate-based
- Synthesize inverse boolean flags only when they add real value

## Supported Frameworks

Multi-targeted:

| Target           | Notes                                                           |
| :--------------- | :-------------------------------------------------------------- |
| `net8.0`         | Primary modern target                                           |
| `netstandard2.0` | Broad library compatibility (C# features downgraded internally) |

Minimal API surface; no conditional features are currently framework-specific.

## Repository Layout

```text
src/                          # Library source
  SystemCommandLine.ConfigBinder/
samples/                      # Demonstrations
  ConfigBinder.Demo/
tests/                        # Unit tests (xUnit)
  SystemCommandLine.ConfigBinder.Tests/
```

## Installation

### GitHub Packages

```xml
<PackageReference Include="SystemCommandLine.ConfigBinder" Version="0.1.*" />
```

Add the package source:

```pwsh
dotnet nuget add source https://nuget.pkg.github.com/roelvandersteen/index.json --name "GitHubPackages"
dotnet add package SystemCommandLine.ConfigBinder --source "GitHubPackages"
```

### Local Development

Until published you can:

1. Add the project in `src/` to your solution and reference it directly.
2. Or pack locally:

```pwsh
dotnet pack src/SystemCommandLine.ConfigBinder/SystemCommandLine.ConfigBinder.csproj -c Release
```

  Then `dotnet add package` pointing at the produced `.nupkg` (or use a local NuGet source feed).

### Versioning (MinVer)

Semantic version numbers are derived from Git tags via [MinVer](https://github.com/adamralph/minver). Tag pattern: `v<semver>` (e.g. `v0.1.0-alpha.2`).

While `< 1.0.0` breaking changes may occur; they will be documented in release notes and batched where possible.

## Quick Start

```csharp
using System.CommandLine;
using ConfigBinder.Demo;
using SystemCommandLine.ConfigBinder;

var binder = new AutoConfigBinder<AppConfig>();
var root = new RootCommand("Sample tool");
binder.AddOptionsTo(root);
root.SetAction(async (parseResult, cancellationToken) =>
{
    var config = binder.Get(parseResult);
    await Console.Out.WriteLineAsync($"Retries: {config.Retries}");
    return await Task.FromResult(0);
});
return await root.Parse(args).InvokeAsync();
```

```csharp
public class AppConfig
{
    [Required]
    [Display(Description = "The service endpoint.")]
    public string Endpoint { get; set; } = string.Empty;

    [Display(Description = "Enable diagnostics.")]
    public bool Diagnostics { get; set; } = true; // yields --no-diagnostics

    [Range(1,10)]
    [Display(Description = "Retry attempts.")]
    public int Retries { get; set; } = 3;
}
```

### Generated Options Example

| Property    | Generated Option                     | Notes                                   |
| :---------- | :----------------------------------- | :-------------------------------------- |
| Endpoint    | `--endpoint`                         | Required (because of `[Required]`)      |
| Diagnostics | `--diagnostics` / `--no-diagnostics` | Inverse added because default is `true` |
| Retries     | `--retries`                          | Default value 3 emitted via factory     |

Arrays (e.g. `string[] Names`) accept repeats: `--names alpha --names beta`.

### Boolean Inversion Logic

An inverse `--no-<name>` option is generated only when:

- Property type is `bool`
- Default value is `true`

This prevents polluting help text with meaningless negative switches.

## Validation

- Missing required options surface as parser errors before your handler runs.
- Additional DataAnnotations (e.g. `[Range]`, `[MaxLength]`) are aggregated after binding; failures raise a `ValidationException`.

## Advanced Notes

### Performance

Initialization (`AddOptionsTo`) performs one reflection scan and builds compiled expression delegates for:

- Retrieving option values from `ParseResult`
- Assigning values to the target instance

At execution, only delegate invocation and validation occurs. This keeps overhead low even for larger option sets.

### Default Value Handling

A default value is injected only when it is non-trivial:

- Value types: skipped if equal to their default (`0`, `false`, etc.)
- Reference types: skipped if `null` or an empty string/array

This avoids cluttering help with meaningless defaults.

### Framework Notes

The `netstandard2.0` build uses older language constructs (no record structs / required members) to maximize reach, without changing public API shape.

## Sample Demo

```pwsh
# From repository root
dotnet run --project samples/ConfigBinder.Demo -- --endpoint https://example/ --retries 5 --names a --names b
```

Everything after `--` is forwarded to the application.

## Tests

The test project (`tests/SystemCommandLine.ConfigBinder.Tests`) covers:

- Option name generation (kebab-case)
- Inverse boolean option creation (`--no-*`)
- Binding arrays & primitives
- Default value heuristics (non-trivial vs trivial)
- Validation failure surface (`[Range]`, `[Required]`)
- Helper methods exposed as `internal` for focused unit tests

## Development

Restore & build:

```pwsh
dotnet restore
dotnet build --no-restore
```

Create a local package:

```pwsh
dotnet pack src/SystemCommandLine.ConfigBinder/SystemCommandLine.ConfigBinder.csproj -c Release
```

## Contributing

Issues & PRs welcome. Please keep additions minimal and focused; complexity will be weighed carefully against the goal of a lightweight helper.

## License

Licensed under MIT-0 (MIT No Attribution). See `LICENSE` for details.
