namespace BuildingBlock.DomainBase
{
    public abstract class Entity<TId>
    {
        public TId Id { get; protected set; } = default!;
        protected Entity() { }
        protected Entity(TId id)
        {
            Id = id;
        }
        public override bool Equals(object? obj)
        {
            if (obj is not Entity<TId> other)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other.GetType())
                return false;
            if (Id == null || other.Id == null)
                return false;
            return Id.Equals(other.Id);
        }
        override public int GetHashCode()
        {
            return (GetType().ToString() + Id).GetHashCode();
        }
        public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
        => a is null && b is null || a is not null && a.Equals(b);

        public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);
    }
}
