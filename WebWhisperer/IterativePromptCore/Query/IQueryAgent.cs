using WebWhisperer.IterativePromptCore.Model;
using WebWhisperer.IterativePromptCore.Types;

namespace WebWhisperer.IterativePromptCore.Query
{
    public interface IQueryAgent
    {
        /// <summary>
        /// Mirror of a <see cref="CommunicationAgent"/> method to make it public for <see cref="QueryAgent"/> class. A legal way to create a new user query.
        /// </summary>
        /// <param name="userQuery"></param>
        public void AddUserQuery(string userQuery = null);

        /// <summary>
        /// When user starts a new query, the current chat must be flushed so that the context is not mixed with the previous query.
        /// </summary>
        public void StartNewQueryAttempt();

        /// <summary>
        /// Sequentially builds the transformations based on the query built so far. 
        /// After any input, the "nextMoves" is saved in order to return the current next moves when whole query performed. 
        /// Using <see cref="CommunicationAgent"/> for comunication with user/bot.
        /// Instead of asking for choosing the next transformation by name, it uses the index of the transformation in the list.
        /// </summary>
        /// <returns></returns>
        public QueryViewModel PerformQuerying(IList<string> queryItems, IList<Field> fields);
    }
}
