CI_BUILD_NUMBER ?= 0

SRC_REVISION=$(shell git rev-parse --short HEAD)

VER_MAJOR=1
VER_MINOR=0
VER_PATCH=$(CI_BUILD_NUMBER)
VERSION = $(VER_MAJOR).$(VER_MINOR).$(VER_PATCH)

.PHONY: debug-watch
debug-watch:
	uv run watchmedo auto-restart --signal SIGKILL --patterns '*.g4;*.sbbcode'  --  make debug

.PHONY: debug
debug:
	uv run antlr4-parse *.g4 parse -gui < example.sbbcode

antlr4.jar:
	curl -L --output antlr4.jar https://www.antlr.org/download/antlr-4.13.2-complete.jar

.PHONY: csharp-gen
csharp-gen: antlr4.jar
	rm -rf /tmp/libSBBCode-csharp
	mkdir -p ./csharp/Internal
	rm -f ./csharp/Internal/*.g.cs

	java -jar antlr4.jar -Dlanguage=CSharp -no-listener *.g4 -o /tmp/libSBBCode-csharp -package libSBBCode.Internal
	for file in /tmp/libSBBCode-csharp/*.cs; do \
        new_name="$${file%.cs}.g.cs" &&  mv "$$file" "$$new_name" \
    ;done

	mv /tmp/libSBBCode-csharp/*.cs ./csharp/libSBBCode/Internal

.PHONY: csharp
csharp: csharp-gen
	rm -rf dist/csharp

	cd csharp && \
	dotnet tool restore && \
	cd libSBBCode && \
	dotnet paket restore --silent --fail-on-checks && \
	dotnet pack libSBBCode.csproj --nologo --output=../../dist/csharp --configuration Release -p:Version=${VERSION} -p:SourceRevisionId=${SRC_REVISION} -p:PaketDisableGlobalRestore=true
	
