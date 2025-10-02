# Unit Test Gap Analyzer

A standalone tool for analyzing .NET solutions to identify unit test coverage gaps and automatically generate test stubs.

## Overview

This tool analyzes your .NET solution to:
- Identify methods that lack unit test coverage
- Calculate test coverage percentages
- Generate intelligent test stubs based on existing test patterns
- Provide recommendations for improving test coverage

## Features

- **AST-based Analysis**: Uses Roslyn to parse C# code and accurately identify methods
- **Intelligent Test Detection**: Analyzes existing tests to understand coverage
- **Cyclomatic Complexity**: Prioritizes methods based on complexity
- **Pattern Recognition**: Learns from existing test patterns in your codebase
- **Configurable**: Customize analysis behavior via configuration file
- **Test Generation**: Automatically generates test stubs following your project's conventions

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code (optional)

### Building the Project

```bash
cd C:\Users\KPeterson\CascadeProjects\UnitTestGapAnalyzer
dotnet build
```

### Running the Analyzer

**Option 1: With command-line argument**
```bash
dotnet run --project UnitTestGapAnalyzer.csproj "C:\Path\To\Your\Solution"
```

**Option 2: Interactive mode**
```bash
dotnet run --project UnitTestGapAnalyzer.csproj
# Then enter the solution path when prompted
```

### Configuration

Create a `testgap-config.json` file in your solution directory to customize the analysis:

```json
{
  "maxProjectsToAnalyze": 10,
  "maxFilesPerProject": 20,
  "minimumComplexity": 1,
  "autoGenerateTests": true,
  "createNewTestProjects": false,
  "onlyAnalyzeSrcProjects": true,
  "testProjectPatterns": ["Test", "Tests", ".Test", ".Tests"],
  "testDirectoryPatterns": ["test", "tests", "Test", "Tests"],
  "srcDirectoryPatterns": ["src", "source", "Sources"],
  "excludedMethods": [
    "ToString",
    "GetHashCode", 
    "Equals",
    "Dispose",
    "Main",
    "ConfigureServices",
    "Configure",
    "OnModelCreating"
  ],
  "excludedClasses": [
    "Program",
    "Startup",
    "DbContext"
  ],
  "excludedProjects": [
    "SampleLibrary",
    "Sample",
    "Demo"
  ]
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `maxProjectsToAnalyze` | int | 10 | Maximum number of projects to analyze |
| `maxFilesPerProject` | int | 20 | Maximum files to analyze per project |
| `minimumComplexity` | int | 1 | Minimum cyclomatic complexity to consider |
| `autoGenerateTests` | bool | true | Automatically generate test stubs |
| `createNewTestProjects` | bool | false | Create new test projects if none exist |
| `onlyAnalyzeSrcProjects` | bool | true | Only analyze projects in src directories |
| `testProjectPatterns` | string[] | ["Test", "Tests"] | Patterns to identify test projects |
| `excludedMethods` | string[] | [...] | Method names to exclude from analysis |
| `excludedClasses` | string[] | [...] | Class names to exclude from analysis |
| `excludedProjects` | string[] | [...] | Project names to exclude from analysis |

## Output

The analyzer generates a JSON report: `test_gap_analysis_result.json` in your solution directory.

### Sample Output

```json
{
  "analysisType": "UnitTestGapAnalysis",
  "solutionPath": "C:\\Path\\To\\Solution",
  "timestamp": "2025-10-02T10:23:00",
  "projectsAnalyzed": 15,
  "unitTestGaps": [
    {
      "className": "UserService",
      "methodName": "CreateUser",
      "filePath": "C:\\Path\\To\\UserService.cs",
      "reason": "Creation method requires unit test validation",
      "testProject": "UserService.Tests",
      "generatedTest": "...",
      "testGenerated": true
    }
  ],
  "summary": {
    "totalUnitTestGaps": 42,
    "testCoverageAssessment": {
      "overallCoverageQuality": "Good",
      "totalMethodsAnalyzed": 150,
      "methodsWithExistingTests": 108,
      "coveragePercentage": 72.0,
      "recommendations": [
        "Focus on testing creation and deletion methods",
        "Consider adding integration tests for complex workflows"
      ]
    },
    "executionTimeMs": 5432
  }
}
```

## How It Works

1. **Project Discovery**: Scans the solution directory for `.csproj` files
2. **Source Analysis**: Uses Roslyn to parse C# files and extract method information
3. **Test Detection**: Identifies existing test projects and analyzes test coverage
4. **Gap Identification**: Compares source methods against test coverage
5. **Test Generation**: Creates test stubs based on existing patterns or intelligent defaults
6. **Report Generation**: Outputs comprehensive JSON report with findings

## Architecture

- **Program.cs**: Entry point and orchestration
- **TestGapAnalyzer.cs**: Core analysis engine
- **Models.cs**: Data models for analysis results
- **testgap-config.json**: Configuration file

## Differences from VSIntegrationPOC

This is a standalone version of the unit test gap analyzer that was previously part of the VSIntegrationPOC solution. Key differences:

- **Focused**: Only handles unit test gap analysis (no security scanning)
- **Standalone**: Can be run independently without Visual Studio integration
- **Simplified**: Removed dependencies on security analyzer components
- **Portable**: Easier to integrate into CI/CD pipelines

## Integration with CI/CD

You can integrate this tool into your build pipeline:

```yaml
# Example Azure DevOps pipeline step
- task: DotNetCoreCLI@2
  displayName: 'Run Unit Test Gap Analysis'
  inputs:
    command: 'run'
    projects: '**/UnitTestGapAnalyzer.csproj'
    arguments: '$(Build.SourcesDirectory)'
```

## Contributing

This tool is part of a larger code analysis suite. For improvements or bug fixes, please ensure:
- Tests are added for new functionality
- Code follows existing patterns
- Documentation is updated

## License

Internal use only.

## Support

For questions or issues, contact the development team.
