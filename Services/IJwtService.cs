namespace pms.Services
{
    /// <summary>
    /// Interface for JSON Web Token (JWT) generation service.
    /// Used for authentication in both local and network environments.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a signed JWT for a user with specific claims.
        /// </summary>
        /// <param name="userId">Unique user ID.</param>
        /// <param name="username">User's identity name.</param>
        /// <param name="role">User's security role (Admin/Student).</param>
        /// <returns>A string representing the JWT.</returns>
        string GenerateToken(int userId, string username, string role);
    }
}
