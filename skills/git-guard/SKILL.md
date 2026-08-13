---
name: git-guard
description: >-
  Find out which local git clones have uncommitted work, using the `git-guard` CLI
  (grdev.git-guard-cli). It walks a registered set of folders, finds every repository under
  them, and reports each one's path, its `origin` URL, and whether the working tree is dirty.
  Use whenever the user asks about the state of their repositories across a machine, including
  phrasings like "do I have uncommitted changes anywhere", "which repos are dirty", "scan my
  repositories", "did I leave work unsaved before the reinstall", "what have I not pushed",
  "check all my clones", or "audit my git folders". Also use for the folders it watches — "add
  my source folder to git-guard", "which folders is it monitoring", "where does git-guard keep
  its config" — and when a `git-guard` command has failed and its behaviour needs explaining.
---

# Reporting on local git repositories

`git-guard` answers one question across a whole machine: **which of my clones have work in
them that is not committed?** Nothing prompts, output goes to stdout, and it exits `0` when it
ran.

**It is read-only over the clones.** It never fetches, pulls, pushes, commits, stages or
checks out. The only state it writes is its own list of monitored folders. Deciding what to
*do* about a dirty repository is the user's call — report, then ask.

## Check the tool is there

```bash
git-guard version
```

Not found → `dotnet tool install --global grdev.git-guard-cli`.

## The loop

**1. See which folders are being watched.**

```bash
git-guard folders list
```

```
Monitored Folders:

grdev: D:\sarmis\grdev
work: C:\src\work
```

An empty list means nothing has been registered yet — `scan` will then report nothing at all,
which is not the same as "everything is clean". Say so rather than reporting a clean machine.

**2. Register a folder if one is missing.**

```bash
git-guard folders add D:\sarmis\grdev
git-guard folders add ../other-tree --name other      # -n also works
```

The path is resolved to an absolute one. The name defaults to the last path segment. The
command prints nothing on success.

Registering a folder is enough — you do not list repositories individually. `scan` walks each
monitored folder recursively and finds every `.git` under it, at any depth.

**3. Scan.**

```bash
git-guard repositories scan
```

```
Repositories:

[+]  D:\sarmis\grdev\git-guard-cli => https://github.com/greek-developer/git-guard-cli
[ ]  D:\sarmis\grdev\youtube-cli => https://github.com/greek-developer/youtube-cli
```

`[+]` is a dirty working tree, `[ ]` is clean. Then the path, then the `origin` URL — empty
after the `=>` when the repository has no `origin` remote.

Dirty means *anything* uncommitted: modified, staged, untracked, or a mix. The tool does not
say which, and it says nothing about whether committed work has been **pushed** — a clean
`[ ]` can still be a repository sitting on unpushed commits. If the user's actual question is
"what have I not pushed", say that this tool does not answer it.

## Narrowing the scan

Four filters, all substring matches, all case-insensitive, each repeatable:

| Option | Short | Keeps repositories whose |
|---|---|---|
| `--path-filter-include` | `-pfi` | path contains any of these |
| `--path-filter-exclude` | `-pfe` | path contains none of these |
| `--origin-filter-include` | `-ofi` | `origin` URL contains any of these |
| `--origin-filter-exclude` | `-ofe` | `origin` URL contains none of these |

```bash
git-guard repositories scan -ofi github.com -pfe node_modules -pfe archive
```

Repeat an option to widen an include or to add another exclusion; excludes are applied after
includes. A repository with no `origin` remote matches **no** `-ofi` value, so any origin
include filter drops it silently.

Filtering only shrinks the report. It does not shorten the walk — every monitored tree is
scanned in full either way.

## Where its own state lives

```bash
git-guard get-config-path
```

```
C:\Users\you\.grdev.git-guard-cli\config.json
```

That file holds the monitored folders and nothing else, and is created empty on first use.
There are no environment variables and no other configuration source.

There is **no `folders remove` command.** To drop or rename a monitored folder, edit that JSON
directly — its `folders` array holds `path` and `friendlyName` per entry. Read it, change it,
and tell the user what you changed.

## Exit codes

There is no rich exit-code contract — this is a reporting tool, not a gate.

| Code | Meaning | Do this |
|---|---|---|
| `0` | The command ran | Read stdout |
| `1` | Usage error | An unrecognised command or option — the reason is on stderr |
| other non-zero | The command threw | Read stderr for the exception |

**On a failed invocation, stdout holds help text, not a result.** The reason goes to stderr
while the help dump goes to stdout, so anything that redirects stdout to a file —
`git-guard skill > SKILL.md` above all — captures usage text when the invocation was wrong.
Check the exit code before trusting what you captured.

A scan that finds nothing still exits `0`. Never read `0` as "everything is committed" without
looking at the output — it equally means no folders are registered.

**A monitored folder that has been deleted or is unreadable fails the whole scan,** not just
that folder's share of it, so the repositories under the folders that are fine go unreported
too. `folders list` and `get-config-path` are unaffected — they never walk the disk — so you
can still see the offending entry and remove it from the config file.

## Never

- **Never treat this tool as a way to change a repository.** It reports. If the user wants the
  dirty repositories committed, pushed or cleaned, that is `git` in each one, done
  deliberately — never a sweep driven by a substring filter.
- **Never run `folders add` without checking `folders list` first.** There is no duplicate
  check: adding the same path twice registers it twice and every repository under it is then
  reported twice.
- **Never report a `[ ]` as "nothing to do".** Clean working tree is not the same as pushed,
  and the tool says nothing about the remote.
- **Never add a folder the user did not name** — not a drive root, not the profile directory.
  A monitored tree is walked in full on every scan, so a broad entry turns a fast command into
  a minutes-long one for everyone who runs it afterwards.
- **Never hand-edit `ProductionVersion.json`.** It is written at build time and is the record
  of which build is installed.
