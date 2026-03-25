.PHONY: help build test clean restore run docker-build docker-run format lint docs install-tools

# Default target
help:
	@echo "Redis Cache Patterns - Build Commands"
	@echo "======================================"
	@echo ""
	@echo "Development:"
	@echo "  make restore      - Restore NuGet dependencies"
	@echo "  make build        - Build in Debug mode"
	@echo "  make rebuild      - Clean and build"
	@echo "  make run          - Run the application"
	@echo "  make watch        - Run with file watching"
	@echo ""
	@echo "Testing:"
	@echo "  make test         - Run all tests"
	@echo "  make test-watch   - Run tests with watching"
	@echo "  make coverage     - Generate code coverage report"
	@echo ""
	@echo "Code Quality:"
	@echo "  make format       - Format code with dotnet format"
	@echo "  make lint         - Run code analysis"
	@echo "  make check        - Format check without changes"
	@echo ""
	@echo "Production:"
	@echo "  make release      - Build Release configuration"
	@echo "  make publish      - Publish to output directory"
	@echo ""
	@echo "Docker:"
	@echo "  make docker-build - Build Docker image"
	@echo "  make docker-run   - Run with docker-compose"
	@echo "  make docker-stop  - Stop docker-compose"
	@echo "  make docker-clean - Remove docker containers and volumes"
	@echo ""
	@echo "Documentation:"
	@echo "  make docs         - Generate documentation"
	@echo "  make docs-serve   - Serve documentation locally"
	@echo ""
	@echo "Maintenance:"
	@echo "  make clean        - Clean build artifacts"
	@echo "  make install-tools - Install required tools"
	@echo "  make update       - Update NuGet packages"
	@echo "  make version      - Show .NET version"
	@echo ""

# Configuration
DOTNET := dotnet
CONFIGURATION ?= Debug
OUTPUT_DIR ?= ./bin/$(CONFIGURATION)
PUBLISH_DIR ?= ./publish
DOCKER_IMAGE ?= redis-cache-patterns:latest

# Development targets
restore:
	@echo "Restoring NuGet packages..."
	@$(DOTNET) restore

build: restore
	@echo "Building in $(CONFIGURATION) mode..."
	@$(DOTNET) build -c $(CONFIGURATION) --no-restore

rebuild: clean build
	@echo "Rebuild complete"

run: build
	@echo "Running application..."
	@$(DOTNET) run -c $(CONFIGURATION)

watch:
	@echo "Running with file watching..."
	@$(DOTNET) watch -p RedisCachePatterns.csproj run -c $(CONFIGURATION)

# Testing targets
test: build
	@echo "Running tests..."
	@$(DOTNET) test -c $(CONFIGURATION) --no-build --verbosity normal

test-watch:
	@echo "Running tests with watching..."
	@$(DOTNET) watch -p RedisCachePatterns.csproj test -c $(CONFIGURATION)

coverage: build
	@echo "Generating code coverage..."
	@$(DOTNET) test -c $(CONFIGURATION) --no-build \
		/p:CollectCoverage=true \
		/p:CoverletOutputFormat=opencover \
		/p:CoverletOutput=coverage/coverage.xml

# Code quality targets
format:
	@echo "Formatting code..."
	@$(DOTNET) format

check:
	@echo "Checking code format (no changes)..."
	@$(DOTNET) format --verify-no-changes --verbosity diagnostic

lint:
	@echo "Running code analysis..."
	@$(DOTNET) build -c $(CONFIGURATION) -p:TreatWarningsAsErrors=false

analyze:
	@echo "Running static analysis..."
	@$(DOTNET) build -c Release -p:EnableNETAnalyzers=true

# Release targets
release:
	@echo "Building Release configuration..."
	@$(DOTNET) build -c Release

publish: release
	@echo "Publishing to $(PUBLISH_DIR)..."
	@$(DOTNET) publish -c Release -o $(PUBLISH_DIR)
	@echo "✓ Published to $(PUBLISH_DIR)"

# Docker targets
docker-build:
	@echo "Building Docker image: $(DOCKER_IMAGE)"
	@docker build -t $(DOCKER_IMAGE) .
	@echo "✓ Docker image built: $(DOCKER_IMAGE)"

docker-run: docker-build
	@echo "Starting docker-compose..."
	@docker-compose up --build

docker-stop:
	@echo "Stopping docker-compose..."
	@docker-compose down

docker-clean:
	@echo "Cleaning Docker resources..."
	@docker-compose down -v
	@docker rmi $(DOCKER_IMAGE)
	@echo "✓ Docker resources cleaned"

docker-logs:
	@docker-compose logs -f app

docker-logs-redis:
	@docker-compose logs -f redis

# Documentation targets
docs:
	@echo "Documentation is in docs/ directory"
	@echo "- docs/GETTING_STARTED.md - Installation and quick start"
	@echo "- docs/ARCHITECTURE.md - System design and patterns"
	@echo "- docs/API_REFERENCE.md - Complete API documentation"
	@echo "- docs/DEPLOYMENT.md - Production deployment"
	@echo "- docs/FAQ.md - Frequently asked questions"

docs-serve:
	@echo "Serving documentation at http://localhost:8000"
	@cd docs && python3 -m http.server 8000

# Maintenance targets
clean:
	@echo "Cleaning build artifacts..."
	@$(DOTNET) clean
	@rm -rf $(OUTPUT_DIR)
	@rm -rf $(PUBLISH_DIR)
	@rm -rf coverage/
	@find . -name "*.log" -delete
	@find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
	@find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
	@echo "✓ Clean complete"

install-tools:
	@echo "Installing development tools..."
	@$(DOTNET) tool update -g dotnet-format
	@$(DOTNET) tool update -g dotnet-reportgenerator-globaltool
	@echo "✓ Tools installed"

update:
	@echo "Updating NuGet packages..."
	@$(DOTNET) package search StackExchange.Redis
	@echo "Run: dotnet add package <name> --version <version>"

version:
	@$(DOTNET) --version
	@echo ""
	@$(DOTNET) --info

# Git targets
commit-build: format test
	@echo "Code formatted and tested. Ready to commit."

pre-push: rebuild test lint
	@echo "All checks passed. Ready to push."

# Special targets
install: restore build
	@echo "✓ Installation complete"

dev-setup: install-tools restore build
	@echo "✓ Development environment setup complete"
	@echo ""
	@echo "Next steps:"
	@echo "  1. Start Redis: docker run -p 6379:6379 redis:7-alpine"
	@echo "  2. Run: make run"
	@echo "  3. Visit: http://localhost:5000"

prod-setup: publish
	@echo "✓ Production build complete"
	@echo "Output directory: $(PUBLISH_DIR)"
	@echo ""
	@echo "Deploy with:"
	@echo "  dotnet $(PUBLISH_DIR)/RedisCachePatterns.dll"

benchmarks:
	@echo "Running benchmarks (if available)..."
	@$(DOTNET) run -c Release --project RedisCachePatterns.Benchmarks.csproj

# Phony targets (don't check for files)
.PHONY: help restore build rebuild run watch test test-watch coverage
.PHONY: format check lint analyze release publish
.PHONY: docker-build docker-run docker-stop docker-clean docker-logs docker-logs-redis
.PHONY: docs docs-serve clean install-tools update version
.PHONY: commit-build pre-push install dev-setup prod-setup benchmarks
