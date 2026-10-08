namespace JoFit.Domain.Common.Results
{
    public readonly record struct Error
    {
        private Error(string code, string description, ErrorKindsEnum errorType)
        {
            Code = code;
            Description = description;
            ErrorType = errorType;
        }

        public string Code { get; }
        public string Description { get; }
        public ErrorKindsEnum ErrorType { get; }

        /// <summary>Name of the request field the error belongs to (dotted for nested), if any.</summary>
        public string? Field { get; init; }

        public Error ForField(string field) => this with { Field = field };


        public static Error Failure(string code = nameof(Failure), string description = "General Faiure.") =>
            new(code, description, ErrorKindsEnum.Failure);
        public static Error Unexpected(string code = nameof(Unexpected), string description = "Unexpected error.") =>
            new(code, description, ErrorKindsEnum.Unexpected);
        public static Error Validation(string code = nameof(Validation), string description = "Validation error.") =>
            new(code, description, ErrorKindsEnum.Validation);
        public static Error Conflict(string code = nameof(Conflict), string description = "Conflict error.") =>
            new(code, description, ErrorKindsEnum.Conflict);
        public static Error NotFound(string code = nameof(NotFound), string description = "Not found error.") =>
            new(code, description, ErrorKindsEnum.NotFound);
        public static Error Unauthorized(string code = nameof(Unauthorized), string description = "Unauthorized error.") =>
            new(code, description, ErrorKindsEnum.Unauthorized);
        public static Error Forbidden(string code = nameof(Forbidden), string description = "Forbidden error.") =>
            new(code, description, ErrorKindsEnum.Forbidden);

        public static Error Create(int type, string code, string description) =>
            new(code, description, (ErrorKindsEnum)type);
    }
}
