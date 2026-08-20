#!/bin/bash
# Run integration tests against netopeer2/sysrepo in Docker
# Usage: ./run-integration-tests.sh [--build-only | --up-only | --down]
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker/docker-compose.yml"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

case "${1:-}" in
    --down)
        echo -e "${YELLOW}Tearing down containers...${NC}"
        docker compose -f "$COMPOSE_FILE" down -v
        exit 0
        ;;
    --up-only)
        echo -e "${YELLOW}Starting netopeer2 only (for local dev)...${NC}"
        docker compose -f "$COMPOSE_FILE" up -d netopeer2
        echo -e "${GREEN}netopeer2 is running on localhost:830${NC}"
        echo "Run tests locally with: dotnet test --project IntegrationTests/IntegrationTests.csproj"
        exit 0
        ;;
    --build-only)
        echo -e "${YELLOW}Building containers only...${NC}"
        docker compose -f "$COMPOSE_FILE" build
        exit 0
        ;;
esac

echo -e "${YELLOW}=== DotnetYang Integration Tests ===${NC}"
echo ""

# Copy YANG modules that we want to test with
echo "Copying YANG modules for netopeer2..."
cp "$PROJECT_ROOT/TestData/YangSource/ietf/ietf-interfaces@2018-02-20.yang" \
   "$SCRIPT_DIR/yang-modules/" 2>/dev/null || true
cp "$PROJECT_ROOT/TestData/YangSource/ietf/ietf-yang-types.yang" \
   "$SCRIPT_DIR/yang-modules/" 2>/dev/null || true
cp "$PROJECT_ROOT/TestData/YangSource/ietf/ietf-inet-types@2013-07-15.yang" \
   "$SCRIPT_DIR/yang-modules/" 2>/dev/null || true
cp "$PROJECT_ROOT/TestData/YangSource/ietf/ietf-ip@2018-02-22.yang" \
   "$SCRIPT_DIR/yang-modules/" 2>/dev/null || true
cp "$PROJECT_ROOT/TestData/YangSource/iana/iana-if-type@2023-01-26.yang" \
   "$SCRIPT_DIR/yang-modules/" 2>/dev/null || true

# Build and run
echo ""
echo "Building and starting containers..."
docker compose -f "$COMPOSE_FILE" build

echo ""
echo "Running integration tests..."
docker compose -f "$COMPOSE_FILE" up --abort-on-container-exit --exit-code-from integration-tests

EXIT_CODE=$?

# Cleanup
echo ""
echo "Cleaning up..."
docker compose -f "$COMPOSE_FILE" down -v

if [ $EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✓ All integration tests passed${NC}"
else
    echo -e "${RED}✗ Integration tests failed (exit code: $EXIT_CODE)${NC}"
fi

exit $EXIT_CODE
