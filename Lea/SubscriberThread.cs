namespace Lea
{
    /// <summary>
    /// Specifies which thread the event subscriber will be called in.
    /// </summary>
    public enum SubscriberThread
    {
        /// <summary>
        /// The subecriber method will be called on the same thread as event aggreator class.
        /// </summary>
        PublisherThread,

        /// <summary>
        /// The subscriber method will be called on a new thread (from the ThreadPool).
        /// </summary>
        BackgroundThread,

        /// <summary>
        /// The subscriber method will be called on a thread specified by a SynchronizationContext.
        /// </summary>
        ContextThread
    }
}
