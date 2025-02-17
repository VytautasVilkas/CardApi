public class SecretManager
{
    public string GetAdminSecretCode()
    {
        return Environment.GetEnvironmentVariable("ADMIN_SECRET_CODE")
               ?? throw new InvalidOperationException("ADMIN_SECRET_CODE is not set in the environment.");
    }

    public string GetJwtSecretCode()
    {
        return Environment.GetEnvironmentVariable("JWL_SECRET_CODE")
               ?? throw new InvalidOperationException("JWL_SECRET_CODE is not set in the environment.");
    }
}
