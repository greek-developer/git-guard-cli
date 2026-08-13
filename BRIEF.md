# BRIEF.md — git-guard-cli

## Overview

`git-guard` answers one question across a whole machine: **which of my repositories have
work in them that is not committed?** Point it at the folders where clones live, and a
single `scan` walks all of them and reports each repository's path, its `origin` URL, and
whether the working tree is dirty.

It is a read-only reporting tool over a set of local clones. It does not fetch, pull, push,
commit or stage anything — the only state it writes is its own list of monitored folders.
Deciding what to *do* about a dirty repository is left to whoever is reading the output.

Scope is deliberately narrow. This is not a git wrapper, not a multi-repo task runner, and
not a replacement for `git status` in any one repository.

## Build & run

```powershell
dotnet build                                     # build everything
dotnet run --project src/GitGuard -- --help      # run the CLI
dotnet pack -c Release                           # produce the global tool
```

There are **no tests** — `dotnet test` finds no test project. See [Tests](#tests).

Install the packed tool to exercise it the way users will:

```powershell
dotnet tool install grdev.git-guard-cli --global --add-source ./release --prerelease
```

### Commands

| Command | Does |
|---|---|
| `git-guard get-config-path` | Print the full path of the config file |
| `git-guard folders list` | List the monitored folders as `name: path` |
| `git-guard folders add <path> [--name\|-n <name>]` | Add a folder. The path is resolved to an absolute one; the name defaults to the last path segment |
| `git-guard repositories scan` | Walk every monitored folder and report the repositories found |
| `git-guard version` | Print `version` / `commit-text` / `commit-sha` / `build-time` from `ProductionVersion.json` |
| `git-guard skill` | Print the embedded agent guide to stdout and nothing else, so `> SKILL.md` produces a usable file |

`scan` takes four repeatable filters, all substring matches and all case-insensitive:

| Option | Short | Keeps repositories whose |
|---|---|---|
| `--path-filter-include` | `-pfi` | path contains any of these |
| `--path-filter-exclude` | `-pfe` | path contains none of these |
| `--origin-filter-include` | `-ofi` | `origin` URL contains any of these |
| `--origin-filter-exclude` | `-ofe` | `origin` URL contains none of these |

Each result line is `[+]` when the working tree is dirty and `[ ]` when it is clean,
followed by the path and the `origin` URL.

### Configuration

Everything the tool keeps on the machine lives in one folder named after the package id:

```
%USERPROFILE%\.grdev.git-guard-cli\config.json
```

It holds the monitored folders and nothing else. It is created with an empty folder list on
first use, so there is no setup step — `folders add` is the first thing a new user runs.
There are no environment variables and no other configuration source.

## Layout

Standard grdev layout ([AGENTS.md](AGENTS.md)), less what does not exist yet: there is no
`tests/`, `docs/`, `specs/`, `tasks/` or `scripts/`.

All source is one project, `src/GitGuard`, split by concern: `Program.cs` composes the
command tree, `Commands/` holds one static class per command group, `Config/` holds the
config model and its load/save, `RepositoryManager.cs` does the scanning, `Versioning/` reads
the build-time `ProductionVersion.json`, and `Skills/` reads the embedded agent guide.

`skills/git-guard/SKILL.md` is the one source of truth for the agent guide — it is embedded
into the assembly by the `.csproj` and printed by `git-guard skill`. Edit it there, never in
a copy.

Known deviations from the standard, all pre-existing:

| Deviation | Detail |
|---|---|
| `grdev.gitguard.slnx` | Named `gitguard`, not `git-guard-cli` — it predates the rename |
| No `LICENSE` file | The `.csproj` declares `MIT` via `PackageLicenseExpression`, but the repository carries no licence text |
| No `AssemblyName` override | The binary is `GitGuard.exe`, so `--help` renders usage as `GitGuard [command]` rather than `git-guard`. The installed tool shim is still `git-guard` |

## Stack

| Concern | Choice | Note |
|---|---|---|
| Platform | .NET 10, `net10.0` | |
| Git access | [`LibGit2Sharp`](https://www.nuget.org/packages/LibGit2Sharp) | In-process libgit2 bindings. Chosen over shelling out to `git` — do not add a `git` subprocess path alongside it |
| CLI parsing | `System.CommandLine` | Per the standard's preferred packages |
| JSON | `System.Text.Json` | Per the standard. Config properties carry explicit `[JsonPropertyName]` attributes, so renaming a C# property does not break an existing config file |
| Versioning | `Nerdbank.GitVersioning` | From `Directory.Build.props` |

`Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are all
set once in `Directory.Build.props`. The build is warning-free with them on; nothing is
suppressed and no rule severity has been lowered to keep it that way.

**`ProductionVersion.json` is generated, never written by hand.** The
`GenerateProductionVersion` target in the `.csproj` writes it into the intermediate folder
before `AssignTargetPaths`, so it lands in the build output and in the packed tool. The git
format string it passes needs a per-platform escape (`%%s` on Windows, `%s` elsewhere) because
`Exec` goes through `cmd` on Windows and `sh` on Linux — collapsing that to one form puts a
literal `%s` in the file on the other platform.

Console output is pinned to UTF-8 in `Program.cs`. Without it a redirected `git-guard skill`
is written in the console's legacy code page and every em dash in the guide is mangled, so
the emitted file stops matching the source.

**Scanning is eager and happens once.** `RepositoryManager` has a static constructor that
runs the full recursive walk of every monitored folder, so the first touch of
`RepositoryManager.Repositories` pays for all of it and later touches are free. Any command
that does not need repositories must not reference that type at all.

The walk is `Directory.GetDirectories(path, ".git", SearchOption.AllDirectories)` per
monitored folder, so cost scales with the whole tree, not with the number of repositories in
it. A `LibGit2Sharp.Repository` is opened for every hit and none of them are disposed.

## Tests

**None.** There is no test project, so `dotnet test` has nothing to run.

The parts that could be covered without touching a real repository are the filter
composition in `repositories scan` and the config load/save round-trip. Both currently reach
straight for the filesystem — `ConfigurationManager` reads the user profile directly and
`RepositoryManager` scans in a static constructor — so covering either means giving them a
seam first.

## Never

- **Never make a command write to a repository.** This tool reports; it does not fetch,
  pull, push, commit, stage or checkout. A destructive operation across every clone on a
  machine, driven by a substring filter, is not a feature.
- **Never move the storage folder without moving what is already in it.** Existing users'
  monitored-folder lists live at `%USERPROFILE%\.grdev.git-guard-cli\config.json`; a path
  change that leaves them behind makes the tool look freshly installed and silently reports
  nothing.
- **Never let one unreadable folder end the scan.** A monitored folder that has been deleted
  or is not readable throws out of the `SelectMany` in `ScanFolderForRepositories`, so
  `repositories scan` reports nothing at all — the repositories under the folders that are
  fine are lost with it. `folders list` and `get-config-path` still work, because neither
  touches `RepositoryManager` and its static constructor therefore never runs.

## Decisions

### 2026-08-13

- `pack` output moved from `../../nupkg` to **`../../release`**, and `release/` was added to
  `.gitignore` under a "Project-specific" section. The standard puts packages under
  `./release`; `nupkg/` was not ignored as a directory — only `*.nupkg` and `*.snupkg` inside
  it were, so any other pack output landed committable. `release/` had in turn been covered
  only incidentally by the .NET template's `[Rr]elease/` build-configuration pattern, hence
  the explicit entry. The sibling `choreo-typer-cli` carries the identical change.
- `Directory.Build.props` now sets `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` and
  `EnforceCodeStyleInBuild` for every project. The existing code needed no changes to build
  clean under them.
- The build writes `ProductionVersion.json` and `git-guard version` prints it, per the
  standard's production-version rule.
- The agent guide lives at `skills/git-guard/SKILL.md`, is embedded in the assembly, and is
  printed by `git-guard skill`. `Program.cs` pins console output to UTF-8 so a redirect
  reproduces the file byte for byte.
- `Validated<T>` was brought over from `youtube-cli` alongside `ProductionVersion` rather than
  reshaping the parser's error reporting; the two repositories keep the same file.

- The tool is **`git-guard`** and the package **`grdev.git-guard-cli`**, matching the
  repository name per the standard's naming chain.
- Everything the tool stores lives in **`%USERPROFILE%\.grdev.git-guard-cli\`**, with the
  monitored-folder list as `config.json` inside it. The folder is named for the package id
  per the standard's "Where a tool stores things"; the file is named for what it is, not for
  the tool. `ConfigurationManager.StorageFolderName` is the single place that name appears.
- Adopted the grdev agentic standard. `AGENTS.md` is synced verbatim from
  [`greek-developer/agentic`](https://github.com/greek-developer/agentic) and is never
  edited locally; everything project-specific lives in this file.
- `.editorconfig` added — the `dotnet new editorconfig` baseline with `end_of_line = crlf`
  and `insert_final_newline = true`, and the `[*.cs]` `insert_final_newline` flipped to
  match.
- `.gitattributes` added, pinning the working tree to CRLF with `.github/workflows/**` held
  at LF so a `run:` block still parses on a Linux runner. The repository had no
  `.gitattributes` before, so the tracked files were renormalized in the same change rather
  than left to shift under someone's next unrelated commit.
- This log starts here. Decisions taken before today were never written down, so the
  repository is described as it stands above rather than reconstructed as dated entries.
