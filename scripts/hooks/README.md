# Credential Guard

A **read-only** pre-commit hook that scans staged files for known secret
formats (API keys, tokens, private keys, password assignments, JWTs) and
**hard-blocks the commit** with a clear red report when a match is found.
No file modifications, no network calls, no external services.

## Install

A one-time command points git at this folder. After that, the hook fires
on every `git commit` automatically.

```sh
# POSIX shell (macOS, Linux, Git-bash on Windows)
git config core.hooksPath scripts/hooks
chmod +x scripts/hooks/credential-guard
```

```pwsh
# PowerShell (Windows native)
git config core.hooksPath scripts/hooks
# No chmod needed when core.hooksPath points at a folder.
```

To verify the install:

```sh
sh scripts/hooks/credential-guard --self-test
```

You should see a green "All N self-test checks passed." line. If anything
fails, the hook itself is broken — **do not commit** until it passes.

## How it works

1. `git diff --cached --name-only --diff-filter=ACM` lists staged changes.
2. For each file:
   - **Path-level block** — paths matching `*.pem`, `*.key`, `*.pfx`,
     `.env`, `.env.*`, or `secrets*` are blocked regardless of content
     (unless explicitly allowlisted).
   - **Binary skip** — files reported as `- -` in `git diff --numstat`
     are skipped.
   - **Per-line scan** — staged content (read via `git show :<path>`,
     never from the working tree) is matched against 25+ patterns:
     - Cloud: OpenAI, Anthropic, AWS access/secret, Azure storage
     - Source-control: GitHub PAT, OAuth, app, server, fine-grained PAT
     - SaaS: Slack, Stripe, Google API, Heroku, SendGrid, Mailgun
     - Auth: JWT, PEM/OPENSSH/PGP private-key blocks
     - Code style: `password=`, `api_key=`, `secret=`, `bearer=`,
       `Authorization: Bearer …`, `AccountKey=…`, `DefaultEndpoints…`
3. The allowlist (`credential-guard.allowlist`) is consulted for each hit.
   Allowlist rules can be `string:LITERAL`, `regex:/ERE/`, or `path:GLOB`.
4. On any unmatched hit, the hook prints a red report with **file:line,
   pattern name, and a masked preview** (`sk-p...mnop` — never the full
   value) and **exits 1**, which blocks the commit.

## Bypass (escape hatch)

The hook can be bypassed for a single commit. This is **not recommended**
and should only be used when you are certain the content is safe:

```sh
git commit --no-verify
```

## Adding an allowlist entry

Edit `scripts/hooks/credential-guard.allowlist`. Three rule types:

```text
# Exact-string substring match
string:sk-EXAMPLE-NotARealKey-XXXXXXXXXXXX

# POSIX ERE (surrounding slashes optional)
regex:sk-TEST_[A-Z0-9]{16}

# Glob (path) — ** = any number of path components
path:**/tests/fixtures/**
path:docs/**/*.md
```

Rules are committed to the repo so the whole team shares the same
exceptions. Be conservative.

## Adding a new pattern

Open `scripts/hooks/credential-guard` and find the `cat > "$PATTERNS_TMP"
<<'PATTERNS_EOF'` block. Add a line in the format `label:::ERE`:

```sh
my_provider_key:::mp_live_[A-Za-z0-9]{24,}
```

Then add a positive + negative fixture inside the `self_test()` function
and re-run `--self-test` to confirm. Submit a PR.

**Pattern guidelines:**

- Keep it tight. A false positive in a blocking gate is far worse than a
  false negative.
- Avoid `(?i)` (GNU-only). List explicit uppercase variants if needed.
- Use character classes (`[A-Za-z0-9]`) and quantified length floors
  (`{20,}`) to suppress partial matches in URLs/comments.

## Defense in depth — `.gitignore`

`scripts/hooks/credential-guard` is the last line of defense. The
`.gitignore` has been updated to keep these files out of the index
entirely:

```gitignore
.env
.env.*
*.pem
*.key
secrets.*
```

If a developer runs `git add -f` to force one in, Credential Guard will
catch it at commit time.

## What this hook does NOT do

- **No** content rewriting or auto-fix. It will never `sed -i` your files.
- **No** history rewriting. It will never run `git filter-repo` for you.
  The report tells you the command; you run it.
- **No** key rotation. The provider's dashboard is the only place to do
  that, and Credential Guard has no network access to do it.
- **No** Claude / Anthropic API calls. Detection is fully regex-based
  and offline. The companion `.claude/agents/security-auditor.md`
  agent is the AI-driven review layer; it is invoked on demand, not
  automatically.
- **No** modifications to git refs (`HEAD`, branches, tags).

## Limitations

- Regex matching cannot catch every secret format. A custom, novel, or
  base64-encoded credential may slip through. Combine with human code
  review and the `security-auditor` agent for high-risk changes.
- The hook is best-effort. It is **not** a substitute for:
  - code review,
  - dependency scanning (`dotnet list package --vulnerable`),
  - secret-rotation policy,
  - server-side push protection (GitHub/GitLab have their own).

## Files

```
scripts/hooks/
  pre-commit              thin shim — git invokes this; it execs credential-guard
  credential-guard        the actual implementation (POSIX sh)
  credential-guard.allowlist    shared allowlist (committed)
  README.md               this file
```

The shim exists because git's hook system looks for a file literally
named `pre-commit` (the lifecycle hook). The implementation lives in
`credential-guard` so the system has a stable name independent of
git's hook filenames. To add another pre-commit check in the future
(e.g. a linter), chain it inside `pre-commit`:

```sh
#!/usr/bin/env sh
"$(dirname "$0")/credential-guard" "$@" || exit $?
"$(dirname "$0")/your-linter"        "$@" || exit $?
```
