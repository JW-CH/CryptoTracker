namespace cryptotracker.core.Models
{
    public static class CryptoTrackerConfigValidator
    {
        /// <summary>
        /// Fails fast on config errors that would otherwise corrupt data or open an
        /// unauthenticated MCP endpoint. Duplicate integration names used to merge
        /// into one integration whose sources zero-marked each other's holdings.
        /// </summary>
        public static void Validate(CryptoTrackerConfig config)
        {
            var errors = new List<string>();

            foreach (var integration in config.Integrations)
            {
                if (string.IsNullOrWhiteSpace(integration.Name))
                {
                    errors.Add("An integration has no name.");
                    continue;
                }

                if (integration.Sources.Count == 0)
                {
                    errors.Add($"Integration '{integration.Name}' has no sources. Move type/key/secret into a 'sources' list (see config/example-config.yml).");
                }

                if (integration.Sources.Any(s => s.Type == CryptoTrackerIntegrationType.Unknown))
                {
                    errors.Add($"Integration '{integration.Name}' has a source without a valid type.");
                }
            }

            var duplicateNames = config.Integrations
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .GroupBy(i => i.Name.Trim().ToLowerInvariant())
                .Where(g => g.Count() > 1);

            foreach (var group in duplicateNames)
            {
                errors.Add($"Integration name '{group.First().Name}' is used {group.Count()} times. Names must be unique; use one integration with multiple sources instead.");
            }

            if (config.Mcp?.Enabled == true && (string.IsNullOrWhiteSpace(config.Mcp.Token) || config.Mcp.Token.Length < 32))
            {
                errors.Add("MCP is enabled but mcp.token is missing or shorter than 32 characters.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Invalid config:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
        }
    }
}
