using MVFC.Veragi.Simulator.Shareable.Responses.Catalogs;
using MVFC.Veragi.Simulator.Shareable.Responses.Merchants;
using System.Text.Json.Serialization;

namespace MVFC.Veragi.Simulator.Shareable.Requests.Reconciliation;

public sealed record BankReconciliationEntry(
    [property: JsonPropertyName("createdBy")] string? CreatedBy = null,
    [property: JsonPropertyName("createdDate")] string? CreatedDate = null,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy = null,
    [property: JsonPropertyName("lastModifiedDate")] string? LastModifiedDate = null,
    [property: JsonPropertyName("id")] long? Id = null,
    [property: JsonPropertyName("entryId")] string? EntryId = null,
    [property: JsonPropertyName("referenceDate")] string? ReferenceDate = null,
    [property: JsonPropertyName("merchant")] string? Merchant = null,
    [property: JsonPropertyName("acquirer")] string? Acquirer = null,
    [property: JsonPropertyName("bankAccount")] string? BankAccount = null,
    [property: JsonPropertyName("value")] decimal? Value = null
);
