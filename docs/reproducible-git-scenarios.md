# Reproducible Git scenarios

## Implementation status

Implemented (all todos done). Notes on what was built and how it differs from the original plan below:

- `IhGit.Core`: `BranchRef`, `MergeTargetPlanner` (used by the WPF `LoadPr`), `GitProcess`/`GitCommands`, `LocalGitWorkflow`, and `Scenarios/` (`ScenarioBuilder`, `ScenarioManifest`, `ScenarioGuard`, `LocalRequestStore`).
- `IhGit.Scenarios`: CLI with `list`, `create`, `create-all`, `delete`.
- `IhGit.Tests`: **NUnit** (not xunit); 25 tests using real temp repos. Each test owns its temp folder and only deletes generator-owned directories.
- WPF offline mode: `--offline-scenario <manifest.json>` parsed in `App`, `OfflineSession` registered in DI, `MainViewModel(OfflineSession? = null)`. Offline skips OAuth loading, never saves settings, uses local fetch/push and `LocalRequestStore` instead of GitHub; banner and read-only repo path in `MainWindow.xaml`.
- CI builds/tests only the new projects and publishes WPF; the legacy WinForms project is untouched.

Known gaps and decisions:

- The WPF `BranchVersion`/`BranchType` were kept (delegating only in the planner call) rather than replaced by Core types.
- `MergeOne` still uses the existing CliWrap/LibGit2Sharp code, not `LocalGitWorkflow`; Core is covered by tests but not yet the production path for cherry-pick/push.
- LibGit2Sharp reports no distinct outcome for already-applied picks; the test only asserts no conflict.
- The scenario's `already-applied` expected label is "AlreadyApplied" but is not asserted.
- Offline UI was smoke-tested only for launch (process alive); interactive flows were not exercised.
- The bare origin is accessed with `--git-dir` because of `safe.bareRepository=explicit`.

## Problem and approach

Development currently depends on a work repository containing the precise Git state needed to reproduce an issue. Introduce generated, disposable repositories containing synthetic files and realistic branch history, regression tests using those repositories, and an explicit offline development mode in the WPF application.

Use real Git operations rather than mock Git behavior. Share scenario construction between a small .NET command-line generator and automated tests. Decouple the local Git workflow and repository metadata from WPF/GitHub only as far as needed to exercise the production cherry-pick and conflict-resolution behavior.

## Confirmed scope and behavior

- Focus on Git states and history, not scale/performance or authentication testing.
- Include the generator, regression tests, and offline WPF UI support.
- Offline up/downmerge performs real checkout, fetch, cherry-pick, conflict resolution, and push against generated local repositories and a local bare origin.
- Supply synthetic PR metadata and explicitly local-only PR results. No GitHub calls, real PR creation, reviewer assignment, label mutation, or merge-queue operations in offline mode.
- Preserve normal WPF online behavior. Ignore the legacy `IhGit` WinForms project entirely: do not inspect further, modify, build, test, or provide scenario support for it.

## Current code observations

- `IhGitWpf` targets .NET 10 for Windows and already references LibGit2Sharp, CliWrap, and dependency injection.
- `MainViewModel` mixes Git operations, WPF dialogs, GitHub REST/GraphQL, and repository-specific constants. Changing `RepoPath` alone cannot reproduce the complete workflow offline.
- `LoadPr` obtains commits, reviewers, labels, and branch names from GitHub. `MergeOne` fetches, checks out, cherry-picks, resolves conflicts, pushes, and creates a PR.
- `Commit` currently wraps an Octokit `PullRequestCommit`; use a small shared metadata representation so local commits need not masquerade as GitHub API objects.
- `Fetch` currently depends on the credentials provider. Local-origin fetch must run without requesting credentials.
- Stored OAuth credentials are loaded during view-model initialization. Offline selection must happen before that initialization, not just short-circuit later API calls.
- Existing hidden PR-number dialog demonstrations include absolute local paths. Do not rely on them as scenario infrastructure or let offline scenario selection reach these special cases.
- There is no existing WPF test project. CI currently publishes WPF on Windows without running tests. Use explicit WPF/new-project build and test targets rather than building the entire solution, which also includes legacy WinForms.

## Todos

1. **Extracting testable workflow boundaries**
   - Add a small shared .NET core project for the local Git operations and plain source-request/commit/branch metadata needed by the workflow.
   - Move only the relevant production operations out of `MainViewModel`; preserve progress reporting, cancellation, commit order, branch naming, and WPF conflict dialogs through narrow boundaries.
   - Add online and offline metadata/completion providers through the existing dependency-injection setup in `App.xaml.cs`. Keep REST/GraphQL online behavior intact rather than building a general plugin framework.
   - Return explicit operation outcomes. A failed fetch, checkout, conflict continuation, or push must not become a successful local result.

2. **Generating reusable synthetic scenarios**
   - Add a small .NET console scenario tool using shared scenario construction and actual Git repositories.
   - Produce a working clone, a local bare origin, and a versioned manifest containing scenario ID, source/base branches, selected commit SHAs, synthetic request metadata, and expected initial state.
   - Use `support/v4.15`, `support/v4.16`, `support/v4.17`, `deploy/v4.17.0`, and `stable` conventions where appropriate, with patch/work branch naming handled by the production code.
   - Provide named scenarios for clean single- and multi-commit cherry-picks, content conflicts, modify/delete conflicts in both directions, already-applied changes, staged changes, unstaged changes, untracked files, and an already-in-progress cherry-pick conflict.
   - Construct minimal histories that cause the intended outcome on the intended destination; do not merely place conflict-marker text in files.
   - Set repo-local synthetic identity and reproducible content/history inputs without changing global Git settings. Avoid network access and generated hooks; handle paths containing spaces.
   - Generate into a new owned directory. Refuse existing unrelated destinations and never recursively reset/delete arbitrary user paths. Recreate a scenario in a fresh directory instead of destructively resetting one.
   - Print the generated path and a command for opening it in offline WPF mode.

3. **Wiring explicit offline WPF development mode**
   - Add a session-scoped launch option such as `--offline-scenario <manifest>` and visibly identify the offline mode and scenario in `MainWindow.xaml`.
   - Resolve mode and validate the manifest before constructing services/view models or loading OAuth/TPM credentials.
   - Load synthetic request metadata and real local commit data into the existing selection UI; use shared branch-selection logic for up/downmerge.
   - Reuse the actual progress and conflict-resolution UI. Scenario selection must be explicit and bypass the existing magic PR-number demonstration paths.
   - Keep normal saved repository/credential settings untouched. Disable irrelevant online credential, merge-queue, and request-loading actions; prevent switching mode or repository during an operation.
   - Validate generator ownership, manifest/repository consistency, and the local bare origin before mutations. Revalidate remote destinations before fetch/push and reject network URLs, alternate push URLs, or a changed unapproved origin.
   - After a successful local push, show a local-only summary and persist synthetic PR results in the scenario directory, including source/target branches and selected commits. Clearly distinguish this from a real GitHub PR and from unavailable reviewer/label/queue actions.
   - Report malformed scenarios, missing commits/remotes, Git failures, and cancellation explicitly. Do not produce a completion artifact when the prerequisite push fails.

4. **Adding regression and integration coverage**
   - Add an existing-ecosystem .NET test project using one selected mainstream test framework, referencing the shared production Git workflow and scenario builder.
   - Give every test its own temporary repository and safely clean up only directories that test owns after all Git handles are disposed.
   - Verify scenario manifests and initial Git states, then assert real branch refs, file contents, ordered commits, index conflict entries, dirty states, continuation/abort results, and bare-origin refs after push.
   - Cover both modify/delete directions, already-applied changes, multiple selected commits, and in-progress conflicts. Preserve current behavior unless a tightly coupled defect prevents correct scenario execution; document any necessary behavioral adjustment.
   - Test offline metadata and completion wiring, no credential/network-provider invocation, destination restrictions, settings isolation, and failure propagation.
   - Add focused branch-selection regression cases against the shared production logic.
   - Keep dialog interaction as a documented manual smoke check unless repository-compatible automated WPF testing can cover it without introducing broad UI automation infrastructure.

5. **Documenting and integrating validation**
   - Extend `README.md` with scenario generation and launch commands, the scenario catalog, expected outcomes, recreation/cleanup instructions, test commands, and offline limitations.
   - Register new projects in `IhGit.slnx` without changing the legacy WinForms entry. Update WPF/new-project references and `.github/workflows/dotnet-desktop.yml` to build only WPF and the new components and run their regression tests before publishing. Do not build/test the full solution or change legacy WinForms configuration.
   - Run focused tests and builds, then exercise representative offline flows: clean upmerge/downmerge with local push/results, content conflict continue/abort, and modify/delete resolution.
   - Verify launching offline does not load credentials or change saved work settings, and perform a normal-mode startup smoke check without invoking work-repository mutations.
   - If interactive UI verification is unavailable, state that limitation rather than claiming it was completed.

## Dependencies

- Workflow boundary extraction precedes offline integration and workflow regression tests.
- Scenario generation can proceed independently of boundary extraction; keep its manifest contract small and agreed between the two.
- Both shared production boundaries and scenario generation precede regression coverage and offline integration.
- Documentation and final CI/validation follow integration and coverage.

## Considerations and boundaries

- Synthetic scenarios reproduce Git topology and file relationships, not company source code. Translate a discovered work issue into the smallest synthetic regression scenario.
- Offline mode is not a GitHub emulator: it does not validate OAuth, organization access, branch protections, real reviewers/labels, or merge queues.
- A configurable GitHub sandbox can be a later online integration layer; changing hardcoded production repository targeting is not required by this plan.
- Ignore legacy WinForms entirely, including validation. Do not add performance datasets, broad refactoring, or unrelated bug fixes.
- A fresh generated repo is the unit of reproducibility. Tests must verify the precise intended state/outcome, not just that generation exits successfully.
