#!/usr/bin/env bash
# Select the most recent push run for this exact main commit. A missing or pending run fails closed.
set -euo pipefail
sha=${1:-}
[[ $# == 1 && "$sha" =~ ^[0-9a-f]{40}$ ]]
for workflow in backend-ci.yml frontend-ci.yml vision-ci.yml; do
    result=$(gh run list --repo "$GITHUB_REPOSITORY" --workflow "$workflow" \
        --branch main --commit "$sha" --event push --limit 1 --json conclusion,status \
        --jq '.[0] | select(.status == "completed") | .conclusion')
    if [[ "$result" != success ]]; then
        echo "::error::$workflow has no completed successful push run for $sha."
        exit 1
    fi
done
