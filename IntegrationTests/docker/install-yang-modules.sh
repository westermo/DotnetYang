#!/bin/bash
# Install YANG modules into sysrepo
# Usage: install-yang-modules.sh <directory-with-yang-files>
set -e

YANG_DIR="${1:-/yang-modules}"

echo "Installing YANG modules from $YANG_DIR..."

# Install modules — sysrepoctl will resolve imports automatically
# if dependent modules are available in the search path.
for yang_file in "$YANG_DIR"/*.yang; do
    if [ -f "$yang_file" ]; then
        module_name=$(basename "$yang_file" .yang)
        echo "Installing: $module_name"
        sysrepoctl --install "$yang_file" --search-dirs "$YANG_DIR" 2>&1 || \
            echo "  -> Skipped: $module_name (already installed or dependency issue)"
    fi
done

echo ""
echo "Installed YANG modules:"
sysrepoctl --list
