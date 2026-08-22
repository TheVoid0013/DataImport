using DataImport.Data.Models;
using Facet;
using Facet.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataImport.Presentation.GenericDTO
{
    /// <summary>
    /// Slim projection for list/search results. Deliberately excludes XmlRecord —
    /// that's a large raw-XML blob nobody needs in a paginated list view.
    /// </summary>
    [Facet(typeof(SanctionDetail), exclude: nameof(SanctionDetail.XmlRecord))]
    public partial class SanctionListItemDto { }

    /// <summary>
    /// Full projection for a single-record lookup — includes the raw source XML.
    /// </summary>
    [Facet(typeof(SanctionDetail))]
    public partial class SanctionDetailDto { }

    /// <summary>
    /// Projection of one import run, for the operational history endpoint.
    /// </summary>
    [Facet(typeof(DataImportLog))]
    public partial class ImportLogDto { }
    
    [Facet(typeof(SanctionDetail), exclude: nameof(SanctionDetail.Id))]
    public partial class FreeTextSearchResultDto { }
    
    public class FreeTextSearchResponseDto
    {
        public int TotalCount { get; set; }
        public List<string> DistinctSdnTypes { get; set; } = new();
        public List<string> DistinctCountries { get; set; } = new();
        public List<FreeTextSearchResultDto> Results { get; set; } = new();
    }
}
