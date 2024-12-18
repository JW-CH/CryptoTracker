namespace cryptotracker.core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public partial class BitpandaAssetWallet
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }

        [JsonPropertyName("last_user_action")]
        public LastUserAction LastUserAction { get; set; }
    }

    public partial class Data
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public DataAttributes Attributes { get; set; }
    }

    public partial class DataAttributes
    {
        [JsonPropertyName("cryptocoin")]
        public Cryptocoin Cryptocoin { get; set; }

        [JsonPropertyName("commodity")]
        public Commodity Commodity { get; set; }
    }

    public partial class Commodity
    {
        [JsonPropertyName("metal")]
        public Cryptocoin Metal { get; set; }
    }

    public partial class Cryptocoin
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public CryptocoinAttributes Attributes { get; set; }
    }

    public partial class CryptocoinAttributes
    {
        [JsonPropertyName("wallets")]
        public List<Wallet> Wallets { get; set; }
    }

    public partial class Wallet
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public WalletAttributes Attributes { get; set; }

        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }

    public partial class WalletAttributes
    {
        [JsonPropertyName("cryptocoin_id")]
        public string CryptocoinId { get; set; }

        [JsonPropertyName("cryptocoin_symbol")]
        public string CryptocoinSymbol { get; set; }

        [JsonPropertyName("balance")]
        public string Balance { get; set; }

        [JsonPropertyName("is_default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }
    }

    public partial class LastUserAction
    {
        [JsonPropertyName("date_iso8601")]
        public DateTimeOffset DateIso8601 { get; set; }

        [JsonPropertyName("unix")]
        public string Unix { get; set; }
    }
}
