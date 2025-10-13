namespace BuildingBlock.DomainBase
{
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object> GetEqualityComponents();
        public override bool Equals(object? obj)
        {
            if (obj is not ValueObject other)
                return false;
            using var thisValues = GetEqualityComponents().GetEnumerator();
            using var otherValues = other.GetEqualityComponents().GetEnumerator();
            while (thisValues.MoveNext() && otherValues.MoveNext())
            {
                if (thisValues.Current is null ^ otherValues.Current is null)
                    return false;
                if (thisValues.Current is not null && !thisValues.Current.Equals(otherValues.Current))
                    return false;
            }
            return !thisValues.MoveNext() && !otherValues.MoveNext();
        }
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate(0, (current, hash) => current ^ hash);
        }
    }
}
