namespace Grumpy.RipplesMQ.Client.Interfaces
{
    /// <summary>
    /// Utility to build queue names
    /// </summary>
    public interface IQueueNameUtility
    {
        /// <summary>
        /// Build Queue Name
        /// </summary>
        /// <returns></returns>
        string ReplyQueue<T>(string id = "");

        /// <summary>
        /// Build Queue Name
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="durable">Durable</param>
        /// <returns></returns>
        string Build(string name, bool durable = false);

        /// <summary>
        /// Build Queue Name
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="name">Name</param>
        /// <param name="durable">Durable</param>
        /// <returns></returns>
        string Build(string topic, string name, bool durable);
    }
}