using F23.StringSimilarity;

namespace DataImport.Commands.Services;

public class NGramCosineSimilarityScorer : INameSimilarityScorer
{
    private readonly Cosine _cosine = new Cosine(2); 

    public double Score(string query, string candidate)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(candidate))
            return 0;

        return _cosine.Similarity(query.ToLowerInvariant(), candidate.ToLowerInvariant());
    }
}