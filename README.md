# IhGit

Tool for managing upmerges in my company

[Download here](https://github.com/TheBlubb14/IhGit/releases/download/1.0.0/IhGitWpf.exe)

## Reproducible Git scenarios (no work repo needed)

`IhGit.Scenarios` builds real Git repositories for the situations the app has to handle: a seed history with `support/v4.15`..`v4.19`, optional `deploy/v4.20.0` and `stable`, a feature branch with commits, a bare local `origin.git` and a working clone. Commit dates and identities are fixed, so the same scenario always yields the same SHAs. Nothing reads your global Git config, credentials or the network.

```powershell
dotnet run --project IhGit.Scenarios -- list
dotnet run --project IhGit.Scenarios -- create content-conflict C:\temp\scenario
dotnet run --project IhGit.Scenarios -- create-all C:\temp\scenarios
dotnet run --project IhGit.Scenarios -- delete C:\temp\scenario   # only deletes generator-owned folders
```

| Scenario | First target behaves like |
|---|---|
| `clean-single`, `clean-multi`, `clean-with-deploy` | cherry-pick applies cleanly |
| `content-conflict` | content conflict |
| `modify-delete-destination-deleted`, `delete-modify-source-deleted` | delete/modify conflicts |
| `already-applied` | change already present, no conflict |
| `staged-changes`, `unstaged-changes` | checkout blocked by a dirty tree |
| `untracked-files` | untracked files do not block |
| `cherry-pick-in-progress` | start with an unresolved cherry-pick |

### Offline mode in the WPF app

```powershell
IhGitWpf.exe --offline-scenario "C:\temp\scenario\manifest.json"
```

The app validates the scenario (generator marker, local-only origin), uses the generated clone, runs real local Git (fetch, checkout, cherry-pick, conflict resolution, push to the local bare origin), and shows an offline banner. There are no GitHub calls, credential prompts or settings writes. Instead of a pull request, the result is appended to `local-results.json` in the scenario folder; it is not a real PR (no reviewers, labels or merge queue).

### Tests

`dotnet test IhGit.Tests` generates scenarios in temp folders and asserts real Git state. The legacy WinForms project `IhGit` is not part of build or CI.
