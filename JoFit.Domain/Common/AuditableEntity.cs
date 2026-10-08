namespace JoFit.Domain.Common
{
    public abstract class AuditableEntity : Entity
    {
        protected AuditableEntity()
        { }

        protected AuditableEntity(long id)
        {

        }

        public DateTimeOffset CreatedAtUtc { get; set; }
        public long? CreatedBy { get; set; }
        public DateTimeOffset? LastModifiedUtc { get; set; }
        public long? LastModifiedBy { get; set; }
        public DateTimeOffset? DeletedUtc { get; set; }
        public long? DeletedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
