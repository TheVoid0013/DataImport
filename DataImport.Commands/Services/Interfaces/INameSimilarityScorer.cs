namespace DataImport.Commands.Services;

public interface INameSimilarityScorer
{
    double Score(string query, string candidate);
}