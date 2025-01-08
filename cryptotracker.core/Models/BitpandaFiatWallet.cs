namespace cryptotracker.core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public partial class BitpandaFiatWalletResult
    {
        [JsonPropertyName("data")]
        public List<BitpandaFiatWallet> Data { get; set; }
    }

    public partial class BitpandaFiatWallet
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public Attributes Attributes { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }

    public partial class Attributes
    {
        [JsonPropertyName("fiat_id")]
        public string FiatId { get; set; }

        [JsonPropertyName("fiat_symbol")]
        public string FiatSymbol { get; set; }

        [JsonPropertyName("balance")]
        public string Balance { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("pending_transactions_count")]
        public long PendingTransactionsCount { get; set; }
    }
}
