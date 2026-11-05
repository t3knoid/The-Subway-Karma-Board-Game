#!/usr/bin/env bash
# Deploy the Unity Web (WebGL) build to GitHub Pages.
#
# Publishes the contents of SubwayKarmaGame/Builds/WebGL/ to the `gh-pages` branch of this
# repository's `origin`, then makes sure GitHub Pages serves that branch.
#
# The build is committed from a temporary repository with no .gitattributes, so nothing goes
# to Git LFS (GitHub Pages can't serve LFS files). The gh-pages branch is replaced on every
# deploy; it only ever holds the latest build.
#
# Usage (Git Bash on Windows, or any bash), from anywhere in the repo:
#   tools/deploy_web.sh             build check, commit and push
#   tools/deploy_web.sh --dry-run   do everything except push / enable Pages
#   tools/deploy_web.sh --build-dir <path>   publish a build from somewhere else
#
# Before running: File > Build Profiles > Web > Build into SubwayKarmaGame/Builds/WebGL.
# The script only reads the build folder; it never changes it.
set -euo pipefail

DRY_RUN=0
BUILD_DIR_ARG=""
while (( $# )); do
    case "$1" in
        --dry-run)   DRY_RUN=1 ;;
        --build-dir) shift; BUILD_DIR_ARG="${1:?--build-dir needs a path}" ;;
        -h|--help)   sed -n '2,18p' "$0"; exit 0 ;;
        *) echo "Unknown option: $1 (try --help)" >&2; exit 2 ;;
    esac
    shift
done

info()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn()  { printf '\033[1;33mwarning:\033[0m %s\n' "$*" >&2; }
fail()  { printf '\033[1;31merror:\033[0m %s\n' "$*" >&2; exit 1; }

REPO_ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
BUILD_DIR="${BUILD_DIR_ARG:-$REPO_ROOT/SubwayKarmaGame/Builds/WebGL}"
SETTINGS="$REPO_ROOT/SubwayKarmaGame/ProjectSettings/ProjectSettings.asset"
BRANCH="gh-pages"

# -- 1. Is there a build? -----------------------------------------------------------------
[[ -f "$BUILD_DIR/index.html" ]] || fail "No Web build found at $BUILD_DIR/index.html.
       In Unity: File > Build Profiles > Web > Build, and choose that folder."
[[ -d "$BUILD_DIR/Build" ]] || fail "$BUILD_DIR/Build is missing; the Web build looks incomplete."

# Warn if game code or assets changed after the build was made.
newest_source="$(find "$REPO_ROOT/SubwayKarmaGame/Assets" "$SETTINGS" -type f -newer "$BUILD_DIR/index.html" \
                 ! -name '*.meta' 2>/dev/null | head -n 1 || true)"
if [[ -n "$newest_source" ]]; then
    warn "Files changed since this build was made (e.g. ${newest_source#"$REPO_ROOT/"})."
    warn "Rebuild in Unity first if you want those changes on the site."
fi

# -- 2. Will it load on GitHub Pages? -----------------------------------------------------
# GitHub Pages doesn't send the Content-Encoding headers that compressed Unity builds need,
# unless the build includes Unity's decompression fallback. Without the fallback Unity names
# the files *.gz / *.br; with it, *.unityweb (or plain .wasm/.data when compression is off).
if compgen -G "$BUILD_DIR/Build/*.gz" >/dev/null || compgen -G "$BUILD_DIR/Build/*.br" >/dev/null; then
    fail "This build is compressed without Decompression Fallback; on GitHub Pages the game would stay blank.
       Turn on Project Settings > Player > Web > Publishing Settings > Decompression Fallback, rebuild, then deploy."
fi
compression="$(sed -n 's/^  webGLCompressionFormat: *\([0-9]*\).*/\1/p' "$SETTINGS")"   # 0 Brotli, 1 Gzip, 2 Disabled
fallback="$(sed -n 's/^  webGLDecompressionFallback: *\([0-9]*\).*/\1/p' "$SETTINGS")"
if [[ "$compression" != "2" && "$fallback" != "1" ]]; then
    warn "Project Settings now have Decompression Fallback off; the next Web build won't work on GitHub Pages."
fi

# -- 3. Who commits, and where to? --------------------------------------------------------
ORIGIN_URL="$(git -C "$REPO_ROOT" remote get-url origin)"
EMAIL="$(git -C "$REPO_ROOT" config user.email || true)"
NAME="$(git -C "$REPO_ROOT" config user.name || true)"
[[ -n "$EMAIL" && -n "$NAME" ]] || fail "Set git user.name and user.email for this repository first."
if [[ "$EMAIL" != *@users.noreply.github.com ]]; then
    warn "Committing as $EMAIL. If GitHub email privacy is on, the push will be rejected (GH007);"
    warn "use your noreply address: git config user.email '<id>+<user>@users.noreply.github.com'"
fi

SOURCE_REV="$(git -C "$REPO_ROOT" rev-parse --short HEAD)"
SOURCE_BRANCH="$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD)"
if [[ -n "$(git -C "$REPO_ROOT" status --porcelain)" ]]; then
    SOURCE_REV="$SOURCE_REV (with uncommitted changes)"
fi

# owner/repo from https://github.com/owner/repo(.git) or git@github.com:owner/repo(.git)
SLUG="$(printf '%s' "$ORIGIN_URL" | sed -E 's#^(https://github\.com/|git@github\.com:)##; s#\.git$##')"
OWNER="${SLUG%%/*}"; REPO="${SLUG#*/}"
PAGES_URL="https://${OWNER,,}.github.io/${REPO}/"
# Prefer the real address from GitHub (accounts with a custom domain use it instead).
if command -v gh >/dev/null 2>&1; then
    PAGES_URL="$(gh api "repos/$SLUG/pages" --jq .html_url 2>/dev/null || echo "$PAGES_URL")"
fi

# -- 4. Commit the build in a throwaway repository ----------------------------------------
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

info "Copying build from ${BUILD_DIR#"$REPO_ROOT/"}"
cp -R "$BUILD_DIR"/. "$WORK"/
touch "$WORK/.nojekyll"                     # serve files as-is; no Jekyll processing
rm -f "$WORK/.gitattributes"                # never route the site through Git LFS

git -C "$WORK" init -q -b "$BRANCH"
git -C "$WORK" config user.name  "$NAME"
git -C "$WORK" config user.email "$EMAIL"
git -C "$WORK" config core.autocrlf false
git -C "$WORK" add -A
git -C "$WORK" commit -q -m "Deploy web build from $SOURCE_BRANCH @ $SOURCE_REV" \
                      -m "Built from $SLUG. Published by tools/deploy_web.sh."
SIZE="$(du -sh "$WORK" --exclude=.git 2>/dev/null | cut -f1 || echo '?')"
FILES="$(git -C "$WORK" ls-files | wc -l | tr -d ' ')"
info "Prepared $FILES files ($SIZE) for $BRANCH"

if (( DRY_RUN )); then
    info "Dry run: not pushing. Would publish to $ORIGIN_URL ($BRANCH) -> $PAGES_URL"
    exit 0
fi

# -- 5. Publish ---------------------------------------------------------------------------
info "Pushing to $BRANCH on $SLUG (replaces the previous deployment)"
git -C "$WORK" push -q -f "$ORIGIN_URL" "$BRANCH:$BRANCH"

# Make sure Pages serves gh-pages (needs the GitHub CLI, logged in): enable it the first
# time, or switch it over if it's serving another branch (e.g. main).
if command -v gh >/dev/null 2>&1; then
    current="$(gh api "repos/$SLUG/pages" --jq '.source.branch + " " + .source.path' 2>/dev/null || true)"
    if [[ "$current" == "$BRANCH /" ]]; then
        info "GitHub Pages already serves $BRANCH"
    elif [[ -n "$current" ]]; then
        if gh api -X PUT "repos/$SLUG/pages" -f "source[branch]=$BRANCH" -f "source[path]=/" >/dev/null 2>&1; then
            info "Switched GitHub Pages from '$current' to the $BRANCH branch"
        else
            warn "Pages is serving '$current'. Switch it: Settings > Pages > Deploy from a branch > $BRANCH / (root)."
        fi
    elif gh api -X POST "repos/$SLUG/pages" -f "source[branch]=$BRANCH" -f "source[path]=/" >/dev/null 2>&1; then
        info "Enabled GitHub Pages for the $BRANCH branch"
        PAGES_URL="$(gh api "repos/$SLUG/pages" --jq .html_url 2>/dev/null || echo "$PAGES_URL")"
    else
        warn "Couldn't enable Pages automatically. In GitHub: Settings > Pages > Deploy from a branch > $BRANCH / (root)."
    fi
else
    warn "GitHub CLI (gh) not found. If this is the first deploy, enable Pages in GitHub:"
    warn "Settings > Pages > Deploy from a branch > $BRANCH / (root)."
fi

info "Done. The site updates in a minute or two: $PAGES_URL"
