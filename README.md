# grdev.git-guard-cli

`git-guard` answers one question across a whole machine: **which of my repositories have work
in them that is not committed?** Point it at the folders where your clones live and a single
`git-guard scan` walks all of them, reporting each repository's path, its `origin` URL and
whether the working tree is dirty.

It is read-only. It never fetches, pulls, pushes, commits or stages anything — the only state
it writes is its own list of monitored folders.

**Project page:** [greekdeveloper.com/tools/git-guard-cli](https://greekdeveloper.com/tools/git-guard-cli/)

## Install

```
dotnet tool install --global grdev.git-guard-cli
```

## Usage

Register the folders your clones live in, then scan:

```
git-guard folders add C:\src
git-guard scan
```

Run `git-guard --help` to see every command.


## Versioning

Versions are computed by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
from [`version.json`](version.json) plus the git height — there is no hardcoded version anywhere.
`version.json` holds the `major.minor`; the patch is the number of commits since that value last
changed, so **every commit bumps the patch automatically**.

Install the CLI once:

```powershell
dotnet tool install --global nbgv
```

### Viewing the version

```powershell
nbgv get-version                    # full summary for HEAD
nbgv get-version -v SimpleVersion   # just x.y.z, for scripts
nbgv get-version -f json            # everything, as JSON
```

### Setting the version

The patch bumps on its own with every commit. To change the major or minor, hand-edit the
`version` field in `version.json` and commit it — the patch count restarts from there:

```json
"version": "1.3"
```

Do **not** run `nbgv set-version`. It rewrites `version.json` from scratch and silently drops the
`publicReleaseRefSpec` and `cloudBuild` settings this repo relies on. Never add a `<Version>`
element to `Directory.Build.props` or a `.csproj` either — it would override the computed version.

### Releases

A build from `release/production` is a public release and gets a clean version (`1.3.4`). Every
other branch is a prerelease and gets a commit-id suffix (`1.3.4-g1a2b3c4`). Pushing to
`release/production` triggers the [publish workflow](.github/workflows/publish-nuget.yml).
