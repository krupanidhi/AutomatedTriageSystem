namespace ExcelAnalysis.Infrastructure.Services;

public static class SentimentKeywords
{
    public static readonly HashSet<string> NegativeBuzzwords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Strong Negative - Project Blockers
        "blocked", "blocker", "blocking", "stopped", "halted", "stalled", "stuck",
        "cannot", "can't", "unable", "impossible", "failed", "failure", "failing",
        "broken", "critical", "crisis", "emergency", "urgent", "severe",
        
        // Delays and Timeline Issues
        "delayed", "delay", "behind", "late", "overdue", "missed", "slipping",
        "postponed", "rescheduled", "extended", "pushed back", "not on track",
        
        // Resource and Budget Problems
        "shortage", "insufficient", "lacking", "missing", "unavailable",
        "over budget", "overbudget", "overspent", "expensive", "costly",
        "understaffed", "resource constraint", "no resources",
        
        // Quality and Performance Issues
        "poor", "bad", "terrible", "awful", "horrible", "unacceptable",
        "substandard", "inadequate", "deficient", "defect", "bug", "error",
        "issue", "problem", "concern", "risk", "threat", "vulnerability",
        
        // Stakeholder and Team Issues
        "conflict", "disagreement", "dispute", "complaint", "frustrated",
        "unhappy", "dissatisfied", "concerned", "worried", "anxious",
        "confused", "unclear", "ambiguous", "miscommunication",
        
        // Compliance and Regulatory
        "non-compliant", "violation", "breach", "unauthorized", "unapproved",
        "rejected", "denied", "failed audit", "compliance issue",
        
        // Technical Challenges
        "outage", "downtime", "crash", "failure", "malfunction", "not working",
        "incompatible", "deprecated", "obsolete", "legacy issue",
        
        // Scope and Requirements
        "scope creep", "out of scope", "unclear requirements", "changing requirements",
        "undefined", "ambiguous", "incomplete", "missing requirements",
        
        // Dependencies and Integration
        "dependency", "dependent on", "waiting for", "blocked by", "held up",
        "integration issue", "compatibility issue", "not integrated",
        
        // Risk Indicators
        "at risk", "high risk", "jeopardy", "danger", "threatened",
        "vulnerable", "exposed", "uncertain", "unknown",
        
        // Negative Actions
        "cancelled", "canceled", "terminated", "abandoned", "suspended",
        "withdrawn", "revoked", "removed", "eliminated",
        
        // Negative Outcomes
        "loss", "lost", "losing", "decline", "decreased", "reduced",
        "deteriorated", "worsened", "degraded", "compromised",
        
        // Effort and Difficulty
        "difficult", "hard", "challenging", "complex", "complicated",
        "overwhelming", "struggle", "struggling", "effort", "intensive",
        
        // Communication Issues
        "no response", "unresponsive", "no update", "silence", "ignored",
        "not answered", "pending response", "waiting for response",
        
        // Quality Descriptors
        "incomplete", "partial", "unfinished", "not done", "not complete",
        "not ready", "not available", "not working", "not functional",
        
        // Negative Trends
        "declining", "decreasing", "dropping", "falling", "worsening",
        "deteriorating", "regressing", "backward", "setback",
        
        // Escalation Words
        "escalate", "escalated", "escalation", "elevated", "raised",
        "flagged", "red flag", "warning", "alert", "attention needed"
    };

    public static readonly HashSet<string> PositiveBuzzwords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Progress and Completion
        "completed", "complete", "finished", "done", "delivered", "achieved",
        "accomplished", "success", "successful", "successfully",
        
        // Quality and Performance
        "excellent", "great", "good", "outstanding", "exceptional",
        "high quality", "well done", "impressive", "effective", "efficient",
        
        // Timeline and Schedule
        "on time", "on schedule", "on track", "ahead", "early", "timely",
        "met deadline", "delivered on time",
        
        // Positive Progress
        "progress", "progressing", "advancing", "improving", "improved",
        "enhancement", "enhanced", "optimized", "upgraded", "better",
        
        // Stakeholder Satisfaction
        "satisfied", "happy", "pleased", "approved", "accepted",
        "positive feedback", "well received", "appreciated",
        
        // Team and Collaboration
        "collaborative", "cooperation", "teamwork", "aligned", "agreement",
        "consensus", "support", "supportive", "helpful",
        
        // Innovation and Quality
        "innovative", "creative", "solution", "resolved", "fixed",
        "working", "functional", "operational", "stable", "reliable",
        
        // Positive Outcomes
        "benefit", "advantage", "gain", "increase", "growth",
        "expansion", "improvement", "enhancement", "value added",
        
        // Readiness and Availability
        "ready", "available", "accessible", "prepared", "set",
        "launched", "deployed", "implemented", "active",
        
        // Compliance and Standards
        "compliant", "approved", "certified", "validated", "verified",
        "passed", "met standards", "quality assured"
    };

    public static readonly HashSet<string> NeutralBuzzwords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Status Updates
        "in progress", "ongoing", "continuing", "working on", "developing",
        "planning", "scheduled", "planned", "expected", "anticipated",
        
        // Information Sharing
        "update", "status", "report", "meeting", "discussion", "review",
        "analysis", "assessment", "evaluation", "monitoring",
        
        // Process Words
        "process", "procedure", "workflow", "methodology", "approach",
        "strategy", "plan", "roadmap", "timeline", "schedule"
    };

    public static double CalculateSentimentScore(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var lowerText = text.ToLowerInvariant();
        var words = lowerText.Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '\n', '\r', '\t' }, 
            StringSplitOptions.RemoveEmptyEntries);

        int negativeCount = 0;
        int positiveCount = 0;
        int totalWords = words.Length;

        // Count keyword matches
        foreach (var word in words)
        {
            if (NegativeBuzzwords.Contains(word))
                negativeCount++;
            if (PositiveBuzzwords.Contains(word))
                positiveCount++;
        }

        // Also check for multi-word phrases
        foreach (var phrase in NegativeBuzzwords.Where(p => p.Contains(' ')))
        {
            if (lowerText.Contains(phrase))
                negativeCount += 2; // Multi-word phrases get more weight
        }

        foreach (var phrase in PositiveBuzzwords.Where(p => p.Contains(' ')))
        {
            if (lowerText.Contains(phrase))
                positiveCount += 2;
        }

        // Calculate score
        if (negativeCount == 0 && positiveCount == 0)
            return 0; // Neutral

        // Score ranges from -1 (very negative) to +1 (very positive)
        int totalSentiment = positiveCount - negativeCount;
        int maxPossible = Math.Max(positiveCount + negativeCount, 1);
        
        double score = (double)totalSentiment / maxPossible;
        
        // Clamp to -1 to 1 range
        return Math.Clamp(score, -1, 1);
    }

    public static string GetSentimentLabel(double score)
    {
        return score switch
        {
            >= 0.5 => "Very Positive",
            >= 0.2 => "Positive",
            >= -0.2 => "Neutral",
            >= -0.5 => "Negative",
            _ => "Very Negative"
        };
    }

    public static (int negative, int positive, int neutral) CountKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (0, 0, 0);

        var lowerText = text.ToLowerInvariant();
        var words = lowerText.Split(new[] { ' ', ',', '.', ';', ':', '!', '?', '\n', '\r', '\t' }, 
            StringSplitOptions.RemoveEmptyEntries);

        int negativeCount = 0;
        int positiveCount = 0;
        int neutralCount = 0;

        foreach (var word in words)
        {
            if (NegativeBuzzwords.Contains(word))
                negativeCount++;
            else if (PositiveBuzzwords.Contains(word))
                positiveCount++;
            else if (NeutralBuzzwords.Contains(word))
                neutralCount++;
        }

        return (negativeCount, positiveCount, neutralCount);
    }
}
