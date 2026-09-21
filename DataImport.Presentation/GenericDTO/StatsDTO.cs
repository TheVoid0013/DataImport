namespace DataImport.Presentation.GenericDTO;

public record CountryCountDto(string Country, int Count);
public record SdnTypeCountDto(string SdnType, int Count);

// Unknown = rows with no country (some records have null); Other = everything outside the top N
public record TopCountriesDto(int Total, int Unknown, int Other, List<CountryCountDto> Top);