# Top-level Makefile for 5coy-archive.
# Currently focused on the `web` Blazor WebAssembly SPA.

DOTNET        ?= dotnet
CONFIGURATION ?= Debug

WEB_PROJECT      := web/web.csproj
WEB_TEST_PROJECT := web.test/web.test.csproj

.DEFAULT_GOAL := help

.PHONY: help clean build test run-local

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}'

clean: ## Remove build output for the web app and its tests
	$(DOTNET) clean $(WEB_PROJECT) -c $(CONFIGURATION)
	$(DOTNET) clean $(WEB_TEST_PROJECT) -c $(CONFIGURATION)

build: ## Restore and build the web app
	$(DOTNET) build $(WEB_PROJECT) -c $(CONFIGURATION)

test: ## Run the web unit tests
	$(DOTNET) test $(WEB_TEST_PROJECT) -c $(CONFIGURATION)

run-local: ## Run the web app locally with hot reload (dotnet watch)
	$(DOTNET) watch run --project $(WEB_PROJECT) --launch-profile web
