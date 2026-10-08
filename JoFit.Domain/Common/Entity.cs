using System.ComponentModel.DataAnnotations.Schema;

namespace JoFit.Domain.Common
{
    public abstract class Entity
    {
        public long Id { get; set; }

        private readonly List<DomainEvent> _domainEvents = [];

        [NotMapped]
        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected Entity()
        {

        }

        protected Entity(long id)
        {
            Id = id;
        }

        public void AddDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(DomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
