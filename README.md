# IntelliMacro

IntelliMacro is a modular **.NET 8** solution for building and running macros/commands with a clean separation between core abstractions, runtime hosting, and built-in command packs.

**Keywords:** .NET 8, macros, command system, automation, extensible runtime, plugin architecture

## Projects

- `IntelliMacro.Core` — core abstractions and shared logic
- `IntelliMacro.Runtime` — execution/runtime hosting components
- `IntelliMacro.CoreCommands` — built-in commands

## Features

- Modular architecture (core vs runtime vs commands)
- Extensible command/macro system
- .NET 8 compatible

## Getting Started

### Prerequisites

- .NET SDK 8

### Build

```sh
dotnet build
```

### Test (if available)

```sh
dotnet test
```

## Repository Structure

- `IntelliMacro.Core/`
- `IntelliMacro.Runtime/`
- `IntelliMacro.CoreCommands/`

## Contributing

Pull requests are welcome. For major changes, please open an issue first.

## License

See `LICENSE` (if present).
