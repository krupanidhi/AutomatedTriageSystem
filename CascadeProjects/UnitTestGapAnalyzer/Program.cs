using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnitTestGapAnalyzer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Unit Test Gap Analyzer ===");
            Console.WriteLine();

            string solutionPath;

            if (args.Length > 0)
            {
                solutionPath = args[0];
            }
            else
            {
                Console.Write("Enter the path to your solution directory: ");
                solutionPath = Console.ReadLine()?.Trim() ?? "";
            }

            if (string.IsNullOrEmpty(solutionPath) || !Directory.Exists(solutionPath))
            {
                Console.WriteLine("Error: Invalid solution path.");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await AnalyzeSolution(solutionPath);
                stopwatch.Stop();
                result.Summary.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                // Save results to JSON
                var outputPath = Path.Combine(solutionPath, "test_gap_analysis_result.json");
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(result, jsonOptions);
                File.WriteAllText(outputPath, json);

                Console.WriteLine();
                Console.WriteLine("=== Analysis Complete ===");
                Console.WriteLine($"Results saved to: {outputPath}");
                Console.WriteLine();
                Console.WriteLine("=== Summary ===");
                Console.WriteLine($"Projects Analyzed: {result.ProjectsAnalyzed}");
                Console.WriteLine($"Total Unit Test Gaps: {result.Summary.TotalUnitTestGaps}");
                Console.WriteLine($"Test Coverage: {result.Summary.TestCoverageAssessment.CoveragePercentage:F1}%");
                Console.WriteLine($"Overall Coverage Quality: {result.Summary.TestCoverageAssessment.OverallCoverageQuality}");
                Console.WriteLine($"Execution Time: {result.Summary.ExecutionTimeMs}ms");
                Console.WriteLine();

                if (result.Summary.TestCoverageAssessment.ProjectsWithComprehensiveCoverage.Any())
                {
                    Console.WriteLine("Projects with Comprehensive Coverage (≥80%):");
                    foreach (var project in result.Summary.TestCoverageAssessment.ProjectsWithComprehensiveCoverage)
                    {
                        Console.WriteLine($"  ✅ {project}");
                    }
                    Console.WriteLine();
                }

                if (result.Summary.TestCoverageAssessment.Recommendations.Any())
                {
                    Console.WriteLine("Recommendations:");
                    foreach (var recommendation in result.Summary.TestCoverageAssessment.Recommendations)
                    {
                        Console.WriteLine($"  • {recommendation}");
                    }
                    Console.WriteLine();
                }

                // Display top test gaps
                if (result.UnitTestGaps.Any())
                {
                    Console.WriteLine("Top Test Gaps:");
                    foreach (var gap in result.UnitTestGaps.Take(10))
                    {
                        Console.WriteLine($"  • {gap.ClassName}.{gap.MethodName}");
                        Console.WriteLine($"    Reason: {gap.Reason}");
                        Console.WriteLine($"    Test Project: {Path.GetFileNameWithoutExtension(gap.TestProject)}");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during analysis: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static async Task<AnalysisResult> AnalyzeSolution(string solutionPath)
        {
            var result = new AnalysisResult
            {
                AnalysisType = "UnitTestGapAnalysis",
                SolutionPath = solutionPath,
                Timestamp = DateTime.Now
            };

            Console.WriteLine($"Analyzing solution: {solutionPath}");
            Console.WriteLine("Discovering projects...");

            // Find all .csproj files
            var projectFiles = Directory.GetFiles(solutionPath, "*.csproj", SearchOption.AllDirectories);
            result.ProjectsAnalyzed = projectFiles.Length;

            Console.WriteLine($"Found {projectFiles.Length} projects");

            // Load configuration
            var config = LoadConfiguration(solutionPath);

            // Unit test gap analysis
            Console.WriteLine("Analyzing unit test coverage...");
            var analyzer = new TestGapAnalyzer(config);
            var (testGaps, coverageAssessment) = analyzer.AnalyzeAndGenerateTests(projectFiles);
            result.UnitTestGaps = testGaps;
            result.Summary.TestCoverageAssessment = coverageAssessment;

            // Calculate summary
            result.Summary.TotalUnitTestGaps = result.UnitTestGaps.Count;

            Console.WriteLine($"Analysis complete: {result.Summary.TotalUnitTestGaps} test gaps found");

            return result;
        }

        static TestGapAnalysisConfig LoadConfiguration(string solutionPath)
        {
            var configPath = Path.Combine(solutionPath, "testgap-config.json");
            
            if (File.Exists(configPath))
            {
                try
                {
                    Console.WriteLine($"Loading configuration from: {configPath}");
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<TestGapAnalysisConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (config != null)
                    {
                        Console.WriteLine("Configuration loaded successfully");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load configuration file: {ex.Message}");
                    Console.WriteLine("Using default configuration");
                }
            }
            else
            {
                Console.WriteLine("No configuration file found, using default settings");
            }

            // Return default configuration
            return new TestGapAnalysisConfig
            {
                OnlyAnalyzeSrcProjects = true,
                MaxProjectsToAnalyze = 10,
                MaxFilesPerProject = 20,
                MinimumComplexity = 1,
                AutoGenerateTests = true,
                CreateNewTestProjects = false
            };
        }
    }
}
