public interface ITrap
{
    bool IsActive { get; }
    bool Activate(float warningDuration);
}