using System.Linq.Expressions;

namespace Carrier.Domain;

public sealed record ScoreBreakdown(double Reliability, double Safety, double Experience)
{
    public double Total => Math.Round(Reliability + Safety + Experience, 2);
}

/// <summary>One rule shared by SQL ordering and the explanation shown to shippers.</summary>
public static class RankingPolicy
{
    public const double ReliabilityWeight = 50;
    public const double SafetyWeight = 30;
    public const double ExperienceWeight = 20;
    public const int ExperienceCap = 500;
    public const string Version = "quality-v1";

    public static readonly Expression<Func<CarrierProfile, double>> ScoreExpression = c =>
        c.OnTimeRate * ReliabilityWeight + c.SafetyScore / 100 * SafetyWeight +
        (c.CompletedLoads >= ExperienceCap ? 1.0 : (double)c.CompletedLoads / ExperienceCap) * ExperienceWeight;

    public static ScoreBreakdown Explain(double onTimeRate, double safetyScore, int completedLoads) =>
        new(onTimeRate * ReliabilityWeight, safetyScore / 100 * SafetyWeight,
            Math.Min((double)completedLoads / ExperienceCap, 1) * ExperienceWeight);
}
