using DataImport.Data.Models;
using Facet;
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
}
