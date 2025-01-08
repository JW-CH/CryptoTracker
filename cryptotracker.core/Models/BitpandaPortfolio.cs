namespace cryptotracker.core.Models
{
    using System;
    using System.Collections.Generic;

    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Globalization;

    public partial class BitpandaPortfolio
    {
        [JsonPropertyName("data")]
        public PortfolioData Data { get; set; }
    }

    public partial class PortfolioData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public PortfolioDataAttributes Attributes { get; set; }
    }

    public partial class PortfolioDataAttributes
    {
        [JsonPropertyName("portfolios")]
        public List<Portfolio> Portfolios { get; set; }

        [JsonPropertyName("asset_group_total_returns")]
        public List<AssetGroupTotalReturn> AssetGroupTotalReturns { get; set; }
    }

    public partial class AssetGroupTotalReturn
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public AssetGroupTotalReturnAttributes Attributes { get; set; }
    }

    public partial class AssetGroupTotalReturnAttributes
    {
        [JsonPropertyName("asset_group")]
        public string AssetGroup { get; set; }

        [JsonPropertyName("total_fiat_balance")]
        public string TotalFiatBalance { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("parked_fiat_balance")]
        public string ParkedFiatBalance { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("available_fiat_balance")]
        public string AvailableFiatBalance { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("total_return_percent")]
        public string TotalReturnPercent { get; set; }
    }

    public partial class Portfolio
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("attributes")]
        public PortfolioAttributes Attributes { get; set; }
    }

    public partial class PortfolioAttributes
    {
        [JsonPropertyName("asset_id")]
        public string AssetId { get; set; }

        [JsonPropertyName("asset_pid")]
        public Guid AssetPid { get; set; }

        [JsonPropertyName("asset_symbol")]
        public string AssetSymbol { get; set; }

        [JsonPropertyName("asset_type")]
        public string AssetType { get; set; }

        [JsonPropertyName("asset_group")]
        public string AssetGroup { get; set; }

        [JsonPropertyName("asset_balance")]
        public string AssetBalance { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("invested_amount")]
        public string InvestedAmount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("invested_amount_asset_id")]
        public string InvestedAmountAssetId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("absolute_profit")]
        public string AbsoluteProfit { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("absolute_profit_asset_id")]
        public string AbsoluteProfitAssetId { get; set; }

        [JsonPropertyName("fiat_balance")]
        public string FiatBalance { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("total_return")]
        public string TotalReturn { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("total_return_percent")]
        public string TotalReturnPercent { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("staked_balance_percent")]
        public string StakedBalancePercent { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("parked_asset_balance")]
        public string ParkedAssetBalance { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("available_asset_balance")]
        public string AvailableAssetBalance { get; set; }
    }
}
