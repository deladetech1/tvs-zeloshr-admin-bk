namespace ZelosHR.Api.Entities.Files;

internal static class BlobStorageErrors
{
    internal static string Map(Exception ex)
    {
        var message = ex.Message;
        if (message.Contains("DefaultAzureCredential", StringComparison.OrdinalIgnoreCase))
        {
            return "Azure Storage credentials are not available. Set AzureStorage__ConnectionString on the "
                + "Container App, or ensure Trovesuite__AzureStorage__UserAssignedManagedIdentityClientId "
                + "matches the user-assigned identity with Storage Blob Data Contributor on the account.";
        }

        return string.IsNullOrWhiteSpace(ex.Message) ? "File storage operation failed." : ex.Message;
    }
}
