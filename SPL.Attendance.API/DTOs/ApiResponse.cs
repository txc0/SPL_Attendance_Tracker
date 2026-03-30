namespace SPL.Attendance.API.DTOs
{
    /// <summary>
    /// Standard envelope for all API responses.
    /// Ensures a consistent shape: { success, message, data }.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(string message, T? data = default) => new()
        {
            Success = true,
            Message = message,
            Data    = data
        };

        public static ApiResponse<T> Fail(string message) => new()
        {
            Success = false,
            Message = message
        };
    }
}
