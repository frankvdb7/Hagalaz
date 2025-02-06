namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAnimationType : IType
    {
        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <value>The id.</value>
        int Id { get; }
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        int Priority { get; }
        /// <summary>
        /// Performed after decode
        /// </summary>
        void AfterDecode();
    }
}