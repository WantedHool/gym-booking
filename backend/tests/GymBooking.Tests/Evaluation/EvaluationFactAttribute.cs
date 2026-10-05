namespace GymBooking.Tests.Evaluation;

public sealed class EvaluationFactAttribute : FactAttribute
{
    public EvaluationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_EVALUATION") != "1")
        {
            Skip = "Πείραμα αξιολόγησης — τρέχει μόνο με RUN_EVALUATION=1.";
        }

        Timeout = 30 * 60 * 1000;
    }
}
