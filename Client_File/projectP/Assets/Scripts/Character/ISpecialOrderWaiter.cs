public interface ISpecialOrderWaiter
{
    bool IsWaitingSpecialOrder { get; }
    void ResumeFromSpecialOrderWait();
}
