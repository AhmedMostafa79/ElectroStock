namespace ElectroStock.Application.Dtos
{
    public class TokenDto
    {
        /// <summary>
        ///  Authentication token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Token expiration timestamp.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Type of token (e.g., "Bearer").
        /// </summary>
        public string TokenType { get; set; }
    }
}
