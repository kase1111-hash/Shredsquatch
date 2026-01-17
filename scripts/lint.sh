#!/bin/bash
# Shredsquatch Code Linting Script
# Run this script to check code quality before committing

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

echo "=========================================="
echo "  Shredsquatch Code Linting"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

WARNINGS=0
ERRORS=0

# Function to print section header
section() {
    echo ""
    echo -e "${YELLOW}=== $1 ===${NC}"
}

# Function to print success
success() {
    echo -e "${GREEN}$1${NC}"
}

# Function to print warning
warning() {
    echo -e "${YELLOW}Warning: $1${NC}"
    WARNINGS=$((WARNINGS + 1))
}

# Function to print error
error() {
    echo -e "${RED}Error: $1${NC}"
    ERRORS=$((ERRORS + 1))
}

# Check for TODO/FIXME comments
section "Checking for TODO/FIXME comments"
TODO_COUNT=$(grep -rn --include="*.cs" -E "(TODO|FIXME|HACK|XXX|BUG):" Assets/Scripts 2>/dev/null | wc -l)
if [ "$TODO_COUNT" -gt 0 ]; then
    warning "Found $TODO_COUNT TODO/FIXME comments"
    grep -rn --include="*.cs" -E "(TODO|FIXME|HACK|XXX|BUG):" Assets/Scripts 2>/dev/null | head -10
else
    success "No TODO/FIXME comments found"
fi

# Check for Debug.Log statements
section "Checking for Debug.Log statements"
DEBUG_COUNT=$(grep -rn --include="*.cs" "Debug\.Log" Assets/Scripts 2>/dev/null | wc -l)
if [ "$DEBUG_COUNT" -gt 50 ]; then
    warning "Found $DEBUG_COUNT Debug.Log statements (consider reducing for production)"
else
    success "Debug.Log count is acceptable ($DEBUG_COUNT)"
fi

# Check for empty catch blocks
section "Checking for empty catch blocks"
EMPTY_CATCH=$(grep -rn --include="*.cs" -E "catch\s*\([^)]*\)\s*\{\s*\}" Assets/Scripts 2>/dev/null | wc -l)
if [ "$EMPTY_CATCH" -gt 0 ]; then
    error "Found $EMPTY_CATCH empty catch blocks"
    grep -rn --include="*.cs" -E "catch\s*\([^)]*\)\s*\{\s*\}" Assets/Scripts 2>/dev/null
else
    success "No empty catch blocks found"
fi

# Check for NotImplementedException
section "Checking for NotImplementedException"
NOT_IMPL=$(grep -rn --include="*.cs" "NotImplementedException" Assets/Scripts 2>/dev/null | grep -v "Tests" | wc -l)
if [ "$NOT_IMPL" -gt 0 ]; then
    warning "Found $NOT_IMPL NotImplementedException (excluding tests)"
    grep -rn --include="*.cs" "NotImplementedException" Assets/Scripts 2>/dev/null | grep -v "Tests"
else
    success "No NotImplementedException found in production code"
fi

# Check for hardcoded secrets
section "Checking for potential hardcoded secrets"
SECRETS=$(grep -rn --include="*.cs" -iE "(apikey|api_key|secret_key|password\s*=)" Assets/Scripts 2>/dev/null | grep -v "// " | wc -l)
if [ "$SECRETS" -gt 0 ]; then
    error "Found potential hardcoded secrets"
    grep -rn --include="*.cs" -iE "(apikey|api_key|secret_key|password\s*=)" Assets/Scripts 2>/dev/null | grep -v "// "
else
    success "No obvious hardcoded secrets found"
fi

# Check for missing meta files
section "Checking for missing .meta files"
MISSING_META=0
while IFS= read -r -d '' file; do
    if [ ! -f "${file}.meta" ]; then
        warning "Missing meta file for: $file"
        MISSING_META=$((MISSING_META + 1))
    fi
done < <(find Assets -type f ! -name "*.meta" ! -path "*/\.*" -print0 2>/dev/null)

if [ "$MISSING_META" -eq 0 ]; then
    success "All assets have corresponding .meta files"
fi

# Check for FindObjectOfType in runtime code (performance)
section "Checking for FindObjectOfType usage (performance)"
FIND_OBJECT=$(grep -rn --include="*.cs" "FindObjectOfType" Assets/Scripts 2>/dev/null | grep -v "Editor" | wc -l)
if [ "$FIND_OBJECT" -gt 10 ]; then
    warning "Found $FIND_OBJECT FindObjectOfType calls outside Editor scripts (consider caching)"
else
    success "FindObjectOfType usage is acceptable ($FIND_OBJECT)"
fi

# Check code statistics
section "Code Statistics"
CS_FILES=$(find Assets -name "*.cs" 2>/dev/null | wc -l)
CS_LINES=$(find Assets -name "*.cs" -exec cat {} \; 2>/dev/null | wc -l)
TEST_FILES=$(find Assets -path "*/Tests/*" -name "*.cs" 2>/dev/null | wc -l)

echo "C# files: $CS_FILES"
echo "Lines of code: $CS_LINES"
echo "Test files: $TEST_FILES"

# Summary
echo ""
echo "=========================================="
echo "  Summary"
echo "=========================================="
echo ""

if [ "$ERRORS" -gt 0 ]; then
    echo -e "${RED}Errors: $ERRORS${NC}"
fi

if [ "$WARNINGS" -gt 0 ]; then
    echo -e "${YELLOW}Warnings: $WARNINGS${NC}"
fi

if [ "$ERRORS" -eq 0 ] && [ "$WARNINGS" -eq 0 ]; then
    echo -e "${GREEN}All checks passed!${NC}"
    exit 0
elif [ "$ERRORS" -eq 0 ]; then
    echo -e "${YELLOW}Lint completed with warnings${NC}"
    exit 0
else
    echo -e "${RED}Lint failed with errors${NC}"
    exit 1
fi
