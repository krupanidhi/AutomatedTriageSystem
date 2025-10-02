using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnitTestGapAnalyzer
{
    public class TestGapAnalyzer
    {
        private readonly TestGapAnalysisConfig _config;
        
        public TestGapAnalyzer(TestGapAnalysisConfig config = null)
        {
            _config = config ?? new TestGapAnalysisConfig();
        }

        public (List<UnitTestGap> testGaps, TestCoverageAssessment coverageAssessment) AnalyzeAndGenerateTests(string[] projectFiles)
        {
            var testGaps = new List<UnitTestGap>();
            var coverageAssessment = new TestCoverageAssessment();
            var solutionDir = FindSolutionDirectory(projectFiles);
            Console.WriteLine($"Solution directory: {solutionDir}");
            
            // Filter out projects that are not part of the solution
            var solutionProjects = FilterSolutionProjects(projectFiles, solutionDir);
            Console.WriteLine($"Filtered to {solutionProjects.Length} solution projects (from {projectFiles.Length} total)");
            
            Console.WriteLine("Projects to analyze:");
            foreach (var proj in solutionProjects)
            {
                Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(proj)}");
            }
            
            // Initialize coverage tracking
            int totalMethodsAnalyzed = 0;
            int methodsWithExistingTests = 0;
            var projectsWithComprehensiveCoverage = new List<string>();
            var testQualityIndicators = new List<string>();
            var existingTestPatterns = new List<string>();

            foreach (var projectFile in solutionProjects.Take(_config.MaxProjectsToAnalyze))
            {
                try
                {
                    var projectDir = Path.GetDirectoryName(projectFile) ?? "";
                    var projectName = Path.GetFileNameWithoutExtension(projectFile);
                    
                    // Skip test projects
                    if (IsTestProject(projectName))
                        continue;
                    
                    // Skip excluded projects
                    if (_config.ExcludedProjects.Any(excluded => 
                        projectName.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine($"Skipping {projectName} - excluded project");
                        continue;
                    }
                    
                    // Only analyze projects in "src" directories if configured
                    if (_config.OnlyAnalyzeSrcProjects && !IsInSrcDirectory(projectFile))
                    {
                        Console.WriteLine($"Skipping {projectName} - not in src directory");
                        continue;
                    }

                    Console.WriteLine($"Analyzing project: {projectName}");

                    // Find or create test project
                    var testProject = FindOrCreateTestProject(projectFile, solutionDir);
                    
                    if (testProject == null)
                    {
                        Console.WriteLine($"No test project found for {projectName} - skipping test generation");
                        continue;
                    }
                    
                    // Analyze source files with AST
                    var sourceFiles = GetSourceFiles(projectDir);
                    
                    foreach (var sourceFile in sourceFiles.Take(_config.MaxFilesPerProject))
                    {
                        Console.WriteLine($"  Analyzing source file: {Path.GetFileName(sourceFile)}");
                        var methods = ExtractMethodsWithAST(sourceFile);
                        Console.WriteLine($"  Found {methods.Count} methods to analyze");
                        
                        // Build test coverage map for intelligent analysis
                        var testProjects = new List<TestProjectInfo> { testProject };
                        var coverageMap = BuildTestCoverageMap(testProjects);
                        
                        // Track coverage quality for this project
                        int projectMethodsAnalyzed = 0;
                        int projectMethodsWithTests = 0;
                        
                        foreach (var method in methods)
                        {
                            Console.WriteLine($"    Checking method: {method.ClassName}.{method.MethodName} (complexity: {method.CyclomaticComplexity})");
                            
                            // Check if method should be tested
                            if (!IsBusinessMethod(method.MethodSyntax))
                            {
                                Console.WriteLine($"        - Skipped: Not a business method");
                                continue;
                            }
                            
                            projectMethodsAnalyzed++;
                            totalMethodsAnalyzed++;
                            
                            // Check if method is already covered by existing tests
                            var isCovered = IsMethodCovered(method, coverageMap);
                            Console.WriteLine($"    Has test coverage: {isCovered}");
                            
                            if (isCovered)
                            {
                                projectMethodsWithTests++;
                                methodsWithExistingTests++;
                                Console.WriteLine($"    ✅ COVERED: Method has existing test coverage");
                            }
                            else
                            {
                                Console.WriteLine($"    ✓ UNCOVERED GAP FOUND: {method.ClassName}.{method.MethodName}");
                                var generatedTest = GenerateTestMethod(method, testProject.Framework);
                                var gap = new UnitTestGap
                                {
                                    ClassName = method.ClassName,
                                    MethodName = method.MethodName,
                                    FilePath = sourceFile,
                                    Reason = DetermineTestGapReason(method),
                                    TestProject = testProject.ProjectPath,
                                    GeneratedTest = generatedTest,
                                    TestGenerated = false
                                };
                                
                                testGaps.Add(gap);
                                
                                // Auto-generate and add test to project
                                if (_config.AutoGenerateTests)
                                {
                                    var success = AddTestToProject(gap, testProject);
                                    gap.TestGenerated = success;
                                }
                            }
                        }
                        
                        // Assess project coverage quality
                        if (projectMethodsAnalyzed > 0)
                        {
                            double projectCoverage = (double)projectMethodsWithTests / projectMethodsAnalyzed * 100;
                            Console.WriteLine($"  Project coverage: {projectCoverage:F1}% ({projectMethodsWithTests}/{projectMethodsAnalyzed} methods)");
                            
                            if (projectCoverage >= 80)
                            {
                                projectsWithComprehensiveCoverage.Add(projectName);
                                Console.WriteLine($"  ✅ {projectName} has comprehensive test coverage!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not analyze project {projectFile}: {ex.Message}");
                }
            }

            // Build comprehensive coverage assessment
            coverageAssessment = BuildCoverageAssessment(
                totalMethodsAnalyzed, 
                methodsWithExistingTests, 
                projectsWithComprehensiveCoverage,
                testQualityIndicators,
                existingTestPatterns,
                testGaps.Count
            );

            return (testGaps, coverageAssessment);
        }

        private List<EnhancedMethodInfo> ExtractMethodsWithAST(string sourceFile)
        {
            var methods = new List<EnhancedMethodInfo>();
            
            try
            {
                var code = File.ReadAllText(sourceFile);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetCompilationUnitRoot();
                
                var allMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
                Console.WriteLine($"      Total methods in file: {allMethods.Count}");
                
                var publicMethods = allMethods.Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword)).ToList();
                Console.WriteLine($"      Public methods: {publicMethods.Count}");
                
                var nonPropertyMethods = publicMethods.Where(m => !IsPropertyAccessor(m)).ToList();
                Console.WriteLine($"      Non-property methods: {nonPropertyMethods.Count}");
                
                var methodDeclarations = nonPropertyMethods.Where(m => IsBusinessMethod(m)).ToList();
                Console.WriteLine($"      Business methods: {methodDeclarations.Count}");
                    
                foreach (var method in methodDeclarations)
                {
                    var className = GetContainingClassName(method);
                    var namespaceName = GetContainingNamespace(method);
                    
                    methods.Add(new EnhancedMethodInfo
                    {
                        ClassName = className,
                        MethodName = method.Identifier.ValueText,
                        ReturnType = method.ReturnType?.ToString() ?? "void",
                        Parameters = GetParameterInfo(method),
                        IsAsync = method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                        CyclomaticComplexity = CalculateCyclomaticComplexity(method),
                        FilePath = sourceFile,
                        Namespace = namespaceName,
                        IsPublic = method.Modifiers.Any(SyntaxKind.PublicKeyword),
                        MethodSyntax = method
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AST parsing failed for {sourceFile}: {ex.Message}");
            }
            
            return methods;
        }

        private bool IsBusinessMethod(MethodDeclarationSyntax method)
        {
            var methodName = method.Identifier.ValueText;
            
            // Skip framework methods
            var skipMethods = new[] { "ToString", "GetHashCode", "Equals", "Dispose", "Main", "ConfigureServices", "Configure", "OnModelCreating" };
            if (skipMethods.Contains(methodName))
            {
                Console.WriteLine($"        Skipping framework method: {methodName}");
                return false;
            }
                
            // Skip property accessors
            if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
            {
                Console.WriteLine($"        Skipping property accessor: {methodName}");
                return false;
            }
                
            // Check complexity threshold
            var complexity = CalculateCyclomaticComplexity(method);
            if (complexity < _config.MinimumComplexity)
            {
                Console.WriteLine($"        Skipping low complexity method: {methodName} (complexity: {complexity}, min: {_config.MinimumComplexity})");
                return false;
            }
            
            Console.WriteLine($"        ✅ Business method: {methodName} (complexity: {complexity})");
            return true;
        }

        private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
        {
            var complexity = 1; // Base complexity
            
            var body = method.Body ?? (SyntaxNode)method.ExpressionBody;
            if (body == null) return complexity;
            
            // Count decision points
            var decisionNodes = body.DescendantNodes().Where(node =>
                node.IsKind(SyntaxKind.IfStatement) ||
                node.IsKind(SyntaxKind.WhileStatement) ||
                node.IsKind(SyntaxKind.ForStatement) ||
                node.IsKind(SyntaxKind.ForEachStatement) ||
                node.IsKind(SyntaxKind.SwitchStatement) ||
                node.IsKind(SyntaxKind.CaseSwitchLabel) ||
                node.IsKind(SyntaxKind.ConditionalExpression) ||
                node.IsKind(SyntaxKind.LogicalAndExpression) ||
                node.IsKind(SyntaxKind.LogicalOrExpression));
                
            return complexity + decisionNodes.Count();
        }

        private TestProjectInfo FindOrCreateTestProject(string sourceProjectFile, string solutionDir)
        {
            var sourceProjectName = Path.GetFileNameWithoutExtension(sourceProjectFile);
            var sourceProjectDir = Path.GetDirectoryName(sourceProjectFile) ?? "";
            
            // First, find ALL existing test projects in the solution
            var existingTestProjects = FindAllTestProjects(solutionDir);
            Console.WriteLine($"Found {existingTestProjects.Count} test projects in solution:");
            foreach (var tp in existingTestProjects)
            {
                Console.WriteLine($"  - {tp.ProjectName} ({tp.Framework})");
            }
            
            // Look for the most appropriate existing test project
            var bestMatch = FindBestMatchingTestProject(sourceProjectName, sourceProjectDir, existingTestProjects);
            
            if (bestMatch != null)
            {
                Console.WriteLine($"Using existing test project: {bestMatch.ProjectName} for {sourceProjectName}");
                return bestMatch;
            }
            
            // Only create new test project if explicitly configured to do so
            if (!_config.CreateNewTestProjects)
            {
                Console.WriteLine($"No existing test project found for {sourceProjectName} and CreateNewTestProjects is disabled");
                return null;
            }
            
            // Create new test project as last resort
            var newTestProjectName = $"{sourceProjectName}.Tests";
            var newTestProjectDir = Path.Combine(solutionDir, newTestProjectName);
            var newTestProjectPath = Path.Combine(newTestProjectDir, $"{newTestProjectName}.csproj");
            
            CreateTestProject(newTestProjectDir, newTestProjectName, sourceProjectFile);
            
            return new TestProjectInfo
            {
                ProjectPath = newTestProjectPath,
                ProjectName = newTestProjectName,
                Framework = TestFramework.XUnit, // Default to XUnit
                IsNew = true
            };
        }

        private List<TestProjectInfo> FindAllTestProjects(string solutionDir)
        {
            var testProjects = new List<TestProjectInfo>();
            
            try
            {
                // First, look specifically in test directories
                var testDirectories = new[]
                {
                    Path.Combine(solutionDir, "test"),
                    Path.Combine(solutionDir, "tests"),
                    Path.Combine(solutionDir, "Test"),
                    Path.Combine(solutionDir, "Tests")
                };
                
                foreach (var testDir in testDirectories.Where(Directory.Exists))
                {
                    Console.WriteLine($"Searching for test projects in: {testDir}");
                    var testProjectFiles = Directory.GetFiles(testDir, "*.csproj", SearchOption.AllDirectories);
                    
                    foreach (var projectFile in testProjectFiles)
                    {
                        var projectName = Path.GetFileNameWithoutExtension(projectFile);
                        Console.WriteLine($"  Found potential test project: {projectName}");
                        
                        testProjects.Add(new TestProjectInfo
                        {
                            ProjectPath = projectFile,
                            ProjectName = projectName,
                            Framework = DetectTestFramework(projectFile),
                            IsNew = false
                        });
                    }
                }
                
                // Fallback: Find all .csproj files in the solution and check if they're test projects
                if (!testProjects.Any())
                {
                    Console.WriteLine("No test projects found in test directories, searching entire solution...");
                    var allProjectFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
                    
                    foreach (var projectFile in allProjectFiles)
                    {
                        var projectName = Path.GetFileNameWithoutExtension(projectFile);
                        
                        // Check if this is a test project
                        if (IsTestProject(projectName) || ContainsTestPackages(projectFile))
                        {
                            Console.WriteLine($"  Found test project by name/packages: {projectName}");
                            testProjects.Add(new TestProjectInfo
                            {
                                ProjectPath = projectFile,
                                ProjectName = projectName,
                                Framework = DetectTestFramework(projectFile),
                                IsNew = false
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding test projects: {ex.Message}");
            }
            
            return testProjects;
        }

        private TestProjectInfo FindBestMatchingTestProject(string sourceProjectName, string sourceProjectDir, List<TestProjectInfo> testProjects)
        {
            if (!testProjects.Any())
            {
                Console.WriteLine($"No test projects available for matching");
                return null;
            }
            
            Console.WriteLine($"Matching source project '{sourceProjectName}' against {testProjects.Count} test projects:");
            
            // For this solution structure, ALL source projects should use Platform.CrossCutting8.Test
            // The test organization is done via subfolders within that project
            
            // Strategy 1: Look for the main test project (Platform.CrossCutting8.Test)
            var mainTestProject = testProjects.FirstOrDefault(tp => 
                tp.ProjectName.Contains("Platform.CrossCutting8.Test", StringComparison.OrdinalIgnoreCase) ||
                tp.ProjectName.Contains("CrossCutting8.Test", StringComparison.OrdinalIgnoreCase) ||
                tp.ProjectName.Contains("CrossCutting.Test", StringComparison.OrdinalIgnoreCase));
                
            if (mainTestProject != null)
            {
                Console.WriteLine($"✅ Found main test project: {mainTestProject.ProjectName} for {sourceProjectName}");
                
                // Create a specialized version that knows about the subfolder structure
                return new TestProjectInfo
                {
                    ProjectPath = mainTestProject.ProjectPath,
                    ProjectName = mainTestProject.ProjectName,
                    Framework = mainTestProject.Framework,
                    IsNew = false,
                    SubfolderForSource = GetTestSubfolderForSource(sourceProjectName) // Add this property
                };
            }
            
            // Fallback strategies (keep existing logic)
            Console.WriteLine($"Strategy 2: Looking for exact matches...");
            var exactMatch = testProjects.FirstOrDefault(tp => 
                tp.ProjectName.Equals($"{sourceProjectName}.Test", StringComparison.OrdinalIgnoreCase) ||
                tp.ProjectName.Equals($"{sourceProjectName}.Tests", StringComparison.OrdinalIgnoreCase));
                
            if (exactMatch != null)
            {
                Console.WriteLine($"✅ Found exact match: {exactMatch.ProjectName}");
                return exactMatch;
            }
            
            // Strategy 3: Use the first available test project (fallback)
            Console.WriteLine($"Using fallback test project {testProjects.First().ProjectName} for {sourceProjectName}");
            return testProjects.First();
        }

        private string GetTestSubfolderForSource(string sourceProjectName)
        {
            // Map source project names to their test subfolders
            // Platform.CrossCutting8.Caching -> Caching
            // Platform.CrossCutting8.Configuration -> Configuration
            
            if (sourceProjectName.Contains("Caching", StringComparison.OrdinalIgnoreCase))
                return "Caching";
            if (sourceProjectName.Contains("Configuration", StringComparison.OrdinalIgnoreCase))
                return "Configuration";
            if (sourceProjectName.Contains("Cryptography", StringComparison.OrdinalIgnoreCase))
                return "Cryptography";
            if (sourceProjectName.Contains("IOC", StringComparison.OrdinalIgnoreCase))
                return "IOC";
            if (sourceProjectName.Contains("Logging", StringComparison.OrdinalIgnoreCase))
                return "Logging";
            if (sourceProjectName.Contains("Contracts", StringComparison.OrdinalIgnoreCase))
                return ""; // Might be in root or separate folder
                
            // Default: extract the last part after the last dot
            var parts = sourceProjectName.Split('.');
            if (parts.Length > 1)
            {
                var lastPart = parts.Last();
                // Remove version numbers
                return Regex.Replace(lastPart, @"\d+$", "");
            }
            
            return "";
        }

        private bool ContainsTestPackages(string projectFile)
        {
            try
            {
                var content = File.ReadAllText(projectFile);
                var testPackages = new[] { "Microsoft.NET.Test.Sdk", "xunit", "NUnit", "MSTest", "TestFramework" };
                return testPackages.Any(package => content.Contains(package, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private string ExtractBaseName(string projectName)
        {
            // Remove common suffixes and version numbers
            // Platform.CrossCutting8.All -> Platform.CrossCutting
            // Platform.CrossCutting8.Test -> Platform.CrossCutting
            
            var baseName = projectName;
            
            // Remove common suffixes
            var suffixes = new[] { ".All", ".Test", ".Tests", ".Contracts", ".Configuration", ".Caching", ".IOC", ".Logging", ".Cryptography", ".SampleLibrary" };
            foreach (var suffix in suffixes)
            {
                if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - suffix.Length);
                    break;
                }
            }
            
            // Remove version numbers (e.g., "8" from "Platform.CrossCutting8")
            if (baseName.Length > 0 && char.IsDigit(baseName[baseName.Length - 1]))
            {
                baseName = baseName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            }
            
            return baseName;
        }

        private void CreateTestProject(string testProjectDir, string testProjectName, string sourceProjectFile)
        {
            try
            {
                Directory.CreateDirectory(testProjectDir);
                
                var sourceProjectName = Path.GetFileNameWithoutExtension(sourceProjectFile);
                var testProjectPath = Path.Combine(testProjectDir, $"{testProjectName}.csproj");
                
                // Create test project file
                var testProjectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.6.0"" />
    <PackageReference Include=""xunit"" Version=""2.4.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.4.5"">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include=""coverlet.collector"" Version=""6.0.0"">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include=""Moq"" Version=""4.20.69"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\\{sourceProjectName}\\{sourceProjectName}.csproj"" />
  </ItemGroup>

</Project>";

                File.WriteAllText(testProjectPath, testProjectContent);
                
                Console.WriteLine($"Created new test project: {testProjectName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating test project {testProjectName}: {ex.Message}");
            }
        }

        private string GenerateTestMethod(EnhancedMethodInfo method, TestFramework framework)
        {
            // Try to find existing test patterns first
            var existingTestPattern = AnalyzeExistingTestPatterns(method);
            
            if (existingTestPattern != null)
            {
                return GenerateTestFromPattern(method, framework, existingTestPattern);
            }
            
            // Fallback to intelligent analysis
            return GenerateIntelligentTest(method, framework);
        }

        private string[] GetTestFiles(string testProjectPath)
        {
            var testDir = Path.GetDirectoryName(testProjectPath) ?? "";
            if (!Directory.Exists(testDir))
                return Array.Empty<string>();
            
            return Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("obj") && !f.Contains("bin"))
                .ToArray();
        }
        
        private Dictionary<string, HashSet<string>> BuildTestCoverageMap(List<TestProjectInfo> testProjects)
        {
            var coverageMap = new Dictionary<string, HashSet<string>>();
            
            foreach (var testProject in testProjects)
            {
                var testFiles = GetTestFiles(testProject.ProjectPath);
                
                foreach (var testFile in testFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(testFile);
                        var tree = CSharpSyntaxTree.ParseText(content);
                        var root = tree.GetCompilationUnitRoot();
                        
                        // Extract test methods and what they're testing
                        var testMethods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                            .Where(m => m.AttributeLists.Any(al => 
                                al.Attributes.Any(a => 
                                    a.Name.ToString().Contains("Fact") || 
                                    a.Name.ToString().Contains("Test"))))
                            .ToList();
                        
                        foreach (var testMethod in testMethods)
                        {
                            var coveredMethods = ExtractCoveredMethodsFromTest(testMethod, content);
                            foreach (var coveredMethod in coveredMethods)
                            {
                                if (!coverageMap.ContainsKey(coveredMethod.ClassName))
                                {
                                    coverageMap[coveredMethod.ClassName] = new HashSet<string>();
                                }
                                coverageMap[coveredMethod.ClassName].Add(coveredMethod.MethodName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error analyzing test file {testFile}: {ex.Message}");
                    }
                }
            }
            
            return coverageMap;
        }
        
        private List<(string ClassName, string MethodName)> ExtractCoveredMethodsFromTest(MethodDeclarationSyntax testMethod, string content)
        {
            var coveredMethods = new List<(string, string)>();
            
            // Extract method calls from test body
            var methodCalls = testMethod.DescendantNodes().OfType<InvocationExpressionSyntax>();
            
            foreach (var call in methodCalls)
            {
                var memberAccess = call.Expression as MemberAccessExpressionSyntax;
                if (memberAccess != null)
                {
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    
                    // Try to determine the class being tested
                    var className = ExtractClassNameFromTestContext(testMethod, content, memberAccess);
                    if (!string.IsNullOrEmpty(className))
                    {
                        coveredMethods.Add((className, methodName));
                    }
                }
            }
            
            return coveredMethods;
        }
        
        private string ExtractClassNameFromTestContext(MethodDeclarationSyntax testMethod, string content, MemberAccessExpressionSyntax memberAccess)
        {
            // Look for variable declarations like "var service = new SomeService()"
            var variableDeclarations = testMethod.DescendantNodes().OfType<VariableDeclarationSyntax>();
            
            foreach (var varDecl in variableDeclarations)
            {
                foreach (var variable in varDecl.Variables)
                {
                    var initializer = variable.Initializer?.Value as ObjectCreationExpressionSyntax;
                    if (initializer != null)
                    {
                        var typeName = initializer.Type.ToString();
                        
                        // Check if this variable is being used in the method call
                        var variableName = variable.Identifier.ValueText;
                        var memberExpression = memberAccess.Expression.ToString();
                        
                        if (memberExpression.Contains(variableName))
                        {
                            // Clean up type name (remove generic parameters, etc.)
                            return CleanTypeName(typeName);
                        }
                    }
                }
            }
            
            // Fallback: try to extract from test class name
            var testClassName = testMethod.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText;
            if (testClassName != null && testClassName.EndsWith("Test") || testClassName.EndsWith("Tests"))
            {
                return testClassName.Replace("Test", "").Replace("Tests", "");
            }
            
            return null;
        }
        
        private string CleanTypeName(string typeName)
        {
            // Remove generic parameters
            var genericIndex = typeName.IndexOf('<');
            if (genericIndex > 0)
            {
                typeName = typeName.Substring(0, genericIndex);
            }
            
            // Remove namespace prefixes
            var lastDot = typeName.LastIndexOf('.');
            if (lastDot > 0)
            {
                typeName = typeName.Substring(lastDot + 1);
            }
            
            return typeName;
        }
        
        private bool IsMethodCovered(EnhancedMethodInfo method, Dictionary<string, HashSet<string>> coverageMap)
        {
            if (coverageMap.ContainsKey(method.ClassName))
            {
                var coveredMethods = coverageMap[method.ClassName];
                
                // Check exact method name match
                if (coveredMethods.Contains(method.MethodName))
                {
                    return true;
                }
                
                // Check for common test naming patterns
                var testPatterns = new[]
                {
                    $"Can_I_{method.MethodName}",
                    $"Should_{method.MethodName}",
                    $"Test_{method.MethodName}",
                    $"{method.MethodName}_Should_",
                    $"{method.MethodName}_Test"
                };
                
                foreach (var pattern in testPatterns)
                {
                    if (coveredMethods.Any(m => m.Contains(pattern) || pattern.Contains(m)))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private TestCoverageAssessment BuildCoverageAssessment(
            int totalMethodsAnalyzed,
            int methodsWithExistingTests,
            List<string> projectsWithComprehensiveCoverage,
            List<string> testQualityIndicators,
            List<string> existingTestPatterns,
            int totalGaps)
        {
            var assessment = new TestCoverageAssessment
            {
                TotalMethodsAnalyzed = totalMethodsAnalyzed,
                MethodsWithExistingTests = methodsWithExistingTests,
                ProjectsWithComprehensiveCoverage = projectsWithComprehensiveCoverage
            };

            // Calculate coverage percentage
            if (totalMethodsAnalyzed > 0)
            {
                assessment.CoveragePercentage = (double)methodsWithExistingTests / totalMethodsAnalyzed * 100;
            }

            // Determine overall coverage quality
            if (assessment.CoveragePercentage >= 90)
            {
                assessment.OverallCoverageQuality = "Excellent";
                assessment.Recommendations.Add("Outstanding test coverage! Focus on maintaining existing test quality and adding tests only for new methods.");
            }
            else if (assessment.CoveragePercentage >= 75)
            {
                assessment.OverallCoverageQuality = "Good";
                assessment.Recommendations.Add("Good test coverage. Consider adding tests for the remaining uncovered methods.");
            }
            else if (assessment.CoveragePercentage >= 50)
            {
                assessment.OverallCoverageQuality = "Fair";
                assessment.Recommendations.Add("Moderate test coverage. Prioritize adding tests for critical business methods.");
            }
            else
            {
                assessment.OverallCoverageQuality = "Poor";
                assessment.Recommendations.Add("Low test coverage detected. Consider implementing a comprehensive testing strategy.");
            }

            // Add quality indicators based on analysis
            if (projectsWithComprehensiveCoverage.Any())
            {
                assessment.TestQualityIndicators.Add($"Found {projectsWithComprehensiveCoverage.Count} projects with comprehensive coverage (>80%)");
            }

            if (totalGaps == 0)
            {
                assessment.TestQualityIndicators.Add("No test gaps found - existing coverage appears comprehensive");
            }
            else if (totalGaps < 5)
            {
                assessment.TestQualityIndicators.Add("Very few test gaps found - excellent existing coverage");
            }

            // Add common test patterns found
            assessment.ExistingTestPatterns.AddRange(new[]
            {
                "Can_I_ naming convention",
                "Should_ naming convention", 
                "Proper mocking with Mock<T>",
                "Arrange-Act-Assert pattern",
                "Constructor dependency injection testing"
            });

            Console.WriteLine($"\n📊 COVERAGE ASSESSMENT:");
            Console.WriteLine($"   Overall Quality: {assessment.OverallCoverageQuality}");
            Console.WriteLine($"   Coverage: {assessment.CoveragePercentage:F1}% ({methodsWithExistingTests}/{totalMethodsAnalyzed})");
            Console.WriteLine($"   Projects with comprehensive coverage: {projectsWithComprehensiveCoverage.Count}");
            if (assessment.Recommendations.Any())
            {
                Console.WriteLine($"   Recommendations:");
                foreach (var rec in assessment.Recommendations)
                {
                    Console.WriteLine($"     - {rec}");
                }
            }

            return assessment;
        }

        private bool HasCorrespondingTest(TestProjectInfo testProject, EnhancedMethodInfo method)
        {
            // This method is now deprecated in favor of the coverage map approach
            return false;
        }

        private TestPattern AnalyzeExistingTestPatterns(EnhancedMethodInfo method)
        {
            try
            {
                // Find existing test files for this class
                var testFiles = FindExistingTestFiles(method.ClassName);
                
                if (testFiles.Any())
                {
                    var pattern = ExtractTestPattern(testFiles.First(), method.ClassName);
                    return pattern;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing test patterns: {ex.Message}");
            }
            
            return null;
        }

        private List<string> FindExistingTestFiles(string className)
        {
            var testFiles = new List<string>();
            
            // Look for test files that might contain tests for this class
            var possibleNames = new[]
            {
                $"{className}Test.cs",
                $"{className}Tests.cs",
                $"{className}UnitTest.cs",
                $"{className}UnitTests.cs"
            };
            
            // Search in common test directories
            var searchPaths = new[]
            {
                @"C:\Users\kpeterson\SECURITY\WindSurf\Enterprise-CrossCutting\test",
                @"test",
                @"tests",
                @"Test",
                @"Tests"
            };
            
            foreach (var searchPath in searchPaths)
            {
                if (Directory.Exists(searchPath))
                {
                    foreach (var fileName in possibleNames)
                    {
                        var files = Directory.GetFiles(searchPath, fileName, SearchOption.AllDirectories);
                        testFiles.AddRange(files);
                    }
                }
            }
            
            return testFiles;
        }

        private TestPattern ExtractTestPattern(string testFilePath, string className)
        {
            try
            {
                var content = File.ReadAllText(testFilePath);
                var tree = CSharpSyntaxTree.ParseText(content);
                var root = tree.GetCompilationUnitRoot();
                
                var pattern = new TestPattern
                {
                    UsesArrangeActAssert = content.Contains("//arrange") || content.Contains("// Arrange"),
                    UsesMocking = content.Contains("Mock<") || content.Contains("new Mock"),
                    MockingFramework = content.Contains("Moq") ? "Moq" : "Unknown",
                    ConstructorPattern = ExtractConstructorPattern(content, className),
                    CommonAssertions = ExtractCommonAssertions(content),
                    UsingStatements = ExtractUsingStatements(root),
                    TestNamingPattern = ExtractTestNamingPattern(content)
                };
                
                Console.WriteLine($"Extracted test pattern from {Path.GetFileName(testFilePath)}:");
                Console.WriteLine($"  - Uses AAA: {pattern.UsesArrangeActAssert}");
                Console.WriteLine($"  - Uses Mocking: {pattern.UsesMocking} ({pattern.MockingFramework})");
                Console.WriteLine($"  - Constructor Pattern: {pattern.ConstructorPattern}");
                
                return pattern;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting test pattern: {ex.Message}");
                return null;
            }
        }

        private string ExtractConstructorPattern(string content, string className)
        {
            // Look for how the class is instantiated in tests
            var patterns = new[]
            {
                $@"new {className}Wrapper\([^)]+\)",
                $@"new {className}\([^)]+\)",
                $@"var.*=.*new {className}Wrapper\([^)]+\)",
                $@"var.*=.*new {className}\([^)]+\)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(content, pattern);
                if (match.Success)
                {
                    return match.Value;
                }
            }
            
            return null;
        }

        private List<string> ExtractCommonAssertions(string content)
        {
            var assertions = new List<string>();
            
            var assertionPatterns = new[]
            {
                @"\.Verify\([^)]+\)",
                @"\.Should\(\)\.NotBeNull\(\)",
                @"\.Should\(\)\.Be\([^)]+\)",
                @"Assert\.[^(]+\([^)]+\)"
            };
            
            foreach (var pattern in assertionPatterns)
            {
                var matches = Regex.Matches(content, pattern);
                foreach (Match match in matches)
                {
                    if (!assertions.Contains(match.Value))
                        assertions.Add(match.Value);
                }
            }
            
            return assertions;
        }

        private List<string> ExtractUsingStatements(CompilationUnitSyntax root)
        {
            return root.Usings.Select(u => u.ToString().Trim()).ToList();
        }

        private string ExtractTestNamingPattern(string content)
        {
            var testMethods = Regex.Matches(content, @"public void ([^(]+)\(");
            if (testMethods.Count > 0)
            {
                var firstMethod = testMethods[0].Groups[1].Value;
                
                if (firstMethod.Contains("Can_I_"))
                    return "Can_I_[Action]_[Object]";
                if (firstMethod.Contains("Should_"))
                    return "[Method]_Should_[Behavior]";
                if (firstMethod.Contains("_When_"))
                    return "[Method]_When_[Condition]_Should_[Behavior]";
                    
                return "Custom";
            }
            
            return "Default";
        }

        private string GenerateTestFromPattern(EnhancedMethodInfo method, TestFramework framework, TestPattern pattern)
        {
            var sb = new StringBuilder();
            
            // Generate test method based on framework
            switch (framework)
            {
                case TestFramework.XUnit:
                    sb.AppendLine("        [Fact]");
                    break;
                case TestFramework.NUnit:
                    sb.AppendLine("        [Test]");
                    break;
                case TestFramework.MSTest:
                    sb.AppendLine("        [TestMethod]");
                    break;
            }
            
            // Generate test name based on pattern
            var testName = GenerateTestNameFromPattern(method, pattern);
            sb.AppendLine($"        public void {testName}()");
            sb.AppendLine("        {");
            
            if (pattern.UsesArrangeActAssert)
            {
                sb.AppendLine("            //arrange");
                
                // Generate mocks if pattern uses mocking
                if (pattern.UsesMocking && pattern.MockingFramework == "Moq")
                {
                    var sourceClass = AnalyzeSourceClass(method);
                    if (sourceClass != null)
                    {
                        foreach (var dependency in sourceClass.Dependencies)
                        {
                            sb.AppendLine($"            var mock{dependency.Replace("I", "")} = new Mock<{dependency}>();");
                        }
                        
                        // Generate constructor call with mocks
                        var mockParams = sourceClass.Dependencies.Select(d => $"mock{d.Replace("I", "")}.Object").ToList();
                        var constructorCall = GenerateConstructorCall(method.ClassName, mockParams, pattern);
                        sb.AppendLine($"            var _service = {constructorCall};");
                    }
                }
                
                // Generate method parameters
                foreach (var param in method.Parameters)
                {
                    var defaultValue = GetDefaultValue(param.Type);
                    if (defaultValue == "null")
                    {
                        sb.AppendLine($"            {param.Type} {param.Name} = null;");
                    }
                    else
                    {
                        sb.AppendLine($"            var {param.Name} = {defaultValue};");
                    }
                }
                
                sb.AppendLine();
                sb.AppendLine("            //act");
                var paramList = string.Join(", ", method.Parameters.Select(p => p.Name));
                
                if (method.ReturnType != "void")
                {
                    sb.AppendLine($"            var result = _service.{method.MethodName}({paramList});");
                }
                else
                {
                    sb.AppendLine($"            _service.{method.MethodName}({paramList});");
                }
                
                sb.AppendLine();
                sb.AppendLine("            //assert");
                
                // Generate assertions based on pattern
                if (pattern.UsesMocking)
                {
                    // Add mock verification if applicable
                    sb.AppendLine("            // TODO: Add mock verifications based on expected behavior");
                }
                
                if (method.ReturnType != "void")
                {
                    sb.AppendLine("            // TODO: Add result assertions");
                }
            }
            else
            {
                // Generate simple test without AAA pattern
                sb.AppendLine("            // TODO: Implement test logic");
            }
            
            sb.AppendLine("        }");
            
            return sb.ToString();
        }

        private string GenerateTestNameFromPattern(EnhancedMethodInfo method, TestPattern pattern)
        {
            switch (pattern.TestNamingPattern)
            {
                case "Can_I_[Action]_[Object]":
                    return $"Can_I_{method.MethodName}_{method.ClassName}";
                case "[Method]_Should_[Behavior]":
                    return $"{method.MethodName}_Should_ExecuteSuccessfully";
                case "[Method]_When_[Condition]_Should_[Behavior]":
                    return $"{method.MethodName}_When_ValidInput_Should_ExecuteSuccessfully";
                default:
                    return $"{method.MethodName}_Should_ExecuteSuccessfully";
            }
        }

        private string GenerateConstructorCall(string className, List<string> mockParams, TestPattern pattern)
        {
            if (pattern.ConstructorPattern != null && pattern.ConstructorPattern.Contains("Wrapper"))
            {
                return $"new {className}Wrapper({string.Join(", ", mockParams)})";
            }
            
            return $"new {className}({string.Join(", ", mockParams)})";
        }

        private SourceClassInfo AnalyzeSourceClass(EnhancedMethodInfo method)
        {
            try
            {
                // Find the source file for this method
                var sourceFile = method.FilePath;
                if (File.Exists(sourceFile))
                {
                    var content = File.ReadAllText(sourceFile);
                    var tree = CSharpSyntaxTree.ParseText(content);
                    var root = tree.GetCompilationUnitRoot();
                    
                    var classDeclaration = root.DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(c => c.Identifier.ValueText == method.ClassName);
                    
                    if (classDeclaration != null)
                    {
                        var sourceClass = new SourceClassInfo
                        {
                            ClassName = method.ClassName,
                            Dependencies = ExtractDependencies(classDeclaration),
                            ConstructorParameters = ExtractConstructorParameters(classDeclaration),
                            Interfaces = ExtractImplementedInterfaces(classDeclaration)
                        };
                        
                        Console.WriteLine($"Analyzed source class {method.ClassName}:");
                        Console.WriteLine($"  - Dependencies: {string.Join(", ", sourceClass.Dependencies)}");
                        Console.WriteLine($"  - Constructor params: {sourceClass.ConstructorParameters.Count}");
                        
                        return sourceClass;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing source class: {ex.Message}");
            }
            
            return null;
        }

        private List<string> ExtractDependencies(ClassDeclarationSyntax classDeclaration)
        {
            var dependencies = new List<string>();
            
            // Look for readonly fields (typical dependency injection pattern)
            var fields = classDeclaration.Members.OfType<FieldDeclarationSyntax>()
                .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)))
                .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)));
            
            foreach (var field in fields)
            {
                var fieldType = field.Declaration.Type.ToString();
                if (fieldType.StartsWith("I") && char.IsUpper(fieldType[1])) // Interface pattern
                {
                    dependencies.Add(fieldType);
                }
            }
            
            return dependencies;
        }

        private List<ParameterInfo> ExtractConstructorParameters(ClassDeclarationSyntax classDeclaration)
        {
            var parameters = new List<ParameterInfo>();
            
            var constructor = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            if (constructor != null)
            {
                foreach (var param in constructor.ParameterList.Parameters)
                {
                    parameters.Add(new ParameterInfo
                    {
                        Name = param.Identifier.ValueText,
                        Type = param.Type?.ToString() ?? "object"
                    });
                }
            }
            
            return parameters;
        }

        private List<string> ExtractImplementedInterfaces(ClassDeclarationSyntax classDeclaration)
        {
            var interfaces = new List<string>();
            
            if (classDeclaration.BaseList != null)
            {
                foreach (var baseType in classDeclaration.BaseList.Types)
                {
                    var typeName = baseType.Type.ToString();
                    if (typeName.StartsWith("I") && char.IsUpper(typeName[1]))
                    {
                        interfaces.Add(typeName);
                    }
                }
            }
            
            return interfaces;
        }

        private string GenerateIntelligentTest(EnhancedMethodInfo method, TestFramework framework)
        {
            // Fallback to original logic but with better constructor handling
            var sb = new StringBuilder();
            
            switch (framework)
            {
                case TestFramework.XUnit:
                    sb.AppendLine("        [Fact]");
                    break;
                case TestFramework.NUnit:
                    sb.AppendLine("        [Test]");
                    break;
                case TestFramework.MSTest:
                    sb.AppendLine("        [TestMethod]");
                    break;
            }
            
            var expectedBehavior = GetExpectedBehavior(method);
            sb.AppendLine($"        public void {method.MethodName}_{expectedBehavior}()");
            sb.AppendLine("        {");
            sb.AppendLine("            // Arrange");
            
            // Analyze source class for better constructor handling
            var sourceClass = AnalyzeSourceClass(method);
            if (sourceClass != null && sourceClass.Dependencies.Any())
            {
                // Generate mocks for dependencies
                foreach (var dependency in sourceClass.Dependencies)
                {
                    sb.AppendLine($"            var mock{dependency.Replace("I", "")} = new Mock<{dependency}>();");
                }
                
                var mockParams = sourceClass.Dependencies.Select(d => $"mock{d.Replace("I", "")}.Object");
                sb.AppendLine($"            var sut = new {method.ClassName}({string.Join(", ", mockParams)});");
            }
            else
            {
                sb.AppendLine($"            var sut = new {method.ClassName}();");
            }
            
            // Generate parameters
            if (method.Parameters.Any())
            {
                foreach (var param in method.Parameters)
                {
                    var defaultValue = GetDefaultValue(param.Type);
                    if (defaultValue == "null")
                    {
                        sb.AppendLine($"            {param.Type} {param.Name} = null;");
                    }
                    else
                    {
                        sb.AppendLine($"            var {param.Name} = {defaultValue};");
                    }
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("            // Act");
            var paramList = string.Join(", ", method.Parameters.Select(p => p.Name));
            
            if (method.ReturnType != "void")
            {
                sb.AppendLine($"            var result = sut.{method.MethodName}({paramList});");
            }
            else
            {
                sb.AppendLine($"            sut.{method.MethodName}({paramList});");
            }
            
            sb.AppendLine();
            sb.AppendLine("            // Assert");
            
            // Generate assert section
            if (method.ReturnType != "void")
            {
                sb.AppendLine("            result.Should().NotBeNull();");
                sb.AppendLine("            // TODO: Add specific assertions based on expected behavior");
            }
            else
            {
                sb.AppendLine("            // TODO: Add assertions to verify the method behavior");
            }
            
            sb.AppendLine("        }");
            
            return sb.ToString();
        }

        private bool AddTestToProject(UnitTestGap gap, TestProjectInfo testProject)
        {
            try
            {
                var testDir = Path.GetDirectoryName(testProject.ProjectPath) ?? "";
                
                // Use the correct subfolder if specified
                if (!string.IsNullOrEmpty(testProject.SubfolderForSource))
                {
                    testDir = Path.Combine(testDir, testProject.SubfolderForSource);
                    
                    // Create subfolder if it doesn't exist
                    if (!Directory.Exists(testDir))
                    {
                        Console.WriteLine($"Creating test subfolder: {testProject.SubfolderForSource}");
                        Directory.CreateDirectory(testDir);
                    }
                }
                
                var testFileName = $"{gap.ClassName}Tests.cs";
                var testFilePath = Path.Combine(testDir, testFileName);
                
                Console.WriteLine($"Adding test to: {testProject.SubfolderForSource}/{testFileName}");
                
                // Check if test file already exists
                if (File.Exists(testFilePath))
                {
                    // Append to existing file
                    var existingContent = File.ReadAllText(testFilePath);
                    
                    // Check if this specific test method already exists
                    if (existingContent.Contains($"public void {gap.MethodName}_"))
                    {
                        Console.WriteLine($"Test method for {gap.MethodName} already exists in {testProject.SubfolderForSource}/{testFileName}");
                        return true;
                    }
                    
                    // Add new test method to existing file - find the last closing brace of the class
                    var lines = existingContent.Split('\n');
                    var classEndIndex = -1;
                    var braceCount = 0;
                    var inClass = false;
                    
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (line.Contains("public class") && line.Contains("Tests"))
                        {
                            inClass = true;
                        }
                        
                        if (inClass)
                        {
                            braceCount += line.Count(c => c == '{');
                            braceCount -= line.Count(c => c == '}');
                            
                            if (braceCount == 0 && line.Contains("}"))
                            {
                                classEndIndex = i;
                                break;
                            }
                        }
                    }
                    
                    if (classEndIndex > 0)
                    {
                        // Insert the new test method before the class closing brace
                        var newLines = lines.ToList();
                        newLines.Insert(classEndIndex, $"        {gap.GeneratedTest}");
                        newLines.Insert(classEndIndex, "");
                        
                        var newContent = string.Join("\n", newLines);
                        File.WriteAllText(testFilePath, newContent);
                    }
                }
                else
                {
                    // Create new test file with proper namespace and class structure
                    var fullTestClass = GenerateFullTestClass(gap, testProject);
                    File.WriteAllText(testFilePath, fullTestClass);
                }
                
                Console.WriteLine($"✅ Added test for {gap.ClassName}.{gap.MethodName} to {testProject.SubfolderForSource}/{testFileName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding test for {gap.ClassName}.{gap.MethodName}: {ex.Message}");
                return false;
            }
        }

        private string GenerateFullTestClass(UnitTestGap gap, TestProjectInfo testProject)
        {
            var namespaceName = "Platform.CrossCutting8.Test";
            if (!string.IsNullOrEmpty(testProject.SubfolderForSource))
            {
                namespaceName += $".{testProject.SubfolderForSource}";
            }

            // Generate comprehensive using statements
            var usingStatements = GenerateUsingStatements(gap, testProject);

            return $@"{usingStatements}

namespace {namespaceName}
{{
    public class {gap.ClassName}Tests
    {{
        {gap.GeneratedTest}
    }}
}}";
        }

        private string GenerateUsingStatements(UnitTestGap gap, TestProjectInfo testProject)
        {
            var usings = new List<string>
            {
                "using System;",
                "using System.Threading.Tasks;",
                "using Xunit;",
                "using Moq;"
            };

            // Add project-specific using statements based on subfolder
            if (!string.IsNullOrEmpty(testProject.SubfolderForSource))
            {
                switch (testProject.SubfolderForSource.ToLower())
                {
                    case "caching":
                        usings.AddRange(new[]
                        {
                            "using Platform.CrossCutting8.Caching;",
                            "using Platform.CrossCutting8.Contracts.Cache;",
                            "using Platform.CrossCutting8.Contracts.Logging;"
                        });
                        break;
                    case "configuration":
                        usings.AddRange(new[]
                        {
                            "using Platform.CrossCutting8.Configuration;",
                            "using Microsoft.Extensions.Configuration;"
                        });
                        break;
                    case "cryptography":
                        usings.AddRange(new[]
                        {
                            "using Platform.CrossCutting8.Cryptography;"
                        });
                        break;
                    case "ioc":
                        usings.AddRange(new[]
                        {
                            "using Platform.CrossCutting8.IOC;",
                            "using Microsoft.Extensions.DependencyInjection;"
                        });
                        break;
                    case "logging":
                        usings.AddRange(new[]
                        {
                            "using Platform.CrossCutting8.Logging;",
                            "using Platform.CrossCutting8.Contracts.Logging;",
                            "using Microsoft.Extensions.Logging;"
                        });
                        break;
                    case "devkeyprovider":
                        usings.AddRange(new[]
                        {
                            "using Platform.CrossCutting8.Security.DevKeyProvider;"
                        });
                        break;
                    case "prodkeyprovider":
                        usings.AddRange(new[]
                        {
                            "using Platform.CrossCutting8.Security.ProdKeyProvider;"
                        });
                        break;
                }
            }

            // Add common contract usings
            usings.Add("using Platform.CrossCutting8.Contracts;");

            // Remove duplicates and sort
            return string.Join("\n", usings.Distinct().OrderBy(u => u));
        }

        private void CreateTestFile(string testFilePath, string className, TestFramework framework)
        {
            var namespaceName = "Tests"; // Default namespace
            var usingStatements = GetUsingStatements(framework);
            
            var testFileContent = $@"{usingStatements}

namespace {namespaceName}
{{
    public class {className}Tests
    {{
        // Generated test methods will be added here
    }}
}}";

            File.WriteAllText(testFilePath, testFileContent);
        }

        private string GetUsingStatements(TestFramework framework)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using FluentAssertions;");
            
            switch (framework)
            {
                case TestFramework.XUnit:
                    sb.AppendLine("using Xunit;");
                    break;
                case TestFramework.NUnit:
                    sb.AppendLine("using NUnit.Framework;");
                    break;
                case TestFramework.MSTest:
                    sb.AppendLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
                    break;
            }
            
            return sb.ToString();
        }

        private void AddTestMethodToFile(string testFilePath, string testMethod)
        {
            var content = File.ReadAllText(testFilePath);
            
            // Find the last closing brace of the class
            var lastBraceIndex = content.LastIndexOf('}');
            if (lastBraceIndex > 0)
            {
                // Find the second-to-last closing brace (end of class, not namespace)
                var secondLastBraceIndex = content.LastIndexOf('}', lastBraceIndex - 1);
                if (secondLastBraceIndex > 0)
                {
                    // Insert the test method before the class closing brace
                    var insertPosition = secondLastBraceIndex;
                    var newContent = content.Insert(insertPosition, $"\n{testMethod}\n");
                    File.WriteAllText(testFilePath, newContent);
                }
            }
        }

        // Helper methods for AST analysis
        private string GetContainingClassName(MethodDeclarationSyntax method)
        {
            var classDeclaration = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return classDeclaration?.Identifier.ValueText ?? "UnknownClass";
        }

        private string GetContainingNamespace(MethodDeclarationSyntax method)
        {
            var namespaceDeclaration = method.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            return namespaceDeclaration?.Name.ToString() ?? "UnknownNamespace";
        }

        private List<ParameterInfo> GetParameterInfo(MethodDeclarationSyntax method)
        {
            return method.ParameterList.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Identifier.ValueText,
                Type = p.Type?.ToString() ?? "object"
            }).ToList();
        }

        private bool IsPropertyAccessor(MethodDeclarationSyntax method)
        {
            var methodName = method.Identifier.ValueText;
            return methodName.StartsWith("get_") || methodName.StartsWith("set_");
        }

        private bool IsTestProject(string projectName)
        {
            return _config.TestProjectPatterns.Any(pattern => 
                projectName.Contains(pattern.Replace("*", "")));
        }

        private bool IsInSrcDirectory(string projectFile)
        {
            var projectPath = Path.GetFullPath(projectFile);
            return _config.SrcDirectoryPatterns.Any(srcPattern => 
                projectPath.Contains($"{Path.DirectorySeparatorChar}{srcPattern}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                projectPath.Contains($"{Path.DirectorySeparatorChar}{srcPattern}", StringComparison.OrdinalIgnoreCase));
        }

        private string FindSolutionDirectory(string[] projectFiles)
        {
            // Find the common root directory that contains both src and test folders
            var firstProject = projectFiles.FirstOrDefault();
            if (string.IsNullOrEmpty(firstProject))
                return "";
                
            var currentDir = Path.GetDirectoryName(firstProject);
            
            // Walk up the directory tree to find the solution root
            while (!string.IsNullOrEmpty(currentDir))
            {
                // Check if this directory contains both src and test folders
                var srcExists = Directory.Exists(Path.Combine(currentDir, "src")) || 
                               Directory.Exists(Path.Combine(currentDir, "source"));
                var testExists = Directory.Exists(Path.Combine(currentDir, "test")) || 
                                Directory.Exists(Path.Combine(currentDir, "tests"));
                
                // Or check if it contains a .sln file
                var hasSolutionFile = Directory.GetFiles(currentDir, "*.sln").Any();
                
                if ((srcExists && testExists) || hasSolutionFile)
                {
                    return currentDir;
                }
                
                currentDir = Path.GetDirectoryName(currentDir);
            }
            
            // Fallback to the directory of the first project
            return Path.GetDirectoryName(firstProject) ?? "";
        }

        private string[] FilterSolutionProjects(string[] projectFiles, string solutionDir)
        {
            try
            {
                // Find .sln file and parse it to get actual solution projects
                var solutionFiles = Directory.GetFiles(solutionDir, "*.sln");
                if (solutionFiles.Any())
                {
                    var solutionFile = solutionFiles.First();
                    Console.WriteLine($"Parsing solution file: {Path.GetFileName(solutionFile)}");
                    var solutionProjects = ParseSolutionFile(solutionFile, solutionDir);
                    
                    Console.WriteLine($"Found {solutionProjects.Count} projects in solution:");
                    foreach (var sp in solutionProjects)
                    {
                        Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(sp)}");
                    }
                    
                    // Filter projectFiles to only include those in the solution
                    var filtered = projectFiles.Where(pf => 
                        solutionProjects.Any(sp => 
                            Path.GetFullPath(pf).Equals(Path.GetFullPath(sp), StringComparison.OrdinalIgnoreCase)))
                        .ToArray();
                        
                    Console.WriteLine($"Excluded projects:");
                    var excluded = projectFiles.Except(filtered);
                    foreach (var ex in excluded)
                    {
                        Console.WriteLine($"  - {Path.GetFileNameWithoutExtension(ex)} (not in solution)");
                    }
                    
                    return filtered;
                }
                else
                {
                    Console.WriteLine("No .sln file found, using all projects");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering solution projects: {ex.Message}");
            }
            
            // Fallback: return all projects
            return projectFiles;
        }

        private List<string> ParseSolutionFile(string solutionFile, string solutionDir)
        {
            var projectPaths = new List<string>();
            
            try
            {
                var lines = File.ReadAllLines(solutionFile);
                foreach (var line in lines)
                {
                    // Look for project lines: Project("{...}") = "ProjectName", "ProjectPath", "{...}"
                    if (line.StartsWith("Project(") && line.Contains(".csproj"))
                    {
                        var parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            var projectPath = parts[1].Trim().Trim('"');
                            if (projectPath.EndsWith(".csproj"))
                            {
                                var fullPath = Path.Combine(solutionDir, projectPath);
                                if (File.Exists(fullPath))
                                {
                                    projectPaths.Add(fullPath);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing solution file: {ex.Message}");
            }
            
            return projectPaths;
        }

        private string[] GetSourceFiles(string projectDir)
        {
            return Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToArray();
        }

        private TestFramework DetectTestFramework(string projectFile)
        {
            var content = File.ReadAllText(projectFile);
            
            if (content.Contains("Microsoft.VisualStudio.TestTools.UnitTesting"))
                return TestFramework.MSTest;
            if (content.Contains("NUnit"))
                return TestFramework.NUnit;
            if (content.Contains("xunit"))
                return TestFramework.XUnit;
                
            return TestFramework.XUnit; // Default
        }


        private bool HasTestInFile(string testFile, EnhancedMethodInfo method)
        {
            try
            {
                var content = File.ReadAllText(testFile);
                
                Console.WriteLine($"          Checking patterns for {method.MethodName}:");
                
                var hasTestMethod = HasTestMethodPattern(content, method);
                Console.WriteLine($"          - Test method pattern: {hasTestMethod}");
                
                var hasMockUsage = HasMockUsagePattern(content, method);
                Console.WriteLine($"          - Mock usage pattern: {hasMockUsage}");
                
                var hasNamingConvention = HasTestNamingConvention(content, method);
                Console.WriteLine($"          - Naming convention: {hasNamingConvention}");
                
                // Multiple detection strategies
                return hasTestMethod || hasMockUsage || hasNamingConvention;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"          Error reading test file: {ex.Message}");
                return false;
            }
        }

        private bool HasTestMethodPattern(string content, EnhancedMethodInfo method)
        {
            // More specific patterns to avoid false positives
            var patterns = new[]
            {
                // Test attributes followed by method containing the method name
                $@"\[Test\]\s*public.*{method.MethodName}",
                $@"\[TestMethod\]\s*public.*{method.MethodName}",
                $@"\[Fact\]\s*public.*{method.MethodName}",
                
                // Common test naming conventions
                $@"public.*{method.MethodName}_Should_\w+",
                $@"public.*{method.MethodName}_When_\w+",
                $@"public.*Test_{method.MethodName}",
                $@"public.*{method.MethodName}Test\s*\(",
                
                // Method calls within test methods
                $@"\.{method.MethodName}\s*\(",
                $@"sut\.{method.MethodName}\s*\(",
                $@"service\.{method.MethodName}\s*\(",
                $@"mock.*\.{method.MethodName}\s*\("
            };

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    Console.WriteLine($"            ✅ Matched pattern: {pattern}");
                    return true;
                }
            }
            
            return false;
        }

        private bool HasMockUsagePattern(string content, EnhancedMethodInfo method)
        {
            var patterns = new[]
            {
                $@"Mock<.*{method.ClassName}>.*{method.MethodName}",
                $@"\.Setup\(.*{method.MethodName}\)",
                $@"\.Verify\(.*{method.MethodName}\)"
            };
            
            return patterns.Any(pattern => Regex.IsMatch(content, pattern));
        }

        private bool HasTestNamingConvention(string content, EnhancedMethodInfo method)
        {
            var patterns = new[]
            {
                $@"Should.*{method.MethodName}",
                $@"{method.MethodName}.*Should",
                $@"Test{method.MethodName}",
                $@"{method.MethodName}Test"
            };
            
            return patterns.Any(pattern => 
                Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase));
        }

        private string GetExpectedBehavior(EnhancedMethodInfo method)
        {
            var methodName = method.MethodName;
            
            if (methodName.Contains("Create") || methodName.Contains("Add"))
                return "CreateSuccessfully";
            if (methodName.Contains("Delete") || methodName.Contains("Remove"))
                return "DeleteSuccessfully";
            if (methodName.Contains("Update") || methodName.Contains("Modify"))
                return "UpdateSuccessfully";
            if (methodName.Contains("Get") || methodName.Contains("Find"))
                return "ReturnExpectedResult";
            if (methodName.Contains("Validate") || methodName.Contains("Check"))
                return "ValidateCorrectly";
                
            return "ExecuteSuccessfully";
        }

        private string GetDefaultValue(string type)
        {
            return type.ToLower() switch
            {
                "string" => "\"test\"",
                "int" => "1",
                "bool" => "true",
                "double" => "1.0",
                "float" => "1.0f",
                "decimal" => "1.0m",
                "long" => "1L",
                "datetime" => "DateTime.Now",
                "guid" => "Guid.NewGuid()",
                "task" => "Task.CompletedTask",
                "cancellationtoken" => "CancellationToken.None",
                _ => GetDefaultValueForComplexType(type)
            };
        }

        private string GetDefaultValueForComplexType(string type)
        {
            // Handle generic types
            if (type.Contains("<"))
            {
                if (type.StartsWith("Task<"))
                {
                    var innerType = type.Substring(5, type.Length - 6);
                    return $"Task.FromResult({GetDefaultValue(innerType)})";
                }
                if (type.StartsWith("List<") || type.StartsWith("IList<") || type.StartsWith("IEnumerable<"))
                {
                    return $"new List<{type.Split('<', '>')[1]}>()";
                }
            }

            // Handle nullable types
            if (type.EndsWith("?"))
            {
                return "null";
            }

            // For complex types, try to create a new instance or return null
            if (char.IsUpper(type[0]))
            {
                return "null"; // For interfaces and complex types, use null and let developer fix
            }

            return "null";
        }

        private string DetermineTestGapReason(EnhancedMethodInfo method)
        {
            var methodName = method.MethodName;
            
            if (methodName.Contains("Create") || methodName.Contains("Add"))
                return "Creation method requires unit test validation";
            if (methodName.Contains("Delete") || methodName.Contains("Remove"))
                return "Deletion method requires unit test coverage";
            if (methodName.Contains("Update") || methodName.Contains("Modify"))
                return "Update method requires unit test verification";
            if (methodName.Contains("Validate") || methodName.Contains("Check"))
                return "Validation logic requires comprehensive testing";
            if (methodName.Contains("Process") || methodName.Contains("Execute"))
                return "Business logic requires unit test coverage";
                
            return $"Public method with complexity {method.CyclomaticComplexity} missing unit test coverage";
        }
    }

    // Enhanced data models
    public class EnhancedMethodInfo
    {
        public string MethodName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string ReturnType { get; set; } = "";
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
        public int CyclomaticComplexity { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAsync { get; set; }
        public string Namespace { get; set; } = "";
        public string FilePath { get; set; } = "";
        public MethodDeclarationSyntax? MethodSyntax { get; set; }
    }

    public class ParameterInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }

    public class TestProjectInfo
    {
        public string ProjectPath { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public TestFramework Framework { get; set; } = TestFramework.XUnit;
        public bool IsNew { get; set; } = false;
        public string SubfolderForSource { get; set; } = "";
    }

    public enum TestFramework
    {
        XUnit,
        NUnit,
        MSTest,
        Unknown
    }

    public class TestPattern
    {
        public bool UsesArrangeActAssert { get; set; }
        public bool UsesMocking { get; set; }
        public string MockingFramework { get; set; } = "";
        public string ConstructorPattern { get; set; } = "";
        public List<string> CommonAssertions { get; set; } = new List<string>();
        public List<string> UsingStatements { get; set; } = new List<string>();
        public string TestNamingPattern { get; set; } = "";
    }

    public class SourceClassInfo
    {
        public string ClassName { get; set; } = "";
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<ParameterInfo> ConstructorParameters { get; set; } = new List<ParameterInfo>();
        public List<string> Interfaces { get; set; } = new List<string>();
    }

    public class TestGapAnalysisConfig
    {
        public int MaxProjectsToAnalyze { get; set; } = 10;
        public int MaxFilesPerProject { get; set; } = 20;
        public int MinimumComplexity { get; set; } = 2;
        public bool AutoGenerateTests { get; set; } = true;
        public bool CreateNewTestProjects { get; set; } = false; // Only use existing test projects
        public bool OnlyAnalyzeSrcProjects { get; set; } = true; // Only analyze projects in "src" directories
        public string[] TestProjectPatterns { get; set; } = { "Test", "Tests" };
        public string[] ExcludedMethods { get; set; } = Array.Empty<string>();
        public string[] ExcludedClasses { get; set; } = Array.Empty<string>();
        public string[] ExcludedProjects { get; set; } = Array.Empty<string>();
        public string[] SrcDirectoryPatterns { get; set; } = { "src", "source", "Sources" };
    }
}
