using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnitTestGapAnalyzer
{
    public class AnalysisResult
    {
        [JsonPropertyName("analysisType")]
        public string AnalysisType { get; set; } = "";

        [JsonPropertyName("solutionPath")]
        public string SolutionPath { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [JsonPropertyName("projectsAnalyzed")]
        public int ProjectsAnalyzed { get; set; }

        [JsonPropertyName("unitTestGaps")]
        public List<UnitTestGap> UnitTestGaps { get; set; } = new();

        [JsonPropertyName("summary")]
        public AnalysisSummary Summary { get; set; } = new();
    }

    public class UnitTestGap
    {
        [JsonPropertyName("className")]
        public string ClassName { get; set; } = "";

        [JsonPropertyName("methodName")]
        public string MethodName { get; set; } = "";

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = "";

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";

        [JsonPropertyName("testProject")]
        public string TestProject { get; set; } = "";

        [JsonPropertyName("generatedTest")]
        public string GeneratedTest { get; set; } = "";

        [JsonPropertyName("testGenerated")]
        public bool TestGenerated { get; set; } = false;
    }

    public class TestCoverageAssessment
    {
        [JsonPropertyName("overallCoverageQuality")]
        public string OverallCoverageQuality { get; set; } = "";

        [JsonPropertyName("totalMethodsAnalyzed")]
        public int TotalMethodsAnalyzed { get; set; }

        [JsonPropertyName("methodsWithExistingTests")]
        public int MethodsWithExistingTests { get; set; }

        [JsonPropertyName("coveragePercentage")]
        public double CoveragePercentage { get; set; }

        [JsonPropertyName("testQualityIndicators")]
        public List<string> TestQualityIndicators { get; set; } = new();

        [JsonPropertyName("existingTestPatterns")]
        public List<string> ExistingTestPatterns { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();

        [JsonPropertyName("projectsWithComprehensiveCoverage")]
        public List<string> ProjectsWithComprehensiveCoverage { get; set; } = new();
    }

    public class AnalysisSummary
    {
        [JsonPropertyName("totalUnitTestGaps")]
        public int TotalUnitTestGaps { get; set; }

        [JsonPropertyName("testCoverageAssessment")]
        public TestCoverageAssessment TestCoverageAssessment { get; set; } = new();

        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }
    }
}
