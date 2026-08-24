namespace DataImport.API.Services;

public interface INameSimilarityScorer
{
    double Score(string query, string candidate);
}