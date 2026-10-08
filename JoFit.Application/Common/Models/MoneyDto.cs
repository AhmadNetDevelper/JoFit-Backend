namespace JoFit.Application.Common.Models;

/// <summary>Money in minor units: JOD has 3 decimals, so 28000 is 28.000 JOD. Always an integer.</summary>
public sealed record MoneyDto(long Amount, string Currency = "JOD");
